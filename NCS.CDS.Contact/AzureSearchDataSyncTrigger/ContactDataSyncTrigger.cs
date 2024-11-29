using Azure.Search.Documents.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Cosmos.Services;
using Newtonsoft.Json.Linq;

namespace NCS.DSS.Contact.AzureSearchDataSyncTrigger
{
    public class ContactDataSyncTrigger
    {
        private readonly ILogger<ContactDataSyncTrigger> _logger;
        private readonly ISearchService _searchService;

        public ContactDataSyncTrigger(ILogger<ContactDataSyncTrigger> logger, ISearchService searchService)
        {
            _logger = logger;
            _searchService = searchService;
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

                _logger.LogInformation("Attempting to process {DocumentCount} document(s)", documents.Count);
                if (documents.Count > 0)
                {
                    var contactDetails = documents.Select(doc => new
                    {
                        CustomerId = doc["CustomerId"]?.ToObject<Guid>(),
                        MobileNumber = doc["MobileNumber"]?.ToString(),
                        HomeNumber = doc["HomeNumber"]?.ToString(),
                        AlternativeNumber = doc["AlternativeNumber"]?.ToString(),
                        EmailAddress = doc["EmailAddress"]?.ToString()
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
