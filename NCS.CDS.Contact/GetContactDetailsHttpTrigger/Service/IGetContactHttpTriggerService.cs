using NCS.DSS.Contact.Models;

namespace NCS.DSS.Contact.GetContactDetailsHttpTrigger.Service
{
    public interface IGetContactHttpTriggerService
    {
        Task<ContactDetails> GetContactDetailsForCustomerAsync(Guid customerGuid);
    }
}