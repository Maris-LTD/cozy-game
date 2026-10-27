using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Contracts.Inventory;
using CozyGachapon.Core.Inventory;
using CozyGachapon.Core.PetCatalog;

namespace CozyGachapon.Bootstrap
{
    public sealed class InventoryBootStep : IBootStep
    {
        public const string StepId = "inventory.core";
        private static readonly string[] RequiredPredecessors = { "economy.core" };

        private readonly InventoryConfig config;
        private readonly IServiceRegistry services;
        private readonly IReadOnlyList<PetCopyEntry> initialSavedEntries;

        public InventoryBootStep(
            InventoryConfig config,
            IServiceRegistry services,
            IReadOnlyList<PetCopyEntry> initialSavedEntries = null)
        {
            this.config = config;
            this.services = services;
            this.initialSavedEntries = initialSavedEntries;
        }

        public string Id => StepId;
        public BootPhase Phase => BootPhase.GameplayInit;
        public IReadOnlyList<string> RunsAfter => RequiredPredecessors;
        public bool IsCritical => true;
        public bool IsDone { get; private set; }
        public bool IsFailed { get; private set; }
        public InventoryService Service { get; private set; }

        public void Run()
        {
            if (services == null || !services.TryGet<IPetDatabase>(out var catalog) || catalog == null)
            {
                Service = new InventoryService(null, config);
                Service.MarkFailed();
                IsDone = false;
                IsFailed = true;
                return;
            }

            Service = new InventoryService(catalog, config);
            if (!Service.Initialize(initialSavedEntries))
            {
                IsDone = false;
                IsFailed = true;
                return;
            }

            services.Register<IInventoryService>(Service);
            IsDone = true;
            IsFailed = false;
        }
    }
}
