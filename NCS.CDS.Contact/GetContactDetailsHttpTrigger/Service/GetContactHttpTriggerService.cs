using System;
using System.Threading.Tasks;
using NCS.DSS.Contact.Cosmos.Provider;
using NCS.DSS.Contact.Models;

namespace NCS.DSS.Contact.GetContactDetailsHttpTrigger.Service
{
    public class GetContactHttpTriggerService : IGetContactHttpTriggerService
    {
        public async Task<ContactDetails> GetContactDetailsForCustomerAsync(Guid customerGuid)
        {
            var documentDbProvider = new DocumentDBProvider();
            var contactdetail = await documentDbProvider.GetContactDetailForCustomerAsync(customerGuid);

            return contactdetail;
        }
    }
}
