// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

namespace BigMachines.Generator
{
    public class StateMethod
    {
        public static StateMethod? Create(BigMachinesObject method, VisceralAttribute attribute)
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

            return stateMethod;
        }

        public Location Location { get; private set; } = Location.None;

        public string Name { get; private set; } = string.Empty;

        public int Id { get; internal set; }

        public bool DuplicateId { get; set; }
    }
}
