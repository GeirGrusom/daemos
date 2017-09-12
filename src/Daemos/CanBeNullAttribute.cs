using System;

namespace Daemos
{
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter | AttributeTargets.Method | AttributeTargets.Property)]
    public sealed class CanBeNullAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter | AttributeTargets.Method | AttributeTargets.Property)]
    public sealed class NotNullAttribute : Attribute
    {
    }
}
