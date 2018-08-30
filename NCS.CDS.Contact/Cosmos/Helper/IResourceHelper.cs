using System;
using System.Threading.Tasks;

namespace NCS.DSS.Contact.Cosmos.Helper
{
    public interface IResourceHelper
    {
        bool DoesCustomerExist(Guid customerId);
        Task<bool> IsCustomerReadOnly(Guid customerId);
    }
}