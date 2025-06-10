using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.GetContactDetailsByIdHttpTrigger.Function;
using NCS.DSS.Contact.Models;

namespace NCS.DSS.Contact.GetContactDetailsByIdHttpTrigger.Service
{
    public interface IGetContactDetailsByIdHttpTriggerService
    {
        Task<ContactDetails> GetContactDetailsForCustomerAsync(Guid customerId, Guid contactId, ILogger<GetContactByIdHttpTrigger> logger);
    }
}
