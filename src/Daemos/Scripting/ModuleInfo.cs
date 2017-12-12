// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Scripting
{
    using System;

    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class ModuleInfoAttribute : Attribute
    {
        public Type Type{ get; }

        public ModuleInfoAttribute(Type type)
        {
            this.Type = type;
        }
    }

    public interface IModuleInfo
    {
        void OnInitializeRegister(IDependencyRegister register);
    }
}
