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

        public static bool GetPropertyVisibility(this PropertyInfo propertyInfo)
        {
            // Get the property info from the type
            if (propertyInfo == null)
            {
                return false;
            }

            // Look for the custom FriendlyNameAttribute
            var attribute = propertyInfo.GetCustomAttribute<PropertyVisibilityAttribute>();

            // If the attribute is found, return its Name property.
            // Otherwise, fall back to the property's actual name.
            return attribute?.IsVisible ?? true;
        }

        public static int GetOrder(this PropertyInfo propertyInfo)
        {
        
            if (propertyInfo == null)
            {
                return int.MaxValue;
            }

            var attribute = propertyInfo.GetCustomAttribute<OrderAttribute>();

            return attribute?.Order ?? int.MaxValue;
        }
        public static string GetTabCategory(this PropertyInfo propertyInfo)
        {

            if (propertyInfo == null)
            {
                return "General";
            }

            var attribute = propertyInfo.GetCustomAttribute<TabCategoryAttribute>();

            return attribute?.Category ?? "General";
        }
    }
}
