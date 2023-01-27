using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using NCS.DSS.Contact.Helpers;
using NCS.DSS.Contact.Models;
using Azure.Search;
using Azure.Search.Documents.Models;
using Microsoft.Azure.Documents;
using Microsoft.Extensions.Logging;

namespace NCS.DSS.Contact.AzureSearchDataSyncTrigger
{
    public static class ContactDataSyncTrigger
    {
        [FunctionName("SyncDataForContactDetailsSearchTrigger")]
        public static async Task Run(
            [CosmosDBTrigger("contacts", "contacts", ConnectionStringSetting = "ContactDetailsConnectionString",
                LeaseCollectionName = "contacts-leases", CreateLeaseCollectionIfNotExists = true)]
            IReadOnlyList<Document> documents,
            ILogger log)
        {
            log.LogInformation("Entered SyncDataForContactDetailsSearchTrigger");

            SearchHelper.GetSearchServiceClient();

            log.LogInformation("get search service client");

            var indexClient = SearchHelper.GetIndexClient();

            log.LogInformation("get index client");
            
            log.LogInformation("Documents modified " + documents.Count);

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
                    log.LogInformation("attempting to merge docs to azure search");

                    await indexClient.IndexDocumentsAsync(batch);

                    log.LogInformation("successfully merged docs to azure search");

                }
                catch (Exception e)
                {
                    log.LogError("Failed to index some of the documents: {0}");
                    log.LogError(e.ToString());
                }
            }
        }
    }
}