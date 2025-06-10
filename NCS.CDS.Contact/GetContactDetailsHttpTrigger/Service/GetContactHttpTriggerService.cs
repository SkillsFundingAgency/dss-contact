using NCS.DSS.Contact.Cosmos.Provider;
using NCS.DSS.Contact.Models;

namespace NCS.DSS.Contact.GetContactDetailsHttpTrigger.Service
{
    public class GetContactHttpTriggerService : IGetContactHttpTriggerService
    {
        private readonly ICosmosDBProvider _documentDbProvider;

        public GetContactHttpTriggerService(ICosmosDBProvider documentDbProvider)
        {
            _documentDbProvider = documentDbProvider;
        }

        public async Task<ContactDetails> GetContactDetailsForCustomerAsync(Guid customerGuid)
        {
            var contactdetail = await _documentDbProvider.GetContactDetailForCustomerAsync(customerGuid);

            return contactdetail;
        }
    }
}
