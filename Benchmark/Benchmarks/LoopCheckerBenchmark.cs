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

namespace Benchmark
{
    [Config(typeof(BenchmarkConfig))]
    public class LoopCheckerBenchmark
    {
        public uint[] IdArray { get; }

        public uint IdToFind { get; }

        public List<uint> IdList { get; }

        public HashSet<uint> IdSet { get; }

        public Stack<uint> IdStack { get; }

        public LoopCheckerBenchmark()
        {
            this.IdArray = new uint[]
            {
                0xc0526b51,
                0x1a5bb41e,
                0x594578d5,
                0x8bd1198b,
                0x68daeb41,
            };

            this.IdToFind = this.IdArray[3];
            this.IdList = this.Prepare_List();
            this.IdSet = this.Prepare_HashSet();
            this.IdStack = this.Prepare_Stack();
        }

        [GlobalSetup]
        public void Setup()
        {
        }

        [Benchmark]
        public List<uint> Prepare_List()
        {
            var list = new List<uint>();
            foreach (var x in this.IdArray)
            {
                list.Add(x);
            }

            return list;
        }

        [Benchmark]
        public HashSet<uint> Prepare_HashSet()
        {
            var set = new HashSet<uint>();
            foreach (var x in this.IdArray)
            {
                set.Add(x);
            }

            return set;
        }

        [Benchmark]
        public Stack<uint> Prepare_Stack()
        {
            var s = new Stack<uint>();
            foreach (var x in this.IdArray)
            {
                s.Push(x);
            }

            return s;
        }

        [Benchmark]
        public bool Find_List() => this.IdList.Contains(this.IdToFind);

        [Benchmark]
        public bool Find_HashSet() => this.IdSet.Contains(this.IdToFind);

        [Benchmark]
        public bool Find_Stack() => this.IdStack.Contains(this.IdToFind);
    }
}
