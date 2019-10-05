/*
 * SonarQube :: SCM :: TFVC :: Tests
 * Copyright (c) Lukas Grützmacher.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */

using System;
using FluentAssertions;
using NSubstitute;
using SonarSource.TfsAnnotate;
using Xunit;

namespace SonarTfsAnnotateTests
{
    public class AccountCacheTests
    {
        private static readonly Uri LocalServer = new Uri("https://localtfs/");

        [Fact]
        public void BuildUserName_Email_ReturnsEmail()
        {
            var foundationServiceProvider = Substitute.For<IFoundationServiceProvider>();
            var sut = new AccountCache(foundationServiceProvider);

            var email = "user@github.com";
            var result = sut.BuildUserName(LocalServer, email);

            result.Should().Be(email);
        }
    }
}
