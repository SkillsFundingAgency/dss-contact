using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Cosmos.Client;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.Models;

namespace NCS.DSS.Contact.Cosmos.Provider
{
    public class DocumentDBProvider : IDocumentDBProvider
    {
        private readonly ILogger<DocumentDBProvider> _logger;

        public DocumentDBProvider(ILogger<DocumentDBProvider> logger)
        {
            _logger = logger;
        }

        public async Task<bool> DoesCustomerResourceExist(Guid customerId)
        {
            var documentUri = DocumentDBHelper.CreateCustomerDocumentUri(customerId);

            var client = DocumentDBClient.CreateDocumentClient();

            if (client == null)
            {
                _logger.LogInformation("No client exists.");
                return false;
            }

            try
            {
                var response = await client.ReadDocumentAsync(documentUri);
                if (response.Resource != null)
                    return true;
            }
            catch (DocumentClientException)
            {
                _logger.LogError("Error: DocumentClientException caught.");
                return false;
            }

            return false;
        }

        public async Task<bool> DoesCustomerHaveATerminationDate(Guid customerId)
        {
            var documentUri = DocumentDBHelper.CreateCustomerDocumentUri(customerId);

            var client = DocumentDBClient.CreateDocumentClient();

            if (client == null)
            {
                _logger.LogInformation("No client exists.");
                return false;
            }

            try
            {
                var response = await client.ReadDocumentAsync(documentUri);

                var dateOfTermination = response.Resource?.GetPropertyValue<DateTime?>("DateOfTermination");

                return dateOfTermination.HasValue;
            }
            catch (DocumentClientException)
            {
                _logger.LogError("Error: DocumentClientException caught.");
                return false;
            }
        }

        public bool DoesContactDetailsExistForCustomer(Guid customerId)
        {
            var collectionUri = DocumentDBHelper.CreateDocumentCollectionUri(_logger);

            var client = DocumentDBClient.CreateDocumentClient();

            if (client == null)
            {
                _logger.LogInformation("No client exists.");
                return false;
            }

            var contactDetailsForCustomerQuery = client.CreateDocumentQuery<ContactDetails>(collectionUri, new FeedOptions { MaxItemCount = 1 });
            return contactDetailsForCustomerQuery.Where(x => x.CustomerId == customerId).AsEnumerable().Any();
        }

        public async Task<ContactDetails> GetContactDetailForCustomerAsync(Guid customerId)
        {
            var collectionUri = DocumentDBHelper.CreateDocumentCollectionUri(_logger);

            var client = DocumentDBClient.CreateDocumentClient();

            var contactDetailsForCustomerQuery = client
                ?.CreateDocumentQuery<ContactDetails>(collectionUri, new FeedOptions { MaxItemCount = 1 })
                .Where(x => x.CustomerId == customerId)
                .AsDocumentQuery();

            if (contactDetailsForCustomerQuery == null)
            {
                _logger.LogInformation($"No contact exists with CustomerId [{customerId}]");
                return null;
            }

            var contactDetails = await contactDetailsForCustomerQuery.ExecuteNextAsync<ContactDetails>();

            return contactDetails?.FirstOrDefault();
        }

        public async Task<ContactDetails> GetContactDetailForCustomerAsync(Guid customerId, Guid contactDetailsId)
        {
            var collectionUri = DocumentDBHelper.CreateDocumentCollectionUri(_logger);

            var client = DocumentDBClient.CreateDocumentClient();

            var contactDetailsForCustomerQuery = client
                ?.CreateDocumentQuery<ContactDetails>(collectionUri, new FeedOptions { MaxItemCount = 1 })
                .Where(x => x.CustomerId == customerId && x.ContactId == contactDetailsId)
                .AsDocumentQuery();

            if (contactDetailsForCustomerQuery == null)
            {
                _logger.LogInformation($"No contact exists with both CustomerId [{customerId}] and ContactDetailsId [{contactDetailsId}]");
                return null;
            }

            var contactDetails = await contactDetailsForCustomerQuery.ExecuteNextAsync<ContactDetails>();

            return contactDetails?.FirstOrDefault();
        }

        public async Task<ResourceResponse<Document>> CreateContactDetailsAsync(ContactDetails contactDetails)
        {

            var collectionUri = DocumentDBHelper.CreateDocumentCollectionUri(_logger);

            var client = DocumentDBClient.CreateDocumentClient();

            if (client == null)
            {
                _logger.LogInformation("No client exists.");
                return null;
            }

            var response = await client.CreateDocumentAsync(collectionUri, contactDetails);

            return response;

        }

        public async Task<ResourceResponse<Document>> UpdateContactDetailsAsync(ContactDetails contactDetails)
        {
            var documentUri = DocumentDBHelper.CreateDocumentUri(contactDetails.ContactId.GetValueOrDefault(), _logger);

            var client = DocumentDBClient.CreateDocumentClient();

            if (client == null)
            {
                _logger.LogInformation("No client exists.");
                return null;
            }

            var response = await client.ReplaceDocumentAsync(documentUri, contactDetails);

            return response;
        }

        public async Task<bool> DoesContactDetailsWithEmailExists(string emailAddressToCheck)
        {
            var collectionUri = DocumentDBHelper.CreateDocumentCollectionUri(_logger);
            var client = DocumentDBClient.CreateDocumentClient();
            var contactDetailsForEmailQuery = client
                ?.CreateDocumentQuery<ContactDetails>(collectionUri, new FeedOptions { MaxItemCount = 1 })
                .Where(x => x.EmailAddress == emailAddressToCheck)
                .AsDocumentQuery();
            if (contactDetailsForEmailQuery == null)
            {
                _logger.LogInformation($"No contact exists with email address [{emailAddressToCheck}]");
                return false;
            }

            var contactDetails = await contactDetailsForEmailQuery.ExecuteNextAsync<ContactDetails>();
            return contactDetails.Any();
        }

        public async Task<bool> DoesContactDetailsWithEmailExistsForAnotherCustomer(string email, Guid CustomerId)
        {
            var collectionUri = DocumentDBHelper.CreateDocumentCollectionUri(_logger);
            var client = DocumentDBClient.CreateDocumentClient();
            var contactDetailsForEmailQuery = client
                ?.CreateDocumentQuery<ContactDetails>(collectionUri, new FeedOptions { MaxItemCount = 1, EnableCrossPartitionQuery = true })
                .Where(x => x.EmailAddress == email && x.CustomerId != CustomerId)
                .AsDocumentQuery();
            if (contactDetailsForEmailQuery == null)
            {
                _logger.LogInformation($"No contact exists with both CustomerId [{CustomerId}] and email address [{email}]");
                return false;
            }

            var contactDetails = await contactDetailsForEmailQuery.ExecuteNextAsync<ContactDetails>();
            return contactDetails.Any();
        }

        public async Task<IList<ContactDetails>> GetContactsByEmail(string emailAddressToCheck)
        {
            var collectionUri = DocumentDBHelper.CreateDocumentCollectionUri(_logger);
            var client = DocumentDBClient.CreateDocumentClient();
            var contactDetailsForEmailQuery = client
                ?.CreateDocumentQuery<ContactDetails>(collectionUri, new FeedOptions { MaxItemCount = 1 })
                .Where(x => x.EmailAddress == emailAddressToCheck)
                .AsDocumentQuery();
            if (contactDetailsForEmailQuery == null)
            {
                _logger.LogInformation($"No contact exists with email address [{emailAddressToCheck}]");
                return null;
            }

            var contactDetails = await contactDetailsForEmailQuery.ExecuteNextAsync<ContactDetails>();
            return contactDetails.ToList();
        }

        public async Task<DigitalIdentity> GetIdentityForCustomerAsync(Guid customerId)
        {
            var collectionUri = DocumentDBHelper.CreateDigitalIdentityDocumentUri();
            var client = DocumentDBClient.CreateDocumentClient();

            var identityForCustomerQuery = client
                ?.CreateDocumentQuery<DigitalIdentity>(collectionUri, new FeedOptions { MaxItemCount = 1 })
                .Where(x => x.CustomerId == customerId)
                .AsDocumentQuery();

            if (identityForCustomerQuery == null)
            {
                _logger.LogInformation($"No contact exists with CustomerId [{customerId}]");
                return null;
            }

            var digitalIdentity = await identityForCustomerQuery.ExecuteNextAsync<DigitalIdentity>();

            return digitalIdentity?.FirstOrDefault();
        }
    }
}