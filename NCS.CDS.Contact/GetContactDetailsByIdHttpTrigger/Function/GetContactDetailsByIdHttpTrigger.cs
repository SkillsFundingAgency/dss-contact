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
using NCS.DSS.ContactDetails.Ioc;
using NCS.DSS.ContactDetails.Cosmos.Helper;
using NCS.DSS.ContactDetails.GetContactDetailsByIdHttpTrigger.Service;
using NCS.DSS.ContactDetails.Helpers;

namespace NCS.DSS.ContactDetails.GetContactByIdHttpTrigger
{
    public static class GetContactByIdHttpTrigger
    {
        [FunctionName("GETByID")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Contact Details Retrieved", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Resource Does Not Exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Get request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API Key unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient Access To This Resource", ShowSchema = false)]
        [ResponseType(typeof(Models.ContactDetails))]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers/{customerId}/ContactDetails/{contactid}")]HttpRequestMessage req, TraceWriter log, string customerId, string contactid,
            [Inject]IResourceHelper resourceHelper,
            [Inject]IGetContactDetailsByIdHttpTriggerService getContactDetailsByIdService)
        {
            log.Info("C# HTTP trigger function processed a request.");

            if (!Guid.TryParse(customerId, out var customerGuid))
                return HttpResponseMessageHelper.BadRequest(customerGuid);

            if (!Guid.TryParse(contactid, out var contactGuid))
                return HttpResponseMessageHelper.BadRequest(contactGuid);

            var doesCustomerExist = resourceHelper.DoesCustomerExist(customerGuid);

            if (!doesCustomerExist)
                return HttpResponseMessageHelper.NoContent(customerGuid);

            var contact = await getContactDetailsByIdService.GetContactDetailsForCustomerAsync(customerGuid, contactGuid);

            return contact == null ?
                HttpResponseMessageHelper.NoContent(contactGuid) :
                HttpResponseMessageHelper.Ok(contact);
        }
   

    }
}
