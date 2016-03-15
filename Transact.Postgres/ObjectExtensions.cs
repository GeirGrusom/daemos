using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transact.Postgres
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
