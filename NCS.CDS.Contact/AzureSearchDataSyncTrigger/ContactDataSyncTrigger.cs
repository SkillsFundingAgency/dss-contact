using Azure.Search.Documents.Models;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Helpers;

namespace NCS.DSS.Contact.AzureSearchDataSyncTrigger
{
    public class ContactDataSyncTrigger
    {
        private readonly ILogger<ContactDataSyncTrigger> _logger;

        public ContactDataSyncTrigger(ILogger<ContactDataSyncTrigger> logger)
        {
            _logger = logger;
        }

        [Function("SyncDataForContactDetailsSearchTrigger")]
        public async Task RunAsync(
            [CosmosDBTrigger("contacts", "contacts", Connection = "ContactDetailsConnectionString",
                LeaseContainerName = "contacts-leases", CreateLeaseContainerIfNotExists = true)]
            IReadOnlyList<Document> documents)
        {
            _logger.LogInformation("Entered SyncDataForContactDetailsSearchTrigger");

            SearchHelper.GetSearchServiceClient();

            _logger.LogInformation("get search service client");

            var indexClient = SearchHelper.GetIndexClient();

            _logger.LogInformation("get index client");

            _logger.LogInformation("Documents modified " + documents.Count);

            if (documents.Count > 0)
            {
                var contactDetails = documents.Select(doc => new
                {
                    CustomerId = doc.GetPropertyValue<Guid>("CustomerId"),
                    MobileNumber = doc.GetPropertyValue<string>("MobileNumber"),
                    HomeNumber = doc.GetPropertyValue<string>("HomeNumber"),
                    AlternativeNumber = doc.GetPropertyValue<string>("AlternativeNumber"),
                    EmailAddress = doc.GetPropertyValue<string>("EmailAddress")
                })
                    .ToList();

                var batch = IndexDocumentsBatch.MergeOrUpload(contactDetails);

                try
                {
                    _logger.LogInformation("attempting to merge docs to azure search");

                    await indexClient.IndexDocumentsAsync(batch);

                    _logger.LogInformation("successfully merged docs to azure search");

                }
                catch (Exception e)
                {
                    _logger.LogError("Failed to index some of the documents: {0}");
                    _logger.LogError(e.ToString());
                }
            }
        }
    }
}