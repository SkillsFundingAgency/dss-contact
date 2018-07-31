using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace NCS.DSS.Contact.Helpers
{
    public interface IHttpRequestMessageHelper
    {
        Task<T> GetContactDetailsFromRequest<T>(HttpRequestMessage req);
        Guid? GetTouchpointId(HttpRequestMessage req);
    }
}