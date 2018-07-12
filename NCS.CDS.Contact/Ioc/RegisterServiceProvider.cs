using System;
using Microsoft.Extensions.DependencyInjection;
using NCS.DSS.ContactDetails.Cosmos.Helper;
using NCS.DSS.ContactDetails.GetContactDetailsByIdHttpTrigger.Service;
using NCS.DSS.ContactDetails.Helpers;
using NCS.DSS.ContactDetails.PatchContactDetailsHttpTrigger.Service;
using NCS.DSS.ContactDetails.PostContactDetailsHttpTrigger.Service;
using NCS.DSS.ContactDetails.Validation;


namespace NCS.DSS.ContactDetails.Ioc
{
    public class RegisterServiceProvider
    {
        public IServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddTransient<IGetContactDetailsByIdHttpTriggerService, GetContactDetailsByIdHttpTriggerService>();
            services.AddTransient<IPostContactDetailsHttpTriggerService, PostContactDetailsHttpTriggerService>();
            services.AddTransient<IPatchContactDetailsHttpTriggerService, PatchContactDetailsHttpTriggerService>();

            services.AddTransient<IResourceHelper, ResourceHelper>();
            services.AddTransient<IValidate, Validate>();
            services.AddTransient<IHttpRequestMessageHelper, HttpRequestMessageHelper>();
            return services.BuildServiceProvider(true);
        }
    }
}
