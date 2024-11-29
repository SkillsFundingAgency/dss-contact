using Microsoft.Azure.Cosmos;

namespace NCS.DSS.Contact.Cosmos.Containers
{
    public class DigitalIdentityContainer : IDigitalIdentityContainer
    {
        private readonly Container _container;

        public DigitalIdentityContainer(Container container)
        {
            _container = container;
        }

        public Container GetContainer() => _container;
    }
}
