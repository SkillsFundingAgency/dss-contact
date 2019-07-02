using System;
using System.Threading.Tasks;
using NCS.DSS.Contact.Cosmos.Provider;
using NCS.DSS.Contact.Models;

namespace NCS.DSS.Contact.GetContactDetailsHttpTrigger.Service
{
    public class GetContactHttpTriggerService : IGetContactHttpTriggerService
    {
        private readonly IDocumentDBProvider _documentDbProvider;

        public async Task<ContactDetails> GetContactDetailsForCustomerAsync(Guid customerGuid)
        {
            var contactdetail = await _documentDbProvider.GetContactDetailForCustomerAsync(customerGuid);

            return contactdetail;
        }
    }
}
