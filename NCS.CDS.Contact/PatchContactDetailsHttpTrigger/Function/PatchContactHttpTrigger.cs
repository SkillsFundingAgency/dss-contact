using DFC.HTTP.Standard;
using DFC.Swagger.Standard.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.Cosmos.Provider;
using NCS.DSS.Contact.Models;
using NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.Validation;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Text.Json;
using JsonException = Newtonsoft.Json.JsonException;

namespace NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Function
{
    public class PatchContactHttpTrigger
    {
        private readonly IPatchContactDetailsHttpTriggerService _contactdetailsPatchService;
        private readonly IHttpRequestHelper _httpRequestMessageHelper;
        private readonly IResourceHelper _resourceHelper;
        private readonly ICosmosDBProvider _provider;
        private readonly IValidate _validate;
        private readonly IConvertToDynamic _convertToDynamic;
        private readonly ILogger<PatchContactHttpTrigger> _logger;
        private static readonly string[] PropertyToExclude = { "TargetSite" };

        public PatchContactHttpTrigger(IPatchContactDetailsHttpTriggerService contactdetailsPatchService,
            IHttpRequestHelper httpRequestMessageHelper,
            IResourceHelper resourceHelper,
            ICosmosDBProvider provider,
            IValidate validate,
            IConvertToDynamic convertToDynamic,
            ILogger<PatchContactHttpTrigger> logger)
        {
            _contactdetailsPatchService = contactdetailsPatchService;
            _httpRequestMessageHelper = httpRequestMessageHelper;
            _resourceHelper = resourceHelper;
            _provider = provider;
            _validate = validate;
            _convertToDynamic = convertToDynamic;
            _logger = logger;
        }

        [Function("PATCH")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Contact Details Patched", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Patch request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API Key unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient Access To This Resource", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NotFound, Description = "Resource Does Not Exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.UnprocessableEntity, Description = "Contact Details resource validation error(s)", ShowSchema = false)]

        [ProducesResponseType(typeof(ContactDetails), 200)]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "customers/{customerId}/ContactDetails/{contactId}")]
            HttpRequest req,
            string customerId, string contactId)
        {
            _logger.LogInformation("Function {FunctionName} has been invoked", nameof(PatchContactHttpTrigger));

            string requestBody = null;
            using (var reader = new StreamReader(req.Body))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            req.Body = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));

            if (!string.IsNullOrEmpty(requestBody))
            {
                try
                {
                    JsonDocument.Parse(requestBody);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Invalid JSON format: {ErrorMessage}", ex.Message);
                    return new BadRequestObjectResult("The JSON in the request body is in an invalid format.");
                }
            }

            var touchpointId = _httpRequestMessageHelper.GetDssTouchpointId(req);
            if (string.IsNullOrEmpty(touchpointId))
            {
                _logger.LogWarning("Unable to locate 'TouchpointId' in request header.");
                return new BadRequestObjectResult("Unable to locate 'TouchpointId' in request header.");
            }

            var apimURL = _httpRequestMessageHelper.GetDssApimUrl(req);
            if (string.IsNullOrEmpty(apimURL))
            {
                _logger.LogWarning("Unable to locate 'apimurl' in request header");
                return new BadRequestObjectResult("Unable to locate 'apimurl' in request header");
            }

            if (!Guid.TryParse(customerId, out var customerGuid))
            {
                _logger.LogWarning("Unable to parse 'customerId' to a GUID. Customer ID: {CustomerId}", customerId);
                return new BadRequestObjectResult($"Unable to parse {customerId} into a guid.");
            }

            if (!Guid.TryParse(contactId, out var contactGuid))
            {
                _logger.LogWarning("Unable to parse 'contactId' to a GUID. Contact ID: {ContactId}", contactId);
                return new BadRequestObjectResult($"Unable to parse {contactId} into a guid.");
            }

            _logger.LogInformation("Header validation has succeeded. Touchpoint ID: {TouchpointId}", touchpointId);

                ContactDetailsPatch contactDetailsPatchRequest;

                try
                {
                    contactDetailsPatchRequest = await _httpRequestMessageHelper.GetResourceFromRequest<ContactDetailsPatch>(req);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Json exception caught. Unable to parse ContactDetails from request body. Exception: {ExceptionMessage}", ex.Message);
                    return new UnprocessableEntityObjectResult($"Json exception caught. Unable to parse ContactDetails from request body. Exception: {ex.Message}");
                }

                if (contactDetailsPatchRequest == null)
                {
                    _logger.LogWarning("Unable to retrieve contact details from request data. {ContactDetailsPatch} object is NULL", nameof(contactDetailsPatchRequest));
                    return new UnprocessableEntityObjectResult("Contact details in request body are NULL. Please add data to request body.");
                }

                contactDetailsPatchRequest.LastModifiedTouchpointId = touchpointId;

                _logger.LogInformation("Attempting to check if customer exists. Customer GUID: {CustomerId}", customerGuid);
                var doesCustomerExist = await _resourceHelper.DoesCustomerExist(customerGuid);

                if (!doesCustomerExist)
                {
                    _logger.LogWarning("No customer with ID {CustomerGuid} exists", customerGuid);
                    return new NotFoundObjectResult($"No customer with ID [{customerGuid}]");
                }

                _logger.LogInformation("Customer exists. Customer GUID: {CustomerGuid}", customerGuid);

                _logger.LogInformation("Attempting to check if customer is read only. Customer GUID: {CustomerGuid}", customerGuid);
                var isCustomerReadOnly = await _resourceHelper.IsCustomerReadOnly(customerGuid);

                if (isCustomerReadOnly)
                {
                    _logger.LogWarning("Customer is read-only. Operation is forbidden. Customer GUID: {CustomerGuid}", customerGuid);
                    return new ObjectResult($"Customer with ID [{customerGuid}] is read only, operation forbidden.")
                    {
                        StatusCode = (int)HttpStatusCode.Forbidden
                    };
                }

                _logger.LogInformation("Customer is not read-only. Customer GUID: {CustomerGuid}", customerGuid);

                _logger.LogInformation("Attempting to retrieve ContactDetails. Customer GUID: {CustomerGuid}", customerGuid);
                var contactdetails = await _contactdetailsPatchService.GetContactDetailsForCustomerAsync(customerGuid, contactGuid);

                if (contactdetails == null)
                {
                    _logger.LogWarning("No contact with ID {ContactGuid} exist for Customer {CustomerGuid}", contactGuid, customerGuid);
                    return new NotFoundObjectResult($"No contact with ID [{contactGuid}]");
                }

                _logger.LogInformation("ContactDetails exists for Customer. Customer GUID: {CustomerGuid}", customerGuid);

                _logger.LogInformation("Attempting to validate {ContactDetailsPatch} object", nameof(contactDetailsPatchRequest));
                var errors = _validate.ValidateResource(contactDetailsPatchRequest, contactdetails, false);

                if (errors != null && errors.Any())
                {
                    _logger.LogWarning("Validation for {ContactDetailsPatch} object has failed", nameof(contactDetailsPatchRequest));
                    return new UnprocessableEntityObjectResult(errors);
                }

                _logger.LogInformation("Validation for {ContactDetailsPatch} object has passed", nameof(contactDetailsPatchRequest));

                if (!string.IsNullOrEmpty(contactDetailsPatchRequest.EmailAddress))
                {
                    _logger.LogInformation("Attempting to retrieve ContactDetails using the email address on the request. Customer GUID: {CustomerGuid}", customerGuid);
                    var contacts = await _provider.GetContactsByEmail(contactDetailsPatchRequest.EmailAddress);
                    if (contacts != null)
                    {
                        _logger.LogInformation("Customer has ContactDetails using the email address on the request. Customer GUID: {CustomerGuid}", customerGuid);

                        foreach (var contact in contacts)
                        {
                            _logger.LogInformation(
                                "Attempting to check if customer has a termination date. Customer ID: {CustomerId}",
                                contact.CustomerId.GetValueOrDefault());
                            var isReadOnly = await _provider.DoesCustomerHaveATerminationDate(contact.CustomerId.GetValueOrDefault());

                            if (!isReadOnly && contact.CustomerId != contactdetails.CustomerId)
                            {
                                _logger.LogWarning(
                                    "Customer already uses an email address that does not have a termination date. Email address on the request cannot be used. Customer ID: {CustomerId}. Contact Details ID: {ContactDetailsId}",
                                    contact.CustomerId.GetValueOrDefault(), contact.ContactId.GetValueOrDefault());
                            //if a customer that has the same email address is not readonly (has date of termination)
                            //then email address on the request cannot be used.
                                return new ConflictObjectResult($"Email address already in use by another customer (id: {contact.CustomerId}).");
                            }
                        }
                    }
                    _logger.LogWarning("Retrieving ContactDetails using the email address on the request has returned NULL. Customer GUID: {CustomerGuid}", customerGuid);
                }

                // Set Digital account properties so that contentenhancer can queue change on digital identity topic.
                _logger.LogInformation("Attempting to retrieve DigitalIdentity for customer. Customer GUID: {CustomerGuid}", customerGuid);
                var diaccount = await _provider.GetIdentityForCustomerAsync(contactdetails.CustomerId!.Value);
                if (diaccount != null)
                {
                    _logger.LogInformation(
                        "Customer has a Digital Identity account. Customer GUID: {CustomerGuid}. Identity Store ID: {IdentityStoreId}",
                        customerGuid, diaccount.CustomerId.GetValueOrDefault());

                    if (contactDetailsPatchRequest.EmailAddress == string.Empty)
                    {
                        if (errors == null)
                        {
                            errors = new List<ValidationResult>();
                        }

                        errors.Add(new ValidationResult("Email Address cannot be removed because it is associated with a Digital Account", new List<string> { "EmailAddress" }));
                        _logger.LogWarning("Email Address cannot be removed because it is associated with a Digital Account");
                        return new UnprocessableEntityObjectResult("Email Address cannot be removed because it is associated with a Digital Account");
                    }

                    if (!string.IsNullOrEmpty(contactdetails.EmailAddress) && !string.IsNullOrEmpty(contactDetailsPatchRequest.EmailAddress) && contactdetails.EmailAddress?.ToLower() != contactDetailsPatchRequest.EmailAddress?.ToLower() && diaccount.IdentityStoreId.HasValue)
                    {
                        _logger.LogInformation("Digital Identity account email address has been changed to email address on the request");
                        contactdetails.SetDigitalAccountEmailChanged(contactDetailsPatchRequest.EmailAddress?.ToLower(), diaccount.IdentityStoreId.Value);
                    }
                }

                _logger.LogInformation("Attempting to PATCH a ContactDetails. Customer GUID: {CustomerGuid}", customerGuid);
                var updatedContactDetails = await _contactdetailsPatchService.UpdateAsync(contactdetails, contactDetailsPatchRequest);

                if (updatedContactDetails == null)
                {
                    _logger.LogError("PATCH request unsuccessful. Customer GUID: {CustomerGuid}", customerGuid);
                    _logger.LogInformation("Function {FunctionName} has finished invoking", nameof(PatchContactHttpTrigger));

                    return new BadRequestObjectResult($"Failed to PATCH contact details {contactGuid} in Cosmos DB. Contact details are NULL after creation attempt.");
                }

                _logger.LogInformation("Sending newly created ContactDetails to service bus. Customer GUID: {CustomerGuid}. Contact Details ID: {contactDetailsId}", customerGuid, updatedContactDetails.ContactId.GetValueOrDefault());
                await _contactdetailsPatchService.SendToServiceBusQueueAsync(updatedContactDetails, customerGuid, apimURL);

                _logger.LogInformation("PATCH request successful. Contact Details ID: {ContactDetailsId}", updatedContactDetails.ContactId.GetValueOrDefault());
                _logger.LogInformation("Function {FunctionName} has finished invoking", nameof(PatchContactHttpTrigger));

                return new JsonResult(updatedContactDetails, new JsonSerializerOptions())
                {
                    StatusCode = (int)HttpStatusCode.OK
                };
            }
        }
    }
