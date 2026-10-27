using CozyGachapon.Contracts;
using CozyGachapon.Core.PetCatalog;
using CozyGachapon.Foundation;

namespace CozyGachapon.Bootstrap
{
    public sealed class PetWiring
    {
        private readonly PetCatalogConfig catalogConfig;
        private readonly IServiceRegistry services;
        private BootstrapGraph graph;

        public PetCatalogBootStep BootStep { get; private set; }
        public bool IsRegistered => graph != null;

        public PetWiring(PetCatalogConfig catalogConfig, IServiceRegistry services)
        {
            this.catalogConfig = catalogConfig;
            this.services = services;
        }

        public bool Register(BootstrapGraph targetGraph)
        {
            if (targetGraph == null || graph != null)
            {
                return false;
            }

            graph = targetGraph;
            BootStep = new PetCatalogBootStep(catalogConfig, services);
            return targetGraph.RegisterStep(BootStep);
        }
    }
}
