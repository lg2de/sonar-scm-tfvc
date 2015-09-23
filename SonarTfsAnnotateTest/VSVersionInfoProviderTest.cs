/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarSource.TfsAnnotate;
using System;
using System.IO;

namespace SonarTFSAnnotateTest
{
    [TestClass]
    public class VSVersionInfoProviderTest
    {
        [TestMethod]
        public void GetVSVersionInfo_HighestVersionWithInstallDirectory_64Bit()
        {
            string[] subKeyNames = new string[4] { "b", "14.0", "12.0", "10.0" };
            string parentkeypath = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\VisualStudio\\14.0";

            var mockRegistryHelper = new Mock<RegistryHelper>();
            var mockEnvironmentHelper = new Mock<EnvironmentHelper>();

            mockEnvironmentHelper.Setup(m => m.Is64BitOS()).Returns(true);

            mockRegistryHelper.Setup(m => m.GetRegSubKeysUnderLocalMachine("SOFTWARE\\Wow6432Node\\Microsoft\\VisualStudio")).Returns(subKeyNames);

            mockRegistryHelper.Setup(m => m.GetRegistryValue(It.IsNotIn(parentkeypath), It.IsAny<string>())).Returns("randomstring");
            
            VSVersionInfoProvider vsVersionInfoProvider = new VSVersionInfoProvider(mockRegistryHelper.Object, mockEnvironmentHelper.Object);

            VSVersionInfo vsVersionInfo = vsVersionInfoProvider.GetVSVersionInfo();

            Assert.AreEqual(vsVersionInfo.Version, 12.0m);
            Assert.AreEqual(vsVersionInfo.PathToInstallDirectory, "randomstring");
        }

        [TestMethod]
        public void GetVSVersionInfo_HighestVersionWithInstallDirectory_32Bit()
        {
            string[] subKeyNames = new string[4] { "b", "14.0", "12.0", "10.0" };
            string parentkeypath = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\VisualStudio\\14.0";

            var mockRegistryHelper = new Mock<RegistryHelper>();
            var mockEnvironmentHelper = new Mock<EnvironmentHelper>();

            mockEnvironmentHelper.Setup(m => m.Is64BitOS()).Returns(false);

            mockRegistryHelper.Setup(m => m.GetRegSubKeysUnderLocalMachine("SOFTWARE\\Microsoft\\VisualStudio")).Returns(subKeyNames);

            mockRegistryHelper.Setup(m => m.GetRegistryValue(It.IsNotIn(parentkeypath), It.IsAny<string>())).Returns("randomstring");
            
            VSVersionInfoProvider vsVersionInfoProvider = new VSVersionInfoProvider(mockRegistryHelper.Object, mockEnvironmentHelper.Object);

            VSVersionInfo vsVersionInfo = vsVersionInfoProvider.GetVSVersionInfo();

            Assert.AreEqual(vsVersionInfo.Version, 12.0m);
            Assert.AreEqual(vsVersionInfo.PathToInstallDirectory, "randomstring");
        }

        [TestMethod]
        public void GetVSVersionInfo_VSRegkeyAbsent()
        {
            var mockRegistryHelper = new Mock<RegistryHelper>();

            var mockEnvironmentHelper = new Mock<EnvironmentHelper>();

            mockEnvironmentHelper.Setup(m => m.Is64BitOS()).Returns(true);
            mockRegistryHelper.Setup(m => m.GetRegSubKeysUnderLocalMachine(It.IsAny<string>())).Returns(new string[0]);


            VSVersionInfoProvider vsVersionInfoProvider = new VSVersionInfoProvider(mockRegistryHelper.Object, mockEnvironmentHelper.Object);
            VSVersionInfo vsVersionInfo = vsVersionInfoProvider.GetVSVersionInfo();

            Assert.AreEqual(vsVersionInfo.Version, 0.0m);
            Assert.IsNull(vsVersionInfo.PathToInstallDirectory);
        }

        [TestMethod]
        public void GetVSVersionInfo_NoVersion()
        {
            string[] subKeyNames = new string[3] { "VS", "VC", "VSIP" };
            
            var mockRegistryHelper = new Mock<RegistryHelper>();

            var mockEnvironmentHelper = new Mock<EnvironmentHelper>();

            mockEnvironmentHelper.Setup(m => m.Is64BitOS()).Returns(true);

            mockRegistryHelper.Setup(m => m.GetRegSubKeysUnderLocalMachine("SOFTWARE\\Wow6432Node\\Microsoft\\VisualStudio")).Returns(subKeyNames);

            mockRegistryHelper.Setup(m => m.GetRegistryValue(It.IsAny<string>(), It.IsAny<string>())).Returns("randomstring");

            VSVersionInfoProvider vsVersionInfoProvider = new VSVersionInfoProvider(mockRegistryHelper.Object, mockEnvironmentHelper.Object);
            VSVersionInfo vsVersionInfo = vsVersionInfoProvider.GetVSVersionInfo();

            Assert.AreEqual(vsVersionInfo.Version, 0.0m);
            Assert.IsNull(vsVersionInfo.PathToInstallDirectory);
        }


        [TestMethod]
        public void GetVSVersionInfo_NoInstallDir()
        {
            string[] subKeyNames = new string[3] { "12.0", "10.0", "14.0" };

            var mockRegistryHelper = new Mock<RegistryHelper>();

            var mockEnvironmentHelper = new Mock<EnvironmentHelper>();

            mockEnvironmentHelper.Setup(m => m.Is64BitOS()).Returns(true);

            mockRegistryHelper.Setup(m => m.GetRegSubKeysUnderLocalMachine("SOFTWARE\\Wow6432Node\\Microsoft\\VisualStudio")).Returns(subKeyNames);

            
            VSVersionInfoProvider vsVersionInfoProvider = new VSVersionInfoProvider(mockRegistryHelper.Object, mockEnvironmentHelper.Object);

            VSVersionInfo vsVersionInfo = vsVersionInfoProvider.GetVSVersionInfo();

            Assert.AreEqual(vsVersionInfo.Version, 0.0m);
            Assert.IsNull(vsVersionInfo.PathToInstallDirectory);
        }
    }
}
