using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using NCS.DSS.Contact.Helpers;
using NCS.DSS.Contact.Models;
using Document = Microsoft.Azure.Documents.Document;

namespace NCS.DSS.Contact.AzureSearchDataSyncTrigger
{
    public static class ContactDataSyncTrigger
    {
        [FunctionName("SyncDataForContactDetailsSearchTrigger")]
        public static async Task Run(
            [CosmosDBTrigger("contacts", "contacts", ConnectionStringSetting = "ContactDetailsConnectionString",
                LeaseCollectionName = "contacts-leases", CreateLeaseCollectionIfNotExists = true)]
            IReadOnlyList<Document> documents,
            TraceWriter log)
        {
            log.Info("Entered SyncDataForContactDetailsSearchTrigger");

            SearchHelper.GetSearchServiceClient();

            log.Info("get search service client");

            var indexClient = SearchHelper.GetIndexClient();

            log.Info("get index client");
            
            log.Info("Documents modified " + documents.Count);

            if (documents.Count > 0)
            {
                var contactDetails = documents.Select(doc => new ContactDetails
                    {
                        CustomerId = doc.GetPropertyValue<Guid>("CustomerId"),
                        MobileNumber = doc.GetPropertyValue<string>("MobileNumber"),
                        HomeNumber = doc.GetPropertyValue<string>("HomeNumber"),
                        AlternativeNumber = doc.GetPropertyValue<string>("AlternativeNumber"),
                        EmailAddress = doc.GetPropertyValue<string>("EmailAddress")
                    })
                    .ToList();

                var batch = IndexBatch.MergeOrUpload(contactDetails);
                
                try
                {
                    log.Info("attempting to merge docs to azure search");

                    await indexClient.Documents.IndexAsync(batch);

                    log.Info("successfully merged docs to azure search");

                }
                catch (IndexBatchException e)
                {
                    log.Error(string.Format("Failed to index some of the documents: {0}", 
                        string.Join(", ", e.IndexingResults.Where(r => !r.Succeeded).Select(r => r.Key))));

                    log.Error(e.ToString());
                }
            }
        }
    }
}