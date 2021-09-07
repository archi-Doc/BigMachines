// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using BenchmarkDotNet.Attributes;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Arc.Collection;
using BigMachines;

namespace Benchmark.Test
{
    public class AddOnlyListClass
    {
        public int X { get; set; }

        public AddOnlyListClass(int x)
        {
            this.X = x;
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class AddOnlyListBenchmark
    {
        public AddOnlyListBenchmark()
        {
        }

        [GlobalSetup]
        public void Setup()
        {
        }

        [Benchmark]
        public List<int> List_AddInt()
        {
            var list = new List<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(4);
            list.Add(5);
            list.Add(6);
            return list;
        }

        [Benchmark]
        public AddOnlyList<int> AddOnly_AddInt()
        {
            var list = new AddOnlyList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(4);
            list.Add(5);
            list.Add(6);
            return list;
        }

        [Benchmark]
        public UnorderedList<int> Unordered_AddInt()
        {
            var list = new UnorderedList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(4);
            list.Add(5);
            list.Add(6);
            return list;
        }

        [Benchmark]
        public int List_AddAndEnumInt()
        {
            var list = new List<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(4);
            list.Add(5);
            list.Add(6);

            var sum = 0;
            foreach (var x in list)
            {
                sum += x;
            }

            return sum;
        }

        [Benchmark]
        public int AddOnly_AddAndEnumInt()
        {
            var list = new AddOnlyList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(4);
            list.Add(5);
            list.Add(6);

            var sum = 0;
            foreach (var x in list)
            {
                sum += x;
            }

            return sum;
        }

        [Benchmark]
        public int Unordered_AddAndEnumInt()
        {
            var list = new UnorderedList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(4);
            list.Add(5);
            list.Add(6);

            var sum = 0;
            foreach (var x in list)
            {
                sum += x;
            }

            return sum;
        }

        [Benchmark]
        public List<AddOnlyListClass> List_AddClass()
        {
            var list = new List<AddOnlyListClass>();
            list.Add(new(1));
            list.Add(new(2));
            list.Add(new(3));
            list.Add(new(4));
            list.Add(new(5));
            list.Add(new(6));
            return list;
        }

        [Benchmark]
        public AddOnlyList<AddOnlyListClass> AddOnly_AddClass()
        {
            var list = new AddOnlyList<AddOnlyListClass>();
            list.Add(new(1));
            list.Add(new(2));
            list.Add(new(3));
            list.Add(new(4));
            list.Add(new(5));
            list.Add(new(6));
            return list;
        }

        [Benchmark]
        public UnorderedList<AddOnlyListClass> Unordered_AddClass()
        {
            var list = new UnorderedList<AddOnlyListClass>();
            list.Add(new(1));
            list.Add(new(2));
            list.Add(new(3));
            list.Add(new(4));
            list.Add(new(5));
            list.Add(new(6));
            return list;
        }

        [Benchmark]
        public int List_AddAndEnumClass()
        {
            var list = new List<AddOnlyListClass>();
            list.Add(new(1));
            list.Add(new(2));
            list.Add(new(3));
            list.Add(new(4));
            list.Add(new(5));
            list.Add(new(6));

            var sum = 0;
            foreach (var x in list)
            {
                sum += x.X;
            }

            return sum;
        }

        [Benchmark]
        public int AddOnly_AddAndEnumClass()
        {
            var list = new AddOnlyList<AddOnlyListClass>();
            list.Add(new(1));
            list.Add(new(2));
            list.Add(new(3));
            list.Add(new(4));
            list.Add(new(5));
            list.Add(new(6));

            var sum = 0;
            foreach (var x in list)
            {
                sum += x.X;
            }

            return sum;
        }

        [Benchmark]
        public int Unordered_AddAndEnumClass()
        {
            var list = new UnorderedList<AddOnlyListClass>();
            list.Add(new(1));
            list.Add(new(2));
            list.Add(new(3));
            list.Add(new(4));
            list.Add(new(5));
            list.Add(new(6));

            var sum = 0;
            foreach (var x in list)
            {
                sum += x.X;
            }

            return sum;
        }
    }
}
