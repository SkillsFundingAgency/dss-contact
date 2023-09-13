
using System;
using System.Configuration;
using Microsoft.Azure.Documents.ChangeFeedProcessor.Logging;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;

namespace NCS.DSS.Contact.Cosmos.Helper
{
    public static class DocumentDBHelper
    {
        private static Uri _documentCollectionUri;
        private static readonly string DatabaseId = Environment.GetEnvironmentVariable("DatabaseId");
        private static readonly string CollectionId = Environment.GetEnvironmentVariable("CollectionId");

        private static Uri _customerDocumentCollectionUri;
        private static readonly string CustomerDatabaseId = Environment.GetEnvironmentVariable("CustomerDatabaseId");
        private static readonly string CustomerCollectionId = Environment.GetEnvironmentVariable("CustomerCollectionId");
        private static readonly string DigitalIdentityDatabaseId = Environment.GetEnvironmentVariable("DigitalIdentityDatabaseId");
        private static readonly string DigitalIdentityCollectionId = Environment.GetEnvironmentVariable("DigitalIdentityCollectionId");

        public static Uri CreateDocumentCollectionUri(ILogger logger)
        {
            logger.LogInformation($"Starting to create Document Collection URI.");
            if (_documentCollectionUri != null)
            {
                logger.LogInformation($"Not required. Document Collection URI already exists as [{_documentCollectionUri}].");
                return _documentCollectionUri;
            }

            _documentCollectionUri = UriFactory.CreateDocumentCollectionUri(
                DatabaseId,
                CollectionId);

            logger.LogInformation($"Created Document Collection URI as [{_documentCollectionUri}]");
            return _documentCollectionUri;
        }

        public static Uri CreateDocumentUri(Guid contactDetailsId, ILogger logger)
        {
            logger.LogInformation($"Start creating Document URI with DatabaseId [{DatabaseId}] and CollectionId [{CollectionId}].");
            return UriFactory.CreateDocumentUri(DatabaseId, CollectionId, contactDetailsId.ToString());
        }

        #region CustomerDB

        public static Uri CreateCustomerDocumentCollectionUri()
        {
            if (_customerDocumentCollectionUri != null)
                return _customerDocumentCollectionUri;

            _customerDocumentCollectionUri = UriFactory.CreateDocumentCollectionUri(
                CustomerDatabaseId, CustomerCollectionId);

            return _customerDocumentCollectionUri;
        }

        public static Uri CreateCustomerDocumentUri(Guid customerId)
        {
            return UriFactory.CreateDocumentUri(CustomerDatabaseId, CustomerCollectionId, customerId.ToString());
        }

        public static Uri CreateDigitalIdentityDocumentUri()
        {
            return UriFactory.CreateDocumentCollectionUri(DigitalIdentityDatabaseId, DigitalIdentityCollectionId);
        }

        #endregion   

    }
}
