using DFC.HTTP.Standard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.GetContactDetailsHttpTrigger.Function;
using NCS.DSS.Contact.GetContactDetailsHttpTrigger.Service;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace NCS.DSS.Contact.Tests
{
    [TestFixture]
    public class GetContactHttpTriggerTests
    {
        private const string ValidCustomerId = "7E467BDB-213F-407A-B86A-1954053D3C24";
        private const string InValidId = "1111111-2222-3333-4444-555555555555";
        private Mock<ILogger> _log;
        private HttpRequest _request;
        private Mock<IResourceHelper> _resourceHelper;
        private Mock<IHttpRequestHelper> _httpRequestHelper;
        private Mock<IGetContactHttpTriggerService> _getContactHttpTriggerService;
        private Models.ContactDetails _contact;
        private GetContactHttpTrigger _function;
        private IHttpResponseMessageHelper _httpResponseMessageHelper;

        [SetUp]
        public void Setup()
        {
            _contact = new Models.ContactDetails();
            _request = new DefaultHttpRequest(new DefaultHttpContext());
            _log = new Mock<ILogger>();
            _resourceHelper = new Mock<IResourceHelper>();
            _httpRequestHelper = new Mock<IHttpRequestHelper>();
            _getContactHttpTriggerService = new Mock<IGetContactHttpTriggerService>();
            _httpResponseMessageHelper = new HttpResponseMessageHelper();
            _function = new GetContactHttpTrigger(_resourceHelper.Object, _httpRequestHelper.Object, _getContactHttpTriggerService.Object, _httpResponseMessageHelper);
        }

        [Test]
        public async Task GetContactHttpTrigger_ReturnsStatusCodeBadRequest_WhenTouchpointIdIsNotProvided()
        {
            // Arrange
            _httpRequestHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns((string)null);

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task GetContactHttpTrigger_ReturnsStatusCodeBadRequest_WhenCustomerIdIsInvalid()
        {
            // Arrange
            _httpRequestHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");

            // Act
            var result = await RunFunction(InValidId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task GetContactHttpTrigger_ReturnsStatusCodeNoContent_WhenCustomerDoesNotExist()
        {
            // Arrange
            _httpRequestHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _resourceHelper.Setup(x=>x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(false));

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
        }

        [Test]
        public async Task GetContactHttpTrigger_ReturnsStatusCodeNoContent_WhenContactDoesNotExist()
        {
            // Arrange
            _httpRequestHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _getContactHttpTriggerService.Setup(x=>x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>())).Returns(Task.FromResult<Models.ContactDetails>(null));

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
        }

        [Test]
        public async Task GetContactHttpTrigger_ReturnsStatusCodeOk_WhenContactExists()
        {
            // Arrange
            _httpRequestHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            var contact = new Models.ContactDetails();
            _getContactHttpTriggerService.Setup(x=>x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>())).Returns(Task.FromResult<Models.ContactDetails>(contact));

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        }

        private async Task<HttpResponseMessage> RunFunction(string customerId)
        {
            return await _function.Run(_request, _log.Object, customerId).ConfigureAwait(false);
        }
    }
}
