using PlatformFighter.Miscelaneous;

using System;
using System.Collections;
using System.Collections.Generic;

namespace PlatformFighter
{
    public struct Pool<T> : IEnumerable<T> where T : PoolObject, new()
    {
        public void Resize(ushort capacity, bool keepOld = false)
        {
            this.capacity = capacity;
            Array.Resize(ref pool, capacity);
            queue = new ExposedQueue<ushort>(capacity);
            if (!keepOld)
            {
                used.Clear();
            }
            used.Capacity = capacity;
            for (ushort i = 0; i < capacity; i++)
            {
                pool[i] = new T
                {
                    active = false,
                    whoAmI = i
                };
                pool[i].ResetValues();

                queue.EnqueueWithoutGrow(i);
            }
        }

        public static readonly Pool<T> Empty = new Pool<T>(0);
        public readonly bool IsEmpty => capacity == 0;
        private T[] pool;
        public ExposedList<T> used;
        private ushort capacity;
        public ExposedQueue<ushort> queue;
        public readonly ushort AvailableCount => (ushort)queue.Count; // esto es legal?????? poner readonly en una propiedad????
        public readonly ushort ActiveCount => (ushort)used.Count;
        public readonly ushort Capacity => capacity;
        public readonly ref T this[int index] => ref pool[index];
        public readonly ref T this[ushort index] => ref pool[index];
        public Pool(ushort capacity)
        {
            this.capacity = capacity;
            pool = Array.Empty<T>();
            used = new ExposedList<T>();
            Resize(capacity);
        }
        public readonly void Clear(bool clearItemDatas = false)
        {
            used.Clear();
            if (clearItemDatas)
            {
                for (ushort i = 0; i < pool.Length; i++)
                {
                    pool[i].active = false;
                    queue.EnqueueWithoutGrow(i);
                }
            }
            else
            {
                for (ushort i = 0; i < pool.Length; i++)
                {
                    pool[i].active = false;
                    pool[i].ResetValues();
                    queue.EnqueueWithoutGrow(i);
                }
            }
        }
        public readonly bool Get(out T value)
        {
            if (!queue.TryDequeue(out ushort index))
            {
                value = default;
                return false;
            }
            value = pool[index];
            value.active = true;
            used.Add(value);
            return true;
        }
        public readonly void Return(ushort index)
        {
            ref T item = ref pool[index];
            item.ResetValues();
            item.active = false;
            if (used.Remove(item))
                queue.EnqueueWithoutGrow(index);
        }
        public readonly void Return(T item)
        {
            item.ResetValues();
            item.active = false;
            if (used.Remove(item))
                queue.EnqueueWithoutGrow(item.whoAmI);
        }
        public bool Any() => ActiveCount > 0;
        public IEnumerator<T> GetEnumerator() => used.GetEnumerator();
        public IEnumerable<T> ActiveNodes => used;
        public IEnumerable<T> AllNodes => pool;
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public Span<T> GetSpan() => Any() ? new Span<T>(used.ToArray()) : Span<T>.Empty;
        public Span<T> GetUnsafeSpan() => new Span<T>(used.items, 0, used.Count);
        public IEnumerable<T> Where(Func<T, bool> predicate)
        {
            foreach (var variable in used)
            {
                if (predicate(variable))
                {
                    yield return variable;
                }
            }
        }
    }
    public abstract class PoolObject
    {
        public bool active;
        public ushort whoAmI;
        public abstract void Kill(DeathReason killReason = DeathReason.NotSpecified);
        public abstract void ResetValues();
        public override int GetHashCode() => whoAmI;
    }
}