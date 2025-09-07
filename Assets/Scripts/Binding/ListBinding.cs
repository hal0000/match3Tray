using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Match3Tray.Binding
{
    /// <summary>
    ///     Holds a collection and provides static context for binding expressions.
    /// </summary>
    public class ListBinding<T> : Bindable<IList<T>>
    {
        private static readonly Type _listType = typeof(ListBinding<T>);
        private static readonly Type _itemType = typeof(T);

        private int _count;

        private int _index = -1;
        private T _iterator;

        public ListBinding() : base(new List<T>())
        {
            _count = Value?.Count ?? 0;
        }

        public ListBinding(IList<T> initialItems) : base(initialItems ?? new List<T>())
        {
            _count = Value?.Count ?? 0;
        }

        public new IList<T> Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => base.Value;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (ReferenceEquals(base.Value, value)) return;
                base.Value = value;
                _count = value?.Count ?? 0;
            }
        }

        /// <summary>
        ///     Indexer to access items in the list with binding context
        /// </summary>
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (Value == null || index < 0 || index >= Value.Count)
                    return default;

                SetContext(index, Value);
                return Value[index];
            }
        }

        /// <summary>
        ///     Sets the current context for binding expressions.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetContext(int index, IList<T> items)
        {
            if (items == null || index < 0 || index >= items.Count)
            {
                _index = -1;
                _iterator = default;
                return;
            }

            _index = index;
            _iterator = items[index];
        }

        /// <summary>
        ///     Adds an item to the collection and updates the binding.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            if (Value == null) Value = new List<T>();
            Value.Add(item);
            _count = Value.Count;
        }

        /// <summary>
        ///     Removes an item from the collection and updates the binding.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(T item)
        {
            if (Value == null) return false;
            var result = Value.Remove(item);
            if (result) _count = Value.Count;
            return result;
        }
    }
}