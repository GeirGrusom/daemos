using BenchmarkDotNet.Running;
using System;

namespace Markurion.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<PostgresBenchmarks>();
        }
    }
}