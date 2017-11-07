// <copyright file="ModuleInfo.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;

namespace Daemos.Scripting
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class ModuleInfoAttribute : Attribute
    {
        public Type Type{ get; }

        public ModuleInfoAttribute(Type type)
        {
            Type = type;
        }
    }

    public interface IModuleInfo
    {
        void OnInitializeRegister(IDependencyRegister register);
    }
}
