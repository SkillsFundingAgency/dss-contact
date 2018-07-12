using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NCS.DSS.ContactDetails.Cosmos.Provider;
using NCS.DSS.ContactDetails.Models;

namespace NCS.DSS.ContactDetails.GetContactDetailsByIdHttpTrigger.Service
{
    public class GetContactDetailsByIdHttpTriggerService : IGetContactDetailsByIdHttpTriggerService
    {
        public async Task<Models.ContactDetails> GetContactDetailsForCustomerAsync(Guid customerId, Guid contactId)
        {
            var documentDbProvider = new DocumentDBProvider();
            var address = await documentDbProvider.GetContactDetailsForCustomerAsync(customerId, contactId);

            return address;
        }
    }
}
