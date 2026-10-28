using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace QuackQuack.Systems.Pooling
{
    public enum PoolFamily { Enemy, Projectile, Pickup, Vfx }
    public readonly struct PoolMetrics
    {
        public readonly int Active, Capacity;
        public readonly long Exhaustions;
        public PoolMetrics(int active, int capacity, long exhaustions) { Active = active; Capacity = capacity; Exhaustions = exhaustions; }
    }
    public interface IPoolLifecycle
    {
        void OnAcquire();
        void OnRelease();
        void ResetState();
    }

    // Capacity is prewarmed before gameplay. Exhaustion returns null, never Instantiates.
    public sealed class PoolService : IDisposable
    {
        private sealed class Entry
        {
            public ObjectPool<Component> Pool;
            public PoolFamily Family;
            public Type Type;
            public long Exhaustions;
            public readonly List<Component> Instances = new List<Component>();
        }
        private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
        private readonly Dictionary<Component, Entry> owners = new Dictionary<Component, Entry>();
        private readonly HashSet<Component> active = new HashSet<Component>();
        private bool disposed;
        public bool IsDisposed => disposed;
        public int ActiveCount => active.Count;
        public bool IsRegistered<T>(string key, PoolFamily family) where T : Component, IPoolLifecycle =>
            !disposed && key != null && entries.TryGetValue(key, out var entry) && entry.Type == typeof(T) && entry.Family == family;

        public void Register<T>(string key, PoolFamily family, Func<T> factory, int capacity) where T : Component, IPoolLifecycle
        {
            if (disposed) throw new ObjectDisposedException(nameof(PoolService));
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (entries.ContainsKey(key)) throw new ArgumentException("Duplicate pool key.", nameof(key));
            var entry = new Entry { Family = family, Type = typeof(T) };
            entry.Pool = new ObjectPool<Component>(() =>
            {
                var instance = factory();
                instance.gameObject.SetActive(false);
                entry.Instances.Add(instance);
                owners.Add(instance, entry);
                return instance;
            }, actionOnRelease: item => item.gameObject.SetActive(false), collectionCheck: true,
                defaultCapacity: capacity, maxSize: capacity);
            entries.Add(key, entry);
            var warm = new Component[capacity];
            for (int i = 0; i < capacity; i++) warm[i] = entry.Pool.Get();
            foreach (var item in warm)
            {
                ((IPoolLifecycle)item).ResetState();
                entry.Pool.Release(item);
            }
        }

        public T Acquire<T>(string key) where T : Component, IPoolLifecycle
        {
            if (disposed) throw new ObjectDisposedException(nameof(PoolService));
            if (!entries.TryGetValue(key, out var entry)) throw new KeyNotFoundException(key);
            if (entry.Type != typeof(T)) throw new InvalidOperationException("Pool type mismatch.");
            if (entry.Pool.CountInactive == 0) { entry.Exhaustions++; return null; }
            var item = (T)entry.Pool.Get();
            active.Add(item);
            try
            {
                item.OnAcquire();
                item.gameObject.SetActive(true);
                return item;
            }
            catch
            {
                active.Remove(item);
                item.ResetState();
                entry.Pool.Release(item);
                throw;
            }
        }

        public bool Release(Component item)
        {
            if (disposed || item == null || !owners.TryGetValue(item, out var entry) || !active.Remove(item)) return false;
            var lifecycle = (IPoolLifecycle)item;
            try { lifecycle.OnRelease(); }
            finally
            {
                lifecycle.ResetState();
                entry.Pool.Release(item);
            }
            return true;
        }

        public int GetActiveCount(PoolFamily family)
        {
            int count = 0;
            foreach (var item in active) if (owners[item].Family == family) count++;
            return count;
        }

        public bool IsActive(Component item, PoolFamily family) => !disposed && item != null &&
            active.Contains(item) && owners.TryGetValue(item, out var entry) && entry.Family == family;

        public PoolMetrics GetMetrics(PoolFamily family)
        {
            if (family < PoolFamily.Enemy || family > PoolFamily.Vfx) throw new ArgumentOutOfRangeException(nameof(family));
            int used = 0, capacity = 0; long exhausted = 0;
            foreach (var entry in entries.Values)
                if (entry.Family == family)
                {
                    used += entry.Pool.CountActive; capacity += entry.Instances.Count; exhausted += entry.Exhaustions;
                }
            return new PoolMetrics(used, capacity, exhausted);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (var entry in entries.Values)
            {
                foreach (var item in entry.Instances)
                {
                    if (item == null) continue;
                    if (active.Contains(item)) ((IPoolLifecycle)item).OnRelease();
                    ((IPoolLifecycle)item).ResetState();
                    if (Application.isPlaying) UnityEngine.Object.Destroy(item.gameObject);
                    else UnityEngine.Object.DestroyImmediate(item.gameObject);
                }
                entry.Pool.Clear();
            }
            active.Clear();
            owners.Clear();
            entries.Clear();
        }
    }
}
