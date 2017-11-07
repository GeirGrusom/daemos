// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute
{
    using System;
    using System.Linq;

    /// <summary>
    /// Implements functions to determine if a type is arithmetic.
    /// </summary>
    internal static class ArithmeticTypeHelper
    {
        private static readonly Type[] ArithmeticTypes =
        {
            typeof(byte),
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(float),
            typeof(double),
            typeof(decimal)
        };

        /// <summary>
        /// Determines whether a type is arithmetic
        /// </summary>
        /// <param name="t">Type to test</param>
        /// <returns>True if the type is arithmetic. Otherwise false.</returns>
        internal static bool IsArithmetic(Type t) => ArithmeticTypes.Contains(t);

        /// <summary>
        /// Determines whether a type is arithmetic
        /// </summary>
        /// <typeparam name="T">Type to test</typeparam>
        /// <returns>True if the type is arithmetic. Otherwise false.</returns>
        internal static bool IsArithmetic<T>() => IsArithmetic(typeof(T));
    }
}
