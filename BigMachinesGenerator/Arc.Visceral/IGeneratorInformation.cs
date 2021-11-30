// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.CodeAnalysis;

namespace Arc.Visceral;

public interface IGeneratorInformation
{
    public bool AttachDebugger { get; }

    public bool GenerateToFile { get; }

    public string? CustomNamespace { get; }

    public string? AssemblyName { get; }

    public int AssemblyId { get; }

    public OutputKind OutputKind { get; }

    public string? TargetFolder { get; }
}
