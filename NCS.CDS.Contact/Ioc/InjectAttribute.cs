using System;
using Microsoft.Azure.WebJobs.Description;

namespace NCS.DSS.Contact.Ioc
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class InjectAttribute : Attribute
    {
    }
}
