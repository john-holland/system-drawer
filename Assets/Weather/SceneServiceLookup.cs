using System;
using System.Reflection;
using UnityEngine;

namespace Weather
{
    /// <summary>
    /// Cross-assembly bridge to <see cref="SystemDrawerSceneServices"/> without asmdef cycles.
    /// </summary>
    public static class SceneServiceLookup
    {
        static Type _sceneServicesType;

        static Type SceneServicesType =>
            _sceneServicesType ??= Type.GetType("SystemDrawerSceneServices, SystemDrawer");

        public static bool TryResolve<T>(string key, out T result) where T : UnityEngine.Object
        {
            result = null;
            Type servicesType = SceneServicesType;
            if (servicesType == null)
            {
                result = UnityEngine.Object.FindFirstObjectByType<T>();
                return result != null;
            }

            MethodInfo openGeneric = null;
            MethodInfo[] methods = servicesType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo m = methods[i];
                if (m.Name == "TryResolve" && m.IsGenericMethodDefinition)
                {
                    openGeneric = m;
                    break;
                }
            }

            if (openGeneric == null)
            {
                result = UnityEngine.Object.FindFirstObjectByType<T>();
                return result != null;
            }

            MethodInfo closed = openGeneric.MakeGenericMethod(typeof(T));
            object[] args = { key, null };
            bool ok = (bool)closed.Invoke(null, args);
            result = args[1] as T;
            return ok && result != null;
        }
    }
}
