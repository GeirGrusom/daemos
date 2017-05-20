using System;

namespace Daemos
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ModuleAttribute : Attribute
    {
        public string Name { get; }

        public ModuleAttribute()
        {
        }

        public ModuleAttribute(string name = null)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ScriptAttribute : Attribute
    {
        public string Name { get; }

        public int? State { get; }

        public ScriptAttribute()
            :this(null)
        {
            
        }

        public ScriptAttribute(string name = null, int? state = null)
        {
            Name = name;
            State = state;
        }
    }
}
