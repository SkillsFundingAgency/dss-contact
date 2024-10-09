using System;

namespace DFC.Swagger.Standard.Annotations
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class PostRequestBodyAttribute : Attribute
    {
        public Type Type { get; }

        public string Description { get; }

        public PostRequestBodyAttribute(Type bodyType, string description)
        {
            this.Type = bodyType;
            this.Description = description;
        }
    }
}