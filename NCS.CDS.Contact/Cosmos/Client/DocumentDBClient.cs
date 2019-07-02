using System;
using Microsoft.Azure.Documents.Client;

namespace NCS.DSS.ActionPlan.Cosmos.Client
{
    public static class DocumentDBClient
    {
        private static DocumentClient _documentClient;

        public static DocumentClient CreateDocumentClient()
        {
            if (_documentClient != null)
                return _documentClient;

            _documentClient = InitialiseDocumentClient();

            return _documentClient;
        }

        private static DocumentClient InitialiseDocumentClient()
        {
            // todo: ensure ContactConnectionString is created / has a value
            var connectionString = Environment.GetEnvironmentVariable("ContactConnectionString");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException();

            var endPoint = connectionString.Split(new[] { "AccountEndpoint=" }, StringSplitOptions.None)[1]
                .Split(';')[0]
                .Trim();

            if (string.IsNullOrWhiteSpace(endPoint))
                throw new ArgumentNullException();

            var key = connectionString.Split(new[] { "AccountKey=" }, StringSplitOptions.None)[1]
                .Split(';')[0]
                .Trim();

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException();

            return new DocumentClient(new Uri(endPoint), key);
        }
    }
}
