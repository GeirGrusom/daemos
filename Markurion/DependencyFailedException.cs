using System;
using System.Collections.Generic;
using System.Text;

namespace Markurion
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
