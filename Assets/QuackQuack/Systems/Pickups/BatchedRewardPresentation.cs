using System;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Player;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuackQuack.Systems.Pickups
{
    // Prewarmed overflow representation. Each obligation keeps its own identity,
    // position and attraction state; meshes share only rendering, never reward values.
    public sealed class BatchedRewardPresentation : IDisposable
    {
        private readonly Batch experience, healing;
        public int ActiveCount => experience.Count + healing.Count;
        public bool TryGetPosition(RewardId id, out Vector2 position) =>
            (id.Kind == RewardKind.Experience ? experience : healing).TryGetPosition(id, out position);
        public BatchedRewardPresentation(RewardLedger ledger, RunState run, PlayerController player,
            Func<int, bool> collectExperience, GameObject experiencePrefab, GameObject healingPrefab, Transform parent)
        {
            experience = new Batch(ledger, run, player, collectExperience, experiencePrefab, parent, false);
            healing = new Batch(ledger, run, player, collectExperience, healingPrefab, parent, true);
        }
        public bool TryAdd(RewardEntry entry) => entry != null && (entry.Kind == RewardKind.Experience ? experience : healing).TryAdd(entry);
        public void Advance(float seconds, float radius, float speed)
        { experience.Advance(seconds, radius, speed); healing.Advance(seconds, radius, speed); }
        public void Clear() { experience.Clear(); healing.Clear(); }
        public void Flush() { experience.Flush(); healing.Flush(); }
        public void Dispose() { experience.Dispose(); healing.Dispose(); }

        private sealed class Batch : IDisposable
        {
            private struct Slot { public RewardEntry Entry; public Vector2 Position; public AttractionState Attraction; }
            private sealed class Chunk
            {
                public GameObject Root;
                public Mesh Mesh;
                public MeshRenderer Renderer;
                public Vector3[] Vertices;
                public bool Dirty;
            }
            private readonly RewardLedger ledger;
            private readonly RunState run;
            private readonly PlayerController player;
            private readonly CircleCollider2D body;
            private readonly Func<int, bool> collect;
            private readonly bool isHealing;
            private readonly float contactRadius;
            private readonly Vector2 centerOffset;
            private readonly Slot[] slots;
            private readonly Vector3[] geometry;
            private readonly Chunk[] chunks;
            private readonly int chunkSize;
            private readonly Material material;
            private int generation;
            private bool disposed;
            public int Count { get; private set; }

            public Batch(RewardLedger ledger, RunState run, PlayerController player, Func<int, bool> collect,
                GameObject prefab, Transform parent, bool healing)
            {
                this.ledger = ledger; this.run = run; this.player = player; this.collect = collect; isHealing = healing;
                body = player.GetComponent<CircleCollider2D>();
                slots = new Slot[ledger.DefeatCapacity];
                SpriteRenderer source = null;
                foreach (var candidate in prefab.GetComponentsInChildren<SpriteRenderer>(true))
                    if (candidate.enabled && candidate.sprite != null) { source = candidate; break; }
                if (source == null) { chunks = Array.Empty<Chunk>(); geometry = Array.Empty<Vector3>(); return; }
                var shape = prefab.GetComponent<CircleCollider2D>();
                contactRadius = shape.radius * Mathf.Max(Mathf.Abs(prefab.transform.localScale.x), Mathf.Abs(prefab.transform.localScale.y));
                centerOffset = prefab.transform.TransformVector(shape.offset);
                var vertices = source.sprite.vertices; var uv = source.sprite.uv; var triangles = source.sprite.triangles;
                // Unpacked/rectangular sprites need only one quad. Preserve UV rotation
                // through the original affine mapping; tight atlases keep their exact mesh.
                if (!source.sprite.packed || source.sprite.packingMode == SpritePackingMode.Rectangle)
                {
                    Vector2 min = vertices[0], max = min;
                    foreach (var vertex in vertices) { min = Vector2.Min(min, vertex); max = Vector2.Max(max, vertex); }
                    var quad = new[] { min, new Vector2(min.x, max.y), max, new Vector2(max.x, min.y) };
                    var coordinates = new Vector2[4];
                    int a = triangles[0], b = triangles[1], c = triangles[2];
                    Vector2 ab = vertices[b] - vertices[a], ac = vertices[c] - vertices[a];
                    float determinant = ab.x * ac.y - ab.y * ac.x;
                    for (int i = 0; i < quad.Length; i++)
                    {
                        var delta = quad[i] - vertices[a];
                        float u = (delta.x * ac.y - delta.y * ac.x) / determinant;
                        float v = (ab.x * delta.y - ab.y * delta.x) / determinant;
                        coordinates[i] = uv[a] + u * (uv[b] - uv[a]) + v * (uv[c] - uv[a]);
                    }
                    vertices = quad; uv = coordinates; triangles = new ushort[] { 0, 1, 2, 2, 3, 0 };
                }
                geometry = new Vector3[vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    var point = vertices[i]; if (source.flipX) point.x = -point.x; if (source.flipY) point.y = -point.y;
                    geometry[i] = source.transform.TransformPoint(point) - prefab.transform.position;
                }
                // UInt16 indices on every supported backend, even for diagnostic ledgers.
                chunkSize = Math.Min(1024, 60000 / geometry.Length);
                if (chunkSize < 1) throw new InvalidOperationException("Unsupported reward sprite geometry.");
                chunks = new Chunk[(slots.Length + chunkSize - 1) / chunkSize];
                var shader = Resources.Load<Shader>("RewardBatch");
                if (shader == null) throw new InvalidOperationException("The reward batch shader is missing.");
                material = new Material(shader) { name = "Batched reward material" };
                material.mainTexture = source.sprite.texture;
                for (int c = 0; c < chunks.Length; c++)
                {
                    int capacity = Math.Min(chunkSize, slots.Length - c * chunkSize);
                    var root = new GameObject(healing ? "Overflow healing rewards" : "Overflow experience rewards");
                    root.layer = source.gameObject.layer;
                    root.transform.SetParent(parent, true);
                    var mesh = new Mesh { name = root.name }; mesh.MarkDynamic();
                    var chunk = new Chunk { Root = root, Mesh = mesh, Vertices = new Vector3[capacity * geometry.Length] };
                    var coordinates = new Vector2[chunk.Vertices.Length]; var colors = new Color32[chunk.Vertices.Length];
                    var indices = new int[capacity * triangles.Length];
                    for (int s = 0; s < capacity; s++)
                    {
                        for (int v = 0; v < geometry.Length; v++) { coordinates[s * geometry.Length + v] = uv[v]; colors[s * geometry.Length + v] = source.color; }
                        for (int t = 0; t < triangles.Length; t++) indices[s * triangles.Length + t] = s * geometry.Length + triangles[t];
                    }
                    mesh.vertices = chunk.Vertices; mesh.uv = coordinates; mesh.colors32 = colors;
                    mesh.SetIndices(indices, MeshTopology.Triangles, 0);
                    root.AddComponent<MeshFilter>().sharedMesh = mesh;
                    chunk.Renderer = root.AddComponent<MeshRenderer>(); chunk.Renderer.sharedMaterial = material;
                    chunk.Renderer.sortingLayerID = source.sortingLayerID; chunk.Renderer.sortingOrder = source.sortingOrder;
                    chunk.Renderer.enabled = false; chunks[c] = chunk;
                }
            }
            public bool TryAdd(RewardEntry entry)
            {
                if (disposed || chunks.Length == 0 || Count == slots.Length || ledger.IsClosed) return false;
                if ((entry.State != RewardState.Due && entry.State != RewardState.WaitingForView) ||
                    !ledger.TryGetEntry(entry.Id, out var owned) || !ReferenceEquals(owned, entry)) return false;
                var slot = new Slot { Entry = entry, Position = new Vector2(entry.X, entry.Y) };
                slot.Attraction.Reset(entry.Id.LeaseId);
                slots[Count] = slot; WriteGeometry(Count, slot.Position); Count++;
                chunks[(Count - 1) / chunkSize].Renderer.enabled = true;
                return true;
            }
            public bool TryGetPosition(RewardId id, out Vector2 position)
            {
                for (int i = 0; i < Count; i++) if (slots[i].Entry.Id.Equals(id)) { position = slots[i].Position; return true; }
                position = default; return false;
            }
            public void Flush() { if (!disposed) Upload(); }
            private void WriteGeometry(int index, Vector2 position, bool clear = false)
            {
                var chunk = chunks[index / chunkSize]; int first = (index % chunkSize) * geometry.Length;
                for (int i = 0; i < geometry.Length; i++) chunk.Vertices[first + i] = clear ? Vector3.zero : (Vector3)position + geometry[i];
                chunk.Dirty = true;
            }
            private void Upload()
            {
                for (int i = 0; i < chunks.Length; i++)
                {
                    var chunk = chunks[i];
                    if (chunk.Dirty) { chunk.Mesh.vertices = chunk.Vertices; chunk.Mesh.RecalculateBounds(); chunk.Dirty = false; }
                    chunk.Renderer.enabled = Count > i * chunkSize;
                }
            }
            private void RemoveAt(int index)
            {
                int last = --Count;
                if (index != last) { slots[index] = slots[last]; WriteGeometry(index, slots[index].Position); }
                slots[last] = default; WriteGeometry(last, default, true);
            }
            public void Advance(float seconds, float radius, float speed)
            {
                if (disposed || ledger.IsClosed || Count == 0 || !run.IsSimulating) return;
                if (player == null || !player.isActiveAndEnabled || player.State.Health <= 0)
                {
                    for (int j = 0; j < Count; j++) { ledger.CancelAttraction(slots[j].Entry.Id); slots[j].Attraction.Reset(slots[j].Entry.Id.LeaseId); }
                    return;
                }
                Vector2 target = body.transform.TransformPoint(body.offset);
                float contact = body.radius * body.transform.lossyScale.x + contactRadius;
                int version = generation, i = 0;
                while (i < Count && run.IsSimulating)
                {
                    ref var slot = ref slots[i]; var entry = slot.Entry;
                    if (entry.State != RewardState.Grounded && entry.State != RewardState.Attracted) { RemoveAt(i); continue; }
                    bool missing = player.State.Health < player.State.MaxHealth;
                    var center = slot.Position + centerOffset;
                    var next = slot.Attraction.Advance(center, target, radius, speed, seconds, true, true, isHealing, missing);
                    if (slot.Attraction.Phase == AttractionPhase.Discarded)
                    { ledger.TryDiscardCapturedHealing(entry.Id); RemoveAt(i); continue; }
                    if (slot.Attraction.Captured && entry.State == RewardState.Grounded) ledger.TryMarkAttracted(entry.Id);
                    var rootPosition = next - centerOffset;
                    if (rootPosition != slot.Position) { slot.Position = rootPosition; WriteGeometry(i, rootPosition); }
                    if (slot.Attraction.Captured && (next - target).sqrMagnitude <= contact * contact)
                    {
                        bool accepted = isHealing ? ledger.TryCollectHeal(entry.Id, player.State) > 0 : ledger.TryClaimExperience(entry.Id, collect);
                        if (version != generation) return; // A synchronous run end may clear all slots during claim.
                        if (accepted) { RemoveAt(i); continue; }
                    }
                    i++;
                }
                Upload();
            }
            public void Clear()
            {
                if (disposed) return;
                generation++;
                for (int i = 0; i < Count; i++) { ledger.ReturnToPending(slots[i].Entry.Id); WriteGeometry(i, default, true); }
                Array.Clear(slots, 0, Count); Count = 0; Upload();
            }
            public void Dispose()
            {
                if (disposed) return; Clear(); disposed = true; generation++;
                foreach (var chunk in chunks) { Object.Destroy(chunk.Root); Object.Destroy(chunk.Mesh); }
                Object.Destroy(material);
            }
        }
    }
}
