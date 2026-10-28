using BenchmarkDotNet.Running;

if (args.Length == 0)
{
    BenchmarkRunner.Run(
    [
        typeof(EFCore.Performance.Benchmarks.TrackingBenchmarks),
        typeof(EFCore.Performance.Benchmarks.ProjectionBenchmarks)
    ]);
}
else
{
    BenchmarkSwitcher.FromAssembly(typeof(EFCore.Performance.Benchmarks.TrackingBenchmarks).Assembly).Run(args);
}
