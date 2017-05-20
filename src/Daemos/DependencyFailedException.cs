using System;

namespace Daemos
{
    public class DependencyFailedException : Exception
    {
        public Type Type { get; }

        public DependencyFailedException(Type type)
             : base("The type could not be resolved.")
        {
            Type = type;
        }

    }
}
