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
    public class PostContactHttpTrigger
    {
        private readonly IPostContactDetailsHttpTriggerService _contactdetailsPostService;
        private readonly IHttpRequestHelper _httpRequestMessageHelper;
        private readonly IResourceHelper _resourceHelper;
        private readonly IDocumentDBProvider _provider;
        private readonly IValidate _validate;
        private readonly IConvertToDynamic _convertToDynamic;
        private readonly ILogger<PostContactHttpTrigger> _logger;
        private static readonly string[] PropertyToExclude = { "TargetSite" };

        public PostContactHttpTrigger(IPostContactDetailsHttpTriggerService contactdetailsPostService,
            IHttpRequestHelper httpRequestMessageHelper,
            IResourceHelper resourceHelper,
            IDocumentDBProvider provider,
            IValidate validate,
            IConvertToDynamic convertToDynamic,
            ILogger<PostContactHttpTrigger> logger
            )
        {
            _contactdetailsPostService = contactdetailsPostService;
            _httpRequestMessageHelper = httpRequestMessageHelper;
            _resourceHelper = resourceHelper;
            _provider = provider;
            _validate = validate;
            _convertToDynamic = convertToDynamic;
            _logger = logger;
        }

        [Function("POST")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Contact Details Added", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Resource Does Not Exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Post request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API Key unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient Access To This Resource", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Conflict, Description = "Contact Details already exists for customer", ShowSchema = false)]
        [Response(HttpStatusCode = 422, Description = "Contact Details resource validation error(s)", ShowSchema = false)]
        [ProducesResponseType(typeof(ContactDetails), 200)]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customers/{customerId}/ContactDetails/")]
            HttpRequest req, string customerId)
        {
            _logger.LogInformation("Function {FunctionName} has been invoked", nameof(PostContactHttpTrigger));

            var touchpointId = _httpRequestMessageHelper.GetDssTouchpointId(req);
            if (string.IsNullOrEmpty(touchpointId))
            {
                _logger.LogInformation("Unable to locate 'TouchpointId' in request header.");
                return new BadRequestObjectResult(HttpStatusCode.BadRequest);
            }

            var ApimURL = _httpRequestMessageHelper.GetDssApimUrl(req);
            if (string.IsNullOrEmpty(ApimURL))
            {
                _logger.LogInformation("Unable to locate 'apimurl' in request header");
                return new BadRequestObjectResult(HttpStatusCode.BadRequest);
            }

            if (!Guid.TryParse(customerId, out var customerGuid))
            {
                _logger.LogInformation("Unable to parse 'customerId' to a GUID. Customer ID: {CustomerId}", customerId);
                return new BadRequestObjectResult(customerGuid);
            }

            ContactDetails contactDetailsPostRequest;

            try
            {
                contactDetailsPostRequest = await _httpRequestMessageHelper.GetResourceFromRequest<ContactDetails>(req);
            }
            catch (JsonException ex)
            {
                _logger.LogError("Unable to parse ContactDetailsPost from request body. Exception: {ExceptionMessage}", ex.Message);
                return new UnprocessableEntityObjectResult(_convertToDynamic.ExcludeProperty(ex, PropertyToExclude));
            }

            if (contactDetailsPostRequest == null)
            {
                _logger.LogError("ContactDetailsPost object is NULL");
                return new UnprocessableEntityObjectResult(req);
            }

            contactDetailsPostRequest.SetIds(customerGuid, touchpointId);

            _logger.LogInformation("Attempting to validate ContactDetailsPost object");
            var errors = _validate.ValidateResource(contactDetailsPostRequest, null, true);

            if (errors != null && errors.Any())
            {
                _logger.LogError("Validation for ContactDetailsPost object has failed");
                return new UnprocessableEntityObjectResult(errors);
            }

            _logger.LogInformation("Validation for ContactDetailsPost object has passed");

            _logger.LogInformation("Attempting to check if customer exists. Customer GUID: {CustomerId}", customerGuid);
            var doesCustomerExist = await _resourceHelper.DoesCustomerExist(customerGuid);

            if (!doesCustomerExist)
            {
                _logger.LogError("Customer does not exist. Customer GUID: {CustomerGuid}", customerGuid);
                return new NoContentResult();
            }

            _logger.LogInformation("Customer exists. Customer GUID: {CustomerGuid}", customerGuid);

            _logger.LogInformation("Attempting to check if customer is read only. Customer GUID: {CustomerGuid}", customerGuid);
            var isCustomerReadOnly = await _resourceHelper.IsCustomerReadOnly(customerGuid);

            if (isCustomerReadOnly)
            {
                _logger.LogError("Customer is read-only. Operation is forbidden. Customer GUID: {CustomerGuid}", customerGuid);

                return new ObjectResult(customerGuid.ToString())
                {
                    StatusCode = (int)HttpStatusCode.Forbidden
                };
            }

            _logger.LogInformation("Customer is not read-only. Customer GUID: {CustomerGuid}", customerGuid);

            _logger.LogInformation("Attempting to check if customer has ContactDetails. Customer GUID: {CustomerGuid}", customerGuid);
            var doesContactDetailsExist = _contactdetailsPostService.DoesContactDetailsExistForCustomer(customerGuid);

            if (doesContactDetailsExist)
            {
                _logger.LogError("ContactDetails already exists for Customer. Customer GUID: {CustomerGuid}", customerGuid);
                return new ConflictObjectResult(HttpStatusCode.Conflict);
            }

            _logger.LogInformation("ContactDetails does not exist for Customer. Customer GUID: {CustomerGuid}", customerGuid);

            if (!string.IsNullOrEmpty(contactDetailsPostRequest.EmailAddress))
            {
                _logger.LogInformation(
                    "Attempting to retrieve ContactDetails using the email address on the request. Customer GUID: {CustomerGuid}",
                    customerGuid);
                var contacts = await _provider.GetContactsByEmail(contactDetailsPostRequest.EmailAddress);
                
                if (contacts != null)
                {
                    _logger.LogInformation(
                        "Customer has ContactDetails using the email address on the request. Customer GUID: {CustomerGuid}",
                        customerGuid);

                    foreach (var contact in contacts)
                    {
                        _logger.LogInformation(
                            "Attempting to check if customer has a termination date. CustomerId: {CustomerId}",
                            contact.CustomerId.GetValueOrDefault());
                        var isReadOnly = await _provider.DoesCustomerHaveATerminationDate(contact.CustomerId.GetValueOrDefault());
                        
                        if (!isReadOnly)
                        {
                            _logger.LogInformation(
                                "Customer already uses an email address that does not have a termination date." +
                                " Email address on the request cannot be used. CustomerId: {CustomerId}. ContactDetailsId: {ContactDetailsId}",
                                contact.CustomerId.GetValueOrDefault(), contact.ContactId.GetValueOrDefault());

                            //if a customer that has the same email address is not readonly (has date of termination)
                            //then email address on the request cannot be used.
                            return new ConflictObjectResult(HttpStatusCode.Conflict);
                        }
                    }
                }
                _logger.LogError(
                    "Retrieving ContactDetails using the email address on the request has returned NULL. Customer GUID: {CustomerGuid}",
                    customerGuid);
            }

            _logger.LogInformation(
                "Attempting to POST a ContactDetails. Customer GUID: {CustomerGuid}. ContactDetailsId: {ContactDetailsId}",
                customerGuid, contactDetailsPostRequest.ContactId.GetValueOrDefault());
            var contactDetails = await _contactdetailsPostService.CreateAsync(contactDetailsPostRequest);

            if (contactDetails == null)
            {
                _logger.LogError("POST request unsuccessful. Customer GUID: {CustomerGuid}", customerGuid);
                return new BadRequestObjectResult(customerGuid);
            }

            _logger.LogInformation("PATCH request successful. ContactDetailsId: {ContactDetailsId}", contactDetails.ContactId.GetValueOrDefault());

            await _contactdetailsPostService.SendToServiceBusQueueAsync(contactDetails, ApimURL);

            _logger.LogInformation("Function {FunctionName} has finished invoking", nameof(PostContactHttpTrigger));

            return new JsonResult(contactDetails, new JsonSerializerOptions())
            {
                StatusCode = (int)HttpStatusCode.Created
            };
        }
    }
}
