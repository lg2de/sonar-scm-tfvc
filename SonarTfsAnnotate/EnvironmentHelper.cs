/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
using System;

namespace SonarSource.TfsAnnotate
{
    public class EnvironmentHelper
    {
        public virtual bool Is64BitOS()
        {
            return Environment.Is64BitOperatingSystem;
        }
    }
}
