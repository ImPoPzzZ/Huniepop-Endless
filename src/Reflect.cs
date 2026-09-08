using System;
using System.Reflection;

namespace HuniepopEndless
{
    /// <summary>
    /// Thin reflection helpers for poking at HuniePop 2's private fields. HP2 keeps
    /// almost all puzzle state private with no setters, so the run hooks have to reach
    /// in directly. Kept in one place so the fragile bits are easy to audit.
    /// </summary>
    internal static class Reflect
    {
        private const BindingFlags Any =
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        internal static T Get<T>(object target, string field)
        {
            var t = target as Type ?? target.GetType();
            var f = FindField(t, field);
            if (f == null) throw new MissingFieldException(t.FullName, field);
            return (T)f.GetValue(target is Type ? null : target);
        }

        internal static void Set(object target, string field, object value)
        {
            var t = target as Type ?? target.GetType();
            var f = FindField(t, field);
            if (f == null) throw new MissingFieldException(t.FullName, field);
            f.SetValue(target is Type ? null : target, value);
        }

        internal static object Call(object target, string method, params object[] args)
        {
            var t = target.GetType();
            var m = t.GetMethod(method, Any);
            if (m == null) throw new MissingMethodException(t.FullName, method);
            return m.Invoke(target, args);
        }

        private static FieldInfo FindField(Type t, string field)
        {
            for (var cur = t; cur != null; cur = cur.BaseType)
            {
                var f = cur.GetField(field, Any);
                if (f != null) return f;
            }
            return null;
        }
    }
}
