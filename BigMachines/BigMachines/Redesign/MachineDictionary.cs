// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Tinyhand;
using Tinyhand.IO;

#pragma warning disable SA1401

namespace BigMachines;

[TinyhandObject]
public partial class MachineDictionary<TIdentifier, TMachine> : ITinyhandSerialize<MachineDictionary<TIdentifier, TMachine>>, ITinyhandReconstruct<MachineDictionary<TIdentifier, TMachine>>, IStructualObject
    where TIdentifier : notnull
    where TMachine : ITinyhandSerialize<TMachine>
{
    public MachineDictionary()
    {
    }

    static void ITinyhandSerialize<MachineDictionary<TIdentifier, TMachine>>.Serialize(ref TinyhandWriter writer, scoped ref MachineDictionary<TIdentifier, TMachine>? value, TinyhandSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        var identifierFormatter = options.Resolver.GetFormatter<TIdentifier>();
        var machineFormatter = options.Resolver.GetFormatter<TMachine>();

        var array = value.Dictionary.ToArray();
        writer.WriteMapHeader(array.Length);
        foreach (var x in array)
        {
            identifierFormatter.Serialize(ref writer, x.Key, options);
            machineFormatter.Serialize(ref writer, x.Value, options);
        }
    }

    static void ITinyhandSerialize<MachineDictionary<TIdentifier, TMachine>>.Deserialize(ref TinyhandReader reader, scoped ref MachineDictionary<TIdentifier, TMachine>? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return;
        }

        var identifierFormatter = options.Resolver.GetFormatter<TIdentifier>();
        var machineFormatter = options.Resolver.GetFormatter<TMachine>();

        value ??= new();
        var length = reader.ReadMapHeader2();
        for (var i = 0; i < length; i++)
        {
            var identifier = identifierFormatter.Deserialize(ref reader, options);
            var machine = machineFormatter.Deserialize(ref reader, options);
            if (identifier is not null && machine is not null)
            {
                value.Dictionary.TryAdd(identifier, machine);
            }
        }
    }

    static void ITinyhandReconstruct<MachineDictionary<TIdentifier, TMachine>>.Reconstruct([NotNull] scoped ref MachineDictionary<TIdentifier, TMachine>? value, TinyhandSerializerOptions options)
    {
        value ??= new();
    }

    bool IStructualObject.ReadRecord(ref TinyhandReader reader)
    {
        if (!reader.TryRead(out JournalRecord record))
        {
            return false;
        }

        var key = TinyhandSerializer.Deserialize<TIdentifier>(ref reader);
        if (key is null)
        {
            return false;
        }

        if (record == JournalRecord.EraseStorage)
        {// EraseStorage, Key
            // this.EraseInternal();
            return true;
        }
        else if (record == JournalRecord.AddStorage)
        {// AddStorage, Key, Machine
            var machine = TinyhandSerializer.DeserializeObject<TMachine>(ref reader);
            if (machine is not null)
            {
                this.Dictionary.TryAdd(key, machine);
                return true;
            }
        }
        else if (record == JournalRecord.Locator)
        {// Locator, Key, (Data)
            this.Dictionary.TryGetValue(key, out var machine);
            if (machine is IStructualObject structualObject)
            {
                structualObject.ReadRecord(ref reader);
            }
        }

        return false;
    }

    internal ConcurrentDictionary<TIdentifier, TMachine> Dictionary = new();

    IStructualRoot? IStructualObject.StructualRoot { get; set; }

    IStructualObject? IStructualObject.StructualParent { get; set; }

    int IStructualObject.StructualKey { get; set; }
}
