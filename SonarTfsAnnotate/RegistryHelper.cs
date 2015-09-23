/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
using Microsoft.Win32;

namespace SonarSource.TfsAnnotate
{
    public class RegistryHelper
    {
        public virtual string[] GetRegSubKeysUnderLocalMachine(string parentKeyRelativePath)
        {
            string[] subKeyNames = new string[0];
            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(parentKeyRelativePath);
            if (registryKey != null)
            {
                subKeyNames = registryKey.GetSubKeyNames();
                registryKey.Close();
            }
            return subKeyNames;
        }

        public virtual object GetRegistryValue(string keyName, string valueName)
        {
            return Registry.GetValue(keyName, valueName, null);
        }
    }
}
