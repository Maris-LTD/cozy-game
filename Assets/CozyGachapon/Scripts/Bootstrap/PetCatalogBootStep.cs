using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Core.PetCatalog;

namespace CozyGachapon.Bootstrap
{
    public sealed class PetCatalogBootStep : IBootStep
    {
        public const string StepId = "pet.catalog";

        private readonly PetCatalogConfig catalogConfig;
        private readonly IServiceRegistry services;
        private static readonly string[] EmptyRunsAfter = Array.Empty<string>();

        public PetCatalogBootStep(PetCatalogConfig catalogConfig, IServiceRegistry services)
        {
            this.catalogConfig = catalogConfig;
            this.services = services;
        }

        public string Id => StepId;
        public BootPhase Phase => BootPhase.DataLoad;
        public IReadOnlyList<string> RunsAfter => EmptyRunsAfter;
        public bool IsCritical => true;
        public bool IsDone { get; private set; }
        public bool IsFailed { get; private set; }

        public void Run()
        {
            try
            {
                IEnumerable<PetDefinition> pets;
                PetRarityConfig rarityConfig = null;

                if (catalogConfig != null)
                {
                    pets = catalogConfig.Pets;
                    rarityConfig = catalogConfig.RarityConfig;
                }
                else
                {
                    pets = MvpPetFactory.CreateAllMvpPets();
                }

                var database = new PetDatabase(pets, rarityConfig);
                services?.Register<IPetDatabase>(database);

                IsDone = true;
                IsFailed = false;
            }
            catch (Exception)
            {
                IsDone = false;
                IsFailed = true;
                throw;
            }
        }
    }
}
