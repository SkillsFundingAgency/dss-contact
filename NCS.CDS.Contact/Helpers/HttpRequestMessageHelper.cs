using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using NCS.DSS.ContactDetails.Models;

namespace NCS.DSS.ContactDetails.Helpers
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
