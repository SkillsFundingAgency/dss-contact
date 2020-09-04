using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.Cosmos.Provider;
using NCS.DSS.Contact.Helpers;
using NCS.DSS.Contact.Models;
using NCS.DSS.Contact.PostContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.Validation;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace NCS.DSS.Contact.Tests
{
    [TestFixture]
    public class PostContactHttpTriggerTests
    {
        private const string ValidCustomerId = "7E467BDB-213F-407A-B86A-1954053D3C24";
        private const string InValidId = "1111111-2222-3333-4444-555555555555";
        private ILogger _log;
        private HttpRequestMessage _request;
        private IResourceHelper _resourceHelper;
        private IValidate _validate;
        private IHttpRequestMessageHelper _httpRequestMessageHelper;
        private IPostContactDetailsHttpTriggerService _postContactHttpTriggerService;
        private Models.ContactDetails _contactDetails;
        private IDocumentDBProvider _provider;

        [SetUp]
        public void Setup()
        {
            _contactDetails = Substitute.For<Models.ContactDetails>();

            _request = new HttpRequestMessage()
            {
                Content = new StringContent(string.Empty),
                RequestUri = 
                    new Uri($"http://localhost:7071/api/Customers/7E467BDB-213F-407A-B86A-1954053D3C24/ContactDetails/")
            };

            _log = Substitute.For<ILogger>();
            _resourceHelper = Substitute.For<IResourceHelper>();
            _httpRequestMessageHelper = Substitute.For<IHttpRequestMessageHelper>();
            _validate = Substitute.For<IValidate>();
            _postContactHttpTriggerService = Substitute.For<IPostContactDetailsHttpTriggerService>();
            _httpRequestMessageHelper.GetTouchpointId(_request).Returns("0000000001");
            _httpRequestMessageHelper.GetApimURL(_request).Returns("http://localhost:7071/");
            _provider = Substitute.For<IDocumentDBProvider>();
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeBadRequest_WhenTouchpointIdIsNotProvided()
        {
            _httpRequestMessageHelper.GetTouchpointId(_request).Returns((string)null);

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }
        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeBadRequest_WhenCustomerIdIsInvalid()
        {
            var result = await RunFunction(InValidId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeUnprocessableEntity_WhenContactHasFailedValidation()
        {
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetails>(_request).Returns(Task.FromResult(_contactDetails).Result);

            var validationResults = new List<ValidationResult> { new ValidationResult("contactDetail Id is Required") };
            _validate.ValidateResource(Arg.Any<Models.ContactDetails>(), true).Returns(validationResults);

            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual((HttpStatusCode)422, result.StatusCode);
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeUnprocessableEntity_WhenContactRequestIsInvalid()
        {
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetailsPatch>(_request).Throws(new JsonException());

            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual((HttpStatusCode)422, result.StatusCode);
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeNoContent_WhenCustomerDoesNotExist()
        {
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetails>(_request).Returns(Task.FromResult(_contactDetails).Result);

            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).Returns(false);

            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeConflict_WhenContactDetailsForCustomerExists()
        {
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetails>(_request).Returns(Task.FromResult(_contactDetails).Result);

            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).Returns(true);
            _postContactHttpTriggerService.DoesContactDetailsExistForCustomer(Arg.Any<Guid>()).ReturnsForAnyArgs(true);

            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.Conflict, result.StatusCode);
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeBadRequest_WhenUnableToCreateContactRecord()
        {
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetails>(_request).Returns(Task.FromResult(_contactDetails).Result);

            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).ReturnsForAnyArgs(true);

            _postContactHttpTriggerService.CreateAsync(Arg.Any<Models.ContactDetails>()).Returns(Task.FromResult<Models.ContactDetails>(null).Result);

            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeCreated_WhenRequestNotIsValid()
        {
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetails>(_request).Returns(Task.FromResult(_contactDetails).Result);

            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).ReturnsForAnyArgs(true);

            _postContactHttpTriggerService.CreateAsync(Arg.Any<Models.ContactDetails>()).Returns(Task.FromResult<Models.ContactDetails>(null).Result);

            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeCreated_WhenRequestIsValid()
        {
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetails>(_request).Returns(Task.FromResult(_contactDetails).Result);

            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).ReturnsForAnyArgs(true);

            _postContactHttpTriggerService.CreateAsync(Arg.Any<Models.ContactDetails>()).Returns(Task.FromResult(_contactDetails).Result);

            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeConflict_WhenEmailAlreadyExists()
        {
            // Arrange
            var contactDetails = new ContactDetails() { EmailAddress = "test@test.com", CustomerId = new Guid(ValidCustomerId) };
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetails>(_request).Returns(Task.FromResult(contactDetails).Result);
            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).ReturnsForAnyArgs(true);
            _provider.GetContactsByEmail(Arg.Any<string>()).Returns(Task.FromResult<IList<ContactDetails>>(new List<ContactDetails>() { new ContactDetails() }));
            _provider.DoesCustomerHaveATerminationDate(Arg.Any<Guid>()).Returns(Task.FromResult(false));
            _postContactHttpTriggerService.CreateAsync(Arg.Any<Models.ContactDetails>()).Returns(Task.FromResult(contactDetails).Result);

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.Conflict, result.StatusCode);
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeCreated_WhenEmailAlreadyExistsThatIsTerminated()
        {
            // Arrange
            var contactDetails = new ContactDetails() { EmailAddress = "test@test.com", CustomerId = new Guid(ValidCustomerId) };
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetails>(_request).Returns(Task.FromResult(contactDetails).Result);
            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).ReturnsForAnyArgs(true);
            _provider.GetContactsByEmail(Arg.Any<string>()).Returns(Task.FromResult<IList<ContactDetails>>(new List<ContactDetails>() { new ContactDetails() }));
            _provider.DoesCustomerHaveATerminationDate(Arg.Any<Guid>()).Returns(Task.FromResult(false));
            _postContactHttpTriggerService.CreateAsync(Arg.Any<Models.ContactDetails>()).Returns(Task.FromResult(contactDetails).Result);
            _provider.GetContactsByEmail(Arg.Any<string>()).Returns(Task.FromResult<IList<ContactDetails>>(new List<ContactDetails>() { new ContactDetails() { CustomerId = Guid.NewGuid() } }));
            _provider.DoesCustomerHaveATerminationDate(Arg.Any<Guid>()).Returns(Task.FromResult(true));

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        }

        private async Task<HttpResponseMessage> RunFunction(string customerId)
        {
            return await PostContactDetailsHttpTrigger.Function.PostContactByIdHttpTrigger.RunAsync(
                _request, _log, customerId, _resourceHelper, _httpRequestMessageHelper, _validate, _postContactHttpTriggerService, _provider).ConfigureAwait(false);
        }

    }
}