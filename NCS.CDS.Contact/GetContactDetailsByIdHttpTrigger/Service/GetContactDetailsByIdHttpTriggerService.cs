using System;
using System.Threading.Tasks;
using NCS.DSS.Contact.Cosmos.Provider;

namespace NCS.DSS.Contact.GetContactDetailsByIdHttpTrigger.Service
{
    public class GetContactDetailsByIdHttpTriggerService : IGetContactDetailsByIdHttpTriggerService
    {
        public async Task<Contact.Models.ContactDetails> GetContactDetailsForCustomerAsync(Guid customerId, Guid contactId)
        {
            var documentDbProvider = new DocumentDBProvider();
            var contactdetails = await documentDbProvider.GetContactDetailForCustomerAsync(customerId, contactId);

            return contactdetails;
        }
    }
}
