/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
using System.IO;
using System.Text.RegularExpressions;

namespace SonarSource.TfsAnnotate
{
    /// <summary>
    /// Extracts information about highest installed version Visual Studio  
    /// </summary>
    public class VSVersionInfoProvider
    {
        private decimal highestInstalledVSVersion;
        private RegistryHelper registryHelper;
        private EnvironmentHelper environmentHelper;

        public VSVersionInfoProvider(RegistryHelper registryHelper, EnvironmentHelper environmentHelper)
        {
            this.registryHelper = registryHelper;
            this.environmentHelper = environmentHelper;
        }

        public VSVersionInfoProvider() : this(new RegistryHelper(), new EnvironmentHelper())
        {
        }

        /// <summary>
        /// Checks if subKeyName refers to a version
        /// and parses it as a decimal.
        /// </summary>
        private decimal GetVersion(string subKeyName)
        {
            decimal vsVersion = 0.0m;
            Match subKeyVersion = Regex.Match(subKeyName, @"^(?=.*\d)\d*(?:\.\d)?$");
            if (!string.IsNullOrEmpty(subKeyVersion.Value))
            {
                vsVersion = decimal.Parse(subKeyVersion.Value);
            }
            return vsVersion;
        }

        /// <summary>
        /// Get highest VS Version with an install directory path
        /// </summary>
        private string GetLatestVSInstallDirectory(string parentKeyRelativePath, string parentKeyPath)
        {
            string[] subKeyNames = registryHelper.GetRegSubKeysUnderLocalMachine(parentKeyRelativePath);
            string highestVersionInstallDirectory = null;
            foreach (string subKeyName in subKeyNames)
            {
                decimal currentVersion = GetVersion(subKeyName);
                if (currentVersion != 0.0m)
                {
                    string installationPath = registryHelper.GetRegistryValue(Path.Combine(parentKeyPath, subKeyName), "InstallDir") as string;
                    if (currentVersion > highestInstalledVSVersion && installationPath != null)
                    {
                        highestInstalledVSVersion = currentVersion;
                        highestVersionInstallDirectory = installationPath;
                    }
                }
            }
            return highestVersionInstallDirectory;
        }

        private string GetParentKeyPath()
        {
            string parentKeyRelativePath;
            bool is64BitEnvironment = environmentHelper.Is64BitOS();
            if (is64BitEnvironment)
            {
                parentKeyRelativePath = "SOFTWARE\\Wow6432Node\\Microsoft\\VisualStudio";
            }
            else
            {
                parentKeyRelativePath = "SOFTWARE\\Microsoft\\VisualStudio";
            }
            return parentKeyRelativePath;
        }

        /// <summary>
        /// Returns information about the highest installed
        /// Visual Studio Version and it's install directory path.
        /// </summary>
        public VSVersionInfo GetVSVersionInfo()
        {
            VSVersionInfo vsVersionInfo;
            string parentKeyRelativePath = GetParentKeyPath();

            string parentKeyFullPath = Path.Combine("HKEY_LOCAL_MACHINE", parentKeyRelativePath);
            string vsInstallDirectoryPath = GetLatestVSInstallDirectory(parentKeyRelativePath, parentKeyFullPath);

            vsVersionInfo = new VSVersionInfo(highestInstalledVSVersion, vsInstallDirectoryPath);

            return vsVersionInfo;
        }
    }
}
