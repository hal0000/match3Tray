using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Match3Tray.Core
{
    public static class TypeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt(this object o)
        {
            if (o == null) return 0;
            if (o is int i) return i;
            if (o is uint u) return (int)u;
            if (o is float f) return (int)f;
            if (o is long l) return (int)l;
            double.TryParse(o.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d);
            return (int)d;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt(this object o, string key)
        {
            if (o is not IDictionary<string, object> dict) return 0;
            return dict.TryGetValue(key, out var value) ? value.ToInt() : 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToFloat(this object o)
        {
            if (o == null) return 0f;
            if (o is int i) return i;
            if (o is uint u) return u;
            if (o is float f) return f;
            if (o is long l) return l;
            float.TryParse(o.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var fVal);
            return fVal;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToFloat(this object o, string key)
        {
            if (o is not IDictionary<string, object> dict) return 0f;
            return dict.TryGetValue(key, out var value) ? value.ToFloat() : 0f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToLong(this object o)
        {
            if (o == null) return 0;
            if (o is int i) return i;
            if (o is uint u) return u;
            if (o is float f) return (long)f;
            if (o is long l) return l;
            long.TryParse(o.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var lVal);
            return lVal;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToLong(this object o, string key)
        {
            if (o is not IDictionary<string, object> dict) return 0;
            return dict.TryGetValue(key, out var value) ? value.ToLong() : 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ToDouble(this object o)
        {
            if (o == null) return 0;
            if (o is int) return (int)o;
            if (o is uint) return (uint)o;
            if (o is float) return (float)o;
            if (o is long) return (long)o;
            if (o is bool b) return b ? 1 : 0;
            double.TryParse(o.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var dVal);
            return dVal;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ToDouble(this object o, string key)
        {
            if (o is not IDictionary<string, object> dict) return 0;
            return dict.TryGetValue(key, out var value) ? value.ToDouble() : 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ToBool(this object o)
        {
            if (o == null) return false;
            if (o is bool b) return b;
            if (o is string s) return s.Equals("true", StringComparison.OrdinalIgnoreCase) || s.Equals("yes", StringComparison.OrdinalIgnoreCase);
            return o.ToInt() != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ToBool(this object o, string key)
        {
            if (o is not IDictionary<string, object> dict) return false;
            return dict.TryGetValue(key, out var value) && value.ToBool();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToString(this object o, string key)
        {
            if (o is not IDictionary<string, object> dict) return null;
            return dict.TryGetValue(key, out var value) ? value?.ToString() : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> ToList<T>(this IDictionary<string, object> dict, string key)
        {
            dict.TryGetValue(key, out var value);
            return value as List<T>;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<int> ToIntList(this IDictionary<string, object> dict, string key)
        {
            dict.TryGetValue(key, out var value);
            var objList = value as List<object> ?? new List<object>();
            List<int> intList = new(objList.Count);
            foreach (var obj in objList)
                intList.Add(obj.ToInt());
            return intList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InvSqrt(float x) //quake <3
        {
            unsafe
            {
                var xhalf = 0.5f * x;
                var i = *(int*)&x; // float bits -> int
                i = 0x5f3759df - (i >> 1); // magic
                x = *(float*)&i; // int bits -> float
                x = x * (1.5f - xhalf * x * x); // 1. iteration
                return x;
            }
        }
    }
}