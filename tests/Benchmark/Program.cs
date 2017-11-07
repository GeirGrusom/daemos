// <copyright file="Program.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.Benchmark
{
    using BenchmarkDotNet.Running;

    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<PostgresBenchmarks>();
        }
    }
}