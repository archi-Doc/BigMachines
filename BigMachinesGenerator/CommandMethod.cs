// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

namespace BigMachines.Generator
{
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

            var flag = false;
            if (machine.IdentifierObject != null &&
                method.Method_Parameters.Length == 1 &&
                method.Method_Parameters[0] == string.Format(BigMachinesBody.CommandParameterFullName, machine.IdentifierObject.FullName))
            {
            }
            else
            {// Invalid parameter
                flag = true;
            }

            var returnTypeName = method.Method_ReturnObject?.FullName;
            var returnTask = false;
            if (returnTypeName == "void")
            {// void
            }
            else if (returnTypeName == BigMachinesBody.TaskFullName)
            {// Task
                returnTask = true;
            }
            else
            {// Invalid return type
                flag = true;
            }

            if (flag)
            {
                method.Body.ReportDiagnostic(BigMachinesBody.Error_MethodFormat2, attribute.Location);
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
            commandMethod.WithoutLock = methodAttribute.WithoutLock;

            return commandMethod;
        }

        public Location Location { get; private set; } = Location.None;

        public string Name { get; private set; } = string.Empty;

        public uint Id { get; internal set; }

        public bool DuplicateId { get; internal set; }

        public bool ReturnTask { get; internal set; }

        public bool WithoutLock { get; internal set; }
    }
}
