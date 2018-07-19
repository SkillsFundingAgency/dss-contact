using System;
using Microsoft.Extensions.DependencyInjection;
using NCS.DSS.Contact.GetContactDetailsByIdHttpTrigger.Service;
using NCS.DSS.Contact.GetContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.Helpers;
using NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.PostContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.Helpers;
using NCS.DSS.Contact.Validation;

namespace NCS.DSS.Contact.Ioc
{
    public class RegisterServiceProvider
    {
        public IServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddTransient<IGetContactHttpTriggerService, GetContactHttpTriggerService>();
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
