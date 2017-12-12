// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ModuleAttribute : Attribute
    {
        public string Name { get; }

        public ModuleAttribute()
        {
        }

        public ModuleAttribute(string name = null)
        {
            this.Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ScriptAttribute : Attribute
    {
        public string Name { get; }

        public int? State { get; }

        public ScriptAttribute()
            : this(null)
        {
        }

        public ScriptAttribute(string name = null, int? state = null)
        {
            this.Name = name;
            this.State = state;
        }
    }
}
