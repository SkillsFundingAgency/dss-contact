using System;
using System.Net;
using System.Threading.Tasks;
using NCS.DSS.Contact.Cosmos.Provider;
using NCS.DSS.Contact.Models;
using NCS.DSS.Contact.ServiceBus;
using Microsoft.Extensions.Logging;

namespace NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Service
{
    public class PatchContactDetailsHttpTriggerService : IPatchContactDetailsHttpTriggerService
    {
        private readonly ILogger logger;
        private readonly IDocumentDBProvider _documentDbProvider;

        public PatchContactDetailsHttpTriggerService(IDocumentDBProvider documentDbProvider)
        {
            _documentDbProvider = documentDbProvider;
        }

        public PatchContactDetailsHttpTriggerService(ILogger logger)
        {
            this.logger = logger;
        }
        public async Task<ContactDetails> UpdateAsync(ContactDetails contactdetails, ContactDetailsPatch contactdetailsPatch)
        {
            if (contactdetails == null)
            {
                logger.LogInformation($"Contact details do not exist.");
                return null;
            }

            contactdetailsPatch.SetDefaultValues();
            contactdetails.Patch(contactdetailsPatch);

            var response = await _documentDbProvider.UpdateContactDetailsAsync(contactdetails);

            var responseStatusCode = response.StatusCode;

            return responseStatusCode == HttpStatusCode.OK ? contactdetails : null;
        }

        public async Task<ContactDetails> GetContactDetailsForCustomerAsync(Guid customerId, Guid contactId)
        {
            var contactdetails = await _documentDbProvider.GetContactDetailForCustomerAsync(customerId, contactId);

            return contactdetails;
        }

        public async Task SendToServiceBusQueueAsync(ContactDetails contactdetails, Guid customerId, string reqUrl)
        {
            await ServiceBusClient.SendPatchMessageAsync(contactdetails, customerId, reqUrl);
        }
    }
}
