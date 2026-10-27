using CozyGachapon.Contracts;
using CozyGachapon.Core.Inventory;
using CozyGachapon.Foundation;

namespace CozyGachapon.Bootstrap
{
    public sealed class InventoryWiring
    {
        private readonly InventoryConfig config;
        private readonly IServiceRegistry services;
        private BootstrapGraph graph;

        public InventoryBootStep BootStep { get; private set; }
        public bool IsRegistered => graph != null;

        public InventoryWiring(InventoryConfig config, IServiceRegistry services)
        {
            this.config = config;
            this.services = services;
        }

        public bool Register(BootstrapGraph targetGraph)
        {
            if (targetGraph == null || graph != null)
            {
                return false;
            }

            graph = targetGraph;
            BootStep = new InventoryBootStep(config, services);
            return targetGraph.RegisterStep(BootStep);
        }
    }
}
