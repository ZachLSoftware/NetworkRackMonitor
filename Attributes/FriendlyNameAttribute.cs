using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RackMonitor.Attributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class FriendlyNameAttribute : Attribute
    {
        /// <summary>
        /// Gets the friendly name for the property.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FriendlyNameAttribute"/> class.
        /// </summary>
        /// <param name="name">The user-friendly name to associate with the property.</param>
        public FriendlyNameAttribute(string name)
        {
            Name = name;
        }
    }
}
