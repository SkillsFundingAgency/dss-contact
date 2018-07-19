using System;

namespace NCS.DSS.Contact.Cosmos.Helper
{
    public interface IResourceHelper
    {
        bool DoesCustomerExist(Guid customerId);
    }
}