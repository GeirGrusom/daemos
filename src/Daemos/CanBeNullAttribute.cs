// <copyright file="CanBeNullAttribute.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

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
