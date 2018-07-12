using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NCS.DSS.ContactDetails.Cosmos.Provider;
using NCS.DSS.ContactDetails.Models;

namespace NCS.DSS.ContactDetails.PostContactDetailsHttpTrigger.Service
{
    public class PostContactDetailsHttpTriggerService : IPostContactDetailsHttpTriggerService
    {
        public async Task<Models.ContactDetails> CreateContactDetails(Models.ContactDetails contactdetails)
        {
            if (contactdetails == null)
                return null;

            var contactdetailsId = Guid.NewGuid();
            contactdetails.ContactID = contactdetailsId;

            var documentDbProvider = new DocumentDBProvider();

            var response = await documentDbProvider.CreateContactDetailsAsync(contactdetails);

            return response.StatusCode == HttpStatusCode.Created ? (dynamic)response.Resource : (Guid?)null;
        }
    }
}
