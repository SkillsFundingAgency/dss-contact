using Microsoft.Extensions.Logging;

namespace NCS.DSS.Contact.GetContactDetailsByIdHttpTrigger.Service
{
    public interface IGetContactDetailsByIdHttpTriggerService
    {
        Task<Contact.Models.ContactDetails> GetContactDetailsForCustomerAsync(Guid customerId, Guid contactId, ILogger logger);
    }
}
