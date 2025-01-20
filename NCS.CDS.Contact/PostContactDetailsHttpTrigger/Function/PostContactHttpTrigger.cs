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
using System.Net;
using System.Text.Json;
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
        public PostContactByIdHttpTrigger(IResourceHelper resourceHelper,
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
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Post request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient Access To This Resource", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NotFound, Description = "Resource Does Not Exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Conflict, Description = "Contact Details already exists for customer", ShowSchema = false)]
        [Response(HttpStatusCode = (int)422, Description = "Contact Details resource validation error(s)", ShowSchema = false)]
        [ProducesResponseType(typeof(Contact.Models.ContactDetails), 200)]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customers/{customerId}/ContactDetails/")] HttpRequest req,
            string customerId)
        {
            var touchpointId = _responseHelper.GetDssTouchpointId(req);
            if (string.IsNullOrEmpty(touchpointId))
            {
                log.LogInformation("Unable to locate 'TouchpointId' in request header.");
                return new BadRequestObjectResult("Unable to locate 'TouchpointId' in request header.");
            }

            var ApimURL = _responseHelper.GetDssApimUrl(req);
            if (string.IsNullOrEmpty(ApimURL))
            {
                log.LogInformation("Unable to locate 'apimurl' in request header");
                return new BadRequestObjectResult("Unable to locate 'apimurl' in request header");
            }

            log.LogInformation("C# HTTP trigger function Post Contact processed a request. " + touchpointId);

            if (!Guid.TryParse(customerId, out var customerGuid))
            {
                log.LogInformation($"Unable to parse 'customerId' to a GUID [{customerId}]");
                return new BadRequestObjectResult($"Unable to parse 'customerId' to a GUID. Customer ID: {customerId}.");
            }

            ContactDetails contactdetailsRequest;
            try
            {
                contactdetailsRequest = await _responseHelper.GetResourceFromRequest<ContactDetails>(req);
            }
            catch (JsonException ex)
            {
                log.LogError($"Json Exception. Unable to retrieve request body.", ex);
                return new UnprocessableEntityObjectResult($"Json exception. Unable to retrieve request body." + _convertToDynamic.ExcludeProperty(ex, ["TargetSite"]));
            }

            if (contactdetailsRequest == null)
            {
                log.LogInformation($"Unable to retrieve contact details from request data. Contact details returned from database are NULL");
                return new UnprocessableEntityObjectResult($"Unable to retrieve contact details from request data. Contact details returned from database are NULL");
            }

            contactdetailsRequest.SetIds(customerGuid, touchpointId);

            var errors = _validate.ValidateResource(contactdetailsRequest, null, true);

            if (errors != null && errors.Any())
            {
                log.LogInformation("Validation errors present with the resource:\n" + errors);
                return new UnprocessableEntityObjectResult("Validation errors present with the resource:\n" + errors);
            }

            var doesCustomerExist = await _resourceHelper.DoesCustomerExist(customerGuid);

            if (!doesCustomerExist)
            {
                log.LogInformation($"Customer does not exist.");
                return new NotFoundObjectResult($"Customer ({customerGuid}) does not exist.");
            }

            var isCustomerReadOnly = await _resourceHelper.IsCustomerReadOnly(customerGuid);

            if (isCustomerReadOnly)
            {
                log.LogInformation($"Customer ({customerGuid}) is read-only");
                return new ObjectResult($"Customer ({customerGuid}) is read-only")
                {
                    StatusCode = (int)HttpStatusCode.Forbidden
                };
            }

            var doesContactDetailsExist = _contactdetailsPostService.DoesContactDetailsExistForCustomer(customerGuid);

            if (doesContactDetailsExist)
            {
                log.LogInformation($"Contact details already exist for customer ({customerGuid})");
                return new ConflictObjectResult($"Contact details already exist for customer ({customerGuid})");
            }

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
                            log.LogInformation($"The email address {contactdetailsRequest.EmailAddress} cannot be used because it's being used by another customer.");
                            return new ConflictObjectResult($"The email address {contactdetailsRequest.EmailAddress} cannot be used because it's being used by another customer.");
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
                ? new BadRequestObjectResult($"Failed to POST contact details to Cosmos DB for customer {customerGuid}. Contact details are NULL after creation attempt.")
                : new JsonResult(contactDetails, new JsonSerializerOptions())
                {
                    StatusCode = (int)HttpStatusCode.Created
                };
        }
    };
}
