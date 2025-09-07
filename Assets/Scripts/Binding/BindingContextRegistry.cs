using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Match3Tray.Interface;

namespace Match3Tray.Binding
{
    /// <summary>
    ///     Tiny, zero‑allocation registry for IBindingContext instances.
    ///     Register your contexts in Awake(), unregister in OnDestroy(), and Resolve in O(1).
    /// </summary>
    public static class BindingContextRegistry
    {
        private static readonly Dictionary<string, IBindingContext> _map = new(7);

        /// <summary>
        ///     Register an IBindingContext under the given key (e.g. the Type.Name).
        ///     ALSO overrides any existing entry
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Register(in string key, IBindingContext ctx)
        {
            _map[key] = ctx;
        }

        /// <summary>
        ///     Unregister the context previously registered under that key.
        ///     Call from OnDestroy of your context.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unregister(in string key, IBindingContext ctx)
        {
            if (_map.TryGetValue(key, out var existing) && existing == ctx)
                _map.Remove(key);
        }

        /// <summary>
        ///     Resolve the context by key in O(1). Returns null if not found.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IBindingContext Get(in string key)
        {
            _map.TryGetValue(key, out var ctx);
            return ctx;
        }

        /// <summary>
        ///     Clear all entries. Call this on scene unload if you want a clean slate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear()
        {
            _map.Clear();
        }
    }
}