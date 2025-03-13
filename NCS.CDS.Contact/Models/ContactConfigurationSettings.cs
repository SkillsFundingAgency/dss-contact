namespace NCS.DSS.Contact.Models
{
    public class ContactConfigurationSettings
    {
        public required string CosmosDbEndpoint { get; set; }
        public required string Key { get; set; }
        public required string KeyName { get; set; }
        public required string AccessKey { get; set; }
        public required string BaseAddress { get; set; }
        public required string QueueName { get; set; }
        public required string ContactDetailsConnectionString { get; set; }
        public required string ServiceBusConnectionString { get; set; } = string.Empty;
        public required string DatabaseId { get; set; }
        public required string CollectionId { get; set; }
        public required string CustomerDatabaseId { get; set; }
        public required string CustomerCollectionId { get; set; }
        public required string DigitalIdentityDatabaseId { get; set; }
        public required string DigitalIdentityCollectionId { get; set; }
        public required string CustomerSearchIndexName { get; set; }
        public required string SearchServiceAdminApiKey { get; set; }
        public required string SearchServiceName { get; set; }
    }
}