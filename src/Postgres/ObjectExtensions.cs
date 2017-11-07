// <copyright file="ObjectExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

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
