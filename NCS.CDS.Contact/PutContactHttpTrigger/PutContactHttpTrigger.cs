﻿using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System;
using System.Web.Http.Description;
using NCS.DSS.ContactDetails.Annotations;

namespace NCS.DSS.ContactDetails.PutContactHttpTrigger
{
    public static class PutContactHttpTrigger
    {
        [FunctionName("PutContact")]
        [ContactDetailsResponse(HttpStatusCode = (int)HttpStatusCode.Created, Description = "Contact Details Replaced", ShowSchema = true)]
        [ContactDetailsResponse(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Unable to Replace Contact Details", ShowSchema = false)]
        [ContactDetailsResponse(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Forbidden", ShowSchema = false)]
        [ResponseType(typeof(Models.ContactDetails))]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "customers/{customerId}/ContactDetails/{contactid}")]HttpRequestMessage req, TraceWriter log, string customerId, string contactid)
        {
            log.Info("C# HTTP trigger function PutContact processed a request.");

            if (!Guid.TryParse(contactid, out var contactGuid))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(contactid),
                        System.Text.Encoding.UTF8, "application/json")
                };
            }

            var values = "Sucessfully Replaced ContactDetails record with id : " + contactid;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(values),
                    System.Text.Encoding.UTF8, "application/json")
            };
        }

    }
}
