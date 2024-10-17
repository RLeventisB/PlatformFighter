using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Threading;

namespace PlatformFighter.Miscelaneous
{
    [DebuggerDisplay("Count = {Count}")]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    [Serializable]
    public class ExposedList<T> : IList, IList<T>
    {
        public const int DefaultCapacity = 4;

        public T[] items; // Do not rename (binary serialization)
        public int size; // Do not rename (binary serialization)
        public int version; // Do not rename (binary serialization)

#pragma warning disable CA1825 // avoid the extra generic instantiation for Array.Empty<T>()
        public static readonly T[] s_emptyArray = new T[0];
#pragma warning restore CA1825

        // Constructs a List. The list is initially empty and has a capacity
        // of zero. Upon adding the first element to the list the capacity is
        // increased to DefaultCapacity, and then increased in multiples of two
        // as required.
        public ExposedList()
        {
            items = s_emptyArray;
        }

        // Constructs a List with a given initial capacity. The list is
        // initially empty, but will have room for the given number of elements
        // before any reallocations are required.
        //
        public ExposedList(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity needs to be non negative");

            if (capacity == 0)
                items = s_emptyArray;
            else
                items = new T[capacity];
        }

        // Constructs a List, copying the contents of the given collection. The
        // size and capacity of the new list will both be equal to the size of the
        // given collection.
        //
        public ExposedList(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            if (collection is ICollection<T> c)
            {
                int count = c.Count;
                if (count == 0)
                {
                    items = s_emptyArray;
                }
                else
                {
                    items = new T[count];
                    c.CopyTo(items, 0);
                    size = count;
                }
            }
            else
            {
                items = s_emptyArray;
                foreach (var item in collection!)
                {
                    Add(item);
                }
            }
        }

        // Gets and sets the capacity of this list.  The capacity is the size of
        // the public array used to hold items.  When set, the public
        // array of the list is reallocated to the given capacity.
        //
        public int Capacity
        {
            get => items.Length;
            set
            {
                if (value < size)
                {
                    throw new ArgumentOutOfRangeException("value", "New Capacity is too small");
                }

                if (value != items.Length)
                {
                    if (value > 0)
                    {
                        T[] newItems = new T[value];
                        if (size > 0)
                        {
                            Array.Copy(items, newItems, size);
                        }
                        items = newItems;
                    }
                    else
                    {
                        items = s_emptyArray;
                    }
                }
            }
        }

        // Read-only property describing how many elements are in the List.
        public int Count => size;

        bool IList.IsFixedSize => false;

        // Is this List read-only?
        bool ICollection<T>.IsReadOnly => false;

        bool IList.IsReadOnly => false;

        // Is this List synchronized (thread-safe)?
        bool ICollection.IsSynchronized => false;

        // Synchronization root for this object.
        object ICollection.SyncRoot => this;

        // Sets or Gets the element at the given index.
        public T this[int index]
        {
            get
            {
                // Following trick can reduce the range check by one
                if ((uint)index >= (uint)size)
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                return items[index];
            }
            set
            {
                if ((uint)index >= (uint)size)
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                version++;
                items[index] = value;
            }
        }

        public static bool IsCompatibleObject(object value) =>
            // Non-null values are fine.  Only accept nulls if T is a class or Nullable<U>.
            // Note that default(T) is not equal to null for value types except when T is Nullable<U>.
            value is T || (value == null && default(T) == null);

        object IList.this[int index]
        {
            get => this[index];
            set
            {
                ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(value, ExceptionArgument.value);

                try
                {
                    this[index] = (T)value!;
                }
                catch (InvalidCastException)
                {
                    ThrowHelper.ThrowWrongValueTypeArgumentException(value, typeof(T));
                }
            }
        }

        // Adds the given object to the end of this list. The size of the list is
        // increased by one. If required, the capacity of the list is doubled
        // before adding the new element.
        //
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            version++;
            T[] array = items;
            int size = this.size;
            if ((uint)size < (uint)array.Length)
            {
                this.size = size + 1;
                array[size] = item;
            }
            else
            {
                AddWithResize(item);
            }
        }

        // Non-inline from List.Add to improve its code quality as uncommon path
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void AddWithResize(T item)
        {
            Debug.Assert(this.size == items.Length);
            int size = this.size;
            Grow(size + 1);
            this.size = size + 1;
            items[size] = item;
        }

        int IList.Add(object item)
        {
            ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(item, ExceptionArgument.item);

            try
            {
                Add((T)item!);
            }
            catch (InvalidCastException)
            {
                ThrowHelper.ThrowWrongValueTypeArgumentException(item, typeof(T));
            }

            return Count - 1;
        }

        // Adds the elements of the given collection to the end of this list. If
        // required, the capacity of the list is increased to twice the previous
        // capacity or the new size, whichever is larger.
        //
        public void AddRange(IEnumerable<T> collection)
            => InsertRange(size, collection);

        public ReadOnlyCollection<T> AsReadOnly()
            => new ReadOnlyCollection<T>(this);

        // Searches a section of the list for a given element using a binary search
        // algorithm. Elements of the list are compared to the search value using
        // the given IComparer interface. If comparer is null, elements of
        // the list are compared to the search value using the IComparable
        // interface, which in that case must be implemented by all elements of the
        // list and the given search value. This method assumes that the given
        // section of the list is already sorted; if this is not the case, the
        // result will be incorrect.
        //
        // The method returns the index of the given value in the list. If the
        // list does not contain the given value, the method returns a negative
        // integer. The bitwise complement operator (~) can be applied to a
        // negative result to produce the index of the first element (if any) that
        // is larger than the given search value. This is also the index at which
        // the search value should be inserted into the list in order for the list
        // to remain sorted.
        //
        // The method uses the Array.BinarySearch method to perform the
        // search.
        //
        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            if (index < 0)
                ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
            if (count < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            if (size - index < count)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);

            return Array.BinarySearch(items, index, count, item, comparer);
        }

        public int BinarySearch(T item)
            => BinarySearch(0, Count, item, null);

        public int BinarySearch(T item, IComparer<T> comparer)
            => BinarySearch(0, Count, item, comparer);

        // Clears the contents of List.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            version++;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                int size = this.size;
                this.size = 0;
                if (size > 0)
                {
                    Array.Clear(items, 0, size); // Clear the elements so that the gc can reclaim the references.
                }
            }
            else
            {
                size = 0;
            }
        }

        // Contains returns true if the specified element is in the List.
        // It does a linear, O(n) search.  Equality is determined by calling
        // EqualityComparer<T>.Default.Equals().
        //
        public bool Contains(T item) =>
            // PERF: IndexOf calls Array.IndexOf, which publicly
            // calls EqualityComparer<T>.Default.IndexOf, which
            // is specialized for different types. This
            // boosts performance since instead of making a
            // virtual method call each iteration of the loop,
            // via EqualityComparer<T>.Default.Equals, we
            // only make one virtual call to EqualityComparer.IndexOf.

            size != 0 && IndexOf(item) >= 0;

        bool IList.Contains(object item)
        {
            if (IsCompatibleObject(item))
            {
                return Contains((T)item!);
            }
            return false;
        }

        public ExposedList<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            if (converter == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.converter);
            }

            ExposedList<TOutput> list = new ExposedList<TOutput>(size);
            for (int i = 0; i < size; i++)
            {
                list.items[i] = converter(items[i]);
            }
            list.size = size;
            return list;
        }

        // Copies this List into array, which must be of a
        // compatible array type.
        public void CopyTo(T[] array)
            => CopyTo(array, 0);

        // Copies this List into array, which must be of a
        // compatible array type.
        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            if (array.Rank != 1)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
            }

            try
            {
                // Array.Copy will check for NULL.
                Array.Copy(items, 0, array!, arrayIndex, size);
            }
            catch (ArrayTypeMismatchException)
            {
                ThrowHelper.ThrowArgumentException_Argument_InvalidArrayType();
            }
        }

        // Copies a section of this list to the given array at the given index.
        //
        // The method uses the Array.Copy method to copy the elements.
        //
        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            if (size - index < count)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
            }

            // Delegate rest of error checking to Array.Copy.
            Array.Copy(items, index, array, arrayIndex, count);
        }

        public void CopyTo(T[] array, int arrayIndex) =>
            // Delegate rest of error checking to Array.Copy.
            Array.Copy(items, 0, array, arrayIndex, size);

        /// <summary>
        /// Ensures that the capacity of this list is at least the specified <paramref name="capacity"/>.
        /// If the current capacity of the list is less than specified <paramref name="capacity"/>,
        /// the capacity is increased by continuously twice current capacity until it is at least the specified <paramref name="capacity"/>.
        /// </summary>
        /// <param name="capacity">The minimum capacity to ensure.</param>
        /// <returns>The new capacity of this list.</returns>
        public int EnsureCapacity(int capacity)
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (items.Length < capacity)
            {
                Grow(capacity);
                version++;
            }

            return items.Length;
        }

        /// <summary>
        /// Increase the capacity of this list to at least the specified <paramref name="capacity"/>.
        /// </summary>
        /// <param name="capacity">The minimum capacity to ensure.</param>
        public void Grow(int capacity)
        {
            Debug.Assert(items.Length < capacity);

            int newcapacity = items.Length == 0 ? DefaultCapacity : 2 * items.Length;

            // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
            // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
            if ((uint)newcapacity > Array.MaxLength) newcapacity = Array.MaxLength;

            // If the computed capacity is still less than specified, set to the original argument.
            // Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
            if (newcapacity < capacity) newcapacity = capacity;

            Capacity = newcapacity;
        }

        public bool Exists(Predicate<T> match)
            => FindIndex(match) != -1;

        public T Find(Predicate<T> match)
        {
            if (match == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }

            for (int i = 0; i < size; i++)
            {
                if (match(items[i]))
                {
                    return items[i];
                }
            }
            return default;
        }

        public List<T> FindAll(Predicate<T> match)
        {
            if (match == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }

            List<T> list = new List<T>();
            for (int i = 0; i < size; i++)
            {
                if (match(items[i]))
                {
                    list.Add(items[i]);
                }
            }
            return list;
        }

        public int FindIndex(Predicate<T> match)
            => FindIndex(0, size, match);

        public int FindIndex(int startIndex, Predicate<T> match)
            => FindIndex(startIndex, size - startIndex, match);

        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            if ((uint)startIndex > (uint)size)
            {
                ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLessOrEqual();
            }

            if (count < 0 || startIndex > size - count)
            {
                ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
            }

            if (match == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }

            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (match(items[i])) return i;
            }
            return -1;
        }

        public T FindLast(Predicate<T> match)
        {
            if (match == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }

            for (int i = size - 1; i >= 0; i--)
            {
                if (match(items[i]))
                {
                    return items[i];
                }
            }
            return default;
        }

        public int FindLastIndex(Predicate<T> match)
            => FindLastIndex(size - 1, size, match);

        public int FindLastIndex(int startIndex, Predicate<T> match)
            => FindLastIndex(startIndex, startIndex + 1, match);

        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            if (match == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }

            if (size == 0)
            {
                // Special case for 0 length List
                if (startIndex != -1)
                {
                    ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
                }
            }
            else
            {
                // Make sure we're not out of range
                if ((uint)startIndex >= (uint)size)
                {
                    ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
                }
            }

            // 2nd have of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
            if (count < 0 || startIndex - count + 1 < 0)
            {
                ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
            }

            int endIndex = startIndex - count;
            for (int i = startIndex; i > endIndex; i--)
            {
                if (match(items[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public void ForEach(Action<T> action)
        {
            if (action == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.action);
            }

            int version = this.version;

            for (int i = 0; i < size; i++)
            {
                if (version != this.version)
                {
                    break;
                }
                action(items[i]);
            }

            if (version != this.version)
                ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
        }

        // Returns an enumerator for this list with the given
        // permission for removal of elements. If modifications made to the list
        // while an enumeration is in progress, the MoveNext and
        // GetObject methods of the enumerator will throw an exception.
        //
        public Enumerator GetEnumerator()
            => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator()
            => new Enumerator(this);

        public ExposedList<T> GetRange(int index, int count)
        {
            if (index < 0)
            {
                ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
            }

            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (size - index < count)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
            }

            ExposedList<T> list = new ExposedList<T>(count);
            Array.Copy(items, index, list.items, 0, count);
            list.size = count;
            return list;
        }

        // Returns the index of the first occurrence of a given value in a range of
        // this list. The list is searched forwards from beginning to end.
        // The elements of the list are compared to the given value using the
        // Object.Equals method.
        //
        // This method uses the Array.IndexOf method to perform the
        // search.
        //
        public int IndexOf(T item)
            => Array.IndexOf(items, item, 0, size);

        int IList.IndexOf(object item)
        {
            if (IsCompatibleObject(item))
            {
                return IndexOf((T)item!);
            }
            return -1;
        }

        // Returns the index of the first occurrence of a given value in a range of
        // this list. The list is searched forwards, starting at index
        // index and ending at count number of elements. The
        // elements of the list are compared to the given value using the
        // Object.Equals method.
        //
        // This method uses the Array.IndexOf method to perform the
        // search.
        //
        public int IndexOf(T item, int index)
        {
            if (index > size)
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
            return Array.IndexOf(items, item, index, size - index);
        }

        // Returns the index of the first occurrence of a given value in a range of
        // this list. The list is searched forwards, starting at index
        // index and upto count number of elements. The
        // elements of the list are compared to the given value using the
        // Object.Equals method.
        //
        // This method uses the Array.IndexOf method to perform the
        // search.
        //
        public int IndexOf(T item, int index, int count)
        {
            if (index > size)
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();

            if (count < 0 || index > size - count)
                ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();

            return Array.IndexOf(items, item, index, count);
        }

        // Inserts an element into this list at a given index. The size of the list
        // is increased by one. If required, the capacity of the list is doubled
        // before inserting the new element.
        //
        public void Insert(int index, T item)
        {
            // Note that insertions at the end are legal.
            if ((uint)index > (uint)size)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_ListInsert);
            }
            if (size == items.Length) Grow(size + 1);
            if (index < size)
            {
                Array.Copy(items, index, items, index + 1, size - index);
            }
            items[index] = item;
            size++;
            version++;
        }

        void IList.Insert(int index, object item)
        {
            ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(item, ExceptionArgument.item);

            try
            {
                Insert(index, (T)item!);
            }
            catch (InvalidCastException)
            {
                ThrowHelper.ThrowWrongValueTypeArgumentException(item, typeof(T));
            }
        }

        // Inserts the elements of the given collection at a given index. If
        // required, the capacity of the list is increased to twice the previous
        // capacity or the new size, whichever is larger.  Ranges may be added
        // to the end of the list by setting index to the List's size.
        //
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (collection == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection);
            }

            if ((uint)index > (uint)size)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
            }

            if (collection is ICollection<T> c)
            {
                int count = c.Count;
                if (count > 0)
                {
                    if (items.Length - size < count)
                    {
                        Grow(size + count);
                    }
                    if (index < size)
                    {
                        Array.Copy(items, index, items, index + count, size - index);
                    }

                    // If we're inserting a List into itself, we want to be able to deal with that.
                    if (this == c)
                    {
                        // Copy first part of _items to insert location
                        Array.Copy(items, 0, items, index, index);
                        // Copy last part of _items back to inserted location
                        Array.Copy(items, index + count, items, index * 2, size - index);
                    }
                    else
                    {
                        c.CopyTo(items, index);
                    }
                    size += count;
                }
            }
            else
            {
                using (IEnumerator<T> en = collection.GetEnumerator())
                {
                    while (en.MoveNext())
                    {
                        Insert(index++, en.Current);
                    }
                }
            }
            version++;
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at the end
        // and ending at the first element in the list. The elements of the list
        // are compared to the given value using the Object.Equals method.
        //
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        //
        public int LastIndexOf(T item)
        {
            if (size == 0)
            {
                // Special case for empty list
                return -1;
            }
            return LastIndexOf(item, size - 1, size);
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at index
        // index and ending at the first element in the list. The
        // elements of the list are compared to the given value using the
        // Object.Equals method.
        //
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        //
        public int LastIndexOf(T item, int index)
        {
            if (index >= size)
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessException();
            return LastIndexOf(item, index, index + 1);
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at index
        // index and upto count elements. The elements of
        // the list are compared to the given value using the Object.Equals
        // method.
        //
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        //
        public int LastIndexOf(T item, int index, int count)
        {
            if (Count != 0 && index < 0)
            {
                ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
            }

            if (Count != 0 && count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (size == 0)
            {
                // Special case for empty list
                return -1;
            }

            if (index >= size)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_BiggerThanCollection);
            }

            if (count > index + 1)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_BiggerThanCollection);
            }

            return Array.LastIndexOf(items, item, index, count);
        }

        // Removes the element at the given index. The size of the list is
        // decreased by one.
        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        void IList.Remove(object item)
        {
            if (IsCompatibleObject(item))
            {
                Remove((T)item!);
            }
        }

        // This method removes all items which matches the predicate.
        // The complexity is O(n).
        public int RemoveAll(Predicate<T> match)
        {
            if (match == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }

            int freeIndex = 0; // the first free slot in items array

            // Find the first item which needs to be removed.
            while (freeIndex < size && !match(items[freeIndex])) freeIndex++;
            if (freeIndex >= size) return 0;

            int current = freeIndex + 1;
            while (current < size)
            {
                // Find the first item which needs to be kept.
                while (current < size && match(items[current])) current++;

                if (current < size)
                {
                    // copy item to the free slot.
                    items[freeIndex++] = items[current++];
                }
            }

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(items, freeIndex, size - freeIndex); // Clear the elements so that the gc can reclaim the references.
            }

            int result = size - freeIndex;
            size = freeIndex;
            version++;
            return result;
        }

        // Removes the element at the given index. The size of the list is
        // decreased by one.
        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)size)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessException();
            }
            size--;
            if (index < size)
            {
                Array.Copy(items, index + 1, items, index, size - index);
            }
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                items[size] = default!;
            }
            version++;
        }

        // Removes a range of elements from this list.
        public void RemoveRange(int index, int count)
        {
            if (index < 0)
            {
                ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
            }

            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (size - index < count)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);

            if (count > 0)
            {
                size -= count;
                if (index < size)
                {
                    Array.Copy(items, index + count, items, index, size - index);
                }

                version++;
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    Array.Clear(items, size, count);
                }
            }
        }

        // Reverses the elements in this list.
        public void Reverse()
            => Reverse(0, Count);

        // Reverses the elements in a range of this list. Following a call to this
        // method, an element in the range given by index and count
        // which was previously located at index i will now be located at
        // index index + (index + count - i - 1).
        //
        public void Reverse(int index, int count)
        {
            if (index < 0)
            {
                ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
            }

            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (size - index < count)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);

            if (count > 1)
            {
                Array.Reverse(items, index, count);
            }
            version++;
        }

        // Sorts the elements in this list.  Uses the default comparer and
        // Array.Sort.
        public void Sort()
            => Sort(0, Count, null);

        // Sorts the elements in this list.  Uses Array.Sort with the
        // provided comparer.
        public void Sort(IComparer<T> comparer)
            => Sort(0, Count, comparer);

        // Sorts the elements in a section of this list. The sort compares the
        // elements to each other using the given IComparer interface. If
        // comparer is null, the elements are compared to each other using
        // the IComparable interface, which in that case must be implemented by all
        // elements of the list.
        //
        // This method uses the Array.Sort method to sort the elements.
        //
        public void Sort(int index, int count, IComparer<T> comparer)
        {
            if (index < 0)
            {
                ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
            }

            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (size - index < count)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);

            if (count > 1)
            {
                Array.Sort(items, index, count, comparer);
            }
            version++;
        }

        public void Sort(Comparison<T> comparison)
        {
            if (comparison == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.comparison);
            }

            ArraySortHelper<T>.Sort(new Span<T>(items, 0, size), comparison);
            version++;
        }

        // ToArray returns an array containing the contents of the List.
        // This requires copying the List, which is an O(n) operation.
        public T[] ToArray()
        {
            if (size == 0)
            {
                return s_emptyArray;
            }

            T[] array = new T[size];
            Array.Copy(items, array, size);
            return array;
        }

        // Sets the capacity of this list to the size of the list. This method can
        // be used to minimize a list's memory overhead once it is known that no
        // new elements will be added to the list. To completely clear a list and
        // release all memory referenced by the list, execute the following
        // statements:
        //
        // list.Clear();
        // list.TrimExcess();
        //
        public void TrimExcess()
        {
            int threshold = (int)(items.Length * 0.9);
            if (size < threshold)
            {
                Capacity = size;
            }
        }

        public bool TrueForAll(Predicate<T> match)
        {
            if (match == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }

            for (int i = 0; i < size; i++)
            {
                if (!match(items[i]))
                {
                    return false;
                }
            }
            return true;
        }
        public Span<T> GetUnsafeSpan() => new Span<T>(items, 0, size);
        public Span<T> GetSafeSpan() => new Span<T>(ToArray());
        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            public readonly ExposedList<T> list;
            public int index;
            public readonly int version;
            public T current;

            public Enumerator(ExposedList<T> list)
            {
                this.list = list;
                index = 0;
                version = list.version;
                current = default;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                ExposedList<T> localList = list;

                if (version == localList.version && (uint)index < (uint)localList.size)
                {
                    current = localList.items[index];
                    index++;
                    return true;
                }
                return MoveNextRare();
            }

            public bool MoveNextRare()
            {
                if (version != list.version)
                {
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
                }

                index = list.size + 1;
                current = default;
                return false;
            }

            public T Current => current!;

            object IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index == list.size + 1)
                    {
                        ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();
                    }
                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                if (version != list.version)
                {
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
                }

                index = 0;
                current = default;
            }
        }
    }
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    [ComVisible(false)]
    public class ExposedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary, IReadOnlyDictionary<TKey, TValue>, ISerializable, IDeserializationCallback
    {
        public struct Entry
        {
            public int hashCode; // Lower 31 bits of hash code, -1 if unused
            public int next; // Index of next entry, -1 if last
            public TKey key; // Key of entry
            public TValue value; // Value of entry
        }
        public int[] buckets;
        public Entry[] entries;
        public int count;
        public int version;
        public int freeList;
        public int freeCount;
        public IEqualityComparer<TKey> comparer;
        public KeyCollection keys;
        public ValueCollection values;
        public object _syncRoot;

        // constants for serialization
        public const string VersionName = "Version";
        public const string HashSizeName = "HashSize"; // Must save buckets.Length
        public const string KeyValuePairsName = "KeyValuePairs";
        public const string ComparerName = "Comparer";
        public ExposedDictionary() : this(0, null)
        {
        }
        public ExposedDictionary(int capacity) : this(capacity, null)
        {
        }
        public ExposedDictionary(IEqualityComparer<TKey> comparer) : this(0, comparer)
        {
        }
        public ExposedDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException("capacity");
            if (capacity > 0) Initialize(capacity);
            this.comparer = comparer ?? EqualityComparer<TKey>.Default;
        }
        public ExposedDictionary(IDictionary<TKey, TValue> dictionary) : this(dictionary, null)
        {
        }
        public ExposedDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) : this(dictionary is not null ? dictionary.Count : 0, comparer)
        {
            if (dictionary is null)
            {
                throw new ArgumentNullException("dictionary");
            }

            foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            {
                Add(pair.Key, pair.Value);
            }
        }

        protected ExposedDictionary(SerializationInfo info, StreamingContext context)
        {
            //We can't do anything with the keys and values until the entire graph has been deserialized
            //and we have a resonable estimate that GetHashCode is not going to fail.  For the time being,
            //we'll just cache this.  The graph is not valid until OnDeserialization has been called.
            HashHelpers.SerializationInfoTable.Add(this, info);
        }

        public IEqualityComparer<TKey> Comparer => comparer;
        public int Count => count - freeCount;
        public KeyCollection Keys => keys ??= new KeyCollection(this);

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => keys ??= new KeyCollection(this);
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => keys ??= new KeyCollection(this);
        public ValueCollection Values => values ??= new ValueCollection(this);
        ICollection<TValue> IDictionary<TKey, TValue>.Values => values ??= new ValueCollection(this);
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => values ??= new ValueCollection(this);
        public TValue this[TKey key]
        {
            get
            {
                int i = FindEntry(key);
                if (i >= 0) return entries[i].value;
                throw new KeyNotFoundException();
            }
            set
            {
                Insert(key, value, false);
            }
        }
        public TValue this[int hashCode]
        {
            get
            {
                int i = FindEntryByHash(hashCode);
                if (i >= 0) return entries[i].value;
                throw new KeyNotFoundException();
            }
        }
        public ref TValue FindValue(TKey key)
        {
            int i = FindEntry(key);
            if (i >= 0)
                return ref entries[i].value;
            return ref Unsafe.NullRef<TValue>();
        }
        public void Add(TKey key, TValue value) => Insert(key, value, true);

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair) => Add(keyValuePair.Key, keyValuePair.Value);

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
        {
            int i = FindEntry(keyValuePair.Key);
            if (i >= 0 && EqualityComparer<TValue>.Default.Equals(entries[i].value, keyValuePair.Value))
            {
                return true;
            }
            return false;
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
        {
            int i = FindEntry(keyValuePair.Key);
            if (i >= 0 && EqualityComparer<TValue>.Default.Equals(entries[i].value, keyValuePair.Value))
            {
                Remove(keyValuePair.Key);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            if (count > 0)
            {
                for (int i = 0; i < buckets.Length; i++) buckets[i] = -1;
                Array.Clear(entries, 0, count);
                freeList = -1;
                count = 0;
                freeCount = 0;
                version++;
            }
        }

        public bool ContainsKey(TKey key) => FindEntry(key) >= 0;

        public bool ContainsValue(TValue value)
        {
            if (value is null)
            {
                for (int i = 0; i < count; i++)
                {
                    if (entries[i].hashCode >= 0 && entries[i].value is null) return true;
                }
            }
            else
            {
                EqualityComparer<TValue> c = EqualityComparer<TValue>.Default;
                for (int i = 0; i < count; i++)
                {
                    if (entries[i].hashCode >= 0 && c.Equals(entries[i].value, value)) return true;
                }
            }
            return false;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            if (array is null)
            {
                throw new ArgumentNullException("array");
            }

            if (index < 0 || index > array.Length)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            if (array.Length - index < Count)
            {
                throw new ArgumentException();
            }

            int count = this.count;
            Entry[] entries = this.entries;
            for (int i = 0; i < count; i++)
            {
                if (entries[i].hashCode >= 0)
                {
                    array[index++] = new KeyValuePair<TKey, TValue>(entries[i].key, entries[i].value);
                }
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this, Enumerator.KeyValuePair);

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => new Enumerator(this, Enumerator.KeyValuePair);

        [SecurityCritical] // auto-generated_required
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info is null)
            {
                throw new ArgumentNullException("info");
            }
            info.AddValue(VersionName, version);

            info.AddValue(HashSizeName, buckets is null ? 0 : buckets.Length); //This is the length of the bucket array.
            if (buckets is not null)
            {
                KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[Count];
                CopyTo(array, 0);
                info.AddValue(KeyValuePairsName, array, typeof(KeyValuePair<TKey, TValue>[]));
            }
        }

        public int FindEntry(TKey key)
        {
            if (key is null)
            {
                throw new ArgumentNullException("key");
            }

            if (buckets is not null)
            {
                int hashCode = comparer.GetHashCode(key) & 0x7FFFFFFF;
                for (int i = buckets[hashCode % buckets.Length]; i >= 0; i = entries[i].next)
                {
                    if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key)) return i;
                }
            }
            return -1;
        }
        public int FindEntryByHash(int hashCode)
        {
            if (buckets is not null)
            {
                hashCode &= 0x7FFFFFFF;
                for (int i = buckets[hashCode % buckets.Length]; i >= 0; i = entries[i].next)
                {
                    if (entries[i].hashCode == hashCode) return i;
                }
            }
            return -1;
        }
        public void Initialize(int capacity)
        {
            int size = HashHelpers.GetPrime(capacity);
            buckets = new int[size];
            for (int i = 0; i < buckets.Length; i++) buckets[i] = -1;
            entries = new Entry[size];
            freeList = -1;
        }

        public void Insert(TKey key, TValue value, bool add)
        {
            if (key is null)
            {
                throw new ArgumentNullException("key");
            }

            if (buckets is null) Initialize(0);
            int hashCode = comparer.GetHashCode(key) & 0x7FFFFFFF;
            int targetBucket = hashCode % buckets.Length;

            for (int i = buckets[targetBucket]; i >= 0; i = entries[i].next)
            {
                if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key))
                {
                    if (add)
                    {
                        throw new ArgumentException("An object already exists in this dictionary");
                    }
                    entries[i].value = value;
                    version++;
                    return;
                }
            }
            int index;
            if (freeCount > 0)
            {
                index = freeList;
                freeList = entries[index].next;
                freeCount--;
            }
            else
            {
                if (count == entries.Length)
                {
                    Resize();
                    targetBucket = hashCode % buckets.Length;
                }
                index = count;
                count++;
            }

            entries[index].hashCode = hashCode;
            entries[index].next = buckets[targetBucket];
            entries[index].key = key;
            entries[index].value = value;
            buckets[targetBucket] = index;
            version++;
        }

        public virtual void OnDeserialization(object sender)
        {
            HashHelpers.SerializationInfoTable.TryGetValue(this, out SerializationInfo siInfo);

            if (siInfo is null)
            {
                // It might be necessary to call OnDeserialization from a container if the container object also implements
                // OnDeserialization. However, remoting will call OnDeserialization again.
                // We can return immediately if this function is called twice. 
                // Note we set remove the serialization info from the table at the end of this method.
                return;
            }

            int realVersion = siInfo.GetInt32(VersionName);
            int hashsize = siInfo.GetInt32(HashSizeName);
            comparer = (IEqualityComparer<TKey>)siInfo.GetValue(ComparerName, typeof(IEqualityComparer<TKey>));

            if (hashsize != 0)
            {
                buckets = new int[hashsize];
                for (int i = 0; i < buckets.Length; i++) buckets[i] = -1;
                entries = new Entry[hashsize];
                freeList = -1;

                KeyValuePair<TKey, TValue>[] array = (KeyValuePair<TKey, TValue>[])
                    siInfo.GetValue(KeyValuePairsName, typeof(KeyValuePair<TKey, TValue>[]));

                if (array is null)
                {
                    throw new SerializationException("Missing keys");
                }

                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i].Key is null)
                    {
                        throw new SerializationException("Null keys");
                    }
                    Insert(array[i].Key, array[i].Value, true);
                }
            }
            else
            {
                buckets = null;
            }

            version = realVersion;
            HashHelpers.SerializationInfoTable.Remove(this);
        }
        public void Resize() => Resize(HashHelpers.ExpandPrime(count), false);
        public void Resize(int newSize, bool forceNewHashCodes)
        {
            int[] newBuckets = new int[newSize];
            for (int i = 0; i < newBuckets.Length; i++) newBuckets[i] = -1;
            Entry[] newEntries = new Entry[newSize];
            Array.Copy(entries, 0, newEntries, 0, count);
            if (forceNewHashCodes)
            {
                for (int i = 0; i < count; i++)
                {
                    if (newEntries[i].hashCode != -1)
                    {
                        newEntries[i].hashCode = comparer.GetHashCode(newEntries[i].key) & 0x7FFFFFFF;
                    }
                }
            }
            for (int i = 0; i < count; i++)
            {
                if (newEntries[i].hashCode >= 0)
                {
                    int bucket = newEntries[i].hashCode % newSize;
                    newEntries[i].next = newBuckets[bucket];
                    newBuckets[bucket] = i;
                }
            }
            buckets = newBuckets;
            entries = newEntries;
        }

        public bool Remove(TKey key)
        {
            if (key is null)
            {
                throw new ArgumentNullException("key");
            }

            if (buckets is not null)
            {
                int hashCode = comparer.GetHashCode(key) & 0x7FFFFFFF;
                int bucket = hashCode % buckets.Length;
                int last = -1;
                for (int i = buckets[bucket]; i >= 0; last = i, i = entries[i].next)
                {
                    if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key))
                    {
                        if (last < 0)
                        {
                            buckets[bucket] = entries[i].next;
                        }
                        else
                        {
                            entries[last].next = entries[i].next;
                        }
                        entries[i].hashCode = -1;
                        entries[i].next = freeList;
                        entries[i].key = default;
                        entries[i].value = default;
                        freeList = i;
                        freeCount++;
                        version++;
                        return true;
                    }
                }
            }
            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            int i = FindEntry(key);
            if (i >= 0)
            {
                value = entries[i].value;
                return true;
            }
            value = default;
            return false;
        }

        // This is a convenience method for the public callers that were converted from using Hashtable.
        // Many were combining key doesn't exist and key exists but null value (for non-value types) checks.
        // This allows them to continue getting that behavior with minimal code delta. This is basically
        // TryGetValue without the out param
        public TValue GetValueOrDefault(TKey key)
        {
            int i = FindEntry(key);
            if (i >= 0)
            {
                return entries[i].value;
            }
            return default;
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get { return false; }
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index) => CopyTo(array, index);

        void ICollection.CopyTo(Array array, int index)
        {
            if (array is null)
            {
                throw new ArgumentNullException("array");
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException("array", "Multidimensional arrays are not supported");
            }

            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException("array", "Lower bound of array needs to be non zero");
            }

            if (index < 0 || index > array.Length)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            if (array.Length - index < Count)
            {
                throw new ArgumentException();
            }

            switch (array)
            {
                case KeyValuePair<TKey, TValue>[] pairs:
                    CopyTo(pairs, index);
                    break;
                case DictionaryEntry[] dictionaryEntries:
                    {
                        Entry[] entries = this.entries;
                        for (int i = 0; i < count; i++)
                        {
                            if (entries[i].hashCode >= 0)
                            {
                                dictionaryEntries[index++] = new DictionaryEntry(entries[i].key, entries[i].value);
                            }
                        }
                        break;
                    }
                default:
                    {
                        if (array is not object[] objects)
                        {
                            throw new ArgumentException("");
                        }

                        try
                        {
                            int count = this.count;
                            Entry[] entries = this.entries;
                            for (int i = 0; i < count; i++)
                            {
                                if (entries[i].hashCode >= 0)
                                {
                                    objects[index++] = new KeyValuePair<TKey, TValue>(entries[i].key, entries[i].value);
                                }
                            }
                        }
                        catch (ArrayTypeMismatchException)
                        {
                            throw new ArgumentException("");
                        }
                        break;
                    }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this, Enumerator.KeyValuePair);

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot is null)
                {
                    Interlocked.CompareExchange<object>(ref _syncRoot, new object(), null);
                }
                return _syncRoot;
            }
        }
        bool IDictionary.IsFixedSize
        {
            get { return false; }
        }
        bool IDictionary.IsReadOnly
        {
            get { return false; }
        }
        ICollection IDictionary.Keys
        {
            get { return Keys; }
        }

        ICollection IDictionary.Values
        {
            get { return Values; }
        }

        object IDictionary.this[object key]
        {
            get
            {
                if (IsCompatibleKey(key))
                {
                    int i = FindEntry((TKey)key);
                    if (i >= 0)
                    {
                        return entries[i].value;
                    }
                }
                return null;
            }
            set
            {
                if (key is null)
                {
                    throw new ArgumentNullException("key");
                }
                if (default(TValue) is not null && value is null)
                {
                    throw new ArgumentNullException("value");
                }

                try
                {
                    TKey tempKey = (TKey)key;
                    try
                    {
                        this[tempKey] = (TValue)value;
                    }
                    catch (InvalidCastException)
                    {
                        throw new ArgumentException();
                    }
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException();
                }
            }
        }

        public static bool IsCompatibleKey(object key)
        {
            if (key is null)
            {
                throw new ArgumentNullException("key");
            }
            return key is TKey;
        }

        void IDictionary.Add(object key, object value)
        {
            if (key is null)
            {
                throw new ArgumentNullException("key");
            }
            if (default(TValue) is not null && value is null)
            {
                throw new ArgumentNullException("value");
            }

            try
            {
                TKey tempKey = (TKey)key;

                try
                {
                    Add(tempKey, (TValue)value);
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException();
                }
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException();
            }
        }

        bool IDictionary.Contains(object key)
        {
            if (IsCompatibleKey(key))
            {
                return ContainsKey((TKey)key);
            }

            return false;
        }

        IDictionaryEnumerator IDictionary.GetEnumerator() => new Enumerator(this, Enumerator.DictEntry);

        void IDictionary.Remove(object key)
        {
            if (IsCompatibleKey(key))
            {
                Remove((TKey)key);
            }
        }

        [Serializable]
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
        {
            public readonly ExposedDictionary<TKey, TValue> dictionary;
            public readonly int version;
            public int index;
            public KeyValuePair<TKey, TValue> current;
            public readonly int getEnumeratorRetType; // What should Enumerator.Current return?

            public const int DictEntry = 1;
            public const int KeyValuePair = 2;

            public Enumerator(ExposedDictionary<TKey, TValue> dictionary, int getEnumeratorRetType)
            {
                this.dictionary = dictionary;
                version = dictionary.version;
                index = 0;
                this.getEnumeratorRetType = getEnumeratorRetType;
                current = new KeyValuePair<TKey, TValue>();
            }

            public bool MoveNext()
            {
                if (version != dictionary.version)
                {
                    throw new InvalidOperationException();
                }

                // Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
                // dictionary.count+1 could be negative if dictionary.count is Int32.MaxValue
                while ((uint)index < (uint)dictionary.count)
                {
                    if (dictionary.entries[index].hashCode >= 0)
                    {
                        current = new KeyValuePair<TKey, TValue>(dictionary.entries[index].key, dictionary.entries[index].value);
                        index++;
                        return true;
                    }
                    index++;
                }

                index = dictionary.count + 1;
                current = new KeyValuePair<TKey, TValue>();
                return false;
            }

            public KeyValuePair<TKey, TValue> Current
            {
                get { return current; }
            }

            public void Dispose()
            {
            }

            object IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index == dictionary.count + 1)
                    {
                        throw new InvalidOperationException();
                    }

                    if (getEnumeratorRetType == DictEntry)
                    {
                        return new DictionaryEntry(current.Key, current.Value);
                    }
                    return new KeyValuePair<TKey, TValue>(current.Key, current.Value);
                }
            }

            void IEnumerator.Reset()
            {
                if (version != dictionary.version)
                {
                    throw new InvalidOperationException();
                }

                index = 0;
                current = new KeyValuePair<TKey, TValue>();
            }

            DictionaryEntry IDictionaryEnumerator.Entry
            {
                get
                {
                    if (index == 0 || index == dictionary.count + 1)
                    {
                        throw new InvalidOperationException();
                    }

                    return new DictionaryEntry(current.Key, current.Value);
                }
            }

            object IDictionaryEnumerator.Key
            {
                get
                {
                    if (index == 0 || index == dictionary.count + 1)
                    {
                        throw new InvalidOperationException();
                    }

                    return current.Key;
                }
            }

            object IDictionaryEnumerator.Value
            {
                get
                {
                    if (index == 0 || index == dictionary.count + 1)
                    {
                        throw new InvalidOperationException();
                    }

                    return current.Value;
                }
            }
        }

        [DebuggerDisplay("Count = {Count}")]
        [Serializable]
        public sealed class KeyCollection : ICollection<TKey>, ICollection, IReadOnlyCollection<TKey>
        {
            public readonly ExposedDictionary<TKey, TValue> dictionary;

            public KeyCollection(ExposedDictionary<TKey, TValue> dictionary)
            {
                this.dictionary = dictionary ?? throw new ArgumentNullException("dictionary");
            }

            public Enumerator GetEnumerator() => new Enumerator(dictionary);

            public void CopyTo(TKey[] array, int index)
            {
                if (array is null)
                {
                    throw new ArgumentNullException("array");
                }

                if (index < 0 || index > array.Length)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                if (array.Length - index < dictionary.Count)
                {
                    throw new ArgumentException();
                }

                int count = dictionary.count;
                Entry[] entries = dictionary.entries;
                for (int i = 0; i < count; i++)
                {
                    if (entries[i].hashCode >= 0) array[index++] = entries[i].key;
                }
            }

            public int Count => dictionary.Count;

            bool ICollection<TKey>.IsReadOnly => true;

            void ICollection<TKey>.Add(TKey item) => throw new NotSupportedException();
            void ICollection<TKey>.Clear() => throw new NotSupportedException();
            bool ICollection<TKey>.Contains(TKey item) => dictionary.ContainsKey(item);
            bool ICollection<TKey>.Remove(TKey item) => throw new NotSupportedException();

            IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() => new Enumerator(dictionary);

            IEnumerator IEnumerable.GetEnumerator() => new Enumerator(dictionary);

            void ICollection.CopyTo(Array array, int index)
            {
                if (array is null)
                {
                    throw new ArgumentNullException("array");
                }
                if (array.Rank != 1)
                {
                    throw new ArgumentException("array", "Multidimensional arrays are not supported");
                }

                if (array.GetLowerBound(0) != 0)
                {
                    throw new ArgumentException("array", "Lower bound of array needs to be non zero");
                }

                if (index < 0 || index > array.Length)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                if (array.Length - index < dictionary.Count)
                {
                    throw new ArgumentException();
                    ;
                }

                if (array is TKey[] keys)
                {
                    CopyTo(keys, index);
                }
                else
                {
                    if (array is not object[] objects)
                    {
                        throw new ArgumentException("");
                    }

                    int count = dictionary.count;
                    Entry[] entries = dictionary.entries;
                    try
                    {
                        for (int i = 0; i < count; i++)
                        {
                            if (entries[i].hashCode >= 0) objects[index++] = entries[i].key;
                        }
                    }
                    catch (ArrayTypeMismatchException)
                    {
                        throw new ArgumentException("");
                    }
                }
            }

            bool ICollection.IsSynchronized
            {
                get { return false; }
            }

            object ICollection.SyncRoot
            {
                get { return ((ICollection)dictionary).SyncRoot; }
            }

            [Serializable]
            public struct Enumerator : IEnumerator<TKey>, IEnumerator
            {
                public readonly ExposedDictionary<TKey, TValue> dictionary;
                public int index;
                public readonly int version;
                public TKey currentKey;

                public Enumerator(ExposedDictionary<TKey, TValue> dictionary)
                {
                    this.dictionary = dictionary;
                    version = dictionary.version;
                    index = 0;
                    currentKey = default;
                }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    if (version != dictionary.version)
                    {
                        throw new InvalidOperationException();
                    }

                    while ((uint)index < (uint)dictionary.count)
                    {
                        if (dictionary.entries[index].hashCode >= 0)
                        {
                            currentKey = dictionary.entries[index].key;
                            index++;
                            return true;
                        }
                        index++;
                    }

                    index = dictionary.count + 1;
                    currentKey = default;
                    return false;
                }

                public TKey Current => currentKey;
                object IEnumerator.Current
                {
                    get
                    {
                        if (index == 0 || index == dictionary.count + 1)
                        {
                            throw new InvalidOperationException();
                        }

                        return currentKey;
                    }
                }

                void IEnumerator.Reset()
                {
                    if (version != dictionary.version)
                    {
                        throw new InvalidOperationException();
                    }

                    index = 0;
                    currentKey = default;
                }
            }
        }

        [DebuggerDisplay("Count = {Count}")]
        [Serializable]
        public sealed class ValueCollection : ICollection<TValue>, ICollection, IReadOnlyCollection<TValue>
        {
            public readonly ExposedDictionary<TKey, TValue> dictionary;
            public ValueCollection(ExposedDictionary<TKey, TValue> dictionary)
            {
                this.dictionary = dictionary ?? throw new ArgumentNullException("dictionary");
            }

            public Enumerator GetEnumerator() => new Enumerator(dictionary);

            public void CopyTo(TValue[] array, int index)
            {
                if (array is null)
                {
                    throw new ArgumentNullException("array");
                }

                if (index < 0 || index > array.Length)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                if (array.Length - index < dictionary.Count)
                {
                    throw new ArgumentException();
                    ;
                }

                int count = dictionary.count;
                Entry[] entries = dictionary.entries;
                for (int i = 0; i < count; i++)
                {
                    if (entries[i].hashCode >= 0) array[index++] = entries[i].value;
                }
            }

            public int Count => dictionary.Count;

            bool ICollection<TValue>.IsReadOnly => true;

            void ICollection<TValue>.Add(TValue item) => throw new NotSupportedException();

            bool ICollection<TValue>.Remove(TValue item) => throw new NotSupportedException();

            void ICollection<TValue>.Clear() => throw new NotSupportedException();

            bool ICollection<TValue>.Contains(TValue item) => dictionary.ContainsValue(item);

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => new Enumerator(dictionary);

            IEnumerator IEnumerable.GetEnumerator() => new Enumerator(dictionary);

            void ICollection.CopyTo(Array array, int index)
            {
                if (array is null)
                {
                    throw new ArgumentNullException("array");
                }

                if (array.Rank != 1)
                {
                    throw new ArgumentException("array", "Multidimensional arrays are not supported");
                }

                if (array.GetLowerBound(0) != 0)
                {
                    throw new ArgumentException("array", "Lower bound of array needs to be non zero");
                }

                if (index < 0 || index > array.Length)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                if (array.Length - index < dictionary.Count)
                    throw new ArgumentException();
                ;

                if (array is TValue[] values)
                {
                    CopyTo(values, index);
                }
                else
                {
                    if (array is not object[] objects)
                    {
                        throw new ArgumentException();
                    }

                    int count = dictionary.count;
                    Entry[] entries = dictionary.entries;
                    try
                    {
                        for (int i = 0; i < count; i++)
                        {
                            if (entries[i].hashCode >= 0) objects[index++] = entries[i].value;
                        }
                    }
                    catch (ArrayTypeMismatchException)
                    {
                        throw new ArgumentException("");
                    }
                }
            }

            bool ICollection.IsSynchronized
            {
                get { return false; }
            }

            object ICollection.SyncRoot
            {
                get { return ((ICollection)dictionary).SyncRoot; }
            }

            [Serializable]
            public struct Enumerator : IEnumerator<TValue>, IEnumerator
            {
                public ExposedDictionary<TKey, TValue> dictionary;
                public int index;
                public int version;
                public TValue currentValue;

                public Enumerator(ExposedDictionary<TKey, TValue> dictionary)
                {
                    this.dictionary = dictionary;
                    version = dictionary.version;
                    index = 0;
                    currentValue = default;
                }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    if (version != dictionary.version)
                    {
                        throw new InvalidOperationException();
                    }

                    while ((uint)index < (uint)dictionary.count)
                    {
                        if (dictionary.entries[index].hashCode >= 0)
                        {
                            currentValue = dictionary.entries[index].value;
                            index++;
                            return true;
                        }
                        index++;
                    }
                    index = dictionary.count + 1;
                    currentValue = default;
                    return false;
                }

                public TValue Current
                {
                    get { return currentValue; }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        if (index == 0 || index == dictionary.count + 1)
                        {
                            throw new InvalidOperationException();
                        }

                        return currentValue;
                    }
                }

                void IEnumerator.Reset()
                {
                    if (version != dictionary.version)
                    {
                        throw new InvalidOperationException();
                    }
                    index = 0;
                    currentValue = default;
                }
            }
        }
    }
    // how dare thee for not making this public!!
    public static class HashHelpers
    {
        // Table of prime numbers to use as hash table sizes. 
        // A typical resize algorithm would pick the smallest prime number in this array
        // that is larger than twice the previous capacity. 
        // Suppose our Hashtable currently has capacity x and enough elements are added 
        // such that a resize needs to occur. Resizing first computes 2x then finds the 
        // first prime in the table greater than 2x, i.e. if primes are ordered 
        // p_1, p_2, ..., p_i, ..., it finds p_n such that p_n-1 < 2x < p_n. 
        // Doubling is important for preserving the asymptotic complexity of the 
        // hashtable operations such as add.  Having a prime guarantees that double 
        // hashing does not lead to infinite loops.  IE, your hash function will be 
        // h1(key) + i*h2(key), 0 <= i < size.  h2 and the size must be relatively prime.
        public static readonly int[] primes =
        {
            3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
            1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
            17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
            187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
            1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
        };

        // Used by Hashtable and Dictionary's SeralizationInfo .ctor's to store the SeralizationInfo
        // object until OnDeserialization is called.
        public static ConditionalWeakTable<object, SerializationInfo> s_SerializationInfoTable;

        public static ConditionalWeakTable<object, SerializationInfo> SerializationInfoTable
        {
            get
            {
                if (s_SerializationInfoTable is null)
                {
                    ConditionalWeakTable<object, SerializationInfo> newTable = new ConditionalWeakTable<object, SerializationInfo>();
                    Interlocked.CompareExchange(ref s_SerializationInfoTable, newTable, null);
                }

                return s_SerializationInfoTable;
            }
        }

        public static bool IsPrime(int candidate)
        {
            if ((candidate & 1) != 0)
            {
                int limit = (int)Math.Sqrt(candidate);
                for (int divisor = 3; divisor <= limit; divisor += 2)
                {
                    if (candidate % divisor == 0)
                        return false;
                }
                return true;
            }
            return candidate == 2;
        }

        public static int GetPrime(int min)
        {
            if (min < 0)
                throw new ArgumentException("min");

            for (int i = 0; i < primes.Length; i++)
            {
                int prime = primes[i];
                if (prime >= min) return prime;
            }

            //outside of our predefined table. 
            //compute the hard way. 
            for (int i = min | 1; i < int.MaxValue; i += 2)
            {
                if (IsPrime(i) && (i - 1) % 101 != 0)
                    return i;
            }
            return min;
        }

        public static int GetMinPrime() => primes[0];

        // Returns size of hashtable to grow to.
        public static int ExpandPrime(int oldSize)
        {
            int newSize = 2 * oldSize;

            // Allow the hashtables to grow to maximum possible size (~2G elements) before encoutering capacity overflow.
            // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
            if ((uint)newSize > MaxPrimeArrayLength && MaxPrimeArrayLength > oldSize)
            {
                return MaxPrimeArrayLength;
            }

            return GetPrime(newSize);
        }
        /// <summary>Returns approximate reciprocal of the divisor: ceil(2**64 / divisor).</summary>
        /// <remarks>This should only be used on 64-bit.</remarks>
        public static ulong GetFastModMultiplier(uint divisor) =>
            ulong.MaxValue / divisor + 1;

        /// <summary>Performs a mod operation using the multiplier pre-computed with <see cref="GetFastModMultiplier"/>.</summary>
        /// <remarks>This should only be used on 64-bit.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint FastMod(uint value, uint divisor, ulong multiplier)
        {
            // We use modified Daniel Lemire's fastmod algorithm (https://github.com/dotnet/runtime/pull/406),
            // which allows to avoid the long multiplication if the divisor is less than 2**31.
            Debug.Assert(divisor <= int.MaxValue);

            // This is equivalent of (uint)Math.BigMul(multiplier * value, divisor, out _). This version
            // is faster than BigMul currently because we only need the high bits.
            uint highbits = (uint)(((((multiplier * value) >> 32) + 1) * divisor) >> 32);

            Debug.Assert(highbits == value % divisor);
            return highbits;
        }

        // This is the maximum prime smaller than Array.MaxArrayLength
        public const int MaxPrimeArrayLength = 0x7FEFFFFD;
    }
    /// <summary>
    /// Represents a thread-safe collection of keys and values.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <remarks>
    /// All public and protected members of <see cref="ExposedConcurrentDictionary{TKey,TValue}"/> are thread-safe and may be used
    /// concurrently from multiple threads.
    /// </remarks>

    //[DebuggerTypeProxy(typeof(Mscorlib_DictionaryDebugView<,>))]
    [DebuggerDisplay("Count = {Count}")]
    public class ExposedConcurrentDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary
    {
        public volatile Node[] m_buckets; // A singly-linked list for each bucket.

        public object[] m_locks; // A set of locks, each guarding a section of the table.

        public volatile int[] m_countPerLock; // The number of elements guarded by each lock.
        public IEqualityComparer<TKey> m_comparer; // Key equality comparer

        public KeyValuePair<TKey, TValue>[] m_serializationArray; // Used for custom serialization

        public int m_serializationConcurrencyLevel; // used to save the concurrency level in serialization

        public int m_serializationCapacity; // used to save the capacity in serialization

        // The default concurrency level is DEFAULT_CONCURRENCY_MULTIPLIER * #CPUs. The higher the
        // DEFAULT_CONCURRENCY_MULTIPLIER, the more concurrent writes can take place without interference
        // and blocking, but also the more expensive operations that require all locks become (e.g. table
        // resizing, ToArray, Count, etc). According to brief benchmarks that we ran, 4 seems like a good
        // compromise.
        public const int DEFAULT_CONCURRENCY_MULTIPLIER = 4;

        // The default capacity, i.e. the initial # of buckets. When choosing this value, we are making
        // a trade-off between the size of a very small dictionary, and the number of resizes when
        // constructing a large dictionary. Also, the capacity should not be divisible by a small prime.
        public const int DEFAULT_CAPACITY = 31;

        /// <summary>
        /// Initializes a new instance of the <see
        /// cref="ExposedConcurrentDictionary{TKey,TValue}"/>
        /// class that is empty, has the default concurrency level, has the default initial capacity, and
        /// uses the default comparer for the key type.
        /// </summary>
        public ExposedConcurrentDictionary() : this(DefaultConcurrencyLevel, DEFAULT_CAPACITY)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see
        /// cref="ExposedConcurrentDictionary{TKey,TValue}"/>
        /// class that is empty, has the specified concurrency level and capacity, and uses the default
        /// comparer for the key type.
        /// </summary>
        /// <param name="concurrencyLevel">The estimated number of threads that will update the
        /// <see cref="ExposedConcurrentDictionary{TKey,TValue}"/> concurrently.</param>
        /// <param name="capacity">The initial number of elements that the <see
        /// cref="ExposedConcurrentDictionary{TKey,TValue}"/>
        /// can contain.</param>
        /// <exception cref="T:ArgumentOutOfRangeException"><paramref name="concurrencyLevel"/> is
        /// less than 1.</exception>
        /// <exception cref="T:ArgumentOutOfRangeException"> <paramref name="capacity"/> is less than
        /// 0.</exception>
        public ExposedConcurrentDictionary(int concurrencyLevel, int capacity) : this(concurrencyLevel, capacity, EqualityComparer<TKey>.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExposedConcurrentDictionary{TKey,TValue}"/>
        /// class that contains elements copied from the specified <see
        /// cref="T:System.Collections.IEnumerable{KeyValuePair{TKey,TValue}}"/>, has the default concurrency
        /// level, has the default initial capacity, and uses the default comparer for the key type.
        /// </summary>
        /// <param name="collection">The <see
        /// cref="T:System.Collections.IEnumerable{KeyValuePair{TKey,TValue}}"/> whose elements are copied to
        /// the new
        /// <see cref="ExposedConcurrentDictionary{TKey,TValue}"/>.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="collection"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="collection"/> contains one or more
        /// duplicate keys.</exception>
        public ExposedConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : this(collection, EqualityComparer<TKey>.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExposedConcurrentDictionary{TKey,TValue}"/>
        /// class that is empty, has the specified concurrency level and capacity, and uses the specified
        /// <see cref="T:System.Collections.Generic.IEqualityComparer{TKey}"/>.
        /// </summary>
        /// <param name="comparer">The <see cref="T:System.Collections.Generic.IEqualityComparer{TKey}"/>
        /// implementation to use when comparing keys.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="comparer"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        public ExposedConcurrentDictionary(IEqualityComparer<TKey> comparer) : this(DefaultConcurrencyLevel, DEFAULT_CAPACITY, comparer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExposedConcurrentDictionary{TKey,TValue}"/>
        /// class that contains elements copied from the specified <see
        /// cref="T:System.Collections.IEnumerable"/>, has the default concurrency level, has the default
        /// initial capacity, and uses the specified
        /// <see cref="T:System.Collections.Generic.IEqualityComparer{TKey}"/>.
        /// </summary>
        /// <param name="collection">The <see
        /// cref="T:System.Collections.IEnumerable{KeyValuePair{TKey,TValue}}"/> whose elements are copied to
        /// the new
        /// <see cref="ExposedConcurrentDictionary{TKey,TValue}"/>.</param>
        /// <param name="comparer">The <see cref="T:System.Collections.Generic.IEqualityComparer{TKey}"/>
        /// implementation to use when comparing keys.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="collection"/> is a null reference
        /// (Nothing in Visual Basic). -or-
        /// <paramref name="comparer"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public ExposedConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
            : this(DefaultConcurrencyLevel, collection, comparer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExposedConcurrentDictionary{TKey,TValue}"/> 
        /// class that contains elements copied from the specified <see cref="T:System.Collections.IEnumerable"/>, 
        /// has the specified concurrency level, has the specified initial capacity, and uses the specified 
        /// <see cref="T:System.Collections.Generic.IEqualityComparer{TKey}"/>.
        /// </summary>
        /// <param name="concurrencyLevel">The estimated number of threads that will update the 
        /// <see cref="ExposedConcurrentDictionary{TKey,TValue}"/> concurrently.</param>
        /// <param name="collection">The <see cref="T:System.Collections.IEnumerable{KeyValuePair{TKey,TValue}}"/> whose elements are copied to the new 
        /// <see cref="ExposedConcurrentDictionary{TKey,TValue}"/>.</param>
        /// <param name="comparer">The <see cref="T:System.Collections.Generic.IEqualityComparer{TKey}"/> implementation to use 
        /// when comparing keys.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="collection"/> is a null reference (Nothing in Visual Basic).
        /// -or-
        /// <paramref name="comparer"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="T:ArgumentOutOfRangeException">
        /// <paramref name="concurrencyLevel"/> is less than 1.
        /// </exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="collection"/> contains one or more duplicate keys.</exception>
        public ExposedConcurrentDictionary(
            int concurrencyLevel, IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
            : this(concurrencyLevel, DEFAULT_CAPACITY, comparer)
        {
            if (collection is null) throw new ArgumentNullException("collection");
            if (comparer is null) throw new ArgumentNullException("comparer");

            InitializeFromCollection(collection);
        }

        public void InitializeFromCollection(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            foreach (KeyValuePair<TKey, TValue> pair in collection)
            {
                if (pair.Key is null) throw new ArgumentNullException("key");

                if (!TryAddpublic(pair.Key, pair.Value, false, false, out TValue dummy))
                {
                    throw new ArgumentException("Contains duplicate keys", "collection");
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExposedConcurrentDictionary{TKey,TValue}"/>
        /// class that is empty, has the specified concurrency level, has the specified initial capacity, and
        /// uses the specified <see cref="T:System.Collections.Generic.IEqualityComparer{TKey}"/>.
        /// </summary>
        /// <param name="concurrencyLevel">The estimated number of threads that will update the
        /// <see cref="ExposedConcurrentDictionary{TKey,TValue}"/> concurrently.</param>
        /// <param name="capacity">The initial number of elements that the <see
        /// cref="ExposedConcurrentDictionary{TKey,TValue}"/>
        /// can contain.</param>
        /// <param name="comparer">The <see cref="T:System.Collections.Generic.IEqualityComparer{TKey}"/>
        /// implementation to use when comparing keys.</param>
        /// <exception cref="T:ArgumentOutOfRangeException">
        /// <paramref name="concurrencyLevel"/> is less than 1. -or-
        /// <paramref name="capacity"/> is less than 0.
        /// </exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="comparer"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        public ExposedConcurrentDictionary(int concurrencyLevel, int capacity, IEqualityComparer<TKey> comparer)
        {
            if (concurrencyLevel < 1)
            {
                throw new ArgumentOutOfRangeException("concurrencyLevel", "Concurrency level must be positive");
            }
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity", "Capacity must not be negative");
            }

            // The capacity should be at least as large as the concurrency level. Otherwise, we would have locks that don't guard
            // any buckets.
            if (capacity < concurrencyLevel)
            {
                capacity = concurrencyLevel;
            }

            m_locks = new object[concurrencyLevel];
            for (int i = 0; i < m_locks.Length; i++)
            {
                m_locks[i] = new object();
            }

            m_countPerLock = new int[m_locks.Length];
            m_buckets = new Node[capacity];
            m_comparer = comparer ?? throw new ArgumentNullException("comparer");
        }


        /// <summary>
        /// Attempts to add the specified key and value to the <see cref="ExposedConcurrentDictionary{TKey,
        /// TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add. The value can be a null reference (Nothing
        /// in Visual Basic) for reference types.</param>
        /// <returns>true if the key/value pair was added to the <see cref="ExposedConcurrentDictionary{TKey,
        /// TValue}"/>
        /// successfully; otherwise, false.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.OverflowException">The <see cref="ExposedConcurrentDictionary{TKey, TValue}"/>
        /// contains too many elements.</exception>
        public bool TryAdd(TKey key, TValue value)
        {
            if (key is null) throw new ArgumentNullException("key");
            return TryAddpublic(key, value, false, true, out _);
        }

        /// <summary>
        /// Determines whether the <see cref="ExposedConcurrentDictionary{TKey, TValue}"/> contains the specified
        /// key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="ExposedConcurrentDictionary{TKey,
        /// TValue}"/>.</param>
        /// <returns>true if the <see cref="ExposedConcurrentDictionary{TKey, TValue}"/> contains an element with
        /// the specified key; otherwise, false.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        public bool ContainsKey(TKey key)
        {
            if (key is null) throw new ArgumentNullException("key");

            return TryGetValue(key, out _);
        }

        /// <summary>
        /// Attempts to remove and return the value with the specified key from the
        /// <see cref="ExposedConcurrentDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove and return.</param>
        /// <param name="value">When this method returns, <paramref name="value"/> contains the object removed from the
        /// <see cref="ExposedConcurrentDictionary{TKey,TValue}"/> or the default value of <typeparamref
        /// name="TValue"/>
        /// if the operation failed.</param>
        /// <returns>true if an object was removed successfully; otherwise, false.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        public bool TryRemove(TKey key, out TValue value)
        {
            if (key is null) throw new ArgumentNullException("key");

            return TryRemovepublic(key, out value, false, default);
        }

        /// <summary>
        /// Removes the specified key from the dictionary if it exists and returns its associated value.
        /// If matchValue flag is set, the key will be removed only if is associated with a particular
        /// value.
        /// </summary>
        /// <param name="key">The key to search for and remove if it exists.</param>
        /// <param name="value">The variable into which the removed value, if found, is stored.</param>
        /// <param name="matchValue">Whether removal of the key is conditional on its value.</param>
        /// <param name="oldValue">The conditional value to compare against if <paramref name="matchValue"/> is true</param>
        /// <returns></returns>
        public bool TryRemovepublic(TKey key, out TValue value, bool matchValue, TValue oldValue)
        {
            while (true)
            {
                Node[] buckets = m_buckets;

                GetBucketAndLockNo(m_comparer.GetHashCode(key), out int bucketNo, out int lockNo, buckets.Length);

                lock (m_locks[lockNo])
                {
                    // If the table just got resized, we may not be holding the right lock, and must retry.
                    // This should be a rare occurence.
                    if (buckets != m_buckets)
                    {
                        continue;
                    }

                    Node prev = null;
                    for (Node curr = m_buckets[bucketNo]; curr is not null; curr = curr.m_next)
                    {
                        Assert((prev is null && curr == m_buckets[bucketNo]) || prev.m_next == curr);

                        if (m_comparer.Equals(curr.m_key, key))
                        {
                            if (matchValue)
                            {
                                bool valuesMatch = EqualityComparer<TValue>.Default.Equals(oldValue, curr.m_value);
                                if (!valuesMatch)
                                {
                                    value = default;
                                    return false;
                                }
                            }

                            if (prev is null)
                            {
                                m_buckets[bucketNo] = curr.m_next;
                            }
                            else
                            {
                                prev.m_next = curr.m_next;
                            }

                            value = curr.m_value;
                            m_countPerLock[lockNo]--;
                            return true;
                        }
                        prev = curr;
                    }
                }

                value = default;
                return false;
            }
        }

        /// <summary>
        /// Attempts to get the value associated with the specified key from the <see
        /// cref="ExposedConcurrentDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, <paramref name="value"/> contains the object from
        /// the
        /// <see cref="ExposedConcurrentDictionary{TKey,TValue}"/> with the spedified key or the default value of
        /// <typeparamref name="TValue"/>, if the operation failed.</param>
        /// <returns>true if the key was found in the <see cref="ExposedConcurrentDictionary{TKey,TValue}"/>;
        /// otherwise, false.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (key is null) throw new ArgumentNullException("key");

            // We must capture the m_buckets field in a local variable. It is set to a new table on each table resize.
            Node[] buckets = m_buckets;
            GetBucketAndLockNo(m_comparer.GetHashCode(key), out int bucketNo, out _, buckets.Length);

            // We can get away w/out a lock here.
            Node n = buckets[bucketNo];

            // The memory barrier ensures that the load of the fields of 'n' doesn’t move before the load from buckets[i].
            Thread.MemoryBarrier();
            while (n is not null)
            {
                if (m_comparer.Equals(n.m_key, key))
                {
                    value = n.m_value;
                    return true;
                }
                n = n.m_next;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Compares the existing value for the specified key with a specified value, and if they’re equal,
        /// updates the key with a third value.
        /// </summary>
        /// <param name="key">The key whose value is compared with <paramref name="comparisonValue"/> and
        /// possibly replaced.</param>
        /// <param name="newValue">The value that replaces the value of the element with <paramref
        /// name="key"/> if the comparison results in equality.</param>
        /// <param name="comparisonValue">The value that is compared to the value of the element with
        /// <paramref name="key"/>.</param>
        /// <returns>true if the value with <paramref name="key"/> was equal to <paramref
        /// name="comparisonValue"/> and replaced with <paramref name="newValue"/>; otherwise,
        /// false.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null
        /// reference.</exception>
        public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
        {
            if (key is null) throw new ArgumentNullException("key");

            int hashcode = m_comparer.GetHashCode(key);
            IEqualityComparer<TValue> valueComparer = EqualityComparer<TValue>.Default;

            while (true)
            {
                Node[] buckets = m_buckets;
                GetBucketAndLockNo(hashcode, out int bucketNo, out int lockNo, buckets.Length);

                lock (m_locks[lockNo])
                {
                    // If the table just got resized, we may not be holding the right lock, and must retry.
                    // This should be a rare occurence.
                    if (buckets != m_buckets)
                    {
                        continue;
                    }

                    // Try to find this key in the bucket
                    Node prev = null;
                    for (Node node = buckets[bucketNo]; node is not null; node = node.m_next)
                    {
                        Assert((prev is null && node == m_buckets[bucketNo]) || prev.m_next == node);
                        if (m_comparer.Equals(node.m_key, key))
                        {
                            if (valueComparer.Equals(node.m_value, comparisonValue))
                            {
                                // Replace the old node with a new node. Unfortunately, we cannot simply
                                // change node.m_value in place. We don't know the size of TValue, so
                                // its writes may not be atomic.
                                Node newNode = new Node(node.m_key, newValue, hashcode, node.m_next);

                                if (prev is null)
                                {
                                    buckets[bucketNo] = newNode;
                                }
                                else
                                {
                                    prev.m_next = newNode;
                                }

                                return true;
                            }

                            return false;
                        }

                        prev = node;
                    }

                    //didn't find the key
                    return false;
                }
            }
        }

        /// <summary>
        /// Removes all keys and values from the <see cref="ExposedConcurrentDictionary{TKey,TValue}"/>.
        /// </summary>
        public void Clear()
        {
            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);

                m_buckets = new Node[DEFAULT_CAPACITY];
                Array.Clear(m_countPerLock, 0, m_countPerLock.Length);
            }
            finally
            {
                ReleaseLocks(0, locksAcquired);
            }
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection"/> to an array of
        /// type <see cref="T:System.Collections.Generic.KeyValuePair{TKey,TValue}"/>, starting at the
        /// specified array index.
        /// </summary>
        /// <param name="array">The one-dimensional array of type <see
        /// cref="T:System.Collections.Generic.KeyValuePair{TKey,TValue}"/>
        /// that is the destination of the <see
        /// cref="T:System.Collections.Generic.KeyValuePair{TKey,TValue}"/> elements copied from the <see
        /// cref="T:System.Collections.ICollection"/>. The array must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying
        /// begins.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="array"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:ArgumentOutOfRangeException"><paramref name="index"/> is less than
        /// 0.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="index"/> is equal to or greater than
        /// the length of the <paramref name="array"/>. -or- The number of elements in the source <see
        /// cref="T:System.Collections.ICollection"/>
        /// is greater than the available space from <paramref name="index"/> to the end of the destination
        /// <paramref name="array"/>.</exception>
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            if (array is null) throw new ArgumentNullException("array");
            if (index < 0) throw new ArgumentOutOfRangeException("index", "Index is negative");

            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);

                int count = 0;

                for (int i = 0; i < m_locks.Length; i++)
                {
                    count += m_countPerLock[i];
                }

                if (array.Length - count < index || count < 0) //"count" itself or "count + index" can overflow
                {
                    throw new ArgumentException("Array has a lesser length than the dictionary", "array");
                }

                CopyToPairs(array, index);
            }
            finally
            {
                ReleaseLocks(0, locksAcquired);
            }
        }

        /// <summary>
        /// Copies the key and value pairs stored in the <see cref="ExposedConcurrentDictionary{TKey,TValue}"/> to a
        /// new array.
        /// </summary>
        /// <returns>A new array containing a snapshot of key and value pairs copied from the <see
        /// cref="ExposedConcurrentDictionary{TKey,TValue}"/>.</returns>
        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);
                int count = 0;
                checked
                {
                    for (int i = 0; i < m_locks.Length; i++)
                    {
                        count += m_countPerLock[i];
                    }
                }

                KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[count];

                CopyToPairs(array, 0);
                return array;
            }
            finally
            {
                ReleaseLocks(0, locksAcquired);
            }
        }

        /// <summary>
        /// Copy dictionary contents to an array - shared implementation between ToArray and CopyTo.
        /// 
        /// Important: the caller must hold all locks in m_locks before calling CopyToPairs.
        /// </summary>
        public void CopyToPairs(KeyValuePair<TKey, TValue>[] array, int index)
        {
            Node[] buckets = m_buckets;
            for (int i = 0; i < buckets.Length; i++)
            {
                for (Node current = buckets[i]; current is not null; current = current.m_next)
                {
                    array[index] = new KeyValuePair<TKey, TValue>(current.m_key, current.m_value);
                    index++; //this should never flow, CopyToPairs is only called when there's no overflow risk
                }
            }
        }

        /// <summary>
        /// Copy dictionary contents to an array - shared implementation between ToArray and CopyTo.
        /// 
        /// Important: the caller must hold all locks in m_locks before calling CopyToEntries.
        /// </summary>
        public void CopyToEntries(DictionaryEntry[] array, int index)
        {
            Node[] buckets = m_buckets;
            for (int i = 0; i < buckets.Length; i++)
            {
                for (Node current = buckets[i]; current is not null; current = current.m_next)
                {
                    array[index] = new DictionaryEntry(current.m_key, current.m_value);
                    index++; //this should never flow, CopyToEntries is only called when there's no overflow risk
                }
            }
        }

        /// <summary>
        /// Copy dictionary contents to an array - shared implementation between ToArray and CopyTo.
        /// 
        /// Important: the caller must hold all locks in m_locks before calling CopyToObjects.
        /// </summary>
        public void CopyToObjects(object[] array, int index)
        {
            Node[] buckets = m_buckets;
            for (int i = 0; i < buckets.Length; i++)
            {
                for (Node current = buckets[i]; current is not null; current = current.m_next)
                {
                    array[index] = new KeyValuePair<TKey, TValue>(current.m_key, current.m_value);
                    index++; //this should never flow, CopyToObjects is only called when there's no overflow risk
                }
            }
        }

        /// <summary>Returns an enumerator that iterates through the <see
        /// cref="ExposedConcurrentDictionary{TKey,TValue}"/>.</summary>
        /// <returns>An enumerator for the <see cref="ExposedConcurrentDictionary{TKey,TValue}"/>.</returns>
        /// <remarks>
        /// The enumerator returned from the dictionary is safe to use concurrently with
        /// reads and writes to the dictionary, however it does not represent a moment-in-time snapshot
        /// of the dictionary.  The contents exposed through the enumerator may contain modifications
        /// made to the dictionary after <see cref="GetEnumerator"/> was called.
        /// </remarks>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            Node[] buckets = m_buckets;

            for (int i = 0; i < buckets.Length; i++)
            {
                Node current = buckets[i];

                // The memory barrier ensures that the load of the fields of 'current' doesn’t move before the load from buckets[i].
                Thread.MemoryBarrier();
                while (current is not null)
                {
                    yield return new KeyValuePair<TKey, TValue>(current.m_key, current.m_value);
                    current = current.m_next;
                }
            }
        }

        /// <summary>
        /// Shared public implementation for inserts and updates.
        /// If key exists, we always return false; and if updateIfExists == true we force update with value;
        /// If key doesn't exist, we always add value and return true;
        /// </summary>
        public bool TryAddpublic(TKey key, TValue value, bool updateIfExists, bool acquireLock, out TValue resultingValue)
        {
            int hashcode = m_comparer.GetHashCode(key);

            while (true)
            {
                Node[] buckets = m_buckets;
                GetBucketAndLockNo(hashcode, out int bucketNo, out int lockNo, buckets.Length);

                bool resizeDesired = false;
                bool lockTaken = false;
                try
                {
                    if (acquireLock)
                        Monitor.Enter(m_locks[lockNo], ref lockTaken);

                    // If the table just got resized, we may not be holding the right lock, and must retry.
                    // This should be a rare occurence.
                    if (buckets != m_buckets)
                    {
                        continue;
                    }

                    // Try to find this key in the bucket
                    Node prev = null;
                    for (Node node = buckets[bucketNo]; node is not null; node = node.m_next)
                    {
                        Assert((prev is null && node == m_buckets[bucketNo]) || prev.m_next == node);
                        if (m_comparer.Equals(node.m_key, key))
                        {
                            // The key was found in the dictionary. If updates are allowed, update the value for that key.
                            // We need to create a new node for the update, in order to support TValue types that cannot
                            // be written atomically, since lock-free reads may be happening concurrently.
                            if (updateIfExists)
                            {
                                Node newNode = new Node(node.m_key, value, hashcode, node.m_next);
                                if (prev is null)
                                {
                                    buckets[bucketNo] = newNode;
                                }
                                else
                                {
                                    prev.m_next = newNode;
                                }
                                resultingValue = value;
                            }
                            else
                            {
                                resultingValue = node.m_value;
                            }
                            return false;
                        }
                        prev = node;
                    }

                    // The key was not found in the bucket. Insert the key-value pair.
                    buckets[bucketNo] = new Node(key, value, hashcode, buckets[bucketNo]);
                    checked
                    {
                        m_countPerLock[lockNo]++;
                    }

                    //
                    // If this lock has element / bucket ratio greater than 1, resize the entire table.
                    // Note: the formula is chosen to avoid overflow, but has a small inaccuracy due to
                    // rounding.
                    //
                    if (m_countPerLock[lockNo] > buckets.Length / m_locks.Length)
                    {
                        resizeDesired = true;
                    }
                }
                finally
                {
                    if (lockTaken)
                        Monitor.Exit(m_locks[lockNo]);
                }

                //
                // The fact that we got here means that we just performed an insertion. If necessary, we will grow the table.
                //
                // Concurrency notes:
                // - Notice that we are not holding any locks at when calling GrowTable. This is necessary to prevent deadlocks.
                // - As a result, it is possible that GrowTable will be called unnecessarily. But, GrowTable will obtain lock 0
                //   and then verify that the table we passed to it as the argument is still the current table.
                //
                if (resizeDesired)
                {
                    GrowTable(buckets);
                }

                resultingValue = value;
                return true;
            }
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <value>The value associated with the specified key. If the specified key is not found, a get
        /// operation throws a
        /// <see cref="T:Sytem.Collections.Generic.KeyNotFoundException"/>, and a set operation creates a new
        /// element with the specified key.</value>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.Collections.Generic.KeyNotFoundException">The property is retrieved and
        /// <paramref name="key"/>
        /// does not exist in the collection.</exception>
        public TValue this[TKey key]
        {
            get
            {
                if (!TryGetValue(key, out TValue value))
                {
                    throw new KeyNotFoundException();
                }
                return value;
            }
            set
            {
                if (key is null) throw new ArgumentNullException("key");
                TryAddpublic(key, value, true, true, out _);
            }
        }

        /// <summary>
        /// Gets the number of key/value pairs contained in the <see
        /// cref="ExposedConcurrentDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <exception cref="T:System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <value>The number of key/value paris contained in the <see
        /// cref="ExposedConcurrentDictionary{TKey,TValue}"/>.</value>
        /// <remarks>Count has snapshot semantics and represents the number of items in the <see
        /// cref="ExposedConcurrentDictionary{TKey,TValue}"/>
        /// at the moment when Count was accessed.</remarks>
        public int Count
        {
            get
            {
                int count = 0;

                int acquiredLocks = 0;
                try
                {
                    // Acquire all locks
                    AcquireAllLocks(ref acquiredLocks);

                    // Compute the count, we allow overflow
                    for (int i = 0; i < m_countPerLock.Length; i++)
                    {
                        count += m_countPerLock[i];
                    }
                }
                finally
                {
                    // Release locks that have been acquired earlier
                    ReleaseLocks(0, acquiredLocks);
                }

                return count;
            }
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ExposedConcurrentDictionary{TKey,TValue}"/> 
        /// if the key does not already exist.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="valueFactory">The function used to generate a value for the key</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="valueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>The value for the key.  This will be either the existing value for the key if the
        /// key is already in the dictionary, or the new value for the key as returned by valueFactory
        /// if the key was not in the dictionary.</returns>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            if (key is null) throw new ArgumentNullException("key");
            if (valueFactory is null) throw new ArgumentNullException("valueFactory");

            if (TryGetValue(key, out TValue resultingValue))
            {
                return resultingValue;
            }
            TryAddpublic(key, valueFactory(key), false, true, out resultingValue);
            return resultingValue;
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ExposedConcurrentDictionary{TKey,TValue}"/> 
        /// if the key does not already exist.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">the value to be added, if the key does not already exist</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>The value for the key.  This will be either the existing value for the key if the 
        /// key is already in the dictionary, or the new value if the key was not in the dictionary.</returns>
        public TValue GetOrAdd(TKey key, TValue value)
        {
            if (key is null) throw new ArgumentNullException("key");

            TryAddpublic(key, value, false, true, out TValue resultingValue);
            return resultingValue;
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ExposedConcurrentDictionary{TKey,TValue}"/> if the key does not already 
        /// exist, or updates a key/value pair in the <see cref="ExposedConcurrentDictionary{TKey,TValue}"/> if the key 
        /// already exists.
        /// </summary>
        /// <param name="key">The key to be added or whose value should be updated</param>
        /// <param name="addValueFactory">The function used to generate a value for an absent key</param>
        /// <param name="updateValueFactory">The function used to generate a new value for an existing key
        /// based on the key's existing value</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="addValueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="updateValueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>The new value for the key.  This will be either be the result of addValueFactory (if the key was 
        /// absent) or the result of updateValueFactory (if the key was present).</returns>
        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            if (key is null) throw new ArgumentNullException("key");
            if (addValueFactory is null) throw new ArgumentNullException("addValueFactory");
            if (updateValueFactory is null) throw new ArgumentNullException("updateValueFactory");

            TValue newValue;
            while (true)
            {
                if (TryGetValue(key, out TValue oldValue))
                //key exists, try to update
                {
                    newValue = updateValueFactory(key, oldValue);
                    if (TryUpdate(key, newValue, oldValue))
                    {
                        return newValue;
                    }
                }
                else //try add
                {
                    newValue = addValueFactory(key);
                    if (TryAddpublic(key, newValue, false, true, out TValue resultingValue))
                    {
                        return resultingValue;
                    }
                }
            }
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ExposedConcurrentDictionary{TKey,TValue}"/> if the key does not already 
        /// exist, or updates a key/value pair in the <see cref="ExposedConcurrentDictionary{TKey,TValue}"/> if the key 
        /// already exists.
        /// </summary>
        /// <param name="key">The key to be added or whose value should be updated</param>
        /// <param name="addValue">The value to be added for an absent key</param>
        /// <param name="updateValueFactory">The function used to generate a new value for an existing key based on 
        /// the key's existing value</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="updateValueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>The new value for the key.  This will be either be the result of addValueFactory (if the key was 
        /// absent) or the result of updateValueFactory (if the key was present).</returns>
        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            if (key is null) throw new ArgumentNullException("key");
            if (updateValueFactory is null) throw new ArgumentNullException("updateValueFactory");
            TValue newValue;
            while (true)
            {
                if (TryGetValue(key, out TValue oldValue))
                //key exists, try to update
                {
                    newValue = updateValueFactory(key, oldValue);
                    if (TryUpdate(key, newValue, oldValue))
                    {
                        return newValue;
                    }
                }
                else //try add
                {
                    if (TryAddpublic(key, addValue, false, true, out TValue resultingValue))
                    {
                        return resultingValue;
                    }
                }
            }
        }


        /// <summary>
        /// Gets a value that indicates whether the <see cref="ExposedConcurrentDictionary{TKey,TValue}"/> is empty.
        /// </summary>
        /// <value>true if the <see cref="ExposedConcurrentDictionary{TKey,TValue}"/> is empty; otherwise,
        /// false.</value>
        public bool IsEmpty
        {
            get
            {
                int acquiredLocks = 0;
                try
                {
                    // Acquire all locks
                    AcquireAllLocks(ref acquiredLocks);

                    for (int i = 0; i < m_countPerLock.Length; i++)
                    {
                        if (m_countPerLock[i] != 0)
                        {
                            return false;
                        }
                    }
                }
                finally
                {
                    // Release locks that have been acquired earlier
                    ReleaseLocks(0, acquiredLocks);
                }

                return true;
            }
        }

        #region IDictionary<TKey,TValue> members

        /// <summary>
        /// Adds the specified key and value to the <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <exception cref="T:System.ArgumentException">
        /// An element with the same key already exists in the <see
        /// cref="ExposedConcurrentDictionary{TKey,TValue}"/>.</exception>
        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            if (!TryAdd(key, value))
            {
                throw new ArgumentException("Key already exists", "key");
            }
        }

        /// <summary>
        /// Removes the element with the specified key from the <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>true if the element is successfully remove; otherwise false. This method also returns
        /// false if
        /// <paramref name="key"/> was not found in the original <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        bool IDictionary<TKey, TValue>.Remove(TKey key) => TryRemove(key, out _);

        /// <summary>
        /// Gets a collection containing the keys in the <see
        /// cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>.
        /// </summary>
        /// <value>An <see cref="T:System.Collections.Generic.ICollection{TKey}"/> containing the keys in the
        /// <see cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>.</value>
        public ICollection<TKey> Keys
        {
            get { return GetKeys(); }
        }

        /// <summary>
        /// Gets a collection containing the values in the <see
        /// cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>.
        /// </summary>
        /// <value>An <see cref="T:System.Collections.Generic.ICollection{TValue}"/> containing the values in
        /// the
        /// <see cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>.</value>
        public ICollection<TValue> Values
        {
            get { return GetValues(); }
        }

        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> Members

        /// <summary>
        /// Adds the specified value to the <see cref="T:System.Collections.Generic.ICollection{TValue}"/>
        /// with the specified key.
        /// </summary>
        /// <param name="keyValuePair">The <see cref="T:System.Collections.Generic.KeyValuePair{TKey,TValue}"/>
        /// structure representing the key and value to add to the <see
        /// cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="keyValuePair"/> of <paramref
        /// name="keyValuePair"/> is null.</exception>
        /// <exception cref="T:System.OverflowException">The <see
        /// cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>
        /// contains too many elements.</exception>
        /// <exception cref="T:System.ArgumentException">An element with the same key already exists in the
        /// <see cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/></exception>
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair) => ((IDictionary<TKey, TValue>)this).Add(keyValuePair.Key, keyValuePair.Value);

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.ICollection{TKey,TValue}"/>
        /// contains a specific key and value.
        /// </summary>
        /// <param name="keyValuePair">The <see cref="T:System.Collections.Generic.KeyValuePair{TKey,TValue}"/>
        /// structure to locate in the <see
        /// cref="T:System.Collections.Generic.ICollection{TValue}"/>.</param>
        /// <returns>true if the <paramref name="keyValuePair"/> is found in the <see
        /// cref="T:System.Collections.Generic.ICollection{TKey,TValue}"/>; otherwise, false.</returns>
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
        {
            if (!TryGetValue(keyValuePair.Key, out TValue value))
            {
                return false;
            }
            return EqualityComparer<TValue>.Default.Equals(value, keyValuePair.Value);
        }

        /// <summary>
        /// Gets a value indicating whether the dictionary is read-only.
        /// </summary>
        /// <value>true if the <see cref="T:System.Collections.Generic.ICollection{TKey,TValue}"/> is
        /// read-only; otherwise, false. For <see
        /// cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>, this property always returns
        /// false.</value>
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Removes a key and value from the dictionary.
        /// </summary>
        /// <param name="keyValuePair">The <see
        /// cref="T:System.Collections.Generic.KeyValuePair{TKey,TValue}"/>
        /// structure representing the key and value to remove from the <see
        /// cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>.</param>
        /// <returns>true if the key and value represented by <paramref name="keyValuePair"/> is successfully
        /// found and removed; otherwise, false.</returns>
        /// <exception cref="T:System.ArgumentNullException">The Key property of <paramref
        /// name="keyValuePair"/> is a null reference (Nothing in Visual Basic).</exception>
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
        {
            if (keyValuePair.Key is null) throw new ArgumentNullException("Item key is null.", "keyValuePair");

            return TryRemovepublic(keyValuePair.Key, out _, true, keyValuePair.Value);
        }

        #endregion

        #region IEnumerable Members

        /// <summary>Returns an enumerator that iterates through the <see
        /// cref="ExposedConcurrentDictionary{TKey,TValue}"/>.</summary>
        /// <returns>An enumerator for the <see cref="ExposedConcurrentDictionary{TKey,TValue}"/>.</returns>
        /// <remarks>
        /// The enumerator returned from the dictionary is safe to use concurrently with
        /// reads and writes to the dictionary, however it does not represent a moment-in-time snapshot
        /// of the dictionary.  The contents exposed through the enumerator may contain modifications
        /// made to the dictionary after <see cref="GetEnumerator"/> was called.
        /// </remarks>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region IDictionary Members

        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key">The object to use as the key.</param>
        /// <param name="value">The object to use as the value.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="key"/> is of a type that is not assignable to the key type <typeparamref
        /// name="TKey"/> of the <see cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>. -or-
        /// <paramref name="value"/> is of a type that is not assignable to <typeparamref name="TValue"/>,
        /// the type of values in the <see cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>.
        /// -or- A value with the same key already exists in the <see
        /// cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>.
        /// </exception>
        void IDictionary.Add(object key, object value)
        {
            if (key is null) throw new ArgumentNullException("key");
            if (key is not TKey) throw new ArgumentException("Type of key mismatches dictionary's key type", "key");

            TValue typedValue;
            try
            {
                typedValue = (TValue)value;
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException("Type of vlaue mismatches dictionary's value type", "value");
            }

            ((IDictionary<TKey, TValue>)this).Add((TKey)key, typedValue);
        }

        /// <summary>
        /// Gets whether the <see cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/> contains an
        /// element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>.</param>
        /// <returns>true if the <see cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/> contains
        /// an element with the specified key; otherwise, false.</returns>
        /// <exception cref="T:System.ArgumentNullException"> <paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        bool IDictionary.Contains(object key)
        {
            if (key is null) throw new ArgumentNullException("key");

            return key is TKey key1 && ContainsKey(key1);
        }

        /// <summary>Provides an <see cref="T:System.Collections.Generics.IDictionaryEnumerator"/> for the
        /// <see cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>.</summary>
        /// <returns>An <see cref="T:System.Collections.Generics.IDictionaryEnumerator"/> for the <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>.</returns>
        IDictionaryEnumerator IDictionary.GetEnumerator() => new DictionaryEnumerator(this);

        /// <summary>
        /// Gets a value indicating whether the <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/> has a fixed size.
        /// </summary>
        /// <value>true if the <see cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/> has a
        /// fixed size; otherwise, false. For <see
        /// cref="T:System.Collections.Generic.ConcurrentDictionary{TKey,TValue}"/>, this property always
        /// returns false.</value>
        bool IDictionary.IsFixedSize
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether the <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/> is read-only.
        /// </summary>
        /// <value>true if the <see cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/> is
        /// read-only; otherwise, false. For <see
        /// cref="T:System.Collections.Generic.ConcurrentDictionary{TKey,TValue}"/>, this property always
        /// returns false.</value>
        bool IDictionary.IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.ICollection"/> containing the keys of the <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <value>An <see cref="T:System.Collections.ICollection"/> containing the keys of the <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>.</value>
        ICollection IDictionary.Keys
        {
            get { return GetKeys(); }
        }

        /// <summary>
        /// Removes the element with the specified key from the <see
        /// cref="T:System.Collections.IDictionary"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        void IDictionary.Remove(object key)
        {
            if (key is null) throw new ArgumentNullException("key");

            if (key is TKey key1)
            {
                _ = TryRemove(key1, out _);
            }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.ICollection"/> containing the values in the <see
        /// cref="T:System.Collections.IDictionary"/>.
        /// </summary>
        /// <value>An <see cref="T:System.Collections.ICollection"/> containing the values in the <see
        /// cref="T:System.Collections.IDictionary"/>.</value>
        ICollection IDictionary.Values
        {
            get { return GetValues(); }
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <value>The value associated with the specified key, or a null reference (Nothing in Visual Basic)
        /// if <paramref name="key"/> is not in the dictionary or <paramref name="key"/> is of a type that is
        /// not assignable to the key type <typeparamref name="TKey"/> of the <see
        /// cref="T:System.Collections.Generic.ConcurrentDictionary{TKey,TValue}"/>.</value>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.ArgumentException">
        /// A value is being assigned, and <paramref name="key"/> is of a type that is not assignable to the
        /// key type <typeparamref name="TKey"/> of the <see
        /// cref="T:System.Collections.Generic.ConcurrentDictionary{TKey,TValue}"/>. -or- A value is being
        /// assigned, and <paramref name="key"/> is of a type that is not assignable to the value type
        /// <typeparamref name="TValue"/> of the <see
        /// cref="T:System.Collections.Generic.ConcurrentDictionary{TKey,TValue}"/>
        /// </exception>
        object IDictionary.this[object key]
        {
            get
            {
                if (key is null) throw new ArgumentNullException("key");

                if (key is TKey key1 && TryGetValue(key1, out TValue value))
                {
                    return value;
                }

                return null;
            }
            set
            {
                if (key is null) throw new ArgumentNullException("key");

                if (key is not TKey) throw new ArgumentException("Type of key mismatches dictionary's key type", "key");
                if (value is not TValue) throw new ArgumentException("Type of value mismatches dictionary's value type", "key");

                this[(TKey)key] = (TValue)value;
            }
        }

        #endregion

        #region ICollection Members

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.ICollection"/> to an array, starting
        /// at the specified array index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from
        /// the <see cref="T:System.Collections.ICollection"/>. The array must have zero-based
        /// indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying
        /// begins.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="array"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:ArgumentOutOfRangeException"><paramref name="index"/> is less than
        /// 0.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="index"/> is equal to or greater than
        /// the length of the <paramref name="array"/>. -or- The number of elements in the source <see
        /// cref="T:System.Collections.ICollection"/>
        /// is greater than the available space from <paramref name="index"/> to the end of the destination
        /// <paramref name="array"/>.</exception>
        void ICollection.CopyTo(Array array, int index)
        {
            if (array is null) throw new ArgumentNullException("array");
            if (index < 0) throw new ArgumentOutOfRangeException("index", "Index is negative");

            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);

                int count = 0;

                for (int i = 0; i < m_locks.Length; i++)
                {
                    count += m_countPerLock[i];
                }

                if (array.Length - count < index || count < 0) //"count" itself or "count + index" can overflow
                {
                    throw new ArgumentException("Array has a lesser length than the dictionary", "array");
                }

                // To be consistent with the behavior of ICollection.CopyTo() in Dictionary<TKey,TValue>,
                // we recognize three types of target arrays:
                //    - an array of KeyValuePair<TKey, TValue> structs
                //    - an array of DictionaryEntry structs
                //    - an array of objects

                if (array is KeyValuePair<TKey, TValue>[] pairs)
                {
                    CopyToPairs(pairs, index);
                    return;
                }

                if (array is DictionaryEntry[] entries)
                {
                    CopyToEntries(entries, index);
                    return;
                }

                if (array is object[] objects)
                {
                    CopyToObjects(objects, index);
                    return;
                }

                throw new ArgumentException("Array has an incorrect type.", "array");
            }
            finally
            {
                ReleaseLocks(0, locksAcquired);
            }
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection"/> is
        /// synchronized with the SyncRoot.
        /// </summary>
        /// <value>true if access to the <see cref="T:System.Collections.ICollection"/> is synchronized
        /// (thread safe); otherwise, false. For <see
        /// cref="T:System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>, this property always
        /// returns false.</value>
        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see
        /// cref="T:System.Collections.ICollection"/>. This property is not supported.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The SyncRoot property is not supported.</exception>
        object ICollection.SyncRoot
        {
            get { throw new NotSupportedException("Sync root is not supported"); }
        }

        #endregion

        /// <summary>
        /// Replaces the public table with a larger one. To prevent multiple threads from resizing the
        /// table as a result of races, the table of buckets that was deemed too small is passed in as
        /// an argument to GrowTable(). GrowTable() obtains a lock, and then checks whether the bucket
        /// table has been replaced in the meantime or not.
        /// </summary>
        /// <param name="buckets">Reference to the bucket table that was deemed too small.</param>
        public void GrowTable(Node[] buckets)
        {
            int locksAcquired = 0;
            try
            {
                // The thread that first obtains m_locks[0] will be the one doing the resize operation
                AcquireLocks(0, 1, ref locksAcquired);

                // Make sure nobody resized the table while we were waiting for lock 0:
                if (buckets != m_buckets)
                {
                    // We assume that since the table reference is different, it was already resized. If we ever
                    // decide to do table shrinking, or replace the table for other reasons, we will have to revisit
                    // this logic.
                    return;
                }

                // Compute the new table size. We find the smallest integer larger than twice the previous table size, and not divisible by
                // 2,3,5 or 7. We can consider a different table-sizing policy in the future.
                int newLength;
                try
                {
                    checked
                    {
                        // Double the size of the buckets table and add one, so that we have an odd integer.
                        newLength = buckets.Length * 2 + 1;

                        // Now, we only need to check odd integers, and find the first that is not divisible
                        // by 3, 5 or 7.
                        while (newLength % 3 == 0 || newLength % 5 == 0 || newLength % 7 == 0)
                        {
                            newLength += 2;
                        }

                        Assert(newLength % 2 != 0);
                    }
                }
                catch (OverflowException)
                {
                    // If we were to resize the table, its new size will not fit into a 32-bit signed int. Just return.
                    return;
                }

                Node[] newBuckets = new Node[newLength];
                int[] newCountPerLock = new int[m_locks.Length];

                // Now acquire all other locks for the table
                AcquireLocks(1, m_locks.Length, ref locksAcquired);

                // Copy all data into a new table, creating new nodes for all elements
                for (int i = 0; i < buckets.Length; i++)
                {
                    Node current = buckets[i];
                    while (current is not null)
                    {
                        Node next = current.m_next;
                        GetBucketAndLockNo(current.m_hashcode, out int newBucketNo, out int newLockNo, newBuckets.Length);

                        newBuckets[newBucketNo] = new Node(current.m_key, current.m_value, current.m_hashcode, newBuckets[newBucketNo]);

                        checked
                        {
                            newCountPerLock[newLockNo]++;
                        }

                        current = next;
                    }
                }

                // And finally adjust m_buckets and m_countPerLock to point to data for the new table
                m_buckets = newBuckets;
                m_countPerLock = newCountPerLock;
            }
            finally
            {
                // Release all locks that we took earlier
                ReleaseLocks(0, locksAcquired);
            }
        }

        /// <summary>
        /// Computes the bucket and lock number for a particular key. 
        /// </summary>
        public void GetBucketAndLockNo(
            int hashcode, out int bucketNo, out int lockNo, int bucketCount)
        {
            bucketNo = (hashcode & 0x7fffffff) % bucketCount;
            lockNo = bucketNo % m_locks.Length;

            Assert(bucketNo >= 0 && bucketNo < bucketCount);
            Assert(lockNo >= 0 && lockNo < m_locks.Length);
        }

        /// <summary>
        /// The number of concurrent writes for which to optimize by default.
        /// </summary>
        public static int DefaultConcurrencyLevel
        {
            get { return DEFAULT_CONCURRENCY_MULTIPLIER * Environment.ProcessorCount; }
        }

        /// <summary>
        /// Acquires all locks for this hash table, and increments locksAcquired by the number
        /// of locks that were successfully acquired. The locks are acquired in an increasing
        /// order.
        /// </summary>
        public void AcquireAllLocks(ref int locksAcquired)
        {
#if ETW_EVENTING
            if (CDSCollectionETWBCLProvider.Log.IsEnabled())
            {
                CDSCollectionETWBCLProvider.Log.ConcurrentDictionary_AcquiringAllLocks(m_buckets.Length);
            }
#endif

            AcquireLocks(0, m_locks.Length, ref locksAcquired);
            Assert(locksAcquired == m_locks.Length);
        }

        /// <summary>
        /// Acquires a contiguous range of locks for this hash table, and increments locksAcquired
        /// by the number of locks that were successfully acquired. The locks are acquired in an
        /// increasing order.
        /// </summary>
        public void AcquireLocks(int fromInclusive, int toExclusive, ref int locksAcquired)
        {
            Assert(fromInclusive <= toExclusive);

            for (int i = fromInclusive; i < toExclusive; i++)
            {
                bool lockTaken = false;
                try
                {
                    Monitor.Enter(m_locks[i], ref lockTaken);
                }
                finally
                {
                    if (lockTaken)
                    {
                        locksAcquired++;
                    }
                }
            }
        }

        /// <summary>
        /// Releases a contiguous range of locks.
        /// </summary>
        public void ReleaseLocks(int fromInclusive, int toExclusive)
        {
            Assert(fromInclusive <= toExclusive);

            for (int i = fromInclusive; i < toExclusive; i++)
            {
                Monitor.Exit(m_locks[i]);
            }
        }

        /// <summary>
        /// Gets a collection containing the keys in the dictionary.
        /// </summary>
        public ReadOnlyCollection<TKey> GetKeys()
        {
            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);
                List<TKey> keys = new List<TKey>();

                for (int i = 0; i < m_buckets.Length; i++)
                {
                    Node current = m_buckets[i];
                    while (current is not null)
                    {
                        keys.Add(current.m_key);
                        current = current.m_next;
                    }
                }

                return new ReadOnlyCollection<TKey>(keys);
            }
            finally
            {
                ReleaseLocks(0, locksAcquired);
            }
        }

        /// <summary>
        /// Gets a collection containing the values in the dictionary.
        /// </summary>
        public ReadOnlyCollection<TValue> GetValues()
        {
            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);
                List<TValue> values = new List<TValue>();

                for (int i = 0; i < m_buckets.Length; i++)
                {
                    Node current = m_buckets[i];
                    while (current is not null)
                    {
                        values.Add(current.m_value);
                        current = current.m_next;
                    }
                }

                return new ReadOnlyCollection<TValue>(values);
            }
            finally
            {
                ReleaseLocks(0, locksAcquired);
            }
        }

        /// <summary>
        /// A helper method for asserts.
        /// </summary>
        [Conditional("DEBUG")]
        public static void Assert(bool condition)
        {
            if (!condition)
            {
                throw new Exception("Assertion failed.");
            }
        }

        /// <summary>
        /// A node in a singly-linked list representing a particular hash table bucket.
        /// </summary>
        public class Node
        {
            public TKey m_key;
            public TValue m_value;
            public volatile Node m_next;
            public int m_hashcode;

            public Node(TKey key, TValue value, int hashcode, Node next)
            {
                m_key = key;
                m_value = value;
                m_next = next;
                m_hashcode = hashcode;
            }
        }

        /// <summary>
        /// A public class to represent enumeration over the dictionary that implements the 
        /// IDictionaryEnumerator interface.
        /// </summary>
        public class DictionaryEnumerator : IDictionaryEnumerator
        {
            readonly IEnumerator<KeyValuePair<TKey, TValue>> m_enumerator; // Enumerator over the dictionary.

            public DictionaryEnumerator(ExposedConcurrentDictionary<TKey, TValue> dictionary)
            {
                m_enumerator = dictionary.GetEnumerator();
            }

            public DictionaryEntry Entry
            {
                get { return new DictionaryEntry(m_enumerator.Current.Key, m_enumerator.Current.Value); }
            }

            public object Key
            {
                get { return m_enumerator.Current.Key; }
            }

            public object Value
            {
                get { return m_enumerator.Current.Value; }
            }

            public object Current
            {
                get { return Entry; }
            }

            public bool MoveNext() => m_enumerator.MoveNext();

            public void Reset() => m_enumerator.Reset();
        }

        /// <summary>
        /// Get the data array to be serialized
        /// </summary>
        [OnSerializing]
        public void OnSerializing(StreamingContext context)
        {
            // save the data into the serialization array to be saved
            m_serializationArray = ToArray();
            m_serializationConcurrencyLevel = m_locks.Length;
            m_serializationCapacity = m_buckets.Length;
        }

        /// <summary>
        /// Construct the dictionary from a previously seiralized one
        /// </summary>
        [OnDeserialized]
        public void OnDeserialized(StreamingContext context)
        {
            KeyValuePair<TKey, TValue>[] array = m_serializationArray;

            m_buckets = new Node[m_serializationCapacity];
            m_countPerLock = new int[m_serializationConcurrencyLevel];

            m_locks = new object[m_serializationConcurrencyLevel];
            for (int i = 0; i < m_locks.Length; i++)
            {
                m_locks[i] = new object();
            }

            InitializeFromCollection(array);
            m_serializationArray = null;
        }
    }
    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.

    /*=============================================================================
    **
    **
    ** Purpose: A circular-array implementation of a generic queue.
    **
    **
    =============================================================================*/

    // A simple ExposedQueue of generic objects.  Internally it is implemented as a
    // circular buffer, so Enqueue can be O(n).  Dequeue is O(1).
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    [TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class ExposedQueue<T> : ICollection,
        IReadOnlyCollection<T>
    {
        public T[] array;
        public int head; // The index from which to dequeue if the queue isn't empty.
        public int tail; // The index at which to enqueue if the queue isn't full.
        public int size; // Number of elements.
        public int _version;

        // Creates a queue with room for capacity objects. The default initial
        // capacity and grow factor are used.
        public ExposedQueue()
        {
            array = Array.Empty<T>();
        }

        // Creates a queue with room for capacity objects. The default grow factor
        // is used.
        public ExposedQueue(int capacity)
        {
            if (capacity < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException_NeedNonNegNum("capacity");
            array = new T[capacity];
        }

        // Fills a ExposedQueue with the elements of an ICollection.  Uses the enumerator
        // to get each of the elements.
        public ExposedQueue(IEnumerable<T> collection)
        {
            ArgumentNullException.ThrowIfNull(collection);

            array = collection.ToArray();
            size = array.Length;
            if (size != array.Length) tail = size;
        }

        public int Count => size;

        public bool IsSynchronized => false;

        object ICollection.SyncRoot => this;

        // Removes all Objects from the queue.
        public void Clear()
        {
            if (size != 0)
            {
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    if (head < tail)
                    {
                        Array.Clear(array, head, size);
                    }
                    else
                    {
                        Array.Clear(array, head, array.Length - head);
                        Array.Clear(array, 0, tail);
                    }
                }

                size = 0;
            }

            head = 0;
            tail = 0;
            _version++;
        }

        // CopyTo copies a collection into an Array, starting at a particular
        // index into the array.
        public void CopyTo(T[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);

            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
            }

            if (array.Length - arrayIndex < size)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
            }

            int numToCopy = size;
            if (numToCopy == 0) return;

            int firstPart = Math.Min(this.array.Length - head, numToCopy);
            Array.Copy(this.array, head, array, arrayIndex, firstPart);
            numToCopy -= firstPart;
            if (numToCopy > 0)
            {
                Array.Copy(this.array, 0, array, arrayIndex + this.array.Length - head, numToCopy);
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);

            if (array.Rank != 1)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
            }

            if (array.GetLowerBound(0) != 0)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NonZeroLowerBound);
            }

            int arrayLen = array.Length;
            if (index < 0 || index > arrayLen)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
            }

            if (arrayLen - index < size)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
            }

            int numToCopy = size;
            if (numToCopy == 0) return;

            try
            {
                int firstPart = this.array.Length - head < numToCopy ? this.array.Length - head : numToCopy;
                Array.Copy(this.array, head, array, index, firstPart);
                numToCopy -= firstPart;

                if (numToCopy > 0)
                {
                    Array.Copy(this.array, 0, array, index + this.array.Length - head, numToCopy);
                }
            }
            catch (ArrayTypeMismatchException)
            {
                ThrowHelper.ThrowArgumentException_Argument_InvalidArrayType();
            }
        }

        // Adds item to the tail of the queue.
        public void Enqueue(T item)
        {
            if (size == array.Length)
            {
                Grow(size + 1);
            }

            array[tail] = item;
            MoveNext(ref tail);
            size++;
            _version++;
        }
        public void EnqueueWithoutGrow(T item)
        {
            array[tail] = item;
            MoveNext(ref tail);
            size++;
            _version++;
        }
        // GetEnumerator returns an IEnumerator over this ExposedQueue.  This
        // Enumerator will support removing.
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <internalonly/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        // Removes the object at the head of the queue and returns it. If the queue
        // is empty, this method throws an
        // InvalidOperationException.
        public T Dequeue()
        {
            int head = this.head;
            T[] array = this.array;

            if (size == 0)
            {
                ThrowForEmptyExposedQueue();
            }

            T removed = array[head];
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                array[head] = default!;
            }
            MoveNext(ref this.head);
            size--;
            _version++;
            return removed;
        }

        public bool TryDequeue([MaybeNullWhen(false)] out T result)
        {
            int head = this.head;
            T[] array = this.array;

            if (size == 0)
            {
                result = default;
                return false;
            }

            result = array[head];
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                array[head] = default!;
            }
            MoveNext(ref this.head);
            size--;
            _version++;
            return true;
        }

        // Returns the object at the head of the queue. The object remains in the
        // queue. If the queue is empty, this method throws an
        // InvalidOperationException.
        public T Peek()
        {
            if (size == 0)
            {
                ThrowForEmptyExposedQueue();
            }

            return array[head];
        }

        public bool TryPeek([MaybeNullWhen(false)] out T result)
        {
            if (size == 0)
            {
                result = default;
                return false;
            }

            result = array[head];
            return true;
        }

        // Returns true if the queue contains at least one object equal to item.
        // Equality is determined using EqualityComparer<T>.Default.Equals().
        public bool Contains(T item)
        {
            if (size == 0)
            {
                return false;
            }

            if (head < tail)
            {
                return Array.IndexOf(array, item, head, size) >= 0;
            }

            // We've wrapped around. Check both partitions, the least recently enqueued first.
            return
                Array.IndexOf(array, item, head, array.Length - head) >= 0 ||
                Array.IndexOf(array, item, 0, tail) >= 0;
        }

        // Iterates over the objects in the queue, returning an array of the
        // objects in the ExposedQueue, or an empty array if the queue is empty.
        // The order of elements in the array is first in to last in, the same
        // order produced by successive calls to Dequeue.
        public T[] ToArray()
        {
            if (size == 0)
            {
                return Array.Empty<T>();
            }

            T[] arr = new T[size];

            if (head < tail)
            {
                Array.Copy(array, head, arr, 0, size);
            }
            else
            {
                Array.Copy(array, head, arr, 0, array.Length - head);
                Array.Copy(array, 0, arr, array.Length - head, tail);
            }

            return arr;
        }

        // PRIVATE Grows or shrinks the buffer to hold capacity objects. Capacity
        // must be >= _size.
        public void SetCapacity(int capacity)
        {
            T[] newarray = new T[capacity];
            if (size > 0)
            {
                if (head < tail)
                {
                    Array.Copy(array, head, newarray, 0, size);
                }
                else
                {
                    Array.Copy(array, head, newarray, 0, array.Length - head);
                    Array.Copy(array, 0, newarray, array.Length - head, tail);
                }
            }

            array = newarray;
            head = 0;
            tail = size == capacity ? 0 : size;
            _version++;
        }

        // Increments the index wrapping it if necessary.
        public void MoveNext(ref int index)
        {
            // It is tempting to use the remainder operator here but it is actually much slower
            // than a simple comparison and a rarely taken branch.
            // JIT produces better code than with ternary operator ?:
            int tmp = index + 1;
            if (tmp == array.Length)
            {
                tmp = 0;
            }
            index = tmp;
        }

        public void ThrowForEmptyExposedQueue()
        {
            Debug.Assert(size == 0);
            throw new InvalidOperationException("ExposedQueue is empty.");
        }

        public void TrimExcess()
        {
            int threshold = (int)(array.Length * 0.9);
            if (size < threshold)
            {
                SetCapacity(size);
            }
        }

        /// <summary>
        /// Ensures that the capacity of this ExposedQueue is at least the specified <paramref name="capacity"/>.
        /// </summary>
        /// <param name="capacity">The minimum capacity to ensure.</param>
        /// <returns>The new capacity of this queue.</returns>
        public int EnsureCapacity(int capacity)
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_NeedNonNegNum("capacity");
            }

            if (array.Length < capacity)
            {
                Grow(capacity);
            }

            return array.Length;
        }

        public void Grow(int capacity)
        {
            Debug.Assert(array.Length < capacity);

            const int GrowFactor = 2;
            const int MinimumGrow = 4;

            int newcapacity = GrowFactor * array.Length;

            // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
            // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
            if ((uint)newcapacity > Array.MaxLength) newcapacity = Array.MaxLength;

            // Ensure minimum growth is respected.
            newcapacity = Math.Max(newcapacity, array.Length + MinimumGrow);

            // If the computed capacity is still less than specified, set to the original argument.
            // Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
            if (newcapacity < capacity) newcapacity = capacity;

            SetCapacity(newcapacity);
        }

        // Implements an enumerator for a ExposedQueue.  The enumerator uses the
        // public version number of the list to ensure that no modifications are
        // made to the list while an enumeration is in progress.
        public struct Enumerator : IEnumerator<T>,
            IEnumerator
        {
            public readonly ExposedQueue<T> _q;
            public readonly int _version;
            public int _index; // -1 = not started, -2 = ended/disposed
            public T _currentElement;

            public Enumerator(ExposedQueue<T> q)
            {
                _q = q;
                _version = q._version;
                _index = -1;
                _currentElement = default;
            }

            public void Dispose()
            {
                _index = -2;
                _currentElement = default;
            }

            public bool MoveNext()
            {
                if (_version != _q._version)
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();

                if (_index == -2)
                    return false;

                _index++;

                if (_index == _q.size)
                {
                    // We've run past the last element
                    _index = -2;
                    _currentElement = default;
                    return false;
                }

                // Cache some fields in locals to decrease code size
                T[] array = _q.array;
                int capacity = array.Length;

                // _index represents the 0-based index into the queue, however the queue
                // doesn't have to start from 0 and it may not even be stored contiguously in memory.

                int arrayIndex = _q.head + _index; // this is the actual index into the queue's backing array
                if (arrayIndex >= capacity)
                {
                    // NOTE: Originally we were using the modulo operator here, however
                    // on Intel processors it has a very high instruction latency which
                    // was slowing down the loop quite a bit.
                    // Replacing it with simple comparison/subtraction operations sped up
                    // the average foreach loop by 2x.

                    arrayIndex -= capacity; // wrap around if needed
                }

                _currentElement = array[arrayIndex];
                return true;
            }

            public T Current
            {
                get
                {
                    if (_index < 0)
                        ThrowEnumerationNotStartedOrEnded();
                    return _currentElement!;
                }
            }

            public void ThrowEnumerationNotStartedOrEnded()
            {
                Debug.Assert(_index is -1 or -2);
                if (_index == -1)
                {
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumNotStarted();
                }
                else
                {
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumEnded();
                }
            }

            object IEnumerator.Current => Current;

            void IEnumerator.Reset()
            {
                if (_version != _q._version)
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
                _index = -1;
                _currentElement = default;
            }
        }
    }
}