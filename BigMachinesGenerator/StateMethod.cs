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
            stateMethod.Default = methodAttribute.Default;
            stateMethod.CheckStateChange = methodAttribute.CheckStateChange;
            stateMethod.Name = method.SimpleName;
            if (methodAttribute.Id == int.MinValue)
            {
                stateMethod.Id = method.SimpleName.GetHashCode();
            }
            else
            {
                stateMethod.Id = methodAttribute.Id;
            }

            return stateMethod;
        }

        public Location Location { get; private set; } = Location.None;

        public bool Default { get; private set; }

        public bool CheckStateChange { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public int Id { get; internal set; }
    }
}
