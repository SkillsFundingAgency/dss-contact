using System;

namespace DFC.Swagger.Standard.Annotations
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class Example : Attribute
    {
        public string Description { get; set; }
    }
}
