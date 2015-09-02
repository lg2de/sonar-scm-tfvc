/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
using System.Reflection;

namespace SonarSource.TfsAnnotate
{
    public class AssemblyHelper
    {
        public virtual Assembly LoadAssemblyFromGAC(string parsedAssemblyName)
        {
            return Assembly.Load(parsedAssemblyName);
        }

        public virtual bool CheckAssemblyInGAC(string parsedAssemblyName)
        {
            return Assembly.ReflectionOnlyLoad(parsedAssemblyName).GlobalAssemblyCache;
        }

        public virtual Assembly LoadAssembly(string assemblyPath)
        {
            return Assembly.LoadFrom(assemblyPath);
        }
    }
}
