// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Arc.Visceral;

namespace BigMachines.Generator
{
    public class GeneratorInformation
    {
        // public const string BuiltinMethod = "global::Tinyhand.Formatters.Builtin";

        // public const string GeneratedMethod = "global::Tinyhand.Formatters.Generated";

        // public Queue<TinyhandObject> FormatterGeneration { get; } = new();

        public int FormatterCount { get; set; } = 1;

        public List<string> ModuleInitializerClass { get; } = new();

        public bool TryGetBlock(string blockKey, out GeneratorBlock block) => this.keyToBlock.TryGetValue(blockKey, out block);

        public bool CreateBlock(string blockKey, out GeneratorBlock block)
        {
            if (this.TryGetBlock(blockKey, out block))
            {// Already exists.
                return false;
            }

            // Create new block.
            block = new GeneratorBlock(blockKey, this.blockSerialNumber++);
            this.keyToBlock[blockKey] = block;
            return true;
        }

        public void FinalizeBlock(ScopingStringBuilder ssb)
        {
            foreach (var x in this.keyToBlock.Values)
            {
                ssb.Append(x.SSB);
            }
        }

        private int blockSerialNumber;
        private Dictionary<string, GeneratorBlock> keyToBlock = new();
    }

    public class GeneratorBlock
    {
        public string BlockKey { get; }

        public int SerialNumber { get; }

        public ScopingStringBuilder SSB { get; }

        public GeneratorBlock(string blockKey, int serialNumber)
        {
            this.BlockKey = blockKey;
            this.SerialNumber = serialNumber;
            this.SSB = new();
        }
    }
}
