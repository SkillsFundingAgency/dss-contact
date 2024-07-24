﻿using System;
using System.Net;
using System.Net.Http;
using DFC.Swagger.Standard.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;

namespace NCS.DSS.Contact.PutContactDetailsHttpTrigger
{
    public static class PutContactHttpTrigger
    {
        [Disable]
        [Function("PUT")]
        [Response(HttpStatusCode = (int)HttpStatusCode.Created, Description = "Contact Details Replaced", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Unable to Replace Customer", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient Access To This Resource", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "Unauthorised", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Resource Does Not Exist", ShowSchema = false)]
        [ProducesResponseType(typeof(Contact.Models.ContactDetails), 200)]
        public static IActionResult Run([Microsoft.Azure.Functions.Worker.HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "customers/{customerId}/ContactDetails/{contactid}")]HttpRequest req, ILogger log, string customerId, string contactid)
        {
            log.LogInformation("PutContactHttpTrigger method was executed at " + DateTime.Now);

            if (!Guid.TryParse(contactid, out var contactGuid))
            {
                return new BadRequestObjectResult(new StringContent(JsonConvert.SerializeObject(contactid),
                        System.Text.Encoding.UTF8, "application/json"));
            }

            var values = "Sucessfully Replaced ContactDetails record with id : " + contactid;

            return new OkObjectResult(new StringContent(JsonConvert.SerializeObject(values),
                    System.Text.Encoding.UTF8, "application/json"));
        }

    }
}
