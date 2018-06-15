using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System;
using System.Web.Http.Description;

namespace NCS.DSS.Contact.PatchContactHttpTrigger
{
    public static class PatchContactHttpTrigger
    {
        [FunctionName("PATCH")]
        [ResponseType(typeof(Models.Contact))]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "Patch", Route = "customers/{customerId}/contacts/{contactid}")]HttpRequestMessage req, TraceWriter log, string contactid)
        {
            log.Info("C# HTTP trigger function PatchContact processed a request.");

            if (!Guid.TryParse(contactid, out var contactGuid))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(contactid),
                        System.Text.Encoding.UTF8, "application/json")
                };
            }

            var values = "Sucessfully Updated contact record with id : " + contactid;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(values),
                    System.Text.Encoding.UTF8, "application/json")
            };
        }

    }
}
