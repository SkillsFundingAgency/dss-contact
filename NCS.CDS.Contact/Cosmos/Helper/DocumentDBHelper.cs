
using System;
using System.Configuration;
using Microsoft.Azure.Documents.Client;

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


        public static Uri CreateDocumentCollectionUri()
        {
            if (_documentCollectionUri != null)
                return _documentCollectionUri;

            _documentCollectionUri = UriFactory.CreateDocumentCollectionUri(
                DatabaseId,
                CollectionId);

            return _documentCollectionUri;
        }

        public static Uri CreateDocumentUri(Guid contactDetailsId)
        {
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
