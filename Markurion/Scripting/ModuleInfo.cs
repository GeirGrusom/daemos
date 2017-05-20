using System;
using System.Collections.Generic;
using System.Text;

namespace Markurion.Scripting
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
