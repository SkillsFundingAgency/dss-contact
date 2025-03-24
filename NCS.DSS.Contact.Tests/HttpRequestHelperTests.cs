using System.IO;
using System.Text;
using System.Threading.Tasks;
using DFC.HTTP.Standard;
using Microsoft.AspNetCore.Http;
using NCS.DSS.Contact.Models;
using NCS.DSS.Contact.ReferenceData;
using NUnit.Framework;

namespace NCS.DSS.Contact.Tests
{
    [TestFixture]
    public class HttpRequestHelperTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task GetResourceFromRequest_SetsPreferredContactMethodAsUnknown_WhenPreferredContactMethodValueIsEmptyString()
        {
            // Arrange
            var json = "{\"PreferredContactMethod\": \"\"}";
            var request = GetHttpRequest(json);

            // Act
            var helper = new HttpRequestHelper();
            var contactdetailsPatchRequest = await helper.GetResourceFromRequest<ContactDetailsPatch>(request);

            // Assert
            Assert.That(contactdetailsPatchRequest, Is.InstanceOf<ContactDetailsPatch>());
            Assert.That(contactdetailsPatchRequest.PreferredContactMethod, Is.EqualTo(PreferredContactMethod.Unknown));
        }

        [Test]
        public async Task GetResourceFromRequest_SetsPreferredContactMethodAsNull_WhenPreferredContactMethodValueIsNotProvided()
        {
            // Arrange
            var json = "{}";
            var request = GetHttpRequest(json);

            // Act
            var helper = new HttpRequestHelper();
            var contactdetailsPatchRequest = await helper.GetResourceFromRequest<ContactDetailsPatch>(request);

            // Assert
            Assert.That(contactdetailsPatchRequest, Is.InstanceOf<ContactDetailsPatch>());
            Assert.That(contactdetailsPatchRequest.PreferredContactMethod, Is.Null);
        }

        [Test]
        public async Task GetResourceFromRequest_SetsPreferredContactMethodAsEmail_WhenPreferredContactMethodValueIs1AsString()
        {
            // Arrange
            const string json = "{\"PreferredContactMethod\": \"1\"}";
            var request = GetHttpRequest(json);

            // Act
            var helper = new HttpRequestHelper();
            var contactDetailsPatchRequest = await helper.GetResourceFromRequest<ContactDetails>(request);

            // Assert
            Assert.That(contactDetailsPatchRequest, Is.InstanceOf<ContactDetails>());
            Assert.That(contactDetailsPatchRequest.PreferredContactMethod, Is.EqualTo(PreferredContactMethod.Email));
        }

        [Test]
        public async Task GetResourceFromRequest_SetsPreferredContactMethodAsEmail_WhenPreferredContactMethodValueIs1AsString_PATCH()
        {
            // Arrange
            var json = "{\"PreferredContactMethod\": \"1\"}";
            var request = GetHttpRequest(json);

            // Act
            var helper = new HttpRequestHelper();
            var contactdetailsPatchRequest = await helper.GetResourceFromRequest<ContactDetailsPatch>(request);

            // Assert
            Assert.That(contactdetailsPatchRequest, Is.InstanceOf<ContactDetailsPatch>());
            Assert.That(contactdetailsPatchRequest.PreferredContactMethod, Is.EqualTo(PreferredContactMethod.Email));
        }

        [Test]
        public async Task GetResourceFromRequest_SetsPreferredContactMethodAsEmail_WhenPreferredContactMethodValueIs1AsInteger()
        {
            // Arrange
            var json = "{\"PreferredContactMethod\": 1}";
            var request = GetHttpRequest(json);

            // Act
            var helper = new HttpRequestHelper();
            var contactdetailsPatchRequest = await helper.GetResourceFromRequest<ContactDetailsPatch>(request);

            // Assert
            Assert.That(contactdetailsPatchRequest, Is.InstanceOf<ContactDetailsPatch>());
            Assert.That(contactdetailsPatchRequest.PreferredContactMethod, Is.EqualTo(PreferredContactMethod.Email));
        }

        [Test]
        public async Task GetResourceFromRequest_SetsPreferredContactMethodAsUnknown_WhenPreferredContactMethodValueIsAllAlphabets()
        {
            // Arrange
            var json = "{\"PreferredContactMethod\": \"abc\"}";
            var request = GetHttpRequest(json);

            // Act
            var helper = new HttpRequestHelper();
            var contactdetailsPatchRequest = await helper.GetResourceFromRequest<ContactDetailsPatch>(request);

            // Assert
            Assert.That(contactdetailsPatchRequest, Is.InstanceOf<ContactDetailsPatch>());
            Assert.That(contactdetailsPatchRequest.PreferredContactMethod, Is.EqualTo(PreferredContactMethod.Unknown));
        }

        [Test]
        public async Task GetResourceFromRequest_SetsPreferredContactMethodAsPost_WhenPreferredContactMethodValueIs5AsInteger()
        {
            // Arrange
            var json = "{\"PreferredContactMethod\": 5}";
            var request = GetHttpRequest(json);

            // Act
            var helper = new HttpRequestHelper();
            var contactdetailsPatchRequest = await helper.GetResourceFromRequest<ContactDetailsPatch>(request);

            // Assert
            Assert.That(contactdetailsPatchRequest, Is.InstanceOf<ContactDetailsPatch>());
            Assert.That(contactdetailsPatchRequest.PreferredContactMethod, Is.EqualTo(PreferredContactMethod.Post));
        }

        [Test]
        public async Task GetResourceFromRequest_SetsPreferredContactMethodAsUnknown_WhenPreferredContactMethodValueIs8AsInteger()
        {
            // Arrange
            var json = "{\"PreferredContactMethod\": 8}";
            var request = GetHttpRequest(json);

            // Act
            var helper = new HttpRequestHelper();
            var contactdetailsPatchRequest = await helper.GetResourceFromRequest<ContactDetailsPatch>(request);

            // Assert
            Assert.That(contactdetailsPatchRequest, Is.InstanceOf<ContactDetailsPatch>());
            Assert.That(contactdetailsPatchRequest.PreferredContactMethod, Is.EqualTo(PreferredContactMethod.Unknown));
        }

        private static HttpRequest GetHttpRequest(string json)
        {
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Body = memoryStream;
            request.ContentType = "application/json";
            return request;
        }
    }
}