using Azure.Search.Documents.Models;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NCS.DSS.Contact.AzureSearchDataSyncTrigger
{
    public class ContactDataSyncTrigger
    {
        private readonly ILogger<ContactDataSyncTrigger> _log;

        public ContactDataSyncTrigger(ILogger<ContactDataSyncTrigger> log)
        {
            _log = log;
        }

        [Function("SyncDataForContactDetailsSearchTrigger")]
        public async Task Run(
            [CosmosDBTrigger("contacts", "contacts", ConnectionStringSetting = "ContactDetailsConnectionString",
                LeaseCollectionName = "contacts-leases", CreateLeaseCollectionIfNotExists = true)]
            IReadOnlyList<Document> documents)
        {
            _log.LogInformation("Entered SyncDataForContactDetailsSearchTrigger");

            SearchHelper.GetSearchServiceClient();

            _log.LogInformation("get search service client");

            var indexClient = SearchHelper.GetIndexClient();

            _log.LogInformation("get index client");
            
            _log.LogInformation("Documents modified " + documents.Count);

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
                    _log.LogInformation("attempting to merge docs to azure search");

                    await indexClient.IndexDocumentsAsync(batch);

                    _log.LogInformation("successfully merged docs to azure search");

                }
                catch (Exception e)
                {
                    _log.LogError("Failed to index some of the documents: {0}");
                    _log.LogError(e.ToString());
                }
            }
        }
    }
}