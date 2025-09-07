// --------------------------------------------------------------------------------------------------------------------
// Copyright (C) 2024 Halil Mentes
// All rights reserved.
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Match3Tray.Interface;
using Match3Tray.Logging;
using UnityEngine;

namespace Match3Tray.Binding
{
    /// <summary>
    ///     Base class for all UI components that need to bind to data sources.
    ///     Provides a flexible binding system that connects UI elements to Bindable properties.
    /// </summary>
    [RequireComponent(typeof(MonoBehaviour))]
    public abstract class UIBinding : MonoBehaviour
    {
        /// <summary>
        ///     Cache of property information to avoid repeated reflection lookups.
        /// </summary>
        private static readonly Dictionary<(Type, string), PropertyInfo> _propCache = new(31);

        /// <summary>
        ///     Cache of event information to avoid repeated reflection lookups.
        /// </summary>
        private static readonly Dictionary<(Type, string), EventInfo> sEventCache = new(31);

        /// <summary>
        ///     Shared object array pool for efficient memory usage during binding updates.
        /// </summary>
        private static readonly ArrayPool<object> sPool = ArrayPool<object>.Shared;

        /// <summary>
        ///     Comma-separated list of binding expressions in the format "Context.PropertyName".
        ///     Each expression defines a binding between a UI element and a data source property.
        /// </summary>
        [Tooltip("Comma-separated list of Context.PropertyName")] [SerializeField]
        protected string BindingExpressions;

        /// <summary>
        ///     The index to use for ListBinding context. If >= 0, indicates this is a ListBinding.
        /// </summary>
        [Tooltip("Index for ListBinding items. Set to -1 for direct property binding.")] [SerializeField]
        public int ListIndex = -1;

        /// <summary>
        ///     List of active bindings for this UI component.
        /// </summary>
        protected readonly List<BindingInfo> _bindings = new(8);

        /// <summary>
        ///     Cache for binding expressions to avoid re-parsing
        /// </summary>
        private string[] _cachedExpressions;

        /// <summary>
        ///     Initializes the binding system by parsing binding expressions and setting up event handlers.
        /// </summary>
        public virtual void Start()
        {
            if (string.IsNullOrWhiteSpace(BindingExpressions))
            {
                LoggerExtra.LogError($"[{name}] UIBinding: no BindingExpressions set.");
                return;
            }

            // Cache expressions
            _cachedExpressions = BindingExpressions.Split(',', StringSplitOptions.RemoveEmptyEntries);
            InitializeBindings();
        }

        /// <summary>
        ///     Cleans up event handlers when the UI component is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            foreach (var b in _bindings) b.Event.RemoveEventHandler(b.Source, b.Handler);
            _bindings.Clear();
        }

        /// <summary>
        ///     Initializes the bindings by parsing expressions and setting up event handlers.
        /// </summary>
        protected void InitializeBindings()
        {
            foreach (var raw in _cachedExpressions)
            {
                var expr = raw.Trim();
                var dot = expr.IndexOf('.');
                if (dot < 1 || dot == expr.Length - 1)
                {
                    LoggerExtra.LogError($"[{name}] invalid binding '{expr}'.");
                    continue;
                }

                var ctxName = expr.Substring(0, dot).Trim();
                var remainingPath = expr.Substring(dot + 1).Trim();

                // 1. Try registry
                var ctx = BindingContextRegistry.Get(ctxName);
                // 2. Fallback: hierarchy
                if (ctx == null)
                    ctx = FindContextInHierarchy(transform, ctxName);
                if (ctx == null)
                {
                    LoggerExtra.LogError($"[{name}] UIBinding: context '{ctxName}' not found in registry or parent hierarchy.");
                    continue;
                }

                // Process the remaining path (could be simple property or nested with ListBinding)
                var parts = remainingPath.Split('.');
                object currentObject = ctx;
                var currentType = ctx.GetType();

                for (var i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];

                    // Check if this is a ListBinding access
                    if (part.Contains("[") && part.Contains("]"))
                    {
                        var listParts = part.Split('[', ']');
                        if (listParts.Length != 3)
                        {
                            LoggerExtra.LogError($"[{name}] UIBinding: Invalid ListBinding format in '{part}'");
                            continue;
                        }

                        var listPropertyName = listParts[0];
                        int index;

                        // If ListIndex is set (>= 0), use it instead of the index in the expression
                        if (ListIndex >= 0)
                        {
                            index = ListIndex;
                        }
                        else
                        {
                            if (!int.TryParse(listParts[1], out index))
                            {
                                LoggerExtra.LogError($"[{name}] UIBinding: Invalid index in ListBinding '{part}'");
                                continue;
                            }
                        }

                        // Get the ListBinding property
                        var listProperty = currentType.GetProperty(listPropertyName);
                        if (listProperty == null)
                        {
                            LoggerExtra.LogError($"[{name}] UIBinding: Property '{listPropertyName}' not found on {currentType.Name}");
                            continue;
                        }

                        var listType = listProperty.PropertyType;
                        if (!listType.IsGenericType || listType.GetGenericTypeDefinition() != typeof(ListBinding<>))
                        {
                            LoggerExtra.LogError($"[{name}] UIBinding: Property '{listPropertyName}' is not a ListBinding<>");
                            continue;
                        }

                        // Get the ListBinding instance
                        var listBinding = listProperty.GetValue(currentObject);
                        if (listBinding == null)
                        {
                            LoggerExtra.LogError($"[{name}] UIBinding: ListBinding is null");
                            continue;
                        }

                        // Get the item type from ListBinding<>
                        var itemType = listType.GetGenericArguments()[0];

                        // Get the Value property from ListBinding
                        var valueProperty = listType.GetProperty("Value");
                        if (valueProperty == null)
                        {
                            LoggerExtra.LogError($"[{name}] UIBinding: Value property not found on ListBinding");
                            continue;
                        }

                        // Get the list from Value property
                        var list = valueProperty.GetValue(listBinding);
                        if (list == null)
                        {
                            LoggerExtra.LogError($"[{name}] UIBinding: List is null");
                            continue;
                        }

                        // Get the Count property
                        var countProperty = list.GetType().GetProperty("Count");
                        if (countProperty == null)
                        {
                            LoggerExtra.LogError($"[{name}] UIBinding: Count property not found on list");
                            continue;
                        }

                        // Check index range
                        var count = (int)countProperty.GetValue(list);
                        if (index < 0 || index >= count)
                        {
                            LoggerExtra.LogError($"[{name}] UIBinding: Index {index} out of range");
                            continue;
                        }

                        // Get the item at the specified index using indexer
                        var indexer = list.GetType().GetProperty("Item");
                        if (indexer == null)
                        {
                            LoggerExtra.LogError($"[{name}] UIBinding: Indexer not found on list");
                            continue;
                        }

                        currentObject = indexer.GetValue(list, new object[] { index });
                        if (currentObject == null)
                        {
                            LoggerExtra.LogError($"[{name}] UIBinding: No item found at index {index}");
                            continue;
                        }

                        // Get the Bindable property from the item
                        var bindableProperty = itemType.GetProperty(parts[i + 1]);
                        if (bindableProperty == null)
                        {
                            LoggerExtra.LogError($"[{name}] UIBinding: Property '{parts[i + 1]}' not found on {itemType.Name}");
                            continue;
                        }

                        // Get the property value
                        var propertyValue = bindableProperty.GetValue(currentObject);
                        if (propertyValue == null)
                        {
                            LoggerExtra.LogError($"[{name}] UIBinding: Property '{parts[i + 1]}' is null");
                            continue;
                        }

                        // Set current object and type
                        currentObject = propertyValue;
                        currentType = propertyValue.GetType();
                        i++; // Skip the next part since we already processed it
                    }
                    else
                    {
                        // Regular property access
                        var property = currentType.GetProperty(part);
                        if (property == null)
                        {
                            LoggerExtra.LogError($"[{name}] UIBinding: Property '{part}' not found on {currentType.Name}");
                            continue;
                        }

                        var propertyValue = property.GetValue(currentObject);
                        if (propertyValue == null)
                        {
                            LoggerExtra.LogError($"[{name}] UIBinding: Property '{part}' is null");
                            continue;
                        }

                        currentObject = propertyValue;
                        currentType = propertyValue.GetType();
                    }
                }

                // Now we have the final value, create the binding
                if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(Bindable<>))
                {
                    // Create binding info
                    BindingInfo bindingInfo = new(
                        currentObject,
                        currentType.GetProperty("Value"),
                        () => currentObject,
                        currentType.GetEvent("OnValueChanged"),
                        CreateValueChangedHandler(currentType)
                    );
                    bindingInfo.Event.AddEventHandler(bindingInfo.Source, bindingInfo.Handler);
                    _bindings.Add(bindingInfo);
                }
                else
                {
                    LoggerExtra.LogError($"[{name}] UIBinding: Final value is not a Bindable<>");
                }
            }

            // Initial push to UI
            UpdateBindings();
        }

        /// <summary>
        ///     Generic callback method invoked by reflection whenever any Bindable<T>.OnValueChanged fires.
        /// </summary>
        /// <typeparam name="T">The type of the bound value</typeparam>
        /// <param name="_">The new value (unused)</param>
        private void OnAnyChanged<T>(T _)
        {
            UpdateBindings();
        }

        /// <summary>
        ///     Updates all bindings by collecting current values and notifying the UI component.
        ///     Uses object pooling for efficient memory usage.
        /// </summary>
        private void UpdateBindings()
        {
            var c = _bindings.Count;
            if (c == 0) return;

            // rent a small array
            var arr = sPool.Rent(c);
            try
            {
                for (var i = 0; i < c; i++)
                {
                    var bindable = _bindings[i].Getter();
                    if (bindable != null)
                    {
                        var valueProperty = bindable.GetType().GetProperty("Value");
                        if (valueProperty != null) arr[i] = valueProperty.GetValue(bindable);
                    }
                }

                OnBindingUpdated(c, arr);
            }
            finally
            {
                sPool.Return(arr, true);
            }
        }

        /// <summary>
        ///     Updates a bound value and triggers the binding system.
        /// </summary>
        /// <typeparam name="T">The type of the value to update</typeparam>
        /// <param name="bindingIndex">Index of the binding to update</param>
        /// <param name="newValue">The new value to set</param>
        protected void PushValue<T>(int bindingIndex, T newValue)
        {
            if (bindingIndex < 0 || bindingIndex >= _bindings.Count)
            {
                LoggerExtra.LogError($"[{name}] UIBinding: Invalid binding index {bindingIndex}. Make sure binding expressions are properly set in the inspector.");
                return;
            }

            // reflect or cache the bindable
            var info = _bindings[bindingIndex];
            var bindable = info.Prop.GetValue(info.Source);
            if (bindable != null)
            {
                var valueProperty = bindable.GetType().GetProperty("Value");
                if (valueProperty != null) valueProperty.SetValue(bindable, newValue);
            }
        }

        /// <summary>
        ///     Called with raw bound values (boxed). Derived classes apply formatting
        ///     or UI-specific logic here.
        /// </summary>
        /// <param name="count">Number of values in the update</param>
        /// <param name="values">Array of updated values</param>
        protected abstract void OnBindingUpdated(int count, object[] values);

        /// <summary>
        ///     Recursively searches for an IBindingContext in the hierarchy.
        ///     Optimized with MethodImpl and inlining.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IBindingContext FindContextInHierarchy(Transform current, string contextName)
        {
            while (current != null)
            {
                // Get all components at once to avoid multiple GetComponent calls
                var components = current.GetComponents<MonoBehaviour>();
                var count = components.Length;

                // Use for loop instead of foreach for better performance
                for (var i = 0; i < count; i++)
                    if (components[i] is IBindingContext context &&
                        context.GetType().Name.Equals(contextName, StringComparison.OrdinalIgnoreCase))
                        return context;

                current = current.parent;
            }

            return null;
        }

        /// <summary>
        ///     Creates a value changed handler for the given type.
        /// </summary>
        private Delegate CreateValueChangedHandler(Type bindableType)
        {
            // Get the value type from Bindable<T>
            var valueType = bindableType.GetGenericArguments()[0];

            // Create a typed callback OnAnyChanged<T>
            var method = typeof(UIBinding)
                .GetMethod(nameof(OnAnyChanged), BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(valueType);

            // Get the event type from Bindable<T>
            var eventType = bindableType.GetEvent("OnValueChanged")!.EventHandlerType!;

            // Create the delegate
            return Delegate.CreateDelegate(eventType, this, method);
        }

        /// <summary>
        ///     Structure holding information about a single binding.
        /// </summary>
        protected readonly struct BindingInfo
        {
            /// <summary>
            ///     The source object containing the bound property.
            /// </summary>
            public readonly object Source;

            /// <summary>
            ///     Reflection information about the bound property.
            /// </summary>
            public readonly PropertyInfo Prop;

            /// <summary>
            ///     Delegate that gets the current value of the bound property.
            /// </summary>
            public readonly Func<object> Getter;

            /// <summary>
            ///     Reflection information about the value changed event.
            /// </summary>
            public readonly EventInfo Event;

            /// <summary>
            ///     The event handler delegate for value changes.
            /// </summary>
            public readonly Delegate Handler;

            /// <summary>
            ///     Creates a new binding information structure.
            /// </summary>
            public BindingInfo(object src, PropertyInfo p, Func<object> g, EventInfo e, Delegate h)
            {
                Source = src;
                Prop = p;
                Getter = g;
                Event = e;
                Handler = h;
            }
        }
    }
}