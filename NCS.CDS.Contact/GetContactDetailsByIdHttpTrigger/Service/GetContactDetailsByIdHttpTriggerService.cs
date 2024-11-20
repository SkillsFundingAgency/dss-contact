using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Cosmos.Provider;
using NCS.DSS.Contact.GetContactDetailsByIdHttpTrigger.Function;
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

        public async Task<ContactDetails> GetContactDetailsForCustomerAsync(Guid customerId, Guid contactId, ILogger<GetContactByIdHttpTrigger> logger)
        {
            logger.LogInformation("Starting to create Document Collection URI.");
            var contactdetails = await _documentDbProvider.GetContactDetailForCustomerAsync(customerId, contactId);

            return contactdetails;
        }
    }
}
