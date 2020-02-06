/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.TeamFoundation.Framework.Common;

namespace SonarSource.TfsAnnotate
{
    /// <summary>
    ///     Implements a cache for account identifiers
    /// </summary>
    internal class AccountCache
    {
        private static readonly Regex LiveIdExpression = new Regex("windows live id/");

        private readonly IFoundationServiceProvider foundationServiceProvider;
        private readonly IDictionary<Tuple<Uri, string>, string> emailCache =
            new Dictionary<Tuple<Uri, string>, string>();

        public AccountCache(IFoundationServiceProvider foundationServiceProvider)
        {
            this.foundationServiceProvider = foundationServiceProvider;
        }

        public string BuildUserName(Uri serverUri, string accountName)
        {
            var trimmedUserName = LiveIdExpression.Replace(accountName, string.Empty);
            if (IsEmail(trimmedUserName))
            {
                // Visual Studio Online accounts are already email addresses
                return trimmedUserName;
            }

            var cacheKey = Tuple.Create(serverUri, accountName);
            if (!this.emailCache.TryGetValue(cacheKey, out string result))
            {
                var service = this.foundationServiceProvider.GetIdentityService(serverUri);
                var identity = service.ReadIdentity(
                    IdentitySearchFactor.AccountName,
                    accountName,
                    MembershipQuery.None,
                    ReadIdentityOptions.ExtendedProperties | ReadIdentityOptions.IncludeReadFromSource);

                if (identity == null)
                {
                    result = accountName;
                }
                else
                {
                    // ConfirmedNotificationAddress is set on the TFS profile itself
                    result = identity.GetAttribute("ConfirmedNotificationAddress", string.Empty);
                    if (!IsEmail(result))
                    {
                        // Mail is supposedly fetched from AD
                        result = identity.GetAttribute("Mail", accountName);
                        if (!IsEmail(result))
                        {
                            // Codeplex might return non-valid email addresses
                            result = accountName;
                        }
                    }
                }

                this.emailCache[cacheKey] = result;
            }

            return result;
        }

        private static bool IsEmail(string input)
        {
            return input.Contains('@');
        }
    }
}