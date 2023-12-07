/*
 * SonarQube :: SCM :: TFVC :: Tests
 * Copyright (c) Lukas Grützmacher.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */

using FluentAssertions;
using SonarSource.TfsAnnotate;
using Xunit;

namespace SonarTfsAnnotate.Tests
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData("", "***")]
        [InlineData("a", "***")]
        [InlineData("ab", "***")]
        [InlineData("1234567890", "1***0")]
        [InlineData("12345678901234567890", "12***90")]
        public void Mask_SampleString_CorrectlyConverted(string input, string expected)
        {
            string result = input.Mask();

            result.Should().Be(expected);
        }
    }
}
