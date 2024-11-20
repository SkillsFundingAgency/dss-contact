using System.Net;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Cosmos.Provider;
using NCS.DSS.Contact.Models;
using NCS.DSS.Contact.ServiceBus;

namespace NCS.DSS.Contact.PostContactDetailsHttpTrigger.Service
{
    public class PostContactDetailsHttpTriggerService : IPostContactDetailsHttpTriggerService
    {

        private readonly ILogger<PostContactDetailsHttpTriggerService> _logger;
        private readonly IDocumentDBProvider _documentDbProvider;

        public PostContactDetailsHttpTriggerService(IDocumentDBProvider documentDbProvider, ILogger<PostContactDetailsHttpTriggerService> logger)
        {
            _documentDbProvider = documentDbProvider;
            _logger = logger;
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

            return response.StatusCode == HttpStatusCode.Created ? (dynamic)response.Resource : null;
        }

        public async Task SendToServiceBusQueueAsync(ContactDetails contactdetails, string reqUrl)
        {
            await ServiceBusClient.SendPostMessageAsync(contactdetails, reqUrl);
        }
    }
}
