using System;
using System.Collections.Generic;

namespace Markurion.Postgres
{
    public static class ObjectExtensions
    {
        public static bool Like(this object value, string comparison)
        {
            throw new NotSupportedException();
        }

        public static bool In<T>(this object value, IEnumerable<T> collection)
        {
            throw new NotSupportedException();
        }
    }
}
