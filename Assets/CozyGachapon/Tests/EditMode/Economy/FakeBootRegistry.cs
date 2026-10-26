using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Tests.Economy
{
    public sealed class FakeBootRegistry : IServiceRegistry
    {
        private readonly List<IBootStep> _steps = new List<IBootStep>();
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private readonly Dictionary<Type, int> _registerCounts = new Dictionary<Type, int>();

        public IReadOnlyList<IBootStep> Steps => _steps;

        public bool RegisterStep(IBootStep step)
        {
            if (step == null)
            {
                return false;
            }

            for (int i = 0; i < _steps.Count; i++)
            {
                if (string.Equals(_steps[i].Id, step.Id, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            _steps.Add(step);
            return true;
        }

        public int CountStepsById(string stepId)
        {
            int count = 0;
            for (int i = 0; i < _steps.Count; i++)
            {
                if (string.Equals(_steps[i].Id, stepId, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

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

            _services[key] = service;
            _registerCounts[key] = GetRegistrationCount<T>() + 1;
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

        public int GetRegistrationCount<T>() where T : class
        {
            return _registerCounts.TryGetValue(typeof(T), out int count) ? count : 0;
        }
    }
}
