using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Models;
using Newtonsoft.Json;
using System.Text;

namespace NCS.DSS.Contact.ServiceBus
{
    public class ServiceBusClient : IServiceBusClient
    {
        private readonly ServiceBusSender _serviceBusSender;
        private readonly ILogger<ServiceBusClient> _logger;

        public ServiceBusClient(Azure.Messaging.ServiceBus.ServiceBusClient serviceBusClient, ContactConfigurationSettings contactConfigurationSettings, ILogger<ServiceBusClient> logger)
        {
            _serviceBusSender = serviceBusClient.CreateSender(contactConfigurationSettings.QueueName);
            _logger = logger;
        }

        public async Task SendPostMessageAsync(ContactDetails contactDetails, string reqUrl)
        {
            _logger.LogInformation(
                "Starting {MethodName}. ContactDetailsId: {ContactDetailsId}. CustomerId: {CustomerId}",
                nameof(SendPostMessageAsync), contactDetails.ContactId, contactDetails.CustomerId);

            try
            {
                var messageModel = new
                {
                    TitleMessage = $"New Contact Details record [{contactDetails.ContactId}] added at {DateTime.UtcNow}",
                    CustomerGuid = contactDetails.CustomerId,
                    contactDetails.LastModifiedDate,
                    URL = $"{reqUrl}/{contactDetails.ContactId}",
                    IsNewCustomer = false,
                    TouchpointId = contactDetails.LastModifiedTouchpointId,
                    IsDigitalAccount = contactDetails.IsDigitalAccount ?? null
                };

                var msg = new ServiceBusMessage(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageModel)))
                {
                    ContentType = "application/json",
                    MessageId = contactDetails.CustomerId + " " + DateTime.UtcNow
                };

                await _serviceBusSender.SendMessageAsync(msg);

                _logger.LogInformation(
                    "Successfully completed {MethodName}. ContactDetailsId: {ContactDetailsId}. CustomerId: {CustomerId}",
                    nameof(SendPostMessageAsync), contactDetails.ContactId, contactDetails.CustomerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred in {MethodName}. ContactDetailsId: {ContactDetailsId}. CustomerId: {CustomerId}",
                    nameof(SendPostMessageAsync), contactDetails.ContactId, contactDetails.CustomerId);
                throw;
            }
        }

        public async Task SendPatchMessageAsync(ContactDetails contactDetails, Guid customerId, string reqUrl)
        {
            var messageModel = new
            {
                TitleMessage = $"Contact Details record modification for [{customerId}] at {DateTime.UtcNow}",
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

            var msg = new ServiceBusMessage(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageModel)))
            {
                ContentType = "application/json",
                MessageId = customerId + " " + DateTime.UtcNow
            };

            await _serviceBusSender.SendMessageAsync(msg);
        }
    }
}

