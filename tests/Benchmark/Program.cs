using BenchmarkDotNet.Running;

namespace Daemos.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<PostgresBenchmarks>();
        }
    }
}