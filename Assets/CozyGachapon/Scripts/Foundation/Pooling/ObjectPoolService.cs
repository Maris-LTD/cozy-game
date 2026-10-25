using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Pooling
{
    public class ObjectPoolService : IPoolService
    {
        private readonly PoolingConfig _config;
        private readonly IPoolDiagnosticsSink _sink;
        private readonly PoolRegistry _registry = new PoolRegistry();
        private PoolPrewarmer _prewarmer;

        public PoolServicePhase Phase { get; private set; }
        public bool HasPrewarmFailed { get; private set; }
        public int RegisteredCapacityTotal => _registry.TotalCapacity;

        public ObjectPoolService(PoolingConfig config, IPoolDiagnosticsSink sink)
        {
            _config = config;
            _sink = sink;
            Phase = PoolServicePhase.Accepting;
        }

        public PoolRegistrationResult Register(string id, int clientDeclaredMax, bool forbidRecycle)
        {
            PoolRegistrationError error = PoolRegistrationValidator.Validate(
                Phase,
                id,
                clientDeclaredMax,
                forbidRecycle,
                _config,
                _registry,
                out PoolDefinition definition,
                out int capacity);

            if (error != PoolRegistrationError.None)
            {
                _sink.ReportError(PoolRegistrationValidator.ToDiagnosticCode(error), id);
                return PoolRegistrationResult.Reject(error);
            }

            _registry.Add(new PoolRecord(id, definition, capacity));
            return PoolRegistrationResult.Accept(capacity);
        }

        public bool TryGetStats(string id, out PoolStats stats)
        {
            if (_registry.TryGet(id, out PoolRecord record))
            {
                stats = record.ToStats();
                return true;
            }

            stats = default;
            return false;
        }

        public void BeginPrewarm(Transform root)
        {
            if (Phase != PoolServicePhase.Accepting)
            {
                return;
            }

            Phase = PoolServicePhase.Prewarming;
            if (_registry.Count == 0)
            {
                Phase = PoolServicePhase.Ready;
                return;
            }

            if (root == null)
            {
                FailPrewarm(PoolDiagnosticCode.PrewarmMissingContainer);
                return;
            }

            try
            {
                _prewarmer = new PoolPrewarmer(_registry, root, PrewarmBudget.EffectivePerFrame(_config.PrewarmPerFrame));
            }
            catch (Exception)
            {
                FailPrewarm(PoolDiagnosticCode.PrewarmFailed);
            }
        }

        public void StepPrewarm()
        {
            if (Phase != PoolServicePhase.Prewarming || HasPrewarmFailed || _prewarmer == null)
            {
                return;
            }

            _prewarmer.Step();
            if (_prewarmer.HasFailed)
            {
                FailPrewarm(PoolDiagnosticCode.PrewarmFailed);
                return;
            }

            if (_prewarmer.IsComplete)
            {
                Phase = PoolServicePhase.Ready;
            }
        }

        public int CopyFreeObjects(string id, List<GameObject> buffer)
        {
            buffer.Clear();
            if (!_registry.TryGet(id, out PoolRecord record))
            {
                return 0;
            }

            for (int i = 0; i < record.FreeCount; i++)
            {
                buffer.Add(record.GetFreeAt(i));
            }

            return buffer.Count;
        }

        public void SetPhaseForTest(PoolServicePhase phase)
        {
            Phase = phase;
        }

        private void FailPrewarm(PoolDiagnosticCode code)
        {
            HasPrewarmFailed = true;
            _sink.ReportError(code, null);
        }
    }
}
