using System;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Exporters;
using GDShrapt.Semantics.Benchmarks.Benchmarks;

namespace GDShrapt.Semantics.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        var config = DefaultConfig.Instance
            .AddExporter(JsonExporter.Full)
            .AddExporter(MarkdownExporter.GitHub)
            .WithOptions(ConfigOptions.DisableOptimizationsValidator);

        // Run all benchmarks or specific ones based on args
        if (args.Length == 0)
        {
            Console.WriteLine("GDShrapt.Semantics Benchmarks");
            Console.WriteLine("==============================");
            Console.WriteLine();
            Console.WriteLine("Available benchmarks:");
            Console.WriteLine("  --filter *Project*        - Project analysis benchmarks");
            Console.WriteLine("  --filter *TypeInference*  - Type inference benchmarks");
            Console.WriteLine("  --filter *Reference*      - Reference collection benchmarks");
            Console.WriteLine("  --filter *                - All benchmarks");
            Console.WriteLine();
            Console.WriteLine("Running all benchmarks...");
            Console.WriteLine();

            BenchmarkRunner.Run<ProjectAnalysisBenchmarks>(config);
            BenchmarkRunner.Run<TypeInferenceBenchmarks>(config);
            BenchmarkRunner.Run<ReferenceCollectionBenchmarks>(config);
        }
        else
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
        }
    }
}
