using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NCS.DSS.ContactDetails.Cosmos.Provider;
using NCS.DSS.ContactDetails.Models;

namespace NCS.DSS.ContactDetails.PatchContactDetailsHttpTrigger.Service
{
    public class PatchContactDetailsHttpTriggerService : IPatchContactDetailsHttpTriggerService
    {
        public async Task<Models.ContactDetails> UpdateAsync(Models.ContactDetails contactdetails, Models.ContactDetailsPatch contactdetailsPatch)
        {
            if (contactdetails == null)
                return null;

            contactdetails.Patch(contactdetailsPatch);

            var documentDbProvider = new DocumentDBProvider();
            var response = await documentDbProvider.UpdateContactDetailsAsync(contactdetails);

            var responseStatusCode = response.StatusCode;

            return responseStatusCode == HttpStatusCode.OK ? contactdetails : null;
        }

        public async Task<Models.ContactDetails> GetContactDetailsForCustomerAsync(Guid customerId, Guid contactId)
        {
            var documentDbProvider = new DocumentDBProvider();
            var contactdetails = await documentDbProvider.GetContactDetailsForCustomerAsync(customerId, contactId);

            return contactdetails;
        }

    }
}
