using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System;
using System.Web.Http.Description;
using NCS.DSS.ContactDetails.Annotations;

namespace NCS.DSS.ContactDetails.PostContactByIdHttpTrigger
{
    public static class PostContactByIdHttpTrigger
    {
        [FunctionName("POST")]
        [ContactDetailsResponse(HttpStatusCode = (int)HttpStatusCode.Created, Description = "Contact Details Added", ShowSchema = true)]
        [ContactDetailsResponse(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Unable to Add Contact Details", ShowSchema = false)]
        [ContactDetailsResponse(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Forbidden", ShowSchema = false)]
        [ResponseType(typeof(Models.ContactDetails))]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customers/{customerId}/ContactDetails/{contactid}")]HttpRequestMessage req, TraceWriter log, string customerId, string contactid)
        {
            log.Info("C# HTTP trigger function PostContact processed a request.");

            if (!Guid.TryParse(contactid, out var contactGuid))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(contactid),
                        System.Text.Encoding.UTF8, "application/json")
                };
            }

            var values = "Successfully created new ContactDetails details with Id : " + Guid.NewGuid();

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(values),
                    System.Text.Encoding.UTF8, "application/json")
            };
        }

    }
}
