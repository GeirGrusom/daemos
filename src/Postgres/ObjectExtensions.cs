// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;

namespace Daemos.Postgres
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
