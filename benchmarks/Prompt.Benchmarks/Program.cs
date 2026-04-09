using BenchmarkDotNet.Running;
using Prompt.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(GitStatusParserBenchmarks).Assembly).Run(args);
