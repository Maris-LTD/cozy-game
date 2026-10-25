using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Pooling
{
    public sealed class PoolingPrewarmBootStep : IBootStep
    {
        public const string StepId = "pooling.prewarm";

        private readonly PoolingServiceBootStep _serviceStep;
        private readonly PoolingConfig _config;
        private readonly Transform _root;
        private readonly IPoolDiagnosticsSink _sink;
        private readonly int _targetFps;
        private readonly List<PoolConfigIssue> _issues = new List<PoolConfigIssue>(8);
        private ObjectPoolService _service;
        private bool _hasFailedToStart;

        public string Id => StepId;
        public BootPhase Phase => BootPhase.Prewarm;
        public IReadOnlyList<string> RunsAfter => Array.Empty<string>();
        public bool IsCritical => true;
        public bool IsDone => CurrentService != null && CurrentService.Phase == PoolServicePhase.Ready;
        public bool IsFailed => _hasFailedToStart || (CurrentService != null && CurrentService.HasPrewarmFailed);

        private ObjectPoolService CurrentService => _service != null && _service == _serviceStep.Service ? _service : null;

        public PoolingPrewarmBootStep(
            PoolingServiceBootStep serviceStep,
            PoolingConfig config,
            Transform root,
            IPoolDiagnosticsSink sink,
            int targetFps)
        {
            _serviceStep = serviceStep;
            _config = config;
            _root = root;
            _sink = sink;
            _targetFps = targetFps;
        }

        public void Run()
        {
            _service = _serviceStep.Service;
            if (_service == null || _config == null)
            {
                _hasFailedToStart = true;
                return;
            }

            _issues.Clear();
            PoolingConfigValidator.Validate(_config, _issues);
            PoolingConfigValidator.ValidatePrewarmBudget(_config, _service.RegisteredCapacityTotal, _targetFps, _issues);
            ReportIssues();
            _service.BeginPrewarm(_root);
        }

        public void Pump()
        {
            ObjectPoolService service = CurrentService;
            if (service != null && service.Phase == PoolServicePhase.Prewarming)
            {
                service.StepPrewarm();
            }
        }

        private void ReportIssues()
        {
            for (int i = 0; i < _issues.Count; i++)
            {
                PoolConfigIssue issue = _issues[i];
                if (issue.IsError)
                {
                    _sink.ReportError(issue.Code, issue.PoolId);
                }
                else
                {
                    _sink.ReportWarning(issue.Code, issue.PoolId);
                }
            }
        }
    }
}
