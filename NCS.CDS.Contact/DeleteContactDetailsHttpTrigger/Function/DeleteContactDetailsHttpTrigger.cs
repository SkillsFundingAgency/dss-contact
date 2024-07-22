using System.Net;
using System.Net.Http;
using DFC.Swagger.Standard.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;

namespace NCS.DSS.Contact.DeleteContactDetailsHttpTrigger.Function
{
    public static class DeleteContactDetailsHttpTrigger
    {
        [Disable]
        [Function("DELETE")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Contact Details Deleted", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Resource Does Not Exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Delete request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API Key unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient Access To This Resource", ShowSchema = false)]
        [ProducesResponseType(typeof(Contact.Models.ContactDetails), 200)]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "customers/{customerId}/ContactDetails/{contactid}")]HttpRequest req, ILogger log, string customerId, string contactid)
        {
            return null;
        }

    }
}
