/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace SonarSource.TfsAnnotate
{
    /// <summary>
    /// Loads required assemblies from appropriate path.
    /// </summary>
    public class AssemblyLoader
    {
        private const decimal VSVersion12 = 12.0m;

        private decimal version;
        private string pathToInstallDirectory;
        private AssemblyHelper assemblyHelper;
        private string pathToAssemblyDirectory;

        public AssemblyLoader(VSVersionInfo vsVersionInfo, AssemblyHelper assemblyHelper)
        {
            version = vsVersionInfo.Version;
            pathToInstallDirectory = vsVersionInfo.PathToInstallDirectory;
            InitializeTFSAssemblyDirectory();
            this.assemblyHelper = assemblyHelper;
        }

        public AssemblyLoader(VSVersionInfo vsVersionInfo) : this(vsVersionInfo, new AssemblyHelper())
        {
        }

        public AssemblyLoader() : this(new VSVersionInfoProvider().GetVSVersionInfo())
        {
        }

        /// <summary>
        /// Dynamically loads assemblies during execution.
        /// </summary>
        /// <param name="sender">Information about the caller of this method</param>
        /// <param name="args">Contains details, like Name, about Assembly to be loaded</param>
        public Assembly GetCorrespondingAssemblyOverride(object sender, ResolveEventArgs args)
        {
            try
            {
                if (!string.IsNullOrEmpty(pathToAssemblyDirectory))
                {
                    return GetAssemblyByName(args.Name.Substring(0, args.Name.IndexOf(",")));
                }
                else
                {
                    return GetVersion12GACAssemblyByName(args.Name);
                }
            }
            catch (FileNotFoundException e)
            {
                throw new AssemblyNotFoundException("Unable to load " + e.FileName);
            }
        }

        /// <summary>
        /// Sets the folder path containing the required TFS Assemblies.
        /// Needs to be done only for versions above 12.0 as the 
        /// assemblies are not GACed anymore.
        /// </summary>
        private void InitializeTFSAssemblyDirectory()
        {
            pathToAssemblyDirectory = string.Empty;
            if (version > VSVersion12)
            {
                pathToAssemblyDirectory = Path.Combine(pathToInstallDirectory, "CommonExtensions\\Microsoft\\TeamFoundation\\Team Explorer\\");
            }
        }

        private Assembly GetVersion12GACAssemblyByName(string name)
        {
            AssemblyName assemblyName = new AssemblyName(name);
            assemblyName.Version = new Version("12.0.0.0");
            string parsedAssemblyName = assemblyName.ToString();
            return LoadFromGAC(parsedAssemblyName);
        }

        private Assembly LoadFromGAC(string parsedAssemblyName)
        {
            if (assemblyHelper.CheckAssemblyInGAC(parsedAssemblyName))
            {
                return assemblyHelper.LoadAssemblyFromGAC(parsedAssemblyName);
            }
            else
            {
                Debug.Fail("The Assemblies are not GACed");
                return null;
            }
        }

        private Assembly GetAssemblyByName(string name)
        {
            Debug.Assert(!string.IsNullOrEmpty(pathToAssemblyDirectory), "PathToAssemblyDirectory must not be null or empty.");
            string strTempAssmbPath = Path.Combine(pathToAssemblyDirectory, name + ".dll");

            Assembly assemblyFile = null;
            assemblyFile = assemblyHelper.LoadAssembly(strTempAssmbPath);

            return assemblyFile;
        }
    }
}
