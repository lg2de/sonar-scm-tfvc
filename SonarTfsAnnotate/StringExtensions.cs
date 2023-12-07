/*
 * SonarQube :: SCM :: TFVC :: Tests
 * Copyright (c) Lukas Grützmacher.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */

namespace SonarSource.TfsAnnotate
{
    internal static class StringExtensions
    {
        public static string Mask(this string input)
        {
            var length = input.Length;
            int plainLength = length / 10;
            var prefix = input.Substring(0, plainLength);
            var suffix = input.Substring(length - plainLength, plainLength);
            return $"{prefix}***{suffix}";
        }
    }
}
