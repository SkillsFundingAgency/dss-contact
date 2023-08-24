using System;
using System.Threading.Tasks;
using NCS.DSS.Contact.Cosmos.Provider;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Models;

namespace NCS.DSS.Contact.GetContactDetailsByIdHttpTrigger.Service
{
    public class GetContactDetailsByIdHttpTriggerService : IGetContactDetailsByIdHttpTriggerService
    {
        public async Task<Contact.Models.ContactDetails> GetContactDetailsForCustomerAsync(Guid customerId, Guid contactId, ILogger logger)
        {
            logger.LogInformation($"Starting to create Document Collection URI.");
            var documentDbProvider = new DocumentDBProvider();
            var contactdetails = await documentDbProvider.GetContactDetailForCustomerAsync(customerId, contactId);

            return contactdetails;
        }

        public Task<ContactDetails> GetContactDetailsForCustomerAsync(Guid customerId, Guid contactId)
        {
            throw new NotImplementedException();
        }
    }
}
