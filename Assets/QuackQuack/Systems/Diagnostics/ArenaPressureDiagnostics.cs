using System;
using System.Globalization;
using System.IO;
using QuackQuack.Gameplay.Enemies;
using UnityEngine;

namespace QuackQuack.Systems.Diagnostics
{
    public enum ArenaCapturePhase { FixedBeforeSimulation, ExplicitAfterSimulation }

    // Observational only. Not installed in product scenes; Begin is the opt-in boundary.
    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    public sealed class ArenaPressureDiagnostics : MonoBehaviour
    {
        public struct FrameSample
        {
            public long Sequence, Tick;
            public double SimulationTime;
            public float FixedStep;
            public ArenaCapturePhase Phase;
            public Vector2 PlayerPosition, PlayerVelocity;
            public Bounds PlayerBounds;
            public int Contacts, Overlaps;
            public bool ContactBufferFull, OverlapBufferFull;
        }
        public struct ActorSample
        {
            public int ColliderId;
            public long LeaseId;
            public bool Active;
            public Vector2 Position, Velocity;
        }
        public struct ContactSample
        {
            public int ColliderId, OtherColliderId;
            public long LeaseId, OtherLeaseId;
            public Vector2 Point, Normal, RelativeVelocity;
            public float Separation, NormalImpulse;
        }
        public struct OverlapSample
        {
            public int ColliderId;
            public long LeaseId;
            public bool Valid, Penetrating;
            public float SignedDistance;
            public Vector2 PointA, PointB, Normal;
        }
        private struct ActorBinding
        {
            public Collider2D Shape;
            public Rigidbody2D Body;
            public PooledEnemy Enemy;
        }
        private Rigidbody2D player;
        private Collider2D shape;
        private ActorBinding[] actors;
        private FrameSample[] frames;
        private ActorSample[] actorSamples;
        private ContactSample[] contactSamples;
        private OverlapSample[] overlapSamples;
        private ContactPoint2D[] contacts;
        private Collider2D[] overlaps;
        private ContactFilter2D filter;
        private int next, pairCapacity;
        private long automaticTick;
        public bool IsCapturing { get; private set; }
        public bool AutomaticSampling { get; set; }
        public int Seed { get; private set; }
        public int Count { get; private set; }
        public long Written { get; private set; }
        public long Overwritten => Written - Count;
        public int ActorCount => actors == null ? 0 : actors.Length;
        public int Capacity => frames == null ? 0 : frames.Length;

        private void Awake() => enabled = false;

        public void Begin(int seed, Rigidbody2D observedPlayer, Collider2D playerShape,
            Collider2D[] observedActors, int capacity = 512, int maxPairs = 64, bool automatic = true)
        {
            if (observedPlayer == null || playerShape == null || playerShape.attachedRigidbody != observedPlayer)
                throw new ArgumentException("A player collider attached to its body is required.");
            if (observedActors == null) throw new ArgumentNullException(nameof(observedActors));
            if (capacity < 1 || capacity > 4096 || maxPairs < 1 || maxPairs > 1024 ||
                observedActors.Length > 1024 || (long)capacity * Math.Max(maxPairs, observedActors.Length) > 1048576)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capture buffers must remain bounded.");
            for (int i = 0; i < observedActors.Length; i++)
                if (observedActors[i] == null || observedActors[i] == playerShape)
                    throw new ArgumentException("Observed actors must be valid and distinct from the player.");

            // All capture storage is allocated once here, outside the measured loop.
            actors = new ActorBinding[observedActors.Length];
            for (int i = 0; i < actors.Length; i++)
                actors[i] = new ActorBinding { Shape = observedActors[i], Body = observedActors[i].attachedRigidbody,
                    Enemy = observedActors[i].GetComponentInParent<PooledEnemy>() };
            frames = new FrameSample[capacity];
            actorSamples = new ActorSample[capacity * actors.Length];
            contactSamples = new ContactSample[capacity * maxPairs];
            overlapSamples = new OverlapSample[capacity * maxPairs];
            contacts = new ContactPoint2D[maxPairs]; overlaps = new Collider2D[maxPairs];
            filter = new ContactFilter2D { useTriggers = false };
            Seed = seed; player = observedPlayer; shape = playerShape; pairCapacity = maxPairs;
            next = Count = 0; Written = automaticTick = 0; AutomaticSampling = automatic;
            enabled = true; IsCapturing = true;
        }

        public void Stop() { IsCapturing = false; enabled = false; }
        private void OnDisable() => IsCapturing = false;
        private void FixedUpdate()
        {
            if (AutomaticSampling) Capture(++automaticTick, Time.fixedTimeAsDouble, ArenaCapturePhase.FixedBeforeSimulation);
        }

        public bool Capture(long tick, double simulationTime, ArenaCapturePhase phase)
        {
            if (!IsCapturing || !isActiveAndEnabled || player == null || shape == null || !shape.enabled || !player.simulated) return false;
            if (tick < 0 || double.IsNaN(simulationTime) || double.IsInfinity(simulationTime) || simulationTime < 0 ||
                (phase != ArenaCapturePhase.FixedBeforeSimulation && phase != ArenaCapturePhase.ExplicitAfterSimulation))
                throw new ArgumentOutOfRangeException(nameof(tick));
            int slot = next;
            int contactCount = shape.GetContacts(filter, contacts);
            int overlapCount = shape.Overlap(filter, overlaps);
            frames[slot] = new FrameSample { Sequence = Written, Tick = tick, SimulationTime = simulationTime, Phase = phase,
                FixedStep = Time.fixedDeltaTime,
                PlayerPosition = player.position, PlayerVelocity = player.linearVelocity, PlayerBounds = shape.bounds,
                Contacts = contactCount, Overlaps = overlapCount,
                ContactBufferFull = contactCount == pairCapacity, OverlapBufferFull = overlapCount == pairCapacity };
            for (int i = 0; i < actors.Length; i++)
            {
                var a = actors[i];
                actorSamples[slot * actors.Length + i] = a.Shape == null ? default : new ActorSample {
                    ColliderId = a.Shape.GetInstanceID(), LeaseId = a.Enemy == null ? 0 : a.Enemy.LeaseId,
                    Active = a.Shape.isActiveAndEnabled && (a.Body == null || a.Body.simulated),
                    Position = a.Body == null ? (Vector2)a.Shape.transform.position : a.Body.position,
                    Velocity = a.Body == null ? Vector2.zero : a.Body.linearVelocity };
            }
            for (int i = 0; i < contactCount; i++)
            {
                var c = contacts[i];
                contactSamples[slot * pairCapacity + i] = new ContactSample {
                    ColliderId = Id(c.collider), OtherColliderId = Id(c.otherCollider),
                    LeaseId = Lease(c.collider), OtherLeaseId = Lease(c.otherCollider), Point = c.point,
                    Normal = c.normal, RelativeVelocity = c.relativeVelocity, Separation = c.separation, NormalImpulse = c.normalImpulse };
            }
            for (int i = 0; i < overlapCount; i++)
            {
                var other = overlaps[i]; var d = shape.Distance(other);
                overlapSamples[slot * pairCapacity + i] = new OverlapSample {
                    ColliderId = Id(other), LeaseId = Lease(other), Valid = d.isValid, Penetrating = d.isOverlapped,
                    SignedDistance = d.distance, PointA = d.pointA, PointB = d.pointB, Normal = d.normal };
            }
            next = (next + 1) % frames.Length; Count = Math.Min(Count + 1, frames.Length); Written++;
            return true;
        }

        private static int Id(Collider2D c) => c == null ? 0 : c.GetInstanceID();
        private static long Lease(Collider2D c)
        { var enemy = c == null ? null : c.GetComponentInParent<PooledEnemy>(); return enemy == null ? 0 : enemy.LeaseId; }
        private int Slot(int index) => (next - Count + index + frames.Length) % frames.Length;
        public bool TryGetFrame(int index, out FrameSample sample)
        { sample = default; if (index < 0 || index >= Count) return false; sample = frames[Slot(index)]; return true; }
        public bool TryGetActor(int frame, int actor, out ActorSample sample)
        {
            sample = default; if (frame < 0 || frame >= Count || actor < 0 || actor >= ActorCount) return false;
            sample = actorSamples[Slot(frame) * ActorCount + actor]; return true;
        }
        public bool TryGetContact(int frame, int contact, out ContactSample sample)
        {
            sample = default; if (!TryGetFrame(frame, out var f) || contact < 0 || contact >= f.Contacts) return false;
            sample = contactSamples[Slot(frame) * pairCapacity + contact]; return true;
        }
        public bool TryGetOverlap(int frame, int overlap, out OverlapSample sample)
        {
            sample = default; if (!TryGetFrame(frame, out var f) || overlap < 0 || overlap >= f.Overlaps) return false;
            sample = overlapSamples[Slot(frame) * pairCapacity + overlap]; return true;
        }

        // Explicit stopped export only: string formatting and I/O never run in sampling callbacks.
        public void ExportCsv(string prefix)
        {
            if (IsCapturing) throw new InvalidOperationException("Stop capture before exporting.");
            if (frames == null || string.IsNullOrWhiteSpace(prefix)) throw new ArgumentException("Capture and output prefix required.");
            prefix = Path.GetFullPath(prefix); Directory.CreateDirectory(Path.GetDirectoryName(prefix));
            using var f = new StreamWriter(prefix + ".frames.csv");
            using var a = new StreamWriter(prefix + ".actors.csv");
            using var c = new StreamWriter(prefix + ".contacts.csv");
            using var o = new StreamWriter(prefix + ".overlaps.csv");
            f.WriteLine("seed,sequence,tick,time,phase,fixed_step,px,py,vx,vy,min_x,min_y,max_x,max_y,contacts,overlaps,contacts_full,overlaps_full");
            a.WriteLine("seed,sequence,actor_index,collider_id,lease,active,x,y,vx,vy");
            c.WriteLine("seed,sequence,collider_id,other_id,lease,other_lease,x,y,nx,ny,rvx,rvy,separation,normal_impulse");
            o.WriteLine("seed,sequence,collider_id,lease,valid,penetrating,distance,ax,ay,bx,by,nx,ny");
            for (int i = 0; i < Count; i++)
            {
                TryGetFrame(i, out var s);
                f.WriteLine(FormattableString.Invariant($"{Seed},{s.Sequence},{s.Tick},{s.SimulationTime:R},{s.Phase},{s.FixedStep:R},{s.PlayerPosition.x:R},{s.PlayerPosition.y:R},{s.PlayerVelocity.x:R},{s.PlayerVelocity.y:R},{s.PlayerBounds.min.x:R},{s.PlayerBounds.min.y:R},{s.PlayerBounds.max.x:R},{s.PlayerBounds.max.y:R},{s.Contacts},{s.Overlaps},{s.ContactBufferFull},{s.OverlapBufferFull}"));
                for (int j = 0; j < ActorCount; j++)
                {
                    TryGetActor(i, j, out var v);
                    a.WriteLine(FormattableString.Invariant($"{Seed},{s.Sequence},{j},{v.ColliderId},{v.LeaseId},{v.Active},{v.Position.x:R},{v.Position.y:R},{v.Velocity.x:R},{v.Velocity.y:R}"));
                }
                for (int j = 0; j < s.Contacts; j++)
                {
                    TryGetContact(i, j, out var v);
                    c.WriteLine(FormattableString.Invariant($"{Seed},{s.Sequence},{v.ColliderId},{v.OtherColliderId},{v.LeaseId},{v.OtherLeaseId},{v.Point.x:R},{v.Point.y:R},{v.Normal.x:R},{v.Normal.y:R},{v.RelativeVelocity.x:R},{v.RelativeVelocity.y:R},{v.Separation:R},{v.NormalImpulse:R}"));
                }
                for (int j = 0; j < s.Overlaps; j++)
                {
                    TryGetOverlap(i, j, out var v);
                    o.WriteLine(FormattableString.Invariant($"{Seed},{s.Sequence},{v.ColliderId},{v.LeaseId},{v.Valid},{v.Penetrating},{v.SignedDistance:R},{v.PointA.x:R},{v.PointA.y:R},{v.PointB.x:R},{v.PointB.y:R},{v.Normal.x:R},{v.Normal.y:R}"));
                }
            }
            File.WriteAllText(prefix + ".metadata.txt", "seed=" + Seed.ToString(CultureInfo.InvariantCulture) +
                "\ncapacity=" + Capacity.ToString(CultureInfo.InvariantCulture) + "\nwritten=" + Written.ToString(CultureInfo.InvariantCulture) +
                "\noverwritten=" + Overwritten.ToString(CultureInfo.InvariantCulture) + "\nmax_pairs=" + pairCapacity.ToString(CultureInfo.InvariantCulture) +
                "\nfixed_delta=" + Time.fixedDeltaTime.ToString("R", CultureInfo.InvariantCulture) + "\nunity=" + Application.unityVersion);
        }
    }
}
