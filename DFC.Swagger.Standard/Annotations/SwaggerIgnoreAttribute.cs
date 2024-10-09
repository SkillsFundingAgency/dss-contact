using System;

namespace DFC.Swagger.Standard.Annotations
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter)]
    public class SwaggerIgnoreAttribute : Attribute
    {
    }
}