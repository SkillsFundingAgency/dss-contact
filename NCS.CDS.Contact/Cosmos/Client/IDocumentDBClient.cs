using Microsoft.Azure.Documents.Client;

namespace NCS.DSS.ContactDetails.Cosmos.Client
{
    public interface IDocumentDBClient
    {
        DocumentClient CreateDocumentClient();
    }
}