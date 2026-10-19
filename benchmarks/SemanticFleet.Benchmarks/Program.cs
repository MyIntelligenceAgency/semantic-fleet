using BenchmarkDotNet.Running;
using SemanticFleet.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(PromptSettingsBenchmarks).Assembly).Run(args);

return 0;
