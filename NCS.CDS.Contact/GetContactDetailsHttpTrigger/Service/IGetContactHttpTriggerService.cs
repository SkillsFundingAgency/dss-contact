using System;
using System.Threading.Tasks;

namespace NCS.DSS.Contact.GetContactDetailsHttpTrigger.Service
{
    public interface IGetContactHttpTriggerService
    {
        Task<Models.ContactDetails> GetContactDetailsForCustomerAsync(Guid customerGuid);
    }
}