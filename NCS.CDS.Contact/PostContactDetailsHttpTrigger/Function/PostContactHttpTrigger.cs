using DFC.HTTP.Standard;
using DFC.Swagger.Standard.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.Cosmos.Provider;
using NCS.DSS.Contact.Helpers;
using NCS.DSS.Contact.PostContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.Validation;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace NCS.DSS.Contact.PostContactDetailsHttpTrigger.Function
{
    public class PostContactByIdHttpTrigger
    {
        private readonly IResourceHelper _resourceHelper;
        private readonly IHttpRequestHelper _httpRequestMessageHelper;
        private readonly IValidate _validate;
        private readonly IPostContactDetailsHttpTriggerService _contactdetailsPostService;
        private readonly IDocumentDBProvider _provider;
        private readonly IHttpResponseMessageHelper _responseHelper;

        public PostContactByIdHttpTrigger( IResourceHelper resourceHelper,
            IHttpRequestHelper httpRequestMessageHelper,
            IValidate validate,
            IPostContactDetailsHttpTriggerService contactdetailsPostService,
            IDocumentDBProvider provider,
            IHttpResponseMessageHelper responseHelper)
        {
            _resourceHelper = resourceHelper;
            _httpRequestMessageHelper = httpRequestMessageHelper;
            _validate = validate;
            _contactdetailsPostService = contactdetailsPostService;
            _provider = provider;
            _responseHelper = responseHelper;
        }

        [FunctionName("POST")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Contact Details Added", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Resource Does Not Exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Post request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API Key unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient Access To This Resource", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Conflict, Description = "Contact Details already exists for customer", ShowSchema = false)]
        [Response(HttpStatusCode = (int)422, Description = "Contact Details resource validation error(s)", ShowSchema = false)]
        [ProducesResponseType(typeof(Contact.Models.ContactDetails), 200)]
        public async Task<HttpResponseMessage> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customers/{customerId}/ContactDetails/")]HttpRequest req, ILogger log, 
            string customerId)
        {

            try
            {
                JsonHelper.SerializeObject(JsonConvert.DeserializeObject(await new StreamReader(req.Body).ReadToEndAsync()));
            }
            catch
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Invalid JSON format in the request body.")
                };
            }

            var touchpointId = _httpRequestMessageHelper.GetDssTouchpointId(req);
            if (string.IsNullOrEmpty(touchpointId))
            {
                log.LogInformation("Unable to locate 'TouchpointId' in request header.");
                return _responseHelper.BadRequest();
            }

            var ApimURL = _httpRequestMessageHelper.GetDssApimUrl(req);
            if (string.IsNullOrEmpty(ApimURL))
            {
                log.LogInformation("Unable to locate 'apimurl' in request header");
                return _responseHelper.BadRequest();
            }

            log.LogInformation("C# HTTP trigger function Post Contact processed a request. " + touchpointId);

            if (!Guid.TryParse(customerId, out var customerGuid))
                return _responseHelper.BadRequest(customerGuid);

            Models.ContactDetails contactdetailsRequest;
            try
            {
                contactdetailsRequest = await _httpRequestMessageHelper.GetResourceFromRequest<Contact.Models.ContactDetails>(req);
            }
            catch (JsonException ex)
            {
                return _responseHelper.UnprocessableEntity(ex);
            }

            if (contactdetailsRequest == null)
                return _responseHelper.UnprocessableEntity(req);

            contactdetailsRequest.SetIds(customerGuid, touchpointId);

            var errors = _validate.ValidateResource(contactdetailsRequest, null, true);

            if (errors != null && errors.Any())
                return _responseHelper.UnprocessableEntity(errors);

            var doesCustomerExist = await _resourceHelper.DoesCustomerExist(customerGuid);

            if (!doesCustomerExist)
                return _responseHelper.NoContent(customerGuid);

            var isCustomerReadOnly = await _resourceHelper.IsCustomerReadOnly(customerGuid);

            if (isCustomerReadOnly)
                return _responseHelper.Forbidden(customerGuid);

            var doesContactDetailsExist = _contactdetailsPostService.DoesContactDetailsExistForCustomer(customerGuid);

            if (doesContactDetailsExist)
                return _responseHelper.Conflict();

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
                            return _responseHelper.Conflict();
                        }
                    }
                }
            }

            var contactDetails = await _contactdetailsPostService.CreateAsync(contactdetailsRequest);

            if (contactDetails != null)
                await _contactdetailsPostService.SendToServiceBusQueueAsync(contactDetails, ApimURL);

            return contactDetails == null
                ? _responseHelper.BadRequest(customerGuid)
                : _responseHelper.Created(JsonHelper.SerializeObject(contactDetails));
        }

    }
}
