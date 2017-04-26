using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Markurion
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
