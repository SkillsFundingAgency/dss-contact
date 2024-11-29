using Microsoft.Azure.Cosmos;

namespace NCS.DSS.Contact.Cosmos.Containers
{
    public interface IContactContainer
    {
        Container GetContainer();
    }
}
