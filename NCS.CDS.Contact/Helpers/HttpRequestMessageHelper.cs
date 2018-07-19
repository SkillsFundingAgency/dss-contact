using System.Net.Http;
using System.Threading.Tasks;
using NCS.DSS.Contact.Helpers;

namespace NCS.DSS.Contact.Helpers
{
    public class HttpRequestMessageHelper : IHttpRequestMessageHelper
    {
        public async Task<T> GetContactDetailsFromRequest<T>(HttpRequestMessage req)
        {
            req.Content.Headers.ContentType.MediaType = "application/json";
            return await req.Content.ReadAsAsync<T>();
        }
    }
}
