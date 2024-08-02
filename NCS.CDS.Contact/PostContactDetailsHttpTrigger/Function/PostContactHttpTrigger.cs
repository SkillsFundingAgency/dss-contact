using DFC.HTTP.Standard;
using DFC.Swagger.Standard.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.Cosmos.Provider;
using NCS.DSS.Contact.Models;
using NCS.DSS.Contact.PostContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.Validation;
using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using JsonException = Newtonsoft.Json.JsonException;

namespace NCS.DSS.Contact.PostContactDetailsHttpTrigger.Function
{
    public class PostContactByIdHttpTrigger
    {
        private readonly IResourceHelper _resourceHelper;
        private readonly IHttpRequestHelper _responseHelper;
        private readonly IValidate _validate;
        private readonly IPostContactDetailsHttpTriggerService _contactdetailsPostService;
        private readonly IDocumentDBProvider _provider;
        private readonly ILogger log;
        private readonly IConvertToDynamic _convertToDynamic;
        public PostContactByIdHttpTrigger( IResourceHelper resourceHelper,
            IHttpRequestHelper responseHelper,
            IValidate validate,
            IPostContactDetailsHttpTriggerService contactdetailsPostService,
            IDocumentDBProvider provider,
            ILogger<PostContactByIdHttpTrigger> logger,
            IConvertToDynamic convertToDynamic)
        {
            _resourceHelper = resourceHelper;
            _validate = validate;
            _contactdetailsPostService = contactdetailsPostService;
            _provider = provider;
            _responseHelper = responseHelper;
            log = logger;
            _convertToDynamic = convertToDynamic;
        }

        [Function("POST")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Contact Details Added", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Resource Does Not Exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Post request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API Key unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient Access To This Resource", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Conflict, Description = "Contact Details already exists for customer", ShowSchema = false)]
        [Response(HttpStatusCode = (int)422, Description = "Contact Details resource validation error(s)", ShowSchema = false)]
        [ProducesResponseType(typeof(Contact.Models.ContactDetails), 200)]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customers/{customerId}/ContactDetails/")]HttpRequest req, 
            string customerId)
        {
            var touchpointId = _responseHelper.GetDssTouchpointId(req);
            if (string.IsNullOrEmpty(touchpointId))
            {
                log.LogInformation("Unable to locate 'TouchpointId' in request header.");
                return new BadRequestObjectResult(HttpStatusCode.BadRequest);
            }

            var ApimURL = _responseHelper.GetDssApimUrl(req);
            if (string.IsNullOrEmpty(ApimURL))
            {
                log.LogInformation("Unable to locate 'apimurl' in request header");
                return new BadRequestObjectResult(HttpStatusCode.BadRequest);
            }

            log.LogInformation("C# HTTP trigger function Post Contact processed a request. " + touchpointId);

            if (!Guid.TryParse(customerId, out var customerGuid))
                return new BadRequestObjectResult(customerGuid);

            ContactDetails contactdetailsRequest;
            try
            {
                contactdetailsRequest = await _responseHelper.GetResourceFromRequest<ContactDetails>(req);
            }
            catch (JsonException ex)
            {
                return new UnprocessableEntityObjectResult(_convertToDynamic.ExcludeProperty(ex, ["TargetSite"]));
            }

            if (contactdetailsRequest == null)
                return new UnprocessableEntityObjectResult(req);

            contactdetailsRequest.SetIds(customerGuid, touchpointId);

            var errors = _validate.ValidateResource(contactdetailsRequest, null, true);

            if (errors != null && errors.Any())
                return new UnprocessableEntityObjectResult(errors);

            var doesCustomerExist = await _resourceHelper.DoesCustomerExist(customerGuid);

            if (!doesCustomerExist)
                return new NoContentResult();

            var isCustomerReadOnly = await _resourceHelper.IsCustomerReadOnly(customerGuid);

            if (isCustomerReadOnly)
                return new ObjectResult(customerGuid.ToString())
                {
                    StatusCode = (int)HttpStatusCode.Forbidden
                };

            var doesContactDetailsExist = _contactdetailsPostService.DoesContactDetailsExistForCustomer(customerGuid);

            if (doesContactDetailsExist)
                return new ConflictObjectResult(HttpStatusCode.Conflict);

            if (!string.IsNullOrEmpty(contactdetailsRequest.EmailAddress))
            {
                var contacts = await _provider.GetContactsByEmail(contactdetailsRequest.EmailAddress);
                if (contacts != null)
                {
                    foreach (var contact in contacts)
                    {
                        var isReadOnly = await _provider.DoesCustomerHaveATerminationDate(contact.CustomerId.GetValueOrDefault());
                        if (!isReadOnly)
                        {
                            //if a customer that has the same email address is not readonly (has date of termination)
                            //then email address on the request cannot be used.
                            return new ConflictObjectResult(HttpStatusCode.Conflict);
                        }
                    }
                }
            }

            var contactDetails = await _contactdetailsPostService.CreateAsync(contactdetailsRequest);

            if (contactDetails != null)
            {
                await _contactdetailsPostService.SendToServiceBusQueueAsync(contactDetails, ApimURL);
            }

            return contactDetails == null
                ? new BadRequestObjectResult(customerGuid)
                : new JsonResult(contactDetails, new JsonSerializerOptions())
                {
                    StatusCode = (int)HttpStatusCode.Created
                };
        }
    };
}
