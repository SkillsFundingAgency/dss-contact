using Azure.Search.Documents.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Cosmos.Services;
using Newtonsoft.Json.Linq;

namespace NCS.DSS.Contact.AzureSearchDataSyncTrigger
{
    public class ContactDataSyncTrigger
    {
        private readonly ISearchService _searchService;
        private readonly ILogger<ContactDataSyncTrigger> _logger;

        public ContactDataSyncTrigger(ISearchService searchService, ILogger<ContactDataSyncTrigger> logger)
        {
            _searchService = searchService;
            _logger = logger;
        }

        [Function("SyncDataForContactDetailsSearchTrigger")]
        public async Task RunAsync(
        [CosmosDBTrigger("contacts", "contacts", Connection = "ContactDetailsConnectionString",
            LeaseContainerName = "contacts-leases", CreateLeaseContainerIfNotExists = true)]
        IReadOnlyList<JObject> documents)
        {
            _logger.LogInformation("Function {FunctionName} has been invoked", nameof(ContactDataSyncTrigger));

            try
            {
                var indexClient = _searchService.GetSearchClient();

                _logger.LogInformation("Processing {DocumentCount} documents", documents.Count);

                var contactDetails = documents
                    .Select(doc => new
                    {
                        CustomerId = doc["CustomerId"]?.ToObject<Guid>(),
                        MobileNumber = doc["MobileNumber"]?.ToObject<string>(),
                        HomeNumber = doc["HomeNumber"]?.ToObject<string>(),
                        AlternativeNumber = doc["AlternativeNumber"]?.ToObject<string>(),
                        EmailAddress = doc["EmailAddress"]?.ToObject<string>()
                    })
                    .Where(x => x.CustomerId != null)
                    .ToList();

                if (contactDetails.Count > 0)
                {
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
                    _logger.LogInformation("No valid documents to process.");
                }

                _logger.LogInformation("Function {FunctionName} has finished invoking", nameof(ContactDataSyncTrigger));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred in {FunctionName}. Exception: {ErrorMessage}", nameof(ContactDataSyncTrigger), ex.Message);
                throw;
            }
        }
    }
}
