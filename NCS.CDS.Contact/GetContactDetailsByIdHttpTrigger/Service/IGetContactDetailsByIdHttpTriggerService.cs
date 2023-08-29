using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace NCS.DSS.Contact.GetContactDetailsByIdHttpTrigger.Service
{
    public interface IGetContactDetailsByIdHttpTriggerService
    {
        Task<Contact.Models.ContactDetails> GetContactDetailsForCustomerAsync(Guid customerId, Guid contactId, ILogger logger);
    }
}
