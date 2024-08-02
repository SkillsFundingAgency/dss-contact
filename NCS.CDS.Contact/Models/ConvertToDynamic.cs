using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace NCS.DSS.Contact.Models
{
    public class ConvertToDynamic : IConvertToDynamic
    {        
        public ExpandoObject ExcludeProperty(Exception exception, string[] names)
        {
            dynamic updatedObject = new ExpandoObject();
            foreach (var item in typeof(Exception).GetProperties())
            {
                if (names.Contains(item.Name))
                    continue;
               
                AddProperty(updatedObject, item.Name, item.GetValue(exception));
            }
            return updatedObject;
        }       
        private void AddProperty(ExpandoObject expando, string propertyName, object propertyValue)
        {
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                expandoDict[propertyName] = propertyValue;
            else
                expandoDict.Add(propertyName, propertyValue);
        }
    }
}
