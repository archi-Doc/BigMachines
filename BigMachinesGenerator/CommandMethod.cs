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
        if (returnObject.FullName == BigMachinesBody.CommandStatusFullName)
        {// CommandStatus
            check = true;
        }
        else if (returnObject.OriginalDefinition?.FullName == BigMachinesBody.CommandResultFullName)
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
            if (args[0].FullName == BigMachinesBody.CommandStatusFullName)
            {// Task<CommandStatus>
                check = true;
            }
            else if (args[0].OriginalDefinition?.FullName == BigMachinesBody.CommandResultFullName)
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

        var commandMethod = new CommandMethod(machine, method);
        commandMethod.Location = attribute.Location;
        commandMethod.Name = method.SimpleName;
        // commandMethod.CommandId = commandId;
        commandMethod.WithLock = methodAttribute.WithLock;
        commandMethod.GenerateAllCommand = methodAttribute.GenerateAllCommand;
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

            names[i] = "@" + names[i]; // Verbatim identifiers allow keyword names such as 'event'.
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

    public CommandMethod(BigMachinesObject machineObject, BigMachinesObject method)
    {
        this.MachineObject = machineObject;
        this.Method = method;
    }

    public BigMachinesObject MachineObject { get; private set; }

    public Location Location { get; private set; } = Location.None;

    public BigMachinesObject Method { get; private set; }

    public string Name { get; private set; } = string.Empty;

    // public uint CommandId { get; internal set; }

    // public bool DuplicateId { get; internal set; }

    public bool WithLock { get; internal set; }

    public bool GenerateAllCommand { get; internal set; }

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

        var commandResult = this.ResponseObject is null ? "CommandStatus" : $"CommandResult<{this.ResponseObject.FullName}>";

        using (var method = ssb.ScopeBrace($"public async Task<{commandResult}> {this.Name}({this.ParameterTypesAndNames})"))
        {
            // Generated locals use reserved names so that they cannot collide with command parameter names.
            if (BigMachinesBody.EnableRecursiveDetection)
            {
                ssb.AppendLine("var __locked__ = 0;");
                ssb.AppendLine("try {");
                ssb.IncrementIndent();
                ssb.AppendLine($"__locked__ = ((IBigMachine)this.machine.BigMachine).CheckCircularCommand(this.machine.__machineSerial__, ((ulong)this.machine.__machineSerial__ << 32) | {(uint)FarmHash.Hash64(this.Method.FullName)});");
                if (this.WithLock)
                {
                    ssb.AppendLine("if (__locked__ > 0) await this.machine.Semaphore.EnterAsync().ConfigureAwait(false);");
                }
            }
            else
            {
                if (this.WithLock)
                {
                    ssb.AppendLine("await this.machine.Semaphore.EnterAsync().ConfigureAwait(false);");
                }

                ssb.AppendLine("try {");
                ssb.IncrementIndent();
            }

            if (this.ResponseObject is null)
            {
                ssb.AppendLine("if (this.machine.__operationalState__.HasFlag(OperationalFlags.Terminated)) return CommandStatus.Terminated;");
            }
            else
            {
                ssb.AppendLine("if (this.machine.__operationalState__.HasFlag(OperationalFlags.Terminated)) return new(CommandStatus.Terminated, default);");
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
                ssb.AppendLine("catch (Exception __exception__) { ((IBigMachine)this.machine.BigMachine).ReportException(new(this.machine, __exception__)); return CommandStatus.Failure; }");
            }
            else
            {
                ssb.AppendLine("catch (Exception __exception__) { ((IBigMachine)this.machine.BigMachine).ReportException(new(this.machine, __exception__)); return new(CommandStatus.Failure, default); }");
            }

            if (this.WithLock)
            {
                if (BigMachinesBody.EnableRecursiveDetection)
                {
                    ssb.AppendLine("finally { if (__locked__ > 0) this.machine.Semaphore.Exit(); }");
                }
                else
                {
                    ssb.AppendLine("finally { this.machine.Semaphore.Exit(); }");
                }
            }
        }
    }
}
