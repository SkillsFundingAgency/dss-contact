using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using DFC.HTTP.Standard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.Cosmos.Provider;
using NCS.DSS.Contact.Models;
using NCS.DSS.Contact.PostContactDetailsHttpTrigger.Function;
using NCS.DSS.Contact.PostContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.ReferenceData;
using NCS.DSS.Contact.Validation;
using Newtonsoft.Json;
using NUnit.Framework;

namespace NCS.DSS.Contact.Tests
{
    [TestFixture]
    public class PostContactHttpTriggerTests
    {
        private const string ValidCustomerId = "7E467BDB-213F-407A-B86A-1954053D3C24";
        private const string InValidId = "1111111-2222-3333-4444-555555555555";

        private Mock<IPostContactDetailsHttpTriggerService> _postContactHttpTriggerService;
        private Mock<IHttpRequestHelper> _httpRequestMessageHelper;
        private Mock<IResourceHelper> _resourceHelper;
        private Mock<ICosmosDBProvider> _provider;
        private Mock<IConvertToDynamic> _convertToDynamic;
        private Mock<ILogger<PostContactHttpTrigger>> _logger;

        private ContactDetails _contactDetails;
        private IValidate _validate;
        private HttpRequest _request;
        private PostContactHttpTrigger _function;

        [SetUp]
        public void Setup()
        {
            _postContactHttpTriggerService = new Mock<IPostContactDetailsHttpTriggerService>();
            _httpRequestMessageHelper = new Mock<IHttpRequestHelper>();
            _resourceHelper = new Mock<IResourceHelper>();
            _provider = new Mock<ICosmosDBProvider>();
            _validate = new Validate();
            _convertToDynamic = new Mock<IConvertToDynamic>();
            _logger = new Mock<ILogger<PostContactHttpTrigger>>();

            _contactDetails = new ContactDetails { PreferredContactMethod = PreferredContactMethod.Email, EmailAddress = "some@test.com" };
            _request = new DefaultHttpContext().Request;
            _function = new PostContactHttpTrigger(
                _postContactHttpTriggerService.Object,
                _httpRequestMessageHelper.Object,
                _resourceHelper.Object,
                _provider.Object,
                _validate,
                _convertToDynamic.Object,
                _logger.Object
                );
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeBadRequest_WhenTouchpointIdIsNotProvided()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns((string)null);

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
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
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeUnprocessableEntity_WhenContactHasFailedValidation()
        {
            //Arrange
            _contactDetails.EmailAddress = null;
            _contactDetails.PreferredContactMethod = PreferredContactMethod.Email;
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<ContactDetails>(_request))
                .Returns(Task.FromResult(_contactDetails));

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.That(result, Is.InstanceOf<UnprocessableEntityObjectResult>());
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeUnprocessableEntity_WhenContactRequestIsInvalid()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<ContactDetails>(_request))
                .Throws(new JsonException());

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.That(result, Is.InstanceOf<UnprocessableEntityObjectResult>());
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeNoContent_WhenCustomerDoesNotExist()
        {
            //Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<ContactDetails>(_request))
                .Returns(Task.FromResult(_contactDetails));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(false));

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeConflict_WhenContactDetailsForCustomerExists()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<ContactDetails>(_request))
                .Returns(Task.FromResult(_contactDetails));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _postContactHttpTriggerService.Setup(x => x.DoesContactDetailsExistForCustomer(It.IsAny<Guid>()))
                .Returns(Task.FromResult(true));

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.That(result, Is.InstanceOf<ConflictResult>());
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeBadRequest_WhenUnableToCreateContactRecord()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<ContactDetails>(_request))
                .Returns(Task.FromResult(_contactDetails));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _postContactHttpTriggerService.Setup(x => x.CreateAsync(It.IsAny<ContactDetails>()))
                .Returns(Task.FromResult<ContactDetails>(null));

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeBadRequest_WhenRequestNotIsValid()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<ContactDetails>(_request))
                .Returns(Task.FromResult(_contactDetails));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _postContactHttpTriggerService.Setup(x => x.CreateAsync(It.IsAny<ContactDetails>()))
                .Returns(Task.FromResult<ContactDetails>(null));

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeCreated_WhenRequestIsValid()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<ContactDetails>(_request))
                .Returns(Task.FromResult(_contactDetails));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _postContactHttpTriggerService.Setup(x => x.CreateAsync(It.IsAny<ContactDetails>()))
                .Returns(Task.FromResult(_contactDetails));

            // Act
            var result = await RunFunction(ValidCustomerId);
            var responseResult = result as JsonResult;

            //Assert
            Assert.That(responseResult, Is.InstanceOf<JsonResult>());
            Assert.That(responseResult.StatusCode, Is.EqualTo((int)HttpStatusCode.Created));
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeConflict_WhenEmailAlreadyExists()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            var contactDetails = new ContactDetails
            {
                PreferredContactMethod = PreferredContactMethod.Email,
                EmailAddress = "test@test.com",
                CustomerId = new Guid(ValidCustomerId)
            };
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<ContactDetails>(_request))
                .Returns(Task.FromResult(contactDetails));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _provider.Setup(x => x.GetContactsByEmail(It.IsAny<string>())).Returns(
                Task.FromResult<IList<ContactDetails>>(new List<ContactDetails> { new ContactDetails() }));
            _provider.Setup(x => x.DoesCustomerHaveATerminationDate(It.IsAny<Guid>())).Returns(Task.FromResult(false));
            _postContactHttpTriggerService.Setup(x => x.CreateAsync(It.IsAny<ContactDetails>()))
                .Returns(Task.FromResult(contactDetails));

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.That(result, Is.InstanceOf<ConflictResult>());
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeCreated_WhenEmailAlreadyExistsThatIsTerminated()
        {
            // Arrange
            var contactDetails = new ContactDetails
            {
                PreferredContactMethod = PreferredContactMethod.Email, EmailAddress = "test@test.com",
                CustomerId = new Guid(ValidCustomerId)
            };

            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<ContactDetails>(_request))
                .Returns(Task.FromResult(contactDetails));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _provider.Setup(x => x.GetContactsByEmail(It.IsAny<string>())).Returns(
                Task.FromResult<IList<ContactDetails>>(new List<ContactDetails> { new ContactDetails() }));
            _provider.Setup(x => x.DoesCustomerHaveATerminationDate(It.IsAny<Guid>())).Returns(Task.FromResult(false));
            _postContactHttpTriggerService.Setup(x => x.CreateAsync(It.IsAny<ContactDetails>()))
                .Returns(Task.FromResult(contactDetails));
            _provider.Setup(x => x.GetContactsByEmail(It.IsAny<string>())).Returns(
                Task.FromResult<IList<ContactDetails>>(new List<ContactDetails>
                    { new ContactDetails { CustomerId = Guid.NewGuid() } }));
            _provider.Setup(x => x.DoesCustomerHaveATerminationDate(It.IsAny<Guid>())).Returns(Task.FromResult(true));

            // Act
            var result = await RunFunction(ValidCustomerId);
            var responseResult = result as JsonResult;

            //Assert
            Assert.That(responseResult, Is.InstanceOf<JsonResult>());
            Assert.That(responseResult.StatusCode, Is.EqualTo((int)HttpStatusCode.Created));
        }

        [Test]
        public async Task PostContactContactPrefHttpTrigger_ReturnsStatusCodeCreated_WhenRequestIsValid()
        {
            // Arrange
            var contactDetails = new ContactDetails
            {
                PreferredContactMethod = PreferredContactMethod.Email,
                EmailAddress = "sillyemail@test.com",
                CustomerId = new Guid(ValidCustomerId)
            };
            
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<ContactDetails>(_request))
                .Returns(Task.FromResult(contactDetails));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _provider.Setup(x => x.GetIdentityForCustomerAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult<DigitalIdentity>(null));
            _provider.Setup(x => x.GetContactsByEmail(It.IsAny<string>())).Returns(
                Task.FromResult<IList<ContactDetails>>(new List<ContactDetails> { new ContactDetails() }));
            _provider.Setup(x => x.DoesCustomerHaveATerminationDate(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _postContactHttpTriggerService.Setup(x => x.CreateAsync(It.IsAny<ContactDetails>()))
                .Returns(Task.FromResult(contactDetails));

            // Capture the uploaded contact details
            ContactDetails uploadedContactDetails = null;
            _postContactHttpTriggerService.Setup(x => x.CreateAsync(It.IsAny<ContactDetails>()))
                                           .Callback<ContactDetails>(contact =>
                                           {
                                               contact.PreferredContactMethod = contact.PreferredContactMethod; // Ensure the post is applied
                                               uploadedContactDetails = contact;
                                           })
                                           .Returns(Task.FromResult(contactDetails));

            // Act
            var result = await RunFunction(ValidCustomerId);
            var responseResult = result as JsonResult;

            //Assert
            Assert.That(responseResult, Is.InstanceOf<JsonResult>());
            Assert.That(responseResult.StatusCode, Is.EqualTo((int)HttpStatusCode.Created));
            Assert.That(uploadedContactDetails.PreferredContactMethod,
                Is.EqualTo(PreferredContactMethod.Email),
                $"Expected: {PreferredContactMethod.Email}, But was: {uploadedContactDetails?.PreferredContactMethod}");
        }

        [Test]
        public async Task PostContactHttpTrigger_ReturnsStatusCodeUnprocessableEntity_WhenNoMobileNumberSuppliedForWhatsappPreferredContactMethod()
        {
            //Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            var contactDetails = new ContactDetails
                { PreferredContactMethod = PreferredContactMethod.WhatsApp, CustomerId = new Guid(ValidCustomerId) };
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<ContactDetails>(_request))
                .Returns(Task.FromResult(contactDetails));

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.That(result, Is.InstanceOf<UnprocessableEntityObjectResult>());
        }

        private async Task<IActionResult> RunFunction(string customerId)
        {
            return await _function.RunAsync(
                _request, customerId).ConfigureAwait(false);
        }
    }
}