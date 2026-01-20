using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Locomotion.Narrative
{
    public static class NarrativeReflection
    {
        public static object TryGetMemberValue(GameObject go, string componentTypeName, string memberName)
        {
            if (go == null) return null;
            if (string.IsNullOrWhiteSpace(memberName)) return null;

            if (!string.IsNullOrWhiteSpace(componentTypeName))
            {
                Type t = FindType(componentTypeName);
                if (t == null) return null;
                Component c = go.GetComponent(t);
                if (c == null) return null;
                return TryGetMemberValue(c, memberName);
            }

            // Search all components for the first matching member name.
            var comps = go.GetComponents<Component>();
            for (int i = 0; i < comps.Length; i++)
            {
                var c = comps[i];
                if (c == null) continue;
                object v = TryGetMemberValue(c, memberName);
                if (v != null)
                    return v;
            }

            return null;
        }

        public static bool TrySetMemberValue(GameObject go, string componentTypeName, string memberName, NarrativeValue value)
        {
            if (go == null) return false;
            if (string.IsNullOrWhiteSpace(memberName)) return false;

            Type t = !string.IsNullOrWhiteSpace(componentTypeName) ? FindType(componentTypeName) : null;
            Component c = t != null ? go.GetComponent(t) : null;
            if (c == null && t == null)
            {
                // Search any component that has this member.
                var comps = go.GetComponents<Component>();
                for (int i = 0; i < comps.Length; i++)
                {
                    if (TrySetMemberValue(comps[i], memberName, value))
                        return true;
                }
                return false;
            }

            return TrySetMemberValue(c, memberName, value);
        }

        public static bool TryInvokeMethod(GameObject go, string componentTypeName, string methodName, NarrativeValue[] args, out object returnValue)
        {
            returnValue = null;
            if (go == null) return false;
            if (string.IsNullOrWhiteSpace(methodName)) return false;

            Type t = FindType(componentTypeName);
            if (t == null) return false;

            Component c = go.GetComponent(t);
            if (c == null) return false;

            MethodInfo[] methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo m = methods[i];
                if (!string.Equals(m.Name, methodName, StringComparison.Ordinal))
                    continue;

                var ps = m.GetParameters();
                int argCount = args != null ? args.Length : 0;
                if (ps.Length != argCount)
                    continue;

                object[] callArgs = new object[argCount];
                bool ok = true;
                for (int a = 0; a < argCount; a++)
                {
                    if (!TryConvert(args[a], ps[a].ParameterType, out object converted))
                    {
                        ok = false;
                        break;
                    }
                    callArgs[a] = converted;
                }

                if (!ok) continue;

                returnValue = m.Invoke(c, callArgs);
                return true;
            }

            return false;
        }

        public static bool Compare(object memberValue, ComparisonOperator op, NarrativeValue compareTo)
        {
            if (memberValue == null)
                return false;

            // bool
            if (memberValue is bool b)
            {
                if (compareTo.type != NarrativeValueType.Bool) return false;
                return op switch
                {
                    ComparisonOperator.Equal => b == compareTo.boolValue,
                    ComparisonOperator.NotEqual => b != compareTo.boolValue,
                    _ => false
                };
            }

            // int/float numeric
            if (memberValue is int i)
                return CompareNumeric(i, op, compareTo);
            if (memberValue is float f)
                return CompareNumeric(f, op, compareTo);
            if (memberValue is double d)
                return CompareNumeric(d, op, compareTo);

            if (memberValue is string s)
            {
                string rhs = compareTo.type == NarrativeValueType.String ? (compareTo.stringValue ?? "") : compareTo.ToString();
                return op switch
                {
                    ComparisonOperator.Equal => string.Equals(s, rhs, StringComparison.Ordinal),
                    ComparisonOperator.NotEqual => !string.Equals(s, rhs, StringComparison.Ordinal),
                    _ => false
                };
            }

            return false;
        }

        private static bool CompareNumeric(double lhs, ComparisonOperator op, NarrativeValue rhs)
        {
            double r = rhs.type switch
            {
                NarrativeValueType.Int => rhs.intValue,
                NarrativeValueType.Float => rhs.floatValue,
                NarrativeValueType.Bool => rhs.boolValue ? 1.0 : 0.0,
                _ => double.NaN
            };

            if (double.IsNaN(r))
                return false;

            return op switch
            {
                ComparisonOperator.LessThan => lhs < r,
                ComparisonOperator.LessThanOrEqual => lhs <= r,
                ComparisonOperator.GreaterThan => lhs > r,
                ComparisonOperator.GreaterThanOrEqual => lhs >= r,
                ComparisonOperator.Equal => Math.Abs(lhs - r) <= 1e-6,
                ComparisonOperator.NotEqual => Math.Abs(lhs - r) > 1e-6,
                _ => false
            };
        }

        private static object TryGetMemberValue(Component c, string memberName)
        {
            if (c == null) return null;
            Type t = c.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            PropertyInfo p = t.GetProperty(memberName, flags);
            if (p != null && p.CanRead)
            {
                try { return p.GetValue(c); }
                catch { return null; }
            }

            FieldInfo f = t.GetField(memberName, flags);
            if (f != null)
            {
                try { return f.GetValue(c); }
                catch { return null; }
            }

            return null;
        }

        private static bool TrySetMemberValue(Component c, string memberName, NarrativeValue value)
        {
            if (c == null) return false;
            Type t = c.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            PropertyInfo p = t.GetProperty(memberName, flags);
            if (p != null && p.CanWrite)
            {
                if (TryConvert(value, p.PropertyType, out object converted))
                {
                    p.SetValue(c, converted);
                    return true;
                }
                return false;
            }

            FieldInfo f = t.GetField(memberName, flags);
            if (f != null)
            {
                if (TryConvert(value, f.FieldType, out object converted))
                {
                    f.SetValue(c, converted);
                    return true;
                }
                return false;
            }

            return false;
        }

        private static bool TryConvert(NarrativeValue v, Type targetType, out object converted)
        {
            converted = null;
            if (targetType == typeof(bool) && v.type == NarrativeValueType.Bool)
            {
                converted = v.boolValue;
                return true;
            }
            if (targetType == typeof(int) && (v.type == NarrativeValueType.Int || v.type == NarrativeValueType.Float))
            {
                converted = v.type == NarrativeValueType.Int ? v.intValue : Mathf.RoundToInt(v.floatValue);
                return true;
            }
            if (targetType == typeof(float) && (v.type == NarrativeValueType.Float || v.type == NarrativeValueType.Int))
            {
                converted = v.type == NarrativeValueType.Float ? v.floatValue : (float)v.intValue;
                return true;
            }
            if (targetType == typeof(double) && (v.type == NarrativeValueType.Float || v.type == NarrativeValueType.Int))
            {
                converted = v.type == NarrativeValueType.Float ? (double)v.floatValue : (double)v.intValue;
                return true;
            }
            if (targetType == typeof(string))
            {
                converted = v.type == NarrativeValueType.String ? (v.stringValue ?? "") : v.ToString();
                return true;
            }
            if (targetType == typeof(Vector3) && v.type == NarrativeValueType.Vector3)
            {
                converted = v.vector3Value;
                return true;
            }

            return false;
        }

        private static Type FindType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return null;

            // Try direct first (works for assembly-qualified names)
            Type t = Type.GetType(typeName, throwOnError: false);
            if (t != null)
                return t;

            // Fallback: search loaded assemblies by full name
            var asms = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < asms.Length; i++)
            {
                try
                {
                    t = asms[i].GetType(typeName, throwOnError: false);
                    if (t != null)
                        return t;
                }
                catch { /* ignore */ }
            }

            // Last-chance: match by short name
            for (int i = 0; i < asms.Length; i++)
            {
                Type[] types;
                try { types = asms[i].GetTypes(); }
                catch { continue; }

                for (int j = 0; j < types.Length; j++)
                {
                    if (string.Equals(types[j].Name, typeName, StringComparison.Ordinal))
                        return types[j];
                }
            }

            return null;
        }
    }
}

