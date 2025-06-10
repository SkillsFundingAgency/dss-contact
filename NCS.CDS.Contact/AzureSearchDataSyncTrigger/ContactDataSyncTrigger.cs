using Azure.Search.Documents.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Cosmos.Services;
using System.Text.Json;
using NCS.DSS.Contact.Models;

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
            IReadOnlyList<JsonDocument> documents)
        {
            _logger.LogInformation("Function {FunctionName} has been invoked", nameof(ContactDataSyncTrigger));

            try
            {
                var indexClient = _searchService.GetSearchClient();

                _logger.LogInformation("Processing {DocumentCount} documents", documents.Count);

                var contactDetails = new List<ContactDetailsSync>();

                foreach (var doc in documents)
                {
                    try
                    {
                        var contactDetail = JsonSerializer.Deserialize<ContactDetailsSync>(doc.RootElement.GetRawText());
                        if (contactDetail != null)
                        {
                            contactDetails.Add(contactDetail);
                        }
                    }
                    catch (JsonException e)
                    {
                        _logger.LogError(e, "Failed to process document: {Document}", doc);
                    }
                }

                if (contactDetails.Count > 0)
                {
                    var batch = IndexDocumentsBatch.MergeOrUpload(contactDetails);

                    _logger.LogInformation("Merging or uploading document batch for indexing with {DocumentCount} document(s)", contactDetails.Count);

                    try
                    {
                        _logger.LogInformation("Attempting to index {DocumentCount} document(s) to Azure Search", contactDetails.Count);
                        await indexClient.IndexDocumentsAsync(batch);
                        _logger.LogInformation("Successfully indexed {DocumentCount} document(s) to Azure Search", contactDetails.Count);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Failed to index batch with {DocumentCount} document(s). Exception: {ErrorMessage}", contactDetails.Count, e.Message);
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