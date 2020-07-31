using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NCS.DSS.Contact.ServiceBus
{
    public static class ServiceBusClient
    {
        public static readonly string KeyName = ConfigurationManager.AppSettings["KeyName"];
        public static readonly string AccessKey = ConfigurationManager.AppSettings["AccessKey"];
        public static readonly string BaseAddress = ConfigurationManager.AppSettings["BaseAddress"];
        public static readonly string QueueName = ConfigurationManager.AppSettings["QueueName"];

        public static async Task SendPostMessageAsync(Models.ContactDetails contactDetails, string reqUrl)
        {
            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(KeyName, AccessKey);
            var messagingFactory = MessagingFactory.Create(BaseAddress, tokenProvider);
            var sender = messagingFactory.CreateMessageSender(QueueName);

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

            var msg = new BrokeredMessage(new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageModel))))
            {
                ContentType = "application/json",
                MessageId = contactDetails.CustomerId + " " + DateTime.UtcNow
            };

            //msg.ForcePersistence = true; Required when we save message to cosmos
            await sender.SendAsync(msg);
        }

        public static async Task SendPatchMessageAsync(Models.ContactDetails contactDetails, Guid customerId, string reqUrl)
        {
            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(KeyName, AccessKey);
            var messagingFactory = MessagingFactory.Create(BaseAddress, tokenProvider);
            var sender = messagingFactory.CreateMessageSender(QueueName);
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
                CurrentEmail = contactDetails.CurrentEmail

            };

            var msg = new BrokeredMessage(new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageModel))))
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

