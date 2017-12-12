// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;

    /// <summary>
    /// Specifies that the value cannot be null.
    /// </summary>
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter | AttributeTargets.Method | AttributeTargets.Property)]
    public sealed class NotNullAttribute : Attribute
    {
    }
}
