using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.Helpers;
using NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Function;
using NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.Validation;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace NCS.DSS.Contact.Tests
{
    [TestFixture]
    public class PatchContactHttpTriggerTests
    {
        private const string ValidCustomerId = "7E467BDB-213F-407A-B86A-1954053D3C24";
        private const string ValidContactId = "1e1a555c-9633-4e12-ab28-09ed60d51cb3";
        private const string InValidId = "1111111-2222-3333-4444-555555555555";
        private ILogger _log;
        private HttpRequestMessage _request;
        private IResourceHelper _resourceHelper;
        private IValidate _validate;
        private IHttpRequestMessageHelper _httpRequestMessageHelper;
        private IPatchContactDetailsHttpTriggerService _patchContactHttpTriggerService;
        private Models.ContactDetails _contactDetails;
        private Models.ContactDetailsPatch _contactDetailsPatch;

        [SetUp]
        public void Setup()
        {
            _contactDetails = Substitute.For<Models.ContactDetails>();
            _contactDetailsPatch = Substitute.For<Models.ContactDetailsPatch>();

            _request = new HttpRequestMessage()
            {
                Content = new StringContent(string.Empty),
                RequestUri = 
                    new Uri($"http://localhost:7071/api/Customers/7E467BDB-213F-407A-B86A-1954053D3C24/ContactDetails/1e1a555c-9633-4e12-ab28-09ed60d51cb3")
            };

            _log = Substitute.For<ILogger>();
            _resourceHelper = Substitute.For<IResourceHelper>();
            _validate = Substitute.For<IValidate>();
            _httpRequestMessageHelper = Substitute.For<IHttpRequestMessageHelper>();
            _patchContactHttpTriggerService = Substitute.For<IPatchContactDetailsHttpTriggerService>();
            _httpRequestMessageHelper.GetTouchpointId(_request).Returns("0000000001");
            _httpRequestMessageHelper.GetApimURL(_request).Returns("http://localhost:7071/");
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeBadRequest_WhenTouchpointIdIsNotProvided()
        {
            _httpRequestMessageHelper.GetTouchpointId(_request).Returns((string)null);

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeBadRequest_WhenCustomerIdIsInvalid()
        {
            var result = await RunFunction(InValidId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeUnprocessableEntity_WhenContactHasFailedValidation()
        {
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetailsPatch>(_request).Returns(Task.FromResult(_contactDetailsPatch).Result);

            var validationResults = new List<ValidationResult> { new ValidationResult("contactDetail Id is Required") };
            _validate.ValidateResource(Arg.Any<Models.ContactDetailsPatch>(), _contactDetails, false).Returns(validationResults);

            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual((HttpStatusCode)204, result.StatusCode);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeUnprocessableEntity_WhenContactRequestIsInvalid()
        {
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetailsPatch>(_request).Throws(new JsonException());
            
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual((HttpStatusCode)422, result.StatusCode);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeNoContent_WhenCustomerDoesNotExist()
        {
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetailsPatch>(_request).Returns(Task.FromResult(_contactDetailsPatch).Result);

            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).Returns(false);

            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeNoContent_WhenContactDoesNotExist()
        {
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetailsPatch>(_request).Returns(Task.FromResult(_contactDetailsPatch).Result);

            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).ReturnsForAnyArgs(true);

            _patchContactHttpTriggerService.GetContactDetailsForCustomerAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(Task.FromResult<Models.ContactDetails>(null).Result);

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeBadRequest_WhenUnableToUpdateContactRecord()
        {
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetailsPatch>(_request).Returns(Task.FromResult(_contactDetailsPatch).Result);

            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).ReturnsForAnyArgs(true);

            _patchContactHttpTriggerService.GetContactDetailsForCustomerAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(Task.FromResult<Models.ContactDetails>(_contactDetails).Result);

            _patchContactHttpTriggerService.UpdateAsync(Arg.Any<Models.ContactDetails>(), Arg.Any<Models.ContactDetailsPatch>()).Returns(Task.FromResult<Models.ContactDetails>(null).Result);

            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeOK_WhenRequestIsNotValid()
        {
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetailsPatch>(_request).Returns(Task.FromResult(_contactDetailsPatch).Result);

            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).ReturnsForAnyArgs(true);

            _patchContactHttpTriggerService.GetContactDetailsForCustomerAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(Task.FromResult(_contactDetails).Result);

            _patchContactHttpTriggerService.UpdateAsync(Arg.Any<Models.ContactDetails>(), Arg.Any<Models.ContactDetailsPatch>()).Returns(Task.FromResult<Models.ContactDetails>(null).Result);

            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeOK_WhenRequestIsValid()
        {
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetailsPatch>(_request).Returns(Task.FromResult(_contactDetailsPatch).Result);

            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).ReturnsForAnyArgs(true);

            _patchContactHttpTriggerService.GetContactDetailsForCustomerAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(Task.FromResult(_contactDetails).Result);

            _patchContactHttpTriggerService.UpdateAsync(Arg.Any<Models.ContactDetails>(), Arg.Any<Models.ContactDetailsPatch>()).Returns(Task.FromResult(_contactDetails).Result);

            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        }

        private async Task<HttpResponseMessage> RunFunction(string customerId, string contactDetailId)
        {
            return await PatchContactHttpTrigger.RunAsync(
                _request, _log, customerId, contactDetailId, _resourceHelper, _httpRequestMessageHelper, _validate, _patchContactHttpTriggerService).ConfigureAwait(false);
        }

    }
}