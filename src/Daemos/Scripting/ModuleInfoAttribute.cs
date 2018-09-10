// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Scripting
{
    using System;

    /// <summary>
    /// Defines this assembly as a Daemos module to be loaded
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class ModuleInfoAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleInfoAttribute"/> class.
        /// </summary>
        /// <param name="type">This must be type that implements <see cref="IModuleInfo"/>. The type is initialized to retrieve information about this module.</param>
        public ModuleInfoAttribute(Type type)
        {
            this.Type = type;
        }

        /// <summary>
        /// Gets the ModuleInfo type used to describe this module
        /// </summary>
        public Type Type { get; }
    }
}
