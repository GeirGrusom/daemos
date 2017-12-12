// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    public static class JenkinsHash
    {
        public static int GetHashCode(string input)
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
