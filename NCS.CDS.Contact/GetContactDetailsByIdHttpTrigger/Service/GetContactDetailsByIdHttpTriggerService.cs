using System;
using System.Threading.Tasks;
using NCS.DSS.Contact.Cosmos.Provider;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Models;

namespace NCS.DSS.Contact.GetContactDetailsByIdHttpTrigger.Service
{
    public class GetContactDetailsByIdHttpTriggerService : IGetContactDetailsByIdHttpTriggerService
    {
        private readonly IDocumentDBProvider _documentDbProvider;

        public GetContactDetailsByIdHttpTriggerService(IDocumentDBProvider documentDbProvider)
        {
            _documentDbProvider = documentDbProvider;
        }

        public async Task<Contact.Models.ContactDetails> GetContactDetailsForCustomerAsync(Guid customerId, Guid contactId, ILogger logger)
        {
            logger.LogInformation($"Starting to create Document Collection URI.");
            var contactdetails = await _documentDbProvider.GetContactDetailForCustomerAsync(customerId, contactId);

            return contactdetails;
        }

        public Task<ContactDetails> GetContactDetailsForCustomerAsync(Guid customerId, Guid contactId)
        {
            throw new NotImplementedException();
        }
    }
}
