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
using NCS.DSS.ContactDetails.Helpers;
using NCS.DSS.ContactDetails.Validation;
using System.Linq;
using NCS.DSS.ContactDetails.PatchContactDetailsHttpTrigger.Service;

namespace NCS.DSS.ContactDetails.PatchContactHttpTrigger
{
    public static class PatchContactHttpTrigger
    {
        [FunctionName("PATCH")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Contact Details Patched", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Resource Does Not Exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Patch request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API Key unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient Access To This Resource", ShowSchema = false)]
        [Response(HttpStatusCode = (int)422, Description = "Contact Details resource validation error(s)", ShowSchema = false)]
        [ResponseType(typeof(Models.ContactDetails))]
        public static async Task<HttpResponseMessage> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "customers/{customerId}/ContactDetails/{contactid}")]HttpRequestMessage req, TraceWriter log, 
            string customerId, string contactid,
            [Inject]IResourceHelper resourceHelper,
            [Inject]IHttpRequestMessageHelper httpRequestMessageHelper,
            [Inject]IValidate validate,
            [Inject]IPatchContactDetailsHttpTriggerService contactdetailsPatchService)
        {
            log.Info("C# HTTP trigger function processed a request.");

            if (!Guid.TryParse(customerId, out var customerGuid))
                return HttpResponseMessageHelper.BadRequest(customerGuid);

            if (!Guid.TryParse(contactid, out var contactGuid))
                return HttpResponseMessageHelper.BadRequest(contactGuid);

            Models.ContactDetailsPatch contactdetailsPatchRequest;

            try
            {
                contactdetailsPatchRequest = await httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetailsPatch>(req);
            }
            catch (JsonSerializationException ex)
            {
                return HttpResponseMessageHelper.UnprocessableEntity(ex);
            }

            if (contactdetailsPatchRequest == null)
                return HttpResponseMessageHelper.UnprocessableEntity(req);

            var errors = validate.ValidateResource(contactdetailsPatchRequest);

            if (errors != null && errors.Any())
                return HttpResponseMessageHelper.UnprocessableEntity(errors);

            var doesCustomerExist = resourceHelper.DoesCustomerExist(customerGuid);

            if (!doesCustomerExist)
                return HttpResponseMessageHelper.NoContent(customerGuid);

            var contactdetails = await contactdetailsPatchService.GetContactDetailsForCustomerAsync(customerGuid, contactGuid);

            if (contactdetails == null)
                return HttpResponseMessageHelper.NoContent(contactGuid);

            var updatedContactDetails = await contactdetailsPatchService.UpdateAsync(contactdetails, contactdetailsPatchRequest);

            return updatedContactDetails == null ?
                HttpResponseMessageHelper.BadRequest(contactGuid) :
                HttpResponseMessageHelper.Ok(updatedContactDetails);
        }

    }
}
