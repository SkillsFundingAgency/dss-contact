using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using NCS.DSS.ContactDetails.Cosmos.Client;
using NCS.DSS.ContactDetails.Cosmos.Helper;

namespace NCS.DSS.ContactDetails.Cosmos.Provider
{
    public class DocumentDBProvider : IDocumentDBProvider
    {
        private readonly DocumentDBHelper _documentDbHelper;
        private readonly DocumentDBClient _databaseClient;

        public DocumentDBProvider()
        {
            _documentDbHelper = new DocumentDBHelper();
            _databaseClient = new DocumentDBClient();
        }

        public bool DoesCustomerResourceExist(Guid customerId)
        {
            var collectionUri = _documentDbHelper.CreateCustomerDocumentCollectionUri();

            var client = _databaseClient.CreateDocumentClient();

            if (client == null)
                return false;

            var customerQuery = client.CreateDocumentQuery<Document>(collectionUri, new FeedOptions() { MaxItemCount = 1 });
            return customerQuery.Where(x => x.Id == customerId.ToString()).Select(x => x.Id).AsEnumerable().Any();
        }

        public async Task<ResourceResponse<Document>> GetcontactDetailsAsync(Guid contactDetailsId)
        {
            var documentUri = _documentDbHelper.CreateDocumentUri(contactDetailsId);

            var client = _databaseClient.CreateDocumentClient();

            if (client == null)
                return null;

            var response = await client.ReadDocumentAsync(documentUri);

            return response;
        }

        public async Task<Models.ContactDetails> GetContactDetailsForCustomerAsync(Guid customerId, Guid contactDetailsId)
        {
            var collectionUri = _documentDbHelper.CreateDocumentCollectionUri();

            var client = _databaseClient.CreateDocumentClient();

            var contactDetailsForCustomerQuery = client
                ?.CreateDocumentQuery<Models.ContactDetails>(collectionUri, new FeedOptions { MaxItemCount = 1 })
                .Where(x => x.CustomerId == customerId && x.ContactID == contactDetailsId)
                .AsDocumentQuery();

            if (contactDetailsForCustomerQuery == null)
                return null;

            var contactDetails = await contactDetailsForCustomerQuery.ExecuteNextAsync<Models.ContactDetails>();

            return contactDetails?.FirstOrDefault();
        }


        //public async Task<List<Models.ContactDetails>> GetcontactDetailsForCustomerAsync(Guid customerId)
        //{
        //    var collectionUri = _documentDbHelper.CreateDocumentCollectionUri();

        //    var client = _databaseClient.CreateDocumentClient();

        //    if (client == null)
        //        return null;

        //    var querycontactDetailses = client.CreateDocumentQuery<Models.ContactDetails>(collectionUri)
        //        .Where(so => so.CustomerId == customerId).AsDocumentQuery();

        //    var contactDetailses = new List<Models.ContactDetails>();

        //    while (querycontactDetailses.HasMoreResults)
        //    {
        //        var response = await querycontactDetailses.ExecuteNextAsync<Models.ContactDetails>();
        //        contactDetailses.AddRange(response);
        //    }

        //    return contactDetailses.Any() ? contactDetailses : null;

        //}

        public async Task<ResourceResponse<Document>> CreateContactDetailsAsync(Models.ContactDetails contactDetails)
        {

            var collectionUri = _documentDbHelper.CreateDocumentCollectionUri();

            var client = _databaseClient.CreateDocumentClient();

            if (client == null)
                return null;

            var response = await client.CreateDocumentAsync(collectionUri, contactDetails);

            return response;

        }

        public async Task<ResourceResponse<Document>> UpdateContactDetailsAsync(Models.ContactDetails contactDetails)
        {
            var documentUri = _documentDbHelper.CreateDocumentUri(contactDetails.ContactID.GetValueOrDefault());

            var client = _databaseClient.CreateDocumentClient();

            if (client == null)
                return null;

            var response = await client.ReplaceDocumentAsync(documentUri, contactDetails);

            return response;
        }
    }
}