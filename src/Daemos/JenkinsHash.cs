// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    /// <summary>
    /// Produces a string hash
    /// </summary>
    public static class JenkinsHash
    {
        /// <summary>
        /// Calculates the hash code for the input string
        /// </summary>
        /// <param name="input">Input string to produce hash code for. This value cannot be null.</param>
        /// <returns>Jenkins hash</returns>
        public static int GetHashCode([NotNull] string input)
        {
            int i = 0;
            int hash = 0;
            while (i != input.Length)
            {
                hash += input[i++];
                hash += hash << 10;
                hash ^= hash >> 6;
            }

            hash += hash << 3;
            hash ^= hash >> 11;
            hash += hash << 15;
            return hash;
        }
    }
}
