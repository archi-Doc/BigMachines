// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

/*  BenchmarkDotNet, small template code
 *  PM> Install-Package BenchmarkDotNet
 */

using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Arc.Threading;
using BigMachines;

#pragma warning disable SA1401 // Fields should be private

namespace Benchmark
{
    public class Program
    {
        public static Stopwatch Stopwatch { get; } = new();

        public static async Task Main(string[] args)
        {
            // await Benchmark.Design.CommandDesign1.Test();
            // await Benchmark.Design.CommandDesign2.Test();
            // await Benchmark.Design.CommandDesign3.Test();
            // await Benchmark.Design.CommandDesign4.Test();
            // await Benchmark.Design.CommandDesign5.Test();
            await Benchmark.Design.CommandDesign6.Test();
            await Benchmark.Design.CommandDesign7.Test();
            await Benchmark.Design.CommandDesign8.Test();

            var bm = new BigMachine<int>(ThreadCore.Root);
            BigMachine<int>.EnableLoopChecker = false;
            await Benchmark.Design.SimpleBench.Test(bm);

            var cp = new CommandPost<int>(ThreadCore.Root);
            cp.Open(x =>
            {
                if (x.Message is int i)
                {
                    x.Response = i + 2;
                }
            });

            DebugRun<LoopCheckerBenchmark>();

            var switcher = new BenchmarkSwitcher(new[]
            {
                typeof(LoopCheckerBenchmark),
                typeof(Design.ConcurrentQueueBenchmark),
                typeof(Test.TestBenchmark),
            });
            switcher.Run(args);
        }

        public static void DebugRun<T>()
            where T : new()
        { // Run a benchmark in debug mode.
            var t = new T();
            var type = typeof(T);
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var x in fields)
            { // Set Fields.
                var attr = (ParamsAttribute[])x.GetCustomAttributes(typeof(ParamsAttribute), false);
                if (attr != null && attr.Length > 0)
                {
                    if (attr[0].Values.Length > 0)
                    {
                        x.SetValue(t, attr[0].Values[0]);
                    }
                }
            }

            foreach (var x in properties)
            { // Set Properties.
                var attr = (ParamsAttribute[])x.GetCustomAttributes(typeof(ParamsAttribute), false);
                if (attr != null && attr.Length > 0)
                {
                    if (attr[0].Values.Length > 0)
                    {
                        x.SetValue(t, attr[0].Values[0]);
                    }
                }
            }

            foreach (var x in methods.Where(i => i.GetCustomAttributes(typeof(GlobalSetupAttribute), false).Length > 0))
            { // [GlobalSetupAttribute]
                x.Invoke(t, null);
            }

            foreach (var x in methods.Where(i => i.GetCustomAttributes(typeof(BenchmarkAttribute), false).Length > 0))
            { // [BenchmarkAttribute]
                x.Invoke(t, null);
            }

            foreach (var x in methods.Where(i => i.GetCustomAttributes(typeof(GlobalCleanupAttribute), false).Length > 0))
            { // [GlobalCleanupAttribute]
                x.Invoke(t, null);
            }

            // obsolete code:
            // methods.Where(i => i.CustomAttributes.Select(j => j.AttributeType).Contains(typeof(GlobalSetupAttribute)))
            // bool IsNullableType(Type type) => type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>));
            /* var targetType = IsNullableType(x.FieldType) ? Nullable.GetUnderlyingType(x.FieldType) : x.FieldType;
                        if (targetType != null)
                        {
                            var value = Convert.ChangeType(attr[0].Values[0], targetType);
                            x.SetValue(t, value);
                        }*/
        }
    }

    public class BenchmarkConfig : BenchmarkDotNet.Configs.ManualConfig
    {
        public BenchmarkConfig()
        {
            this.AddExporter(BenchmarkDotNet.Exporters.MarkdownExporter.GitHub);
            this.AddDiagnoser(BenchmarkDotNet.Diagnosers.MemoryDiagnoser.Default);

            // this.AddJob(Job.ShortRun.With(BenchmarkDotNet.Environments.Platform.X64).WithWarmupCount(1).WithIterationCount(1));
            // this.AddJob(BenchmarkDotNet.Jobs.Job.MediumRun.WithGcForce(true).WithId("GcForce medium"));
            // this.AddJob(BenchmarkDotNet.Jobs.Job.ShortRun);
            this.AddJob(BenchmarkDotNet.Jobs.Job.MediumRun);
            // this.AddJob(BenchmarkDotNet.Jobs.Job.LongRun);
        }
    }
}
