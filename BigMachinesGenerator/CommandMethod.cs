// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Tinyhand;

namespace BigMachines.Generator;

public class CommandMethod
{
    public static CommandMethod? Create(BigMachinesObject machine, BigMachinesObject method, VisceralAttribute attribute)
    {
        CommandMethodAttributeMock methodAttribute;
        try
        {
            methodAttribute = CommandMethodAttributeMock.FromArray(attribute.ConstructorArguments, attribute.NamedArguments);
        }
        catch (InvalidCastException)
        {
            method.Body.ReportDiagnostic(BigMachinesBody.Error_AttributePropertyError, attribute.Location);
            return null;
        }

        var returnObject = method.Method_ReturnObject;
        BigMachinesObject? responseObject = null;
        if (returnObject?.FullName == BigMachinesBody.CommandResultResultFullName)
        {
        }
        else if (returnObject?.OriginalDefinition?.FullName == BigMachinesBody.CommandResultResultFullName2)
        {
            responseObject = returnObject.Generics_Arguments[0];
        }
        else
        {
            method.Body.ReportDiagnostic(BigMachinesBody.Error_MethodFormat2, method.Location);
        }

        if (method.Body.Abort)
        {
            return null;
        }

        /*var commandId = methodAttribute.CommandId;
        if (commandId == uint.MaxValue)
        {
            commandId = (uint)FarmHash.Hash64(method.SimpleName);
        }*/

        var commandMethod = new CommandMethod();
        commandMethod.Location = attribute.Location;
        commandMethod.Method = method;
        commandMethod.Name = method.SimpleName;
        // commandMethod.CommandId = commandId;
        commandMethod.WithLock = methodAttribute.WithLock;
        commandMethod.ResponseObject = responseObject;

        return commandMethod;
    }

    public Location Location { get; private set; } = Location.None;

    public BigMachinesObject? Method { get; private set; }

    public string Name { get; private set; } = string.Empty;

    // public uint CommandId { get; internal set; }

    // public bool DuplicateId { get; internal set; }

    public bool WithLock { get; internal set; }

    public BigMachinesObject? ResponseObject { get; private set; }

    public void GenerateCommand(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.Method is null)
        {
            return;
        }

        var commandResult = this.ResponseObject is null ? "CommandResult" : $"CommandResult<{this.ResponseObject.FullName}>";

        StringBuilder? sb = null;
        var types = this.Method.Method_Parameters;
        var names = this.Method.Method_ParameterNames();
        for (var i = 0; i < types.Length; i++)
        {
            if (sb is null)
            {
                sb = new();
            }
            else
            {
                sb.Append(", ");
            }

            sb.Append(types[i]);
            sb.Append(" ");
            sb.Append(names[i]);
        }

        using (var method = ssb.ScopeBrace($"public async Task<{commandResult}> {this.Name}({sb?.ToString()})"))
        {
            if (this.WithLock)
            {
                ssb.AppendLine("await this.machine.Semaphore.EnterAsync().ConfigureAwait(false);");
                ssb.AppendLine("try {");
                ssb.IncrementIndent();
            }

            if (this.ResponseObject is null)
            {
                ssb.AppendLine("if (this.machine.operationalState == OperationalFlag.Terminated) return CommandResult.Terminated;");
            }
            else
            {
                ssb.AppendLine("if (this.machine.operationalState == OperationalFlag.Terminated) return new(CommandResult.Terminated, default);");
            }

            ssb.AppendLine($"return this.machine.{this.Name}({string.Join(", ", names)});");

            if (this.WithLock)
            {
                ssb.DecrementIndent();
                ssb.AppendLine("} finally { this.machine.Semaphore.Exit(); }");
            }
        }
    }
}
