using DFC.HTTP.Standard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.Cosmos.Provider;
using NCS.DSS.Contact.Models;
using NCS.DSS.Contact.PostContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.Validation;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace NCS.DSS.Contact.Tests
{
    [TestFixture]
    public class PostContactHttpTriggerTests
    {
        private const string ValidCustomerId = "7E467BDB-213F-407A-B86A-1954053D3C24";
        private const string InValidId = "1111111-2222-3333-4444-555555555555";
        private Mock<ILogger> _log;
        private HttpRequest _request;
        private Mock<IResourceHelper> _resourceHelper;
        private IValidate _validate;
        private Mock<IHttpRequestHelper> _httpRequestMessageHelper;
        private Mock<IPostContactDetailsHttpTriggerService> _postContactHttpTriggerService;
        private Models.ContactDetails _contactDetails;
        private Mock<IDocumentDBProvider> _provider;
        private IHttpResponseMessageHelper _httpResponseMessageHelper;
        private PostContactDetailsHttpTrigger.Function.PostContactByIdHttpTrigger _function;

        [SetUp]
        public void Setup()
        {
            _contactDetails = new Models.ContactDetails() { PreferredContactMethod = ReferenceData.PreferredContactMethod.Email, EmailAddress = "some@test.com" };
            _request = new DefaultHttpRequest(new DefaultHttpContext());
            _log = new Mock<ILogger>();
            _resourceHelper = new Mock<IResourceHelper>();
            _httpRequestMessageHelper = new Mock<IHttpRequestHelper>();
            _validate = new Validate();
            _postContactHttpTriggerService = new Mock<IPostContactDetailsHttpTriggerService>();
            _provider = new Mock<IDocumentDBProvider>();
            _httpResponseMessageHelper = new HttpResponseMessageHelper();
            _function = new PostContactDetailsHttpTrigger.Function.PostContactByIdHttpTrigger(_resourceHelper.Object, _httpRequestMessageHelper.Object, _validate, _postContactHttpTriggerService.Object, _provider.Object, _httpResponseMessageHelper);
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeBadRequest_WhenTouchpointIdIsNotProvided()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns((string)null);

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeBadRequest_WhenCustomerIdIsInvalid()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");

            // Act
            var result = await RunFunction(InValidId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeUnprocessableEntity_WhenContactHasFailedValidation()
        {
            //Arrange
            _contactDetails.EmailAddress = null;
            _contactDetails.PreferredContactMethod = ReferenceData.PreferredContactMethod.Email;
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<Models.ContactDetails>(_request)).Returns(Task.FromResult(_contactDetails));

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual((HttpStatusCode)422, result.StatusCode);
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeUnprocessableEntity_WhenContactRequestIsInvalid()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x=>x.GetResourceFromRequest<Models.ContactDetails>(_request)).Throws(new JsonException());

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual((HttpStatusCode)422, result.StatusCode);
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeNoContent_WhenCustomerDoesNotExist()
        {
            //Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<Models.ContactDetails>(_request)).Returns(Task.FromResult(_contactDetails));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(false));

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeConflict_WhenContactDetailsForCustomerExists()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<Models.ContactDetails>(_request)).Returns(Task.FromResult(_contactDetails));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _postContactHttpTriggerService.Setup(x => x.DoesContactDetailsExistForCustomer(It.IsAny<Guid>())).Returns(true);

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.Conflict, result.StatusCode);
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeBadRequest_WhenUnableToCreateContactRecord()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<Models.ContactDetails>(_request)).Returns(Task.FromResult(_contactDetails));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _postContactHttpTriggerService.Setup(x => x.CreateAsync(It.IsAny<Models.ContactDetails>())).Returns(Task.FromResult<Models.ContactDetails>(null));

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeCreated_WhenRequestNotIsValid()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<Models.ContactDetails>(_request)).Returns(Task.FromResult(_contactDetails));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _postContactHttpTriggerService.Setup(x => x.CreateAsync(It.IsAny<Models.ContactDetails>())).Returns(Task.FromResult<Models.ContactDetails>(null));

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeCreated_WhenRequestIsValid()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<Models.ContactDetails>(_request)).Returns(Task.FromResult(_contactDetails));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _postContactHttpTriggerService.Setup(x=>x.CreateAsync(It.IsAny<Models.ContactDetails>())).Returns(Task.FromResult(_contactDetails));

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeConflict_WhenEmailAlreadyExists()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            var contactDetails = new ContactDetails() { PreferredContactMethod= ReferenceData.PreferredContactMethod.Email, EmailAddress = "test@test.com", CustomerId = new Guid(ValidCustomerId) };
            _httpRequestMessageHelper.Setup(x=>x.GetResourceFromRequest<Models.ContactDetails>(_request)).Returns(Task.FromResult(contactDetails));
            _resourceHelper.Setup(x=>x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _provider.Setup(x=>x.GetContactsByEmail(It.IsAny<string>())).Returns(Task.FromResult<IList<ContactDetails>>(new List<ContactDetails>() { new ContactDetails() }));
            _provider.Setup(x=>x.DoesCustomerHaveATerminationDate(It.IsAny<Guid>())).Returns(Task.FromResult(false));
            _postContactHttpTriggerService.Setup(x=>x.CreateAsync(It.IsAny<Models.ContactDetails>())).Returns(Task.FromResult(contactDetails));

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
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            var contactDetails = new ContactDetails() {  PreferredContactMethod= ReferenceData.PreferredContactMethod.Email, EmailAddress = "test@test.com", CustomerId = new Guid(ValidCustomerId) };
            _httpRequestMessageHelper.Setup(x=>x.GetResourceFromRequest<Models.ContactDetails>(_request)).Returns(Task.FromResult(contactDetails));
            _resourceHelper.Setup(x=>x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _provider.Setup(x=>x.GetContactsByEmail(It.IsAny<string>())).Returns(Task.FromResult<IList<ContactDetails>>(new List<ContactDetails>() { new ContactDetails() }));
            _provider.Setup(x=>x.DoesCustomerHaveATerminationDate(It.IsAny<Guid>())).Returns(Task.FromResult(false));
            _postContactHttpTriggerService.Setup(x=>x.CreateAsync(It.IsAny<Models.ContactDetails>())).Returns(Task.FromResult(contactDetails));
            _provider.Setup(x=>x.GetContactsByEmail(It.IsAny<string>())).Returns(Task.FromResult<IList<ContactDetails>>(new List<ContactDetails>() { new ContactDetails() { CustomerId = Guid.NewGuid() } }));
            _provider.Setup(x=>x.DoesCustomerHaveATerminationDate(It.IsAny<Guid>())).Returns(Task.FromResult(true));

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeUnprocessableEntity_WhenNoMobileNumberSuppliedForWhatsappPreferredContactMethod()
        {
            //Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            var contactDetails = new ContactDetails() { PreferredContactMethod = ReferenceData.PreferredContactMethod.WhatsApp, CustomerId = new Guid(ValidCustomerId) };
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<Models.ContactDetails>(_request)).Returns(Task.FromResult(contactDetails));

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(422, (int)result.StatusCode);
        }

        private async Task<HttpResponseMessage> RunFunction(string customerId)
        {
            return await _function.RunAsync(
                _request, _log.Object, customerId).ConfigureAwait(false);
        }

    }
}