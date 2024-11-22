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
            _logger.LogInformation("Function {FunctionName} has been invoked", nameof(ContactDataSyncTrigger));

            try
            {
                _logger.LogInformation("Initializing search service client");
                SearchHelper.GetSearchServiceClient();

                _logger.LogInformation("Retrieving index client.");
                var indexClient = SearchHelper.GetIndexClient();

                _logger.LogInformation("Attempting to process {DocumentCount} document(s)", documents.Count);
                if (documents.Count > 0)
                {
                    var contactDetails = documents.Select(doc => new
                    {
                        CustomerId = doc.GetPropertyValue<Guid>("CustomerId"),
                        MobileNumber = doc.GetPropertyValue<string>("MobileNumber"),
                        HomeNumber = doc.GetPropertyValue<string>("HomeNumber"),
                        AlternativeNumber = doc.GetPropertyValue<string>("AlternativeNumber"),
                        EmailAddress = doc.GetPropertyValue<string>("EmailAddress")
                    }).ToList();

                    var batch = IndexDocumentsBatch.MergeOrUpload(contactDetails);

                    _logger.LogInformation("Merging or uploading document batch for indexing with {DocumentCount} document(s)", contactDetails.Count);

                    try
                    {
                        _logger.LogInformation("Attempting to index documents to Azure Search");
                        await indexClient.IndexDocumentsAsync(batch);
                        _logger.LogInformation("Successfully indexed documents to Azure Search");

                        _logger.LogInformation("Function {FunctionName} has finished invoking", nameof(ContactDataSyncTrigger));
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error occurred while indexing documents to Azure Search. Exception: {ErrorMessage}", e.Message);
                    }
                }
                else
                {
                    _logger.LogInformation("No documents to process.");
                    _logger.LogInformation("Function {FunctionName} has finished invoking", nameof(ContactDataSyncTrigger));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred in {FunctionName}. Exception: {ErrorMessage}", nameof(ContactDataSyncTrigger), ex.Message);
                throw;
            }
        }
    }
}
