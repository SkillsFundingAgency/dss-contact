using System;
using System.Net;
using System.Threading.Tasks;
using NCS.DSS.Contact.Cosmos.Provider;

namespace NCS.DSS.Contact.PostContactDetailsHttpTrigger.Service
{
    public class PostContactDetailsHttpTriggerService : IPostContactDetailsHttpTriggerService
    {
        public async Task<Contact.Models.ContactDetails> CreateAsync(Contact.Models.ContactDetails contactdetails)
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
