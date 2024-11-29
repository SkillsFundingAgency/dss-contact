using Microsoft.Azure.Cosmos;

namespace NCS.DSS.Contact.Cosmos.Containers
{
    public interface ICustomerContainer
    {
        Container GetContainer();
    }
}
