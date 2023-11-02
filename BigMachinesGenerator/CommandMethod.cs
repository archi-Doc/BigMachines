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
        if (returnObject is null)
        {
            return null;
        }

        var check = false;
        var returnTask = false;
        BigMachinesObject? responseObject = null;
        if (returnObject.FullName == BigMachinesBody.CommandResultResultFullName)
        {// CommandResult
            check = true;
        }
        else if (returnObject.OriginalDefinition?.FullName == BigMachinesBody.CommandResultResultFullName2)
        {// CommandResult<TResponse>
            check = true;
            responseObject = returnObject.Generics_Arguments[0];
        }
        else if (returnObject.Generics_Kind == VisceralGenericsKind.ClosedGeneric &&
                returnObject.OriginalDefinition?.FullName == BigMachinesBody.TaskFullName2 &&
                returnObject.Generics_Arguments is { } args &&
                args.Length == 1)
        {// Task<TResult>
            returnTask = true;
            if (args[0].FullName == BigMachinesBody.CommandResultResultFullName)
            {// Task<CommandResult>
                check = true;
            }
            else if (args[0].OriginalDefinition?.FullName == BigMachinesBody.CommandResultResultFullName2)
            {// Task<CommandResult<TResponse>>
                check = true;
                responseObject = args[0].Generics_Arguments[0];
            }
        }

        if (!check)
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
        commandMethod.All = methodAttribute.All;
        commandMethod.ReturnTask = returnTask;
        commandMethod.ResponseObject = responseObject;

        StringBuilder? sb = null;
        var types = commandMethod.Method.Method_Parameters;
        var names = commandMethod.Method.Method_ParameterNames();
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

        if (sb is not null)
        {
            commandMethod.ParameterTypesAndNames = sb.ToString();
            commandMethod.ParameterNames = string.Join(", ", names);
        }

        return commandMethod;
    }

    public Location Location { get; private set; } = Location.None;

    public BigMachinesObject? Method { get; private set; }

    public string Name { get; private set; } = string.Empty;

    // public uint CommandId { get; internal set; }

    // public bool DuplicateId { get; internal set; }

    public bool WithLock { get; internal set; }

    public bool All { get; internal set; }

    public bool ReturnTask { get; private set; }

    public BigMachinesObject? ResponseObject { get; private set; }

    public string ParameterTypesAndNames { get; private set; } = string.Empty;

    public string ParameterNames { get; private set; } = string.Empty;

    public void GenerateCommand(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.Method is null)
        {
            return;
        }

        var commandResult = this.ResponseObject is null ? "CommandResult" : $"CommandResult<{this.ResponseObject.FullName}>";

        using (var method = ssb.ScopeBrace($"public async Task<{commandResult}> {this.Name}({this.ParameterTypesAndNames})"))
        {
            ssb.AppendLine("var locked = 0;");
            ssb.AppendLine("try {");
            ssb.IncrementIndent();
            ssb.AppendLine($"locked = ((IBigMachine)this.machine.BigMachine).CheckRecursive(this.machine.machineSerial, ((ulong)this.machine.machineSerial << 32) | {(uint)FarmHash.Hash64(this.Method.FullName)});");
            if (this.WithLock)
            {
                ssb.AppendLine("if (locked > 0) await this.machine.Semaphore.EnterAsync().ConfigureAwait(false);");
            }

            if (this.ResponseObject is null)
            {
                ssb.AppendLine("if (this.machine.operationalState == OperationalFlag.Terminated) return CommandResult.Terminated;");
            }
            else
            {
                ssb.AppendLine("if (this.machine.operationalState == OperationalFlag.Terminated) return new(CommandResult.Terminated, default);");
            }

            if (this.ReturnTask)
            {
                ssb.AppendLine($"return await this.machine.{this.Name}({this.ParameterNames}).ConfigureAwait(false);");
            }
            else
            {
                ssb.AppendLine($"return this.machine.{this.Name}({this.ParameterNames});");
            }

            ssb.DecrementIndent();
            ssb.AppendLine("}");
            if (this.ResponseObject is null)
            {
                ssb.AppendLine("catch (Exception e) { ((IBigMachine)this.machine.BigMachine).ReportException(new(this.machine, e)); return CommandResult.Failure; }");
            }
            else
            {
                ssb.AppendLine("catch (Exception e) { ((IBigMachine)this.machine.BigMachine).ReportException(new(this.machine, e)); return new(CommandResult.Failure, default); }");
            }

            if (this.WithLock)
            {
                ssb.AppendLine("finally { if (locked > 0) this.machine.Semaphore.Exit(); }");
            }
        }
    }
}
