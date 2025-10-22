using RackMonitor.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RackMonitor.Extensions
{
    public static class ReflectionExtensions
    {
        public static string GetFriendlyName<T>(this T value, string propertyName)
        {
            // Get the property info from the type
            PropertyInfo propertyInfo = typeof(T).GetProperty(propertyName);
            if (propertyInfo == null)
            {
                return propertyName; // Or throw an exception
            }

            // Look for the custom FriendlyNameAttribute
            var attribute = propertyInfo.GetCustomAttribute<FriendlyNameAttribute>();

            // If the attribute is found, return its Name property.
            // Otherwise, fall back to the property's actual name.
            return attribute?.Name ?? propertyName;
        }
    }
}
