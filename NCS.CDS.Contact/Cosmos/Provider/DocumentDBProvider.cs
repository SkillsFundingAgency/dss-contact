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
            _logger.LogInformation("Checking existence of customer resource with CustomerId [{CustomerId}]", customerId);

            var documentUri = DocumentDBHelper.CreateCustomerDocumentUri(customerId);
            var client = DocumentDBClient.CreateDocumentClient();

            if (client == null)
            {
                _logger.LogInformation("Failed to create DocumentDB client");
                return false;
            }

            try
            {
                var response = await client.ReadDocumentAsync(documentUri);
                var exists = response.Resource != null;
                _logger.LogInformation("Customer resource exists for CustomerId [{CustomerId}]", customerId);
                return exists;
            }
            catch (DocumentClientException ex)
            {
                _logger.LogError(ex, "Error occurred while checking customer resource existence for CustomerId [{CustomerId}]", customerId);
                return false;
            }
        }

        public async Task<bool> DoesCustomerHaveATerminationDate(Guid customerId)
        {
            _logger.LogInformation("Checking termination date for CustomerId [{CustomerId}]", customerId);

            var documentUri = DocumentDBHelper.CreateCustomerDocumentUri(customerId);
            var client = DocumentDBClient.CreateDocumentClient();

            if (client == null)
            {
                _logger.LogError("Failed to create DocumentDB client");
                return false;
            }

            try
            {
                var response = await client.ReadDocumentAsync(documentUri);
                var dateOfTermination = response.Resource?.GetPropertyValue<DateTime?>("DateOfTermination");
                var hasTerminationDate = dateOfTermination.HasValue;

                _logger.LogInformation("Termination date for CustomerId [{CustomerId}] returned: {HasTerminationDate}", customerId, hasTerminationDate);
                return hasTerminationDate;
            }
            catch (DocumentClientException ex)
            {
                _logger.LogError(ex, "DocumentClientException occurred while checking termination date for CustomerId [{CustomerId}]", customerId);
                return false;
            }
        }

        public bool DoesContactDetailsExistForCustomer(Guid customerId)
        {
            _logger.LogInformation("Checking if ContactDetails exist for CustomerId [{CustomerId}]", customerId);

            var collectionUri = DocumentDBHelper.CreateDocumentCollectionUri(_logger);
            var client = DocumentDBClient.CreateDocumentClient();

            if (client == null)
            {
                _logger.LogError("Failed to create DocumentDB client");
                return false;
            }

            var exists = client.CreateDocumentQuery<ContactDetails>(collectionUri, new FeedOptions { MaxItemCount = 1 })
                               .Where(x => x.CustomerId == customerId)
                               .AsEnumerable()
                               .Any();

            _logger.LogInformation("ContactDetails existence check for CustomerId [{CustomerId}] returned: {Exists}", customerId, exists);
            return exists;
        }

        public async Task<ContactDetails> GetContactDetailForCustomerAsync(Guid customerId)
        {
            _logger.LogInformation("Attempting to retrieve ContactDetails for CustomerId [{CustomerId}]", customerId);

            var collectionUri = DocumentDBHelper.CreateDocumentCollectionUri(_logger);
            var client = DocumentDBClient.CreateDocumentClient();

            var query = client?.CreateDocumentQuery<ContactDetails>(collectionUri, new FeedOptions { MaxItemCount = 1 })
                               .Where(x => x.CustomerId == customerId)
                               .AsDocumentQuery();

            if (query == null)
            {
                _logger.LogInformation("No ContactDetails found for CustomerId [{CustomerId}]", customerId);
                return null;
            }

            var contactDetails = await query.ExecuteNextAsync<ContactDetails>();
            var result = contactDetails?.FirstOrDefault();

            _logger.LogInformation("Successfully retrieved ContactDetails for CustomerId [{CustomerId}]", customerId);
            return result;
        }

        public async Task<ContactDetails> GetContactDetailForCustomerAsync(Guid customerId, Guid contactDetailsId)
        {
            _logger.LogInformation("Attempting to retreive ContactDetails for CustomerId [{CustomerId}] and ContactDetailsId [{ContactDetailsId}]", customerId, contactDetailsId);

            var collectionUri = DocumentDBHelper.CreateDocumentCollectionUri(_logger);
            var client = DocumentDBClient.CreateDocumentClient();

            var query = client?.CreateDocumentQuery<ContactDetails>(collectionUri, new FeedOptions { MaxItemCount = 1 })
                               .Where(x => x.CustomerId == customerId && x.ContactId == contactDetailsId)
                               .AsDocumentQuery();

            if (query == null)
            {
                _logger.LogInformation("No ContactDetails found for CustomerId [{CustomerId}] and ContactDetailsId [{ContactDetailsId}]", customerId, contactDetailsId);
                return null;
            }

            var contactDetails = await query.ExecuteNextAsync<ContactDetails>();
            var result = contactDetails?.FirstOrDefault();

            _logger.LogInformation("Successfully retrieved ContactDetails for CustomerId [{CustomerId}] and ContactDetailsId [{ContactDetailsId}]", customerId, contactDetailsId);
            return result;
        }

        public async Task<ResourceResponse<Document>> CreateContactDetailsAsync(ContactDetails contactDetails)
        {
            _logger.LogInformation("Attempting to create ContactDetails for CustomerId [{CustomerId}]", contactDetails.CustomerId);

            var collectionUri = DocumentDBHelper.CreateDocumentCollectionUri(_logger);
            var client = DocumentDBClient.CreateDocumentClient();

            if (client == null)
            {
                _logger.LogError("Failed to create DocumentDB client");
                return null;
            }

            var response = await client.CreateDocumentAsync(collectionUri, contactDetails);
            _logger.LogInformation("Successfully created ContactDetails for CustomerId [{CustomerId}]", contactDetails.CustomerId);

            return response;
        }

        public async Task<ResourceResponse<Document>> UpdateContactDetailsAsync(ContactDetails contactDetails)
        {
            if (contactDetails?.ContactId == null)
            {
                _logger.LogInformation("ContactId is missing in the {ContactDetails} object.", nameof(contactDetails));
                return null;
            }

            var documentUri = DocumentDBHelper.CreateDocumentUri(contactDetails.ContactId.GetValueOrDefault(), _logger);
            var client = DocumentDBClient.CreateDocumentClient();

            if (client == null)
            {
                _logger.LogError("Failed to create DocumentDB client");
                return null;
            }

            try
            {
                _logger.LogInformation("Attempting to update ContactDetails for ContactId [{ContactId}]", contactDetails.ContactId);
                var response = await client.ReplaceDocumentAsync(documentUri, contactDetails);
                _logger.LogInformation("Successfully updated ContactDetails for ContactId [{ContactId}]", contactDetails.ContactId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating ContactDetails for ContactId [{ContactId}]", contactDetails.ContactId);
                throw;
            }
        }

        public async Task<bool> DoesContactDetailsWithEmailExists(string emailAddressToCheck)
        {
            _logger.LogInformation("Checking existence of ContactDetails using customer email address");

            var collectionUri = DocumentDBHelper.CreateDocumentCollectionUri(_logger);
            var client = DocumentDBClient.CreateDocumentClient();

            if (client == null)
            {
                _logger.LogError("Failed to create DocumentDB client");
                return false;
            }

            var contactDetailsForEmailQuery = client
                .CreateDocumentQuery<ContactDetails>(collectionUri, new FeedOptions { MaxItemCount = 1 })
                .Where(x => x.EmailAddress == emailAddressToCheck)
                .AsDocumentQuery();

            if (contactDetailsForEmailQuery == null)
            {
                _logger.LogInformation("No ContactDetails found with specified email address");
                return false;
            }

            var contactDetails = await contactDetailsForEmailQuery.ExecuteNextAsync<ContactDetails>();
            var exists = contactDetails.Any();

            _logger.LogInformation("ContactDetails existence check using customer email address returned: {Exists}", exists);
            return exists;
        }

        public async Task<bool> DoesContactDetailsWithEmailExistsForAnotherCustomer(string email, Guid customerId)
        {
            _logger.LogInformation("Checking existence of ContactDetails using email address for other customer records");

            var collectionUri = DocumentDBHelper.CreateDocumentCollectionUri(_logger);
            var client = DocumentDBClient.CreateDocumentClient();

            if (client == null)
            {
                _logger.LogError("Failed to create DocumentDB client");
                return false;
            }

            var contactDetailsForEmailQuery = client
                .CreateDocumentQuery<ContactDetails>(collectionUri, new FeedOptions { MaxItemCount = 1, EnableCrossPartitionQuery = true })
                .Where(x => x.EmailAddress == email && x.CustomerId != customerId)
                .AsDocumentQuery();

            if (contactDetailsForEmailQuery == null)
            {
                _logger.LogInformation("No ContactDetails for other customer records found with email address");
                return false;
            }

            var contactDetails = await contactDetailsForEmailQuery.ExecuteNextAsync<ContactDetails>();
            var exists = contactDetails.Any();

            _logger.LogInformation("ContactDetails existence check using customer email address returned: {Exists}", email, customerId, exists);
            return exists;
        }

        public async Task<IList<ContactDetails>> GetContactsByEmail(string emailAddressToCheck)
        {
            _logger.LogInformation("Attempting to retreieve ContactDetails using email address");

            var collectionUri = DocumentDBHelper.CreateDocumentCollectionUri(_logger);
            var client = DocumentDBClient.CreateDocumentClient();

            if (client == null)
            {
                _logger.LogError("Failed to create DocumentDB client");
                return null;
            }

            var contactDetailsForEmailQuery = client
                .CreateDocumentQuery<ContactDetails>(collectionUri, new FeedOptions { MaxItemCount = 1 })
                .Where(x => x.EmailAddress == emailAddressToCheck)
                .AsDocumentQuery();

            if (contactDetailsForEmailQuery == null)
            {
                _logger.LogInformation("No ContactDetails found using email address");
                return null;
            }

            var contactDetails = await contactDetailsForEmailQuery.ExecuteNextAsync<ContactDetails>();
            var result = contactDetails.ToList();

            _logger.LogInformation("Retreieved {Count} ContactDetails using email address", result.Count);
            return result;
        }

        public async Task<DigitalIdentity> GetIdentityForCustomerAsync(Guid customerId)
        {
            _logger.LogInformation("Attempting to retrieve digital identity for CustomerId [{CustomerId}]", customerId);

            var collectionUri = DocumentDBHelper.CreateDigitalIdentityDocumentUri();
            var client = DocumentDBClient.CreateDocumentClient();

            if (client == null)
            {
                _logger.LogError("Failed to create DocumentDB client");
                return null;
            }

            var identityForCustomerQuery = client
                .CreateDocumentQuery<DigitalIdentity>(collectionUri, new FeedOptions { MaxItemCount = 1 })
                .Where(x => x.CustomerId == customerId)
                .AsDocumentQuery();

            if (identityForCustomerQuery == null)
            {
                _logger.LogInformation("No digital identity found for CustomerId [{CustomerId}]", customerId);
                return null;
            }

            var digitalIdentity = await identityForCustomerQuery.ExecuteNextAsync<DigitalIdentity>();
            var result = digitalIdentity.FirstOrDefault();

            if (result != null)
            {
                _logger.LogInformation("Successfully retrieved digital identity for CustomerId [{CustomerId}]", customerId);
            }
            else
            {
                _logger.LogInformation("No digital identity exists for CustomerId [{CustomerId}]", customerId);
            }

            return result;
        }

    }
}