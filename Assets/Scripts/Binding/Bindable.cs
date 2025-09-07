using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Match3Tray.Binding
{
    /// <summary>
    ///     A generic reactive property wrapper that provides change notification and value binding capabilities.
    ///     Supports serialization for Unity inspector integration.
    /// </summary>
    /// <typeparam name="T">The type of value to bind</typeparam>
    [Serializable]
    public class Bindable<T>
    {
        /// <summary>
        ///     The equality comparer for the value type.
        /// </summary>
        private static readonly EqualityComparer<T> _comparer = EqualityComparer<T>.Default;

        /// <summary>
        ///     The underlying value that is being bound.
        /// </summary>
        [SerializeField] private T _value;

        /// <summary>
        ///     The event handler for value changes.
        /// </summary>
        private Action<T> _onValueChanged;

        /// <summary>
        ///     Initializes a new instance of the Bindable class with the default value.
        /// </summary>
        public Bindable()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the Bindable class with the specified initial value.
        /// </summary>
        /// <param name="initialValue">The initial value to set</param>
        public Bindable(T initialValue)
        {
            _value = initialValue;
        }

        /// <summary>
        ///     Gets or sets the current value. Setting a new value will trigger the OnValueChanged event
        ///     if the value is different from the current value.
        /// </summary>
        public T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _value;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (_comparer.Equals(_value, value))
                    return;
                _value = value;
                _onValueChanged?.Invoke(_value);
            }
        }

        /// <summary>
        ///     Event that is raised when the Value property changes.
        /// </summary>
        public event Action<T> OnValueChanged
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            add => _onValueChanged += value;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            remove => _onValueChanged -= value;
        }

        /// <summary>
        ///     Implicitly converts a Bindable to its underlying value type.
        /// </summary>
        /// <param name="bindable">The Bindable instance to convert</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T(Bindable<T> bindable)
        {
            return bindable._value;
        }
    }

    /// <summary>
    ///     Specialized Bindable implementation for integer values to avoid boxing/unboxing overhead.
    /// </summary>
    [Serializable]
    public class BindableInt : Bindable<int>
    {
        /// <summary>
        ///     Initializes a new instance of the BindableInt class with the default value (0).
        /// </summary>
        public BindableInt()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the BindableInt class with the specified initial value.
        /// </summary>
        /// <param name="initialValue">The initial integer value</param>
        public BindableInt(int initialValue) : base(initialValue)
        {
        }
    }

    /// <summary>
    ///     Specialized Bindable implementation for float values to avoid boxing/unboxing overhead.
    /// </summary>
    [Serializable]
    public class BindableFloat : Bindable<float>
    {
        /// <summary>
        ///     Initializes a new instance of the BindableFloat class with the default value (0.0f).
        /// </summary>
        public BindableFloat()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the BindableFloat class with the specified initial value.
        /// </summary>
        /// <param name="initialValue">The initial float value</param>
        public BindableFloat(float initialValue) : base(initialValue)
        {
        }
    }

    /// <summary>
    ///     Specialized Bindable implementation for boolean values to avoid boxing/unboxing overhead.
    /// </summary>
    [Serializable]
    public class BindableBool : Bindable<bool>
    {
        /// <summary>
        ///     Initializes a new instance of the BindableBool class with the default value (false).
        /// </summary>
        public BindableBool()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the BindableBool class with the specified initial value.
        /// </summary>
        /// <param name="initialValue">The initial boolean value</param>
        public BindableBool(bool initialValue) : base(initialValue)
        {
        }
    }
}