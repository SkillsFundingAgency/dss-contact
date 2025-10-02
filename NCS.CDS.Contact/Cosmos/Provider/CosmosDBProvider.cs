using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NCS.DSS.Contact.Models;

namespace NCS.DSS.Contact.Cosmos.Provider
{
    public class CosmosDBProvider : ICosmosDBProvider
    {
        private readonly Container _contactContainer;
        private readonly Container _customerContainer;
        private readonly ILogger<CosmosDBProvider> _logger;

        private static readonly PartitionKey PartitionKey = PartitionKey.None;

        public CosmosDBProvider(
            CosmosClient cosmosClient,
            IOptions<ContactConfigurationSettings> configOptions,
            ILogger<CosmosDBProvider> logger)
        {
            var config = configOptions.Value;

            _contactContainer = GetContainer(cosmosClient, config.DatabaseId, config.CollectionId);
            _customerContainer = GetContainer(cosmosClient, config.CustomerDatabaseId, config.CustomerCollectionId);
            _logger = logger;
        }
        private static Container GetContainer(CosmosClient cosmosClient, string databaseId, string collectionId) 
            => cosmosClient.GetContainer(databaseId, collectionId);

        public async Task<bool> DoesCustomerResourceExist(Guid customerId)
        {
            try
            {
                var response = await _customerContainer.ReadItemAsync<Customer>(
                    customerId.ToString(),
                    PartitionKey);

                return response.Resource != null;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // If a 404 occurs, the resource does not exist
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking customer resource existence. Customer ID: {CustomerId}", customerId);
                throw;
            }
        }

        public async Task<bool> DoesCustomerHaveATerminationDate(Guid customerId)
        {
            _logger.LogTrace("Checking for termination date. Customer ID: {CustomerId}", customerId);

            try
            {
                var response = await _customerContainer.ReadItemAsync<Customer>(
                    customerId.ToString(),
                    PartitionKey);

                var dateOfTermination = response.Resource?.DateOfTermination;
                var hasTerminationDate = dateOfTermination != null;

                _logger.LogTrace("Termination date check completed. CustomerId: {CustomerId}. HasTerminationDate: {HasTerminationDate}", customerId, hasTerminationDate);
                return hasTerminationDate;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("Customer does not exist. Customer ID: {CustomerId}", customerId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking termination date. Customer ID: {CustomerId}", customerId);
                throw;
            }
        }

        public async Task<bool> DoesContactDetailsExistForCustomer(Guid customerId)
        {
            try
            {
                var query = _contactContainer.GetItemLinqQueryable<ContactDetails>()
                    .Where(x => x.CustomerId == customerId)
                    .Take(1)
                    .ToFeedIterator();

                var response = await query.ReadNextAsync();
                return response.Any();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking ContactDetails existence. Customer ID: {CustomerId}", customerId);
                throw;
            }
        }

        public async Task<ContactDetails> GetContactDetailForCustomerAsync(Guid customerId)
        {
            _logger.LogTrace("Attempting to retrieve ContactDetails. Customer ID: {CustomerId}", customerId);

            try
            {
                var query = _contactContainer.GetItemLinqQueryable<ContactDetails>(allowSynchronousQueryExecution: false)
                    .Where(x => x.CustomerId == customerId)
                    .Take(1)
                    .ToFeedIterator();

                var response = await query.ReadNextAsync();
                var contactDetail = response.FirstOrDefault();

                if (contactDetail != null)
                {
                    _logger.LogTrace("Successfully retrieved ContactDetails. Customer ID: {CustomerId}", customerId);
                }
                else
                {
                    _logger.LogInformation("No ContactDetails found. CustomerId: {CustomerId}", customerId);
                }

                return contactDetail;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("No ContactDetails found. Customer ID: {CustomerId}", customerId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving ContactDetails. CustomerId: {CustomerId}. Exception: {ErrorMessage}", customerId, ex.Message);
                throw;
            }
        }


        public async Task<ContactDetails> GetContactDetailForCustomerAsync(Guid customerId, Guid contactDetailsId)
        {
            _logger.LogTrace("Attempting to retrieve ContactDetails. Customer ID: {CustomerId}. ContactDetails ID: {ContactDetailsId}", customerId, contactDetailsId);

            try
            {
                var query = _contactContainer.GetItemLinqQueryable<ContactDetails>()
                    .Where(x => x.CustomerId == customerId && x.ContactId == contactDetailsId)
                    .ToFeedIterator();

                var response = await query.ReadNextAsync();
                if (response.Any())
                {
                    _logger.LogTrace("Successfully retrieved ContactDetails. Customer ID: {CustomerId}. ContactDetails ID: {ContactDetailsId}", customerId, contactDetailsId);
                    return response.FirstOrDefault();
                }

                _logger.LogInformation("No ContactDetails found. Customer ID: {CustomerId}. ContactDetails ID: {ContactDetailsId}", customerId, contactDetailsId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving ContactDetail. Customer ID: {CustomerId}. ContactDetails ID: {ContactDetailsId}", customerId, contactDetailsId);
                throw;
            }
        }

        public async Task<ItemResponse<ContactDetails>> CreateContactDetailsAsync(ContactDetails contactDetails)
        {
            _logger.LogTrace("Creating ContactDetails. Customer ID: {CustomerId}", contactDetails.CustomerId);

            var response = await _contactContainer.CreateItemAsync(
                contactDetails,
                PartitionKey);

            _logger.LogTrace("Finished creating ContactDetails. Customer ID: {CustomerId}", contactDetails.CustomerId);
            return response;
        }

        public async Task<ItemResponse<ContactDetails>> UpdateContactDetailsAsync(ContactDetails contactDetails)
        {
            _logger.LogTrace("Updating ContactDetail. Contact Detail ID: {ContactDetailId}", contactDetails.ContactId);

            if (contactDetails.ContactId == null)
            {
                _logger.LogInformation("ContactId is missing in the {ContactDetails} object.", nameof(contactDetails));
                return null;
            }

            try
            {
                _logger.LogTrace("Attempting to update ContactDetail with ID: {ContactDetailId}", contactDetails.ContactId);

                var response = await _contactContainer.ReplaceItemAsync(
                    contactDetails,
                    contactDetails.ContactId.ToString(),
                    PartitionKey);

                _logger.LogTrace("ContactDetail updated successfully. Contact Detail ID: {ContactDetailId}", contactDetails.ContactId);
                return response;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("ContactDetail does not exist");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while updating ContactDetail ID: {ContactDetailId}. Exception: {ErrorMessage}", contactDetails.ContactId, ex.Message);
                throw;
            }
        }

        public async Task<bool> DoesContactDetailsWithEmailExists(string email)
        {
            _logger.LogTrace("Checking existence of ContactDetails using customer email address");

            try
            {
                var query = _contactContainer.GetItemLinqQueryable<ContactDetails>()
                    .Where(x => x.EmailAddress == email)
                    .Take(1)
                    .ToFeedIterator();

                var response = await query.ReadNextAsync();

                var exists = response.Any();
                _logger.LogTrace("ContactDetails existence check using customer email address returned: {Exists}", exists);
                return exists;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("ContactDetail does not exist");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while checking existence of ContactDetails using customer email address. Exception: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        public async Task<bool> DoesContactDetailsWithEmailExistsForAnotherCustomer(string email, Guid customerId)
        {
            _logger.LogTrace("Checking existence of ContactDetails for another customer using email address");

            try
            {
                var query = _contactContainer.GetItemLinqQueryable<ContactDetails>()
                    .Where(x => x.EmailAddress == email && x.CustomerId != customerId)
                    .Take(1)
                    .ToFeedIterator();

                var response = await query.ReadNextAsync();

                var exists = response.Any();
                _logger.LogTrace("ContactDetails existence check for another customer using email address returned: {Exists}", exists);
                return exists;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("ContactDetail does not exist");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while checking ContactDetails for another customer using email address. Error message: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        public async Task<IList<ContactDetails>> GetContactsByEmail(string emailAddressToCheck)
        {
            _logger.LogTrace("Attempting to retreieve ContactDetails using email address");

            try
            {
                var query = _contactContainer.GetItemLinqQueryable<ContactDetails>()
                    .Where(x => x.EmailAddress == emailAddressToCheck)
                    .ToFeedIterator();

                var contactDetails = new List<ContactDetails>();
                while (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    contactDetails.AddRange(response);
                }

                _logger.LogTrace("Retreieved {Count} ContactDetails using email address", contactDetails.Count);
                return contactDetails;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("ContactDetail does not exist");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving ContactDetails using email address. Error message: {ErrorMessage}", ex.Message);
                throw;
            }
        }
    }
}