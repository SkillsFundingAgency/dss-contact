using Microsoft.Azure.Documents.Client;

namespace NCS.DSS.Contact.Cosmos.Client
{
    public interface IDocumentDBClient
    {
        DocumentClient CreateDocumentClient();
    }
}