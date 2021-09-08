// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BigMachines;

namespace Benchmark.Test
{
    [Config(typeof(BenchmarkConfig))]
    public class LoopCheckerBenchmark2
    {
        [Params(2, 4, 8)]
        public int Size { get; set; }

        public LoopChecker CheckerClass { get; set; } = default!;

        public LoopChecker2 CheckerClass2 { get; set; } = default!;

        public LoopCheckerStruct CheckerStruct { get; set; } = default;

        public LoopCheckerBenchmark2()
        {
        }

        [GlobalSetup]
        public void Setup()
        {
            this.CheckerClass = new();
            this.CheckerClass2 = new();
            this.CheckerStruct = new();
            for (uint n = 0; n < this.Size; n++)
            {
                this.CheckerClass.AddCommandId(n);
                this.CheckerClass2.AddCommandId(n);
                this.CheckerStruct.AddCommandId(n);
            }

            LoopChecker.AsyncLocalInstance.Value = this.CheckerClass;
            LoopChecker2.AsyncLocalInstance.Value = this.CheckerClass2;
            LoopCheckerStruct.AsyncLocalInstance.Value = this.CheckerStruct;
        }

        [Benchmark]
        public LoopChecker Class_Clone()
        {
            return new LoopChecker(this.CheckerClass);
        }

        [Benchmark]
        public LoopChecker2 Class2_Clone()
        {
            return new LoopChecker2(this.CheckerClass2);
        }

        [Benchmark]
        public LoopCheckerStruct Struct_Clone()
        {
            return new LoopCheckerStruct(this.CheckerStruct);
        }

        [Benchmark]
        public bool Class_Find()
        {
            for (var n = 0; n < this.CheckerClass.CommandIdCount; n++)
            {
                if (this.CheckerClass.CommandId[n] == 3)
                {
                    return true;
                }
            }

            return false;
        }

        [Benchmark]
        public bool Class2_Find()
        {
            if (this.CheckerClass2.CommandIdCount == 0)
            {
                return false;
            }
            else if (this.CheckerClass2.CommandIdCount == 1)
            {
                return this.CheckerClass2.CommandId0 == 3;
            }
            else if (this.CheckerClass2.CommandIdCount == 2)
            {
                return this.CheckerClass2.CommandId0 == 3 ||
                    this.CheckerClass2.CommandId1 == 3;
            }
            else
            {
                for (var n = 0; n < this.CheckerClass2.CommandIdCount; n++)
                {
                    if (this.CheckerClass2.CommandId![n] == 3)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        [Benchmark]
        public bool Class2_Find2() => this.CheckerClass2.FindCommandId(3);

        /*[Benchmark]
        public LoopChecker Class_GetCloneSet()
        {
            var c = LoopChecker.AsyncLocalInstance.Value!;
            var c2 = new LoopChecker(c);
            LoopChecker.AsyncLocalInstance.Value = c;
            return c2;
        }

        [Benchmark]
        public LoopCheckerStruct Struct_GetCloneSet()
        {
            var c = LoopCheckerStruct.AsyncLocalInstance.Value;
            var c2 = new LoopCheckerStruct(c);
            LoopCheckerStruct.AsyncLocalInstance.Value = c;
            return c2;
        }*/
    }
}
