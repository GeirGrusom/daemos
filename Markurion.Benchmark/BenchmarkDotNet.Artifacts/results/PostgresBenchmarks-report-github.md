``` ini

BenchmarkDotNet=v0.10.3.0, OS=Microsoft Windows 10.0.14393
Processor=AMD FX(tm)-8120 Eight-Core Processor, ProcessorCount=8
Frequency=3037569 Hz, Resolution=329.2106 ns, Timer=TSC
dotnet cli version=1.0.0
  [Host]     : .NET Core 4.6.25009.03, 64bit RyuJIT
  DefaultJob : .NET Core 4.6.25009.03, 64bit RyuJIT


```
 |            Method |        Mean |    StdDev |
 |------------------ |------------ |---------- |
 | CommitTransaction | 896.1894 us | 9.1645 us |
