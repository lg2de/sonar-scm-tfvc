/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
using System;

namespace SonarSource.TfsAnnotate
{
    [Serializable()]
    public class AssemblyNotFoundException : System.Exception
    {
        public AssemblyNotFoundException() : base()
        {
        }

        public AssemblyNotFoundException(string message) : base(message)
        {
        }

        public AssemblyNotFoundException(string message, System.Exception inner) : base(message, inner)
        {
        }

        protected AssemblyNotFoundException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        {
        }
    }
}
