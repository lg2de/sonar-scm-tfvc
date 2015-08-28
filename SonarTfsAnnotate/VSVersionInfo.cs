/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
namespace SonarSource.TfsAnnotate
{
    /// <summary>
    /// Information about the installed VS version
    /// </summary>
    public class VSVersionInfo
    {
        public VSVersionInfo(decimal version, string path)
        {
            Version = version;
            PathToInstallDirectory = path;
        }

        public decimal Version { get; set; }

        public string PathToInstallDirectory { get; set; }
    }
}
