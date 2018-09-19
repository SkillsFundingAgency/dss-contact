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
        public bool DoesCustomerResourceExist(Guid customerId)
        {
            var collectionUri = DocumentDBHelper.CreateCustomerDocumentCollectionUri();

            var client = DocumentDBClient.CreateDocumentClient();

            if (client == null)
                return false;

            var customerQuery = client.CreateDocumentQuery<Document>(collectionUri, new FeedOptions { MaxItemCount = 1 });
            return customerQuery.Where(x => x.Id == customerId.ToString()).AsEnumerable().Any();
        }

        public async Task<bool> DoesCustomerHaveATerminationDate(Guid customerId)
        {
            var collectionUri = DocumentDBHelper.CreateCustomerDocumentCollectionUri();

            var client = DocumentDBClient.CreateDocumentClient();

            var customerByIdQuery = client
                ?.CreateDocumentQuery<Document>(collectionUri, new FeedOptions { MaxItemCount = 1 })
                .Where(x => x.Id == customerId.ToString())
                .AsDocumentQuery();

            if (customerByIdQuery == null)
                return false;

            var customerQuery = await customerByIdQuery.ExecuteNextAsync<Document>();

            var customer = customerQuery?.FirstOrDefault();

            if (customer == null)
                return false;

            var dateOfTermination = customer.GetPropertyValue<DateTime?>("DateOfTermination");

            return dateOfTermination.HasValue;
        }

        public bool DoesContactDetailsExistForCustomer(Guid customerId)
        {
            var collectionUri = DocumentDBHelper.CreateDocumentCollectionUri();

            var client = DocumentDBClient.CreateDocumentClient();

            if (client == null)
                return false;

            var contactDetailsForCustomerQuery = client.CreateDocumentQuery<ContactDetails>(collectionUri, new FeedOptions { MaxItemCount = 1 });
            return contactDetailsForCustomerQuery.Where(x => x.CustomerId == customerId).AsEnumerable().Any();
        }

        public async Task<ContactDetails> GetContactDetailForCustomerAsync(Guid customerId)
        {
            var collectionUri = DocumentDBHelper.CreateDocumentCollectionUri();

            var client = DocumentDBClient.CreateDocumentClient();

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
            var collectionUri = DocumentDBHelper.CreateDocumentCollectionUri();

            var client = DocumentDBClient.CreateDocumentClient();

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

            var collectionUri = DocumentDBHelper.CreateDocumentCollectionUri();

            var client = DocumentDBClient.CreateDocumentClient();

            if (client == null)
                return null;

            var response = await client.CreateDocumentAsync(collectionUri, contactDetails);

            return response;

        }

        public async Task<ResourceResponse<Document>> UpdateContactDetailsAsync(ContactDetails contactDetails)
        {
            var documentUri = DocumentDBHelper.CreateDocumentUri(contactDetails.ContactId.GetValueOrDefault());

            var client = DocumentDBClient.CreateDocumentClient();

            if (client == null)
                return null;

            var response = await client.ReplaceDocumentAsync(documentUri, contactDetails);

            return response;
        }

       
    }
}