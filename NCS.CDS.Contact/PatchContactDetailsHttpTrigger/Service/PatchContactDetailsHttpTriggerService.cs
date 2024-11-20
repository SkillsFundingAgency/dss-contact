﻿using System.Net;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Cosmos.Provider;
using NCS.DSS.Contact.Models;
using NCS.DSS.Contact.ServiceBus;

namespace NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Service
{
    public class PatchContactDetailsHttpTriggerService : IPatchContactDetailsHttpTriggerService
    {
        private readonly ILogger<PatchContactDetailsHttpTriggerService> _logger;
        private readonly IDocumentDBProvider _documentDbProvider;

        public PatchContactDetailsHttpTriggerService(IDocumentDBProvider documentDbProvider, ILogger<PatchContactDetailsHttpTriggerService> logger)
        {
            _documentDbProvider = documentDbProvider;
            _logger = logger;
        }

        public async Task<ContactDetails> UpdateAsync(ContactDetails contactdetails, ContactDetailsPatch contactdetailsPatch)
        {
            if (contactdetails == null)
            {
                _logger.LogInformation("Contact details do not exist.");
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
