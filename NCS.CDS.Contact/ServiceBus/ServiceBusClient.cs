using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace NCS.DSS.Contact.ServiceBus
{
    public static class ServiceBusClient
    {
        public static readonly string KeyName = Environment.GetEnvironmentVariable("KeyName");
        public static readonly string AccessKey = Environment.GetEnvironmentVariable("AccessKey");
        public static readonly string BaseAddress = Environment.GetEnvironmentVariable("BaseAddress");
        public static readonly string QueueName = Environment.GetEnvironmentVariable("QueueName");

        public static async Task SendPostMessageAsync(Models.ContactDetails contactDetails, string reqUrl)
        {
            var sbcsb = new ServiceBusConnectionStringBuilder(BaseAddress, QueueName, AccessKey);
            var sender = new QueueClient(sbcsb.GetEntityConnectionString(), QueueName);

            var messageModel = new
            {
                TitleMessage = "New Contact Details record {" + contactDetails.ContactId + "} added at " + DateTime.UtcNow,
                CustomerGuid = contactDetails.CustomerId,
                LastModifiedDate = contactDetails.LastModifiedDate,
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

        public static async Task SendPatchMessageAsync(Models.ContactDetails contactDetails, Guid customerId, string reqUrl)
        {
            var sbcsb = new ServiceBusConnectionStringBuilder(BaseAddress, QueueName, AccessKey);
            var sender = new QueueClient(sbcsb.GetEntityConnectionString(), QueueName);
            var messageModel = new
            {
                TitleMessage = "Contact Details record modification for {" + customerId + "} at " + DateTime.UtcNow,
                CustomerGuid = customerId,
                LastModifiedDate = contactDetails.LastModifiedDate,
                URL = reqUrl,
                IsNewCustomer = false,
                TouchpointId = contactDetails.LastModifiedTouchpointId,
                FirstName = contactDetails.FirstName,
                LastName = contactDetails.LastName,
                ChangeEmailAddress = contactDetails.ChangeEmailAddress ?? null,
                IsDigitalAccount = contactDetails.IsDigitalAccount ?? null,
                NewEmail = contactDetails.NewEmail,
                CurrentEmail = contactDetails.CurrentEmail,
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

    public class MessageModel
    {
        public string TitleMessage { get; set; }
        public Guid? CustomerGuid { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public string URL { get; set; }
        public bool IsNewCustomer { get; set; }
        public string TouchpointId { get; set; }
        public bool? IsDigitalAccount { get; set; }
        public string NewEmail { get; set; }
        public string CurrentEmail { get; set; }
    }

}

