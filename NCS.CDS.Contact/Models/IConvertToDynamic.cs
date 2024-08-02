using System;
using System.Dynamic;

namespace NCS.DSS.Contact.Models
{
    public interface IConvertToDynamic
    { 
        public ExpandoObject ExcludeProperty(Exception exception, string[] names);

    }
}
