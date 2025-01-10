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
using Newtonsoft.Json;
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
        private readonly ICosmosDBProvider _provider;
        private readonly IValidate _validate;
        private readonly IConvertToDynamic _convertToDynamic;
        private readonly ILogger<PostContactHttpTrigger> _logger;
        private static readonly string[] PropertyToExclude = { "TargetSite" };

        public PostContactHttpTrigger(IPostContactDetailsHttpTriggerService contactdetailsPostService,
            IHttpRequestHelper httpRequestMessageHelper,
            IResourceHelper resourceHelper,
            ICosmosDBProvider provider,
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
        [Response(HttpStatusCode = (int)HttpStatusCode.UnprocessableEntity, Description = "Contact Details resource validation error(s)", ShowSchema = false)]
        [ProducesResponseType(typeof(ContactDetails), 200)]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customers/{customerId}/ContactDetails/")]
            HttpRequest req, string customerId)
        {
            _logger.LogInformation("Function {FunctionName} has been invoked", nameof(PostContactHttpTrigger));

            try
            {
                JsonConvert.SerializeObject(JsonConvert.DeserializeObject(await new StreamReader(req.Body).ReadToEndAsync()));
            }
            catch
            {
                return new BadRequestObjectResult("Invalid JSON format in the request body.");
            }

            var touchpointId = _httpRequestMessageHelper.GetDssTouchpointId(req);
            if (string.IsNullOrEmpty(touchpointId))
            {
                _logger.LogWarning("Unable to locate 'TouchpointId' in request header.");
                return new BadRequestObjectResult(HttpStatusCode.BadRequest);
            }

            var apimURL = _httpRequestMessageHelper.GetDssApimUrl(req);
            if (string.IsNullOrEmpty(apimURL))
            {
                _logger.LogWarning("Unable to locate 'apimurl' in request header");
                return new BadRequestObjectResult(HttpStatusCode.BadRequest);
            }

            if (!Guid.TryParse(customerId, out var customerGuid))
            {
                _logger.LogWarning("Unable to parse 'customerId' to a GUID. Customer ID: {CustomerId}", customerId);
                return new BadRequestObjectResult(customerGuid);
            }

            _logger.LogInformation("Header validation has succeeded. Touchpoint ID: {TouchpointId}", touchpointId);

            ContactDetails contactDetailsPostRequest;

            try
            {
                contactDetailsPostRequest = await _httpRequestMessageHelper.GetResourceFromRequest<ContactDetails>(req);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Unable to parse ContactDetails from request body. Exception: {ExceptionMessage}", ex.Message);
                return new UnprocessableEntityObjectResult(_convertToDynamic.ExcludeProperty(ex, PropertyToExclude));
            }

            if (contactDetailsPostRequest == null)
            {
                _logger.LogError("{ContactDetailsPost} object is NULL", nameof(contactDetailsPostRequest));
                return new UnprocessableEntityObjectResult(req);
            }

            contactDetailsPostRequest.SetIds(customerGuid, touchpointId);

            _logger.LogInformation("Attempting to validate {ContactDetailsPost} object", nameof(contactDetailsPostRequest));
            var errors = _validate.ValidateResource(contactDetailsPostRequest, null, true);

            if (errors != null && errors.Any())
            {
                _logger.LogError("Validation for {ContactDetailsPost} object has failed", nameof(contactDetailsPostRequest));
                return new UnprocessableEntityObjectResult(errors);
            }

            _logger.LogInformation("Validation for {ContactDetailsPost} object has passed", nameof(contactDetailsPostRequest));

            _logger.LogInformation("Attempting to check if customer exists. Customer GUID: {CustomerGuid}", customerGuid);
            var doesCustomerExist = await _resourceHelper.DoesCustomerExist(customerGuid);

            if (!doesCustomerExist)
            {
                _logger.LogInformation("Customer does not exist. Customer GUID: {CustomerGuid}", customerGuid);
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
            var doesContactDetailsExist = await _contactdetailsPostService.DoesContactDetailsExistForCustomer(customerGuid);

            if (doesContactDetailsExist)
            {
                _logger.LogWarning("ContactDetails already exists for Customer. Customer GUID: {CustomerGuid}", customerGuid);
                return new ConflictResult();
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
                            "Attempting to check if customer has a termination date. Customer ID: {CustomerId}",
                            contact.CustomerId.GetValueOrDefault());
                        var isReadOnly = await _provider.DoesCustomerHaveATerminationDate(contact.CustomerId.GetValueOrDefault());
                        
                        if (!isReadOnly)
                        {
                            _logger.LogWarning(
                                "Customer already uses an email address that does not have a termination date." +
                                " Email address on the request cannot be used. Customer ID: {CustomerId}. Contact Details ID: {ContactDetailsId}",
                                contact.CustomerId.GetValueOrDefault(), contact.ContactId.GetValueOrDefault());

                            //if a customer that has the same email address is not readonly (has date of termination)
                            //then email address on the request cannot be used.
                            return new ConflictResult();
                        }
                    }
                }
                _logger.LogError(
                    "Retrieving ContactDetails using the email address on the request has returned NULL. Customer GUID: {CustomerGuid}",
                    customerGuid);
            }

            _logger.LogInformation(
                "Attempting to POST a ContactDetails. Customer GUID: {CustomerGuid}. Contact Details ID: {ContactDetailsId}",
                customerGuid, contactDetailsPostRequest.ContactId.GetValueOrDefault());

            var contactDetails = await _contactdetailsPostService.CreateAsync(contactDetailsPostRequest);

            if (contactDetails == null)
            {
                _logger.LogError("POST request unsuccessful. Customer GUID: {CustomerGuid}", customerGuid);
                _logger.LogInformation("Function {FunctionName} has finished invoking", nameof(PostContactDetailsHttpTrigger));

                return new BadRequestObjectResult(customerGuid);
            }

            _logger.LogInformation("Sending newly created ContactDetails to service bus. Customer GUID: {CustomerGuid}. Contact Details ID: {contactDetailsId}", customerGuid, contactDetails.ContactId.GetValueOrDefault());
            await _contactdetailsPostService.SendToServiceBusQueueAsync(contactDetails, apimURL);

            _logger.LogInformation("PATCH request successful. Contact Details ID: {ContactDetailsId}", contactDetails.ContactId.GetValueOrDefault());
            _logger.LogInformation("Function {FunctionName} has finished invoking", nameof(PostContactHttpTrigger));

            return new JsonResult(contactDetails, new JsonSerializerOptions())
            {
                StatusCode = (int)HttpStatusCode.Created
            };
        }
    }
}
