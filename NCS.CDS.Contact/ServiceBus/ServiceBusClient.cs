using Microsoft.Azure.ServiceBus;
using NCS.DSS.Contact.Models;
using Newtonsoft.Json;
using System.Text;

namespace NCS.DSS.Contact.ServiceBus
{
    public static class ServiceBusClient
    {
        public static readonly string AccessKey = Environment.GetEnvironmentVariable("AccessKey");
        public static readonly string BaseAddress = Environment.GetEnvironmentVariable("BaseAddress");
        public static readonly string QueueName = Environment.GetEnvironmentVariable("QueueName");
        public static string Connectionstring = $"Endpoint={BaseAddress};SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey={AccessKey}";

        public static async Task SendPostMessageAsync(ContactDetails contactDetails, string reqUrl)
        {
            var sender = new QueueClient(Connectionstring, QueueName);

            var messageModel = new
            {
                TitleMessage = "New Contact Details record {" + contactDetails.ContactId + "} added at " + DateTime.UtcNow,
                CustomerGuid = contactDetails.CustomerId,
                contactDetails.LastModifiedDate,
                URL = reqUrl + "/" + contactDetails.ContactId,
                IsNewCustomer = false,
                TouchpointId = contactDetails.LastModifiedTouchpointId,
                IsDigitalAccount = contactDetails.IsDigitalAccount ?? null
            };


            var msg = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageModel)))
            {
                ContentType = "application/json",
                MessageId = contactDetails.CustomerId + " " + DateTime.UtcNow
            };

            //msg.ForcePersistence = true; Required when we save message to cosmos
            await sender.SendAsync(msg);
        }

        public static async Task SendPatchMessageAsync(ContactDetails contactDetails, Guid customerId, string reqUrl)
        {
            var sender = new QueueClient(Connectionstring, QueueName);
            var messageModel = new
            {
                TitleMessage = "Contact Details record modification for {" + customerId + "} at " + DateTime.UtcNow,
                CustomerGuid = customerId,
                contactDetails.LastModifiedDate,
                URL = reqUrl,
                IsNewCustomer = false,
                TouchpointId = contactDetails.LastModifiedTouchpointId,
                contactDetails.FirstName,
                contactDetails.LastName,
                ChangeEmailAddress = contactDetails.ChangeEmailAddress ?? null,
                IsDigitalAccount = contactDetails.IsDigitalAccount ?? null,
                contactDetails.NewEmail,
                contactDetails.CurrentEmail,
                contactDetails.IdentityStoreId

            };

            var msg = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageModel)))
            {
                ContentType = "application/json",
                MessageId = customerId + " " + DateTime.UtcNow
            };

            //msg.ForcePersistence = true; Required when we save message to cosmos
            await sender.SendAsync(msg);
        }

    }
}

