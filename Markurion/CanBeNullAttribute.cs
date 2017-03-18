using System;
using System.Collections.Generic;
using System.Text;

namespace Markurion
{
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter)]
    public sealed class CanBeNullAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter)]
    public sealed class NotNullAttribute : Attribute
    {
    }
}
