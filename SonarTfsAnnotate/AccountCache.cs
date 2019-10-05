/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Framework.Common;

namespace SonarSource.TfsAnnotate
{
    internal class AccountCache
    {
        private readonly IFoundationServiceProvider foundationServiceProvider;
        private readonly IDictionary<Tuple<Uri, string>, string> emailCache =
            new Dictionary<Tuple<Uri, string>, string>();

        public AccountCache(IFoundationServiceProvider foundationServiceProvider)
        {
            this.foundationServiceProvider = foundationServiceProvider;
        }

        public string BuildUserName(Uri serverUri, string accountName)
        {
            if (IsEmail(accountName))
            {
                // Visual Studio Online accounts are already email addresses
                return accountName;
            }

            var key = Tuple.Create(serverUri, accountName);
            if (!this.emailCache.TryGetValue(key, out string result))
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

                this.emailCache[key] = result;
            }

            return result;
        }

        private static bool IsEmail(string email)
        {
            return email.Contains('@');
        }
    }
}