using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using NCS.DSS.Contact.Cosmos.Client;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.Models;

namespace NCS.DSS.Contact.Cosmos.Provider
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

            var customerQuery = client.CreateDocumentQuery<Document>(collectionUri, new FeedOptions { MaxItemCount = 1 });
            return customerQuery.Where(x => x.Id == customerId.ToString()).Select(x => x.Id).AsEnumerable().Any();
        }

        public async Task<ContactDetails> GetContactDetailForCustomerAsync(Guid customerId)
        {
            var collectionUri = _documentDbHelper.CreateDocumentCollectionUri();

            var client = _databaseClient.CreateDocumentClient();

            var contactDetailsForCustomerQuery = client
                ?.CreateDocumentQuery<ContactDetails>(collectionUri, new FeedOptions { MaxItemCount = 1 })
                .Where(x => x.CustomerId == customerId)
                .AsDocumentQuery();

            if (contactDetailsForCustomerQuery == null)
                return null;

            var contactDetails = await contactDetailsForCustomerQuery.ExecuteNextAsync<ContactDetails>();

            return contactDetails?.FirstOrDefault();
        }

        public async Task<ContactDetails> GetContactDetailForCustomerAsync(Guid customerId, Guid contactDetailsId)
        {
            var collectionUri = _documentDbHelper.CreateDocumentCollectionUri();

            var client = _databaseClient.CreateDocumentClient();

            var contactDetailsForCustomerQuery = client
                ?.CreateDocumentQuery<ContactDetails>(collectionUri, new FeedOptions { MaxItemCount = 1 })
                .Where(x => x.CustomerId == customerId && x.ContactId == contactDetailsId)
                .AsDocumentQuery();

            if (contactDetailsForCustomerQuery == null)
                return null;

            var contactDetails = await contactDetailsForCustomerQuery.ExecuteNextAsync<ContactDetails>();

            return contactDetails?.FirstOrDefault();
        }

        public async Task<ResourceResponse<Document>> CreateContactDetailsAsync(ContactDetails contactDetails)
        {

            var collectionUri = _documentDbHelper.CreateDocumentCollectionUri();

            var client = _databaseClient.CreateDocumentClient();

            if (client == null)
                return null;

            var response = await client.CreateDocumentAsync(collectionUri, contactDetails);

            return response;

        }

        public async Task<ResourceResponse<Document>> UpdateContactDetailsAsync(ContactDetails contactDetails)
        {
            var documentUri = _documentDbHelper.CreateDocumentUri(contactDetails.ContactId.GetValueOrDefault());

            var client = _databaseClient.CreateDocumentClient();

            if (client == null)
                return null;

            var response = await client.ReplaceDocumentAsync(documentUri, contactDetails);

            return response;
        }
    }
}