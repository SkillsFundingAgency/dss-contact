using System.Net;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Cosmos.Provider;
using NCS.DSS.Contact.Models;
using NCS.DSS.Contact.ServiceBus;

namespace NCS.DSS.Contact.PostContactDetailsHttpTrigger.Service
{
    public class PostContactDetailsHttpTriggerService : IPostContactDetailsHttpTriggerService
    {

        private readonly ICosmosDBProvider _documentDbProvider;
        private readonly IServiceBusClient _serviceBusClient;
        private readonly ILogger<PostContactDetailsHttpTriggerService> _logger;


        public PostContactDetailsHttpTriggerService(ICosmosDBProvider documentDbProvider, IServiceBusClient serviceBusClient, ILogger<PostContactDetailsHttpTriggerService> logger)
        {
            _documentDbProvider = documentDbProvider;
            _serviceBusClient = serviceBusClient;
            _logger = logger;
        }

        public async Task<bool> DoesContactDetailsExistForCustomer(Guid customerId)
        {
            var doesContactDetailsExistForCustomer = await _documentDbProvider.DoesContactDetailsExistForCustomer(customerId);

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
            await _serviceBusClient.SendPostMessageAsync(contactdetails, reqUrl);
        }
    }
}
