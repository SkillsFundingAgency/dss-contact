using Microsoft.Azure.Cosmos;

namespace NCS.DSS.Contact.Cosmos.Containers
{
    public class ContactContainer : IContactContainer
    {
        private readonly Container _container;

        public ContactContainer(Container container)
        {
            _container = container;
        }

        public Container GetContainer() => _container;
    }
}
