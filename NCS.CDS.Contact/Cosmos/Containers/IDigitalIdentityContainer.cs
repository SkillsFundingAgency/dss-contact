using Microsoft.Azure.Cosmos;

namespace NCS.DSS.Contact.Cosmos.Containers
{
    public interface IDigitalIdentityContainer
    {
        Container GetContainer();
    }
}
