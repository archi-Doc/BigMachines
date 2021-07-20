﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

namespace BigMachines.Generator
{
    public class StateMethod
    {
        public const string CanEnterName = "CanEnter";
        public const string CanExitName = "CanExit";

        public static StateMethod? Create(BigMachinesObject machine, BigMachinesObject method, VisceralAttribute attribute)
        {
            StateMethodAttributeMock methodAttribute;
            try
            {
                methodAttribute = StateMethodAttributeMock.FromArray(attribute.ConstructorArguments, attribute.NamedArguments);
            }
            catch (InvalidCastException)
            {
                method.Body.ReportDiagnostic(BigMachinesBody.Error_AttributePropertyError, attribute.Location);
                return null;
            }

            var flag = false;
            if (method.Method_Parameters.Length == 1 &&
                method.Method_Parameters[0] == BigMachinesBody.StateParameterFullName)
            {
            }
            else
            {// Invalid parameter
                flag = true;
            }

            if (method.Method_ReturnObject?.FullName != BigMachinesBody.StateResultFullName)
            {// Invalid return type
                flag = true;
            }

            if (flag)
            {
                method.Body.ReportDiagnostic(BigMachinesBody.Error_MethodFormat, attribute.Location);
            }

            if (method.Body.Abort)
            {
                return null;
            }

            var stateMethod = new StateMethod();
            stateMethod.Location = attribute.Location;
            stateMethod.Name = method.SimpleName;
            stateMethod.Id = methodAttribute.Id;

            foreach (var x in machine.GetMembers(VisceralTarget.Method))
            {
                if (x.Method_Parameters.Length == 0 &&
                    x.Method_ReturnObject?.FullName == "bool")
                {
                    if (x.SimpleName == stateMethod.Name + CanEnterName)
                    {
                        stateMethod.CanEnter = true;
                    }
                    else if (x.SimpleName == stateMethod.Name + CanExitName)
                    {
                        stateMethod.CanExit = true;
                    }
                }
            }

            return stateMethod;
        }

        public Location Location { get; private set; } = Location.None;

        public string Name { get; private set; } = string.Empty;

        public bool CanEnter { get; private set; }

        public bool CanExit { get; private set; }

        public int Id { get; internal set; }

        public bool DuplicateId { get; internal set; }
    }
}