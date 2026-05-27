using System;
using System.Collections.Generic;

namespace Workspace.Core
{
    public static class ServiceLocator
    {
        private static Dictionary<Type, object> services
            = new Dictionary<Type, object>();

        public static void Register<T>(T service)
        {
            services[typeof(T)] = service;
        }

        public static void Unregister<T>()
        {
            services.Remove(typeof(T));
        }

        public static T Get<T>()
        {
            if (services.TryGetValue(typeof(T), out object service))
                return (T)service;

            throw new Exception($"ServiceLocator: {typeof(T)}が登録されていません");
        }

        public static bool TryGet<T>(out T service)
        {
            if (services.TryGetValue(typeof(T), out object obj))
            {
                service = (T)obj;
                return true;
            }
            service = default;
            return false;
        }
    }
}