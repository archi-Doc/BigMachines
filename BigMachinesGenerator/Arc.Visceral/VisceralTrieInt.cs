// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Arc.Visceral;

internal class VisceralTrieInt<TObject>
{
    public VisceralTrieInt(TObject obj)
    {
        this.Object = obj;
        this.root = new Node(this, 0);
    }

    internal class VisceralTrieContext
    {
        public VisceralTrieContext(ScopingStringBuilder ssb, Action<VisceralTrieContext, TObject, Node> generateMethod)
        {
            this.Ssb = ssb;
            this.GenerateMethod = generateMethod;
        }

        public ScopingStringBuilder Ssb { get; }

        public string Key { get; } = "key";

        public string FallbackStatement { get; } = "reader.Skip();";

        public Action<VisceralTrieContext, TObject, Node> GenerateMethod { get; }

        public object? ExtraInfo { get; }

        // public bool InsertContinueStatement { get; set; } = false;

        public void AppendFallbackStatement()
        {
            if (!string.IsNullOrEmpty(this.FallbackStatement))
            {
                this.Ssb.AppendLine(this.FallbackStatement);
            }
        }
    }

    public TObject Object { get; }

    public (Node? Node, VisceralTrieAddNodeResult Result) AddNode(int key, TObject member)
    {
        if (this.root.Nexts?.TryGetValue(key, out var node) == true)
        {// Key collision
            return (node, VisceralTrieAddNodeResult.KeyCollision);
        }

        node = this.root.Add(key, this.NodeList.Count, member);
        this.NodeList.Add(node);

        return (node, VisceralTrieAddNodeResult.Success);
    }

    /*public void GenerateLoop(VisceralTrieContext context)
    {
        context.Ssb.AppendLine($"var {context.Key} = reader.ReadInt32();");

        this.Generate(context);

        context.Ssb.AppendLine($"{context.NoMatchingKey}:", false);
        context.AppendFallbackStatement();
    }*/

    public void Generate(VisceralTrieContext context)
    {
        context.Ssb.AppendLine($"var {context.Key} = reader.ReadInt32();");

        if (this.root.Nexts == null)
        {
            context.AppendFallbackStatement();
            // context.Ssb.AppendLine($"goto {context.NoMatchingKey};");
        }
        else
        {
            this.GenerateNode(context, this.root.Nexts.Values.ToArray());
        }

        /*if (context.InsertContinueStatement)
        {
            context.Ssb.AppendLine("continue;");
        }*/

        /*context.Ssb.AppendLine($"{context.NoMatchingKey}:", false);
        context.AppendFallbackStatement();*/
    }

    private void GenerateNode(VisceralTrieContext context, Node[] nexts)
    {// ReadOnlySpan<byte> utf8, ulong key (assgined)
        if (nexts.Length < 4)
        {// linear-search
            this.GenerateValueNexts(context, nexts.ToArray());
        }
        else
        {// binary-search
            var midline = nexts.Length / 2;
            var mid = nexts[midline].Key;
            var left = nexts.Take(midline).ToArray();
            var right = nexts.Skip(midline).ToArray();

            using (var scopeLeft = context.Ssb.ScopeBrace($"if ({context.Key} < {mid})"))
            {// left
                this.GenerateNode(context, left);
            }

            using (var scopeRight = context.Ssb.ScopeBrace("else"))
            {// right
                this.GenerateNode(context, right);
            }
        }
    }

    private void GenerateValueNexts(VisceralTrieContext context, Node[] valueNexts)
    {// valueNexts.Length > 0
        /*if (valueNexts.Length == 1)
        {
            var x = valueNexts[0];
            context.Ssb.AppendLine($"if ({context.Key} != {x.Key}) goto {context.NoMatchingKey};");
            context.GenerateMethod(context, this.Object, x);
            return;
        }*/

        var firstFlag = true;
        foreach (var x in valueNexts)
        {
            var condition = firstFlag ? string.Format("if ({0} == {1})", context.Key, x.Key) : string.Format("else if ({0} == {1})", context.Key, x.Key);
            firstFlag = false;
            using (var c = context.Ssb.ScopeBrace(condition))
            {
                context.GenerateMethod(context, this.Object, x);
            }
        }

        using (var ifElse = context.Ssb.ScopeBrace("else"))
        {
            context.AppendFallbackStatement();
            // context.Ssb.AppendLine($"goto {context.NoMatchingKey};");
        }
    }

    public List<Node> NodeList { get; } = new();

    private readonly Node root;

    internal class Node
    {
        public Node(VisceralTrieInt<TObject> visceralTrie, int key)
        {
            this.VisceralTrie = visceralTrie;
            this.Key = key;
        }

        public VisceralTrieInt<TObject> VisceralTrie { get; }

        public int Key { get; }

        public int Index { get; private set; } = -1;

        public TObject Member { get; private set; } = default!;

        public SortedDictionary<int, Node>? Nexts { get; private set; }

        public bool HasChildren => this.Nexts != null;

        public Node Add(int key, int index, TObject member)
        {// Leaf node
            var node = new Node(this.VisceralTrie, key);
            node.Index = index;
            node.Member = member;

            this.Nexts ??= new();
            this.Nexts.Add(key, node);

            return node;
        }
    }
}
