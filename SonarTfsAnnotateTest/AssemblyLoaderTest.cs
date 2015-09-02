/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Reflection;
using SonarSource.TfsAnnotate;
using System;
using System.IO;

namespace SonarTFSAnnotateTest
{
    [TestClass]
    public class AssemblyLoaderTest
    {

       [TestMethod]
        public void GetCorrespondingAssemblyOverride_TestforDev14()
        {
            var vsVersionInfo = new VSVersionInfo(14.0m, "A");
            var mockAssemblyHelper = new Mock<AssemblyHelper>();
            Assembly assembly = Assembly.GetExecutingAssembly();
            object nullSender = null;

            mockAssemblyHelper.Setup(m => m.LoadAssembly("A\\CommonExtensions\\Microsoft\\TeamFoundation\\Team Explorer\\B.dll")).Returns(assembly);

            AssemblyLoader assemblyLoader = new AssemblyLoader(vsVersionInfo, mockAssemblyHelper.Object);

            Assembly assemblyCorrectName = assemblyLoader.GetCorrespondingAssemblyOverride(nullSender, new ResolveEventArgs("B,  Version=14.0.0.0"));
            Assembly assemblyIncorrectName = assemblyLoader.GetCorrespondingAssemblyOverride(nullSender, new ResolveEventArgs("C,  Version=14.0.0.0"));

            Assert.AreEqual(assembly, assemblyCorrectName);
            Assert.IsNull(assemblyIncorrectName);
        }

        [TestMethod]
        [ExpectedException(typeof(AssemblyNotFoundException))]
        public void GetCorrespondingAssemblyOverride_TestforDev14CatchException()
        {
            var vsVersionInfo = new VSVersionInfo(14.0m, "A");
            var mockAssemblyHelper = new Mock<AssemblyHelper>();
            object nullSender = null;

            mockAssemblyHelper.Setup(m => m.LoadAssembly(It.IsAny<string>())).Throws(new FileNotFoundException()); ;

            AssemblyLoader assemblyLoader = new AssemblyLoader(vsVersionInfo, mockAssemblyHelper.Object);

            Assembly assemblyTest = assemblyLoader.GetCorrespondingAssemblyOverride(nullSender, new ResolveEventArgs("C,  Version=14.0.0.0"));
        }

        [TestMethod]
        public void GetCorrespondingAssemblyOverride_TestforDev12()
        {
            var vsVersionInfo = new VSVersionInfo(12.0m, "A");
            var mockAssemblyHelper = new Mock<AssemblyHelper>();
            Assembly assembly = Assembly.GetExecutingAssembly();
            object nullSender = null;
            string assemblyNameVersion12 = "Hello, Version=12.0.0.0";
            string assemblyNameVersion14 = "Hello, Version=14.0.0.0";
            string assemblyNameNoVersion = "Hello";
            string otherAssemblyNameNoVersion = "Bye";

            mockAssemblyHelper.Setup(m => m.LoadAssemblyFromGAC(assemblyNameVersion12)).Returns(assembly);
            mockAssemblyHelper.Setup(m => m.CheckAssemblyInGAC(It.IsAny<string>())).Returns(true);

            AssemblyLoader assemblyLoader = new AssemblyLoader(vsVersionInfo, mockAssemblyHelper.Object);

            Assembly assemblyVersion14 = assemblyLoader.GetCorrespondingAssemblyOverride(nullSender, new ResolveEventArgs(assemblyNameVersion14));
            Assembly assemblyVersion12 = assemblyLoader.GetCorrespondingAssemblyOverride(nullSender, new ResolveEventArgs(assemblyNameVersion12));
            Assembly assemblyNoVersion = assemblyLoader.GetCorrespondingAssemblyOverride(nullSender, new ResolveEventArgs(assemblyNameNoVersion));
            Assembly assemblyIncorrectName = assemblyLoader.GetCorrespondingAssemblyOverride(nullSender, new ResolveEventArgs(otherAssemblyNameNoVersion));

            Assert.AreEqual(assembly, assemblyVersion14);
            Assert.AreEqual(assembly, assemblyVersion12);
            Assert.AreEqual(assembly, assemblyNoVersion);
            Assert.IsNull(assemblyIncorrectName);
        }


        [TestMethod]
        [ExpectedException(typeof(AssemblyNotFoundException))]
        public void GetCorrespondingAssemblyOverride_TestforDev12CatchException()
        {
            var vsVersionInfo = new VSVersionInfo(12.0m, "A");
            var mockAssemblyHelper = new Mock<AssemblyHelper>();
            object nullSender = null;

            mockAssemblyHelper.Setup(m => m.CheckAssemblyInGAC(It.IsAny<string>())).Throws(new FileNotFoundException());

            AssemblyLoader assemblyLoader = new AssemblyLoader(vsVersionInfo, mockAssemblyHelper.Object);

            Assembly assemblyTest = assemblyLoader.GetCorrespondingAssemblyOverride(nullSender, new ResolveEventArgs("Hello,  Version=14.0.0.0"));
        }

        [TestMethod]
        public void GetCorrespondingAssemblyOverride_TestforLowerVersions()
        {
            var vsVersionInfo = new VSVersionInfo(0.0m, "A");
            var mockAssemblyHelper = new Mock<AssemblyHelper>();
            Assembly assembly = Assembly.GetExecutingAssembly();
            object nullSender = null;
            string assemblyNameVersion12 = "Hello, Version=12.0.0.0";
            string assemblyNameVersion0 = "Hello, Version=0.0.0.0";
            string assemblyNameVersion14 = "Hello, Version=14.0.0.0";
            string assemblyNameNoVersion = "Hello";
            string otherAssemblyNameNoVersion = "Bye";

            mockAssemblyHelper.Setup(m => m.LoadAssemblyFromGAC(assemblyNameVersion12)).Returns(assembly);
            mockAssemblyHelper.Setup(m => m.CheckAssemblyInGAC(It.IsAny<string>())).Returns(true);

            AssemblyLoader assemblyLoader = new AssemblyLoader(vsVersionInfo, mockAssemblyHelper.Object);

            Assembly assemblyVersion14 = assemblyLoader.GetCorrespondingAssemblyOverride(nullSender, new ResolveEventArgs(assemblyNameVersion14));
            Assembly assemblyVersion12 = assemblyLoader.GetCorrespondingAssemblyOverride(nullSender, new ResolveEventArgs(assemblyNameVersion0));
            Assembly assemblyNoVersion = assemblyLoader.GetCorrespondingAssemblyOverride(nullSender, new ResolveEventArgs(assemblyNameNoVersion));
            Assembly assemblyIncorrectName = assemblyLoader.GetCorrespondingAssemblyOverride(nullSender, new ResolveEventArgs(otherAssemblyNameNoVersion));

            Assert.AreEqual(assembly, assemblyVersion14);
            Assert.AreEqual(assembly, assemblyVersion12);
            Assert.AreEqual(assembly, assemblyNoVersion);
            Assert.IsNull(assemblyIncorrectName);
        }


        [TestMethod]
        [ExpectedException(typeof(AssemblyNotFoundException))]
        public void GetCorrespondingAssemblyOverride_TestforLowerVersionsCatchException()
        {
            var vsVersionInfo = new VSVersionInfo(0.0m, "A");
            var mockAssemblyHelper = new Mock<AssemblyHelper>();
            object nullSender = null;

            mockAssemblyHelper.Setup(m => m.CheckAssemblyInGAC(It.IsAny<string>())).Throws(new FileNotFoundException());

            AssemblyLoader assemblyLoader = new AssemblyLoader(vsVersionInfo, mockAssemblyHelper.Object);

            Assembly assemblyTest = assemblyLoader.GetCorrespondingAssemblyOverride(nullSender, new ResolveEventArgs("Hello,  Version=14.0.0.0"));
        }
    }
}