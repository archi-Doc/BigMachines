// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

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

        BigMachinesObject? responseObject = null;
        string? messageFullName = null;

        var flag = false;
        if (method.Method_Parameters.Length == 0)
        {// No message
        }
        else if (method.Method_Parameters.Length == 1)
        {// TMessage
            messageFullName = method.Method_Parameters[0];
        }
        else
        {// Invalid parameter
            flag = true;
        }

        var returnObject = method.Method_ReturnObject;
        var returnTypeName = returnObject?.FullName;
        var returnTask = false;
        if (returnTypeName == "void")
        {// void (Sync + no responce)
        }
        else if (returnTypeName == BigMachinesBody.TaskFullName)
        {// Task (Async + no responce)
            returnTask = true;
        }
        else if (returnObject?.Generics_Kind == VisceralGenericsKind.ClosedGeneric &&
            returnObject.OriginalDefinition?.FullName == BigMachinesBody.TaskFullName2 &&
            returnObject?.Generics_Arguments is { } args &&
            args.Length == 1)
        {// Task<TResponse> (Async + Response)
            returnTask = true;
            responseObject = args[0];
        }
        else
        {// Other (Sync + Response)
            returnTask = false;
            responseObject = returnObject;
        }

        if (flag)
        {
            string? identifierTypeName = string.Empty;
            if (machine.IdentifierObject != null)
            {
                identifierTypeName = machine.IdentifierObject.SimpleName;
            }

            method.Body.ReportDiagnostic(BigMachinesBody.Error_MethodFormat2, attribute.Location, method.SimpleName, identifierTypeName);
        }

        if (method.Body.Abort)
        {
            return null;
        }

        var commandMethod = new CommandMethod();
        commandMethod.Location = attribute.Location;
        commandMethod.Name = method.SimpleName;
        commandMethod.Id = methodAttribute.Id;
        commandMethod.ReturnTask = returnTask;
        commandMethod.WithLock = methodAttribute.WithLock;
        commandMethod.ResponseObject = responseObject;
        commandMethod.MessageFullName = messageFullName;

        return commandMethod;
    }

    public Location Location { get; private set; } = Location.None;

    public string Name { get; private set; } = string.Empty;

    public uint Id { get; internal set; }

    public bool DuplicateId { get; internal set; }

    public bool ReturnTask { get; internal set; }

    public bool WithLock { get; internal set; }

    public BigMachinesObject? ResponseObject { get; private set; }

    public string? MessageFullName { get; private set; }
}
