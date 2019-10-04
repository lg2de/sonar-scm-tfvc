/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Services.Common;

namespace SonarSource.TfsAnnotate
{
    internal class TfsCache : IDisposable
    {
        private readonly VssCredentials credentials;

        private readonly IDictionary<Tuple<Uri, string>, string> emailCache =
            new Dictionary<Tuple<Uri, string>, string>();

        private readonly IDictionary<Uri, TfsTeamProjectCollection> teamCollectionCache =
            new Dictionary<Uri, TfsTeamProjectCollection>();

        public TfsCache(VssCredentials credentials)
        {
            this.credentials = credentials;
        }

        public void Dispose()
        {
            foreach (var teamCollection in teamCollectionCache.Values)
            {
                teamCollection.Dispose();
            }
        }

        private TfsTeamProjectCollection GetTeamProjectCollection(Uri serverUri)
        {
            if (!teamCollectionCache.TryGetValue(serverUri, out var result))
            {
                // create new connection, validate and store
                result = new TfsTeamProjectCollection(serverUri, credentials);
                result.EnsureAuthenticated();
                teamCollectionCache[serverUri] = result;
            }

            return result;
        }

        public void EnsureAuthenticated(Uri serverUri)
        {
            GetTeamProjectCollection(serverUri).EnsureAuthenticated();
        }

        public VersionControlServer GetVersionControlServer(Uri serverUri)
        {
            return GetTeamProjectCollection(serverUri).GetService<VersionControlServer>();
        }

        private IIdentityManagementService GetIdentityManagementService(Uri serverUri)
        {
            return GetTeamProjectCollection(serverUri).GetService<IIdentityManagementService>();
        }

        public string GetEmailOrAccountName(Uri serverUri, string accountName)
        {
            if (IsEmail(accountName))
            {
                // Visual Studio Online accounts are already email addresses
                return accountName;
            }

            var key = Tuple.Create(serverUri, accountName);
            if (!emailCache.TryGetValue(key, out string result))
            {
                var service = GetIdentityManagementService(serverUri);
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

                emailCache[key] = result;
            }

            return result;
        }

        private static bool IsEmail(string email)
        {
            return email.Contains('@');
        }
    }
}