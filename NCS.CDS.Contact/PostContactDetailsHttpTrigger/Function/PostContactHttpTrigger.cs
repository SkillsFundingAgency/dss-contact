using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http.Description;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Ioc;
using NCS.DSS.Contact.PostContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.Annotations;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.Helpers;
using NCS.DSS.Contact.Validation;
using Newtonsoft.Json;

namespace NCS.DSS.Contact.PostContactDetailsHttpTrigger.Function
{
    public static class PostContactByIdHttpTrigger
    {
        [FunctionName("POST")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Contact Details Added", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Resource Does Not Exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Post request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API Key unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient Access To This Resource", ShowSchema = false)]
        [Response(HttpStatusCode = (int)422, Description = "Contact Details resource validation error(s)", ShowSchema = false)]
        [ResponseType(typeof(Contact.Models.ContactDetails))]
        public static async Task<HttpResponseMessage> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customers/{customerId}/ContactDetails/")]HttpRequestMessage req, ILogger log, 
            string customerId,
            [Inject]IResourceHelper resourceHelper,
            [Inject]IHttpRequestMessageHelper httpRequestMessageHelper,
            [Inject]IValidate validate,
            [Inject]IPostContactDetailsHttpTriggerService contactdetailsPostService)
        {
            log.LogInformation("PostContactByID method was executed at " + DateTime.Now);

            if (!Guid.TryParse(customerId, out var customerGuid))
                return HttpResponseMessageHelper.BadRequest(customerGuid);

            Contact.Models.ContactDetails contactdetailsRequest;
            try
            {
                contactdetailsRequest = await httpRequestMessageHelper.GetContactDetailsFromRequest<Contact.Models.ContactDetails>(req);
            }
            catch (JsonSerializationException ex)
            {
                return HttpResponseMessageHelper.UnprocessableEntity(ex);
            }

            if (contactdetailsRequest == null)
                return HttpResponseMessageHelper.UnprocessableEntity(req);

            var errors = validate.ValidateResource(contactdetailsRequest);

            if (errors != null && errors.Any())
                return HttpResponseMessageHelper.UnprocessableEntity(errors);

            var doesCustomerExist = resourceHelper.DoesCustomerExist(customerGuid);

            if (!doesCustomerExist)
                return HttpResponseMessageHelper.NoContent(customerGuid);

            contactdetailsRequest.CustomerId = customerGuid;
            var contactDetails = await contactdetailsPostService.CreateContactDetails(contactdetailsRequest);

            return contactDetails == null
                ? HttpResponseMessageHelper.BadRequest(customerGuid)
                : HttpResponseMessageHelper.Created(contactDetails);
        }

    }
}
