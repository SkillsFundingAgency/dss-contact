using System;
using System.Net;
using System.Threading.Tasks;
using NCS.DSS.Contact.Cosmos.Provider;
using NCS.DSS.Contact.Models;
using NCS.DSS.Contact.ServiceBus;

namespace NCS.DSS.Contact.PostContactDetailsHttpTrigger.Service
{
    public class PostContactDetailsHttpTriggerService : IPostContactDetailsHttpTriggerService
    {
        public bool DoesContactDetailsExistForCustomer(Guid customerId)
        {
            var documentDbProvider = new DocumentDBProvider();

            var doesContactDetailsExistForCustomer = documentDbProvider.DoesContactDetailsExistForCustomer(customerId);

            return doesContactDetailsExistForCustomer;
        }

        public async Task<ContactDetails> CreateAsync(ContactDetails contactdetails)
        {
            if (contactdetails == null)
                return null;

            contactdetails.SetDefaultValues();

            var documentDbProvider = new DocumentDBProvider();

            var response = await documentDbProvider.CreateContactDetailsAsync(contactdetails);

            return response.StatusCode == HttpStatusCode.Created ? (dynamic)response.Resource : (Guid?)null;
        }

        public async Task SendToServiceBusQueueAsync(ContactDetails contactdetails, string reqUrl)
        {
            await ServiceBusClient.SendPostMessageAsync(contactdetails, reqUrl);
        }
    }
}
