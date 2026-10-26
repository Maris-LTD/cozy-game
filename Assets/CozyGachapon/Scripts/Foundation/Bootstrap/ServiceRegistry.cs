using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Foundation
{
    public sealed class ServiceRegistry : IServiceRegistry
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public bool Register<T>(T service) where T : class
        {
            if (service == null)
            {
                return false;
            }

            Type key = typeof(T);
            if (_services.ContainsKey(key))
            {
                return false;
            }

            _services.Add(key, service);
            return true;
        }

        public bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out object raw) && raw is T typed)
            {
                service = typed;
                return true;
            }

            service = null;
            return false;
        }
    }
}
