using System;
using System.Net;
using System.Threading.Tasks;
using NCS.DSS.Contact.Cosmos.Provider;
using NCS.DSS.Contact.Models;
using NCS.DSS.Contact.ServiceBus;
using Microsoft.Extensions.Logging;

namespace NCS.DSS.Contact.PostContactDetailsHttpTrigger.Service
{
    public class PostContactDetailsHttpTriggerService : IPostContactDetailsHttpTriggerService
    {

        private readonly ILogger logger;
        private readonly IDocumentDBProvider _documentDbProvider;

        public PostContactDetailsHttpTriggerService(IDocumentDBProvider documentDbProvider)
        {
            _documentDbProvider = documentDbProvider;
        }

        public PostContactDetailsHttpTriggerService(ILogger logger)
        {
            this.logger = logger;
        }
        public bool DoesContactDetailsExistForCustomer(Guid customerId)
        {
            var doesContactDetailsExistForCustomer = _documentDbProvider.DoesContactDetailsExistForCustomer(customerId);

            return doesContactDetailsExistForCustomer;
        }

        public async Task<ContactDetails> CreateAsync(ContactDetails contactdetails)
        {
            if (contactdetails == null)
                return null;

            contactdetails.SetDefaultValues();

            var response = await _documentDbProvider.CreateContactDetailsAsync(contactdetails);

            return response.StatusCode == HttpStatusCode.Created ? (dynamic)response.Resource : (Guid?)null;
        }

        public async Task SendToServiceBusQueueAsync(ContactDetails contactdetails, string reqUrl)
        {
            await ServiceBusClient.SendPostMessageAsync(contactdetails, reqUrl);
        }
    }
}
