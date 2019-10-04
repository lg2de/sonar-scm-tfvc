/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarSource.TfsAnnotate
{
    class TfsCache : IDisposable
    {
        private readonly VssCredentials credentials;
        private readonly IDictionary<Uri, TfsTeamProjectCollection> teamCollectionCache = new Dictionary<Uri, TfsTeamProjectCollection>();
        private readonly IDictionary<Tuple<Uri, string>, string> emailCache = new Dictionary<Tuple<Uri, string>, string>();

        public TfsCache(VssCredentials credentials)
        {
            this.credentials = credentials;
        }

        private TfsTeamProjectCollection GetTeamProjectCollection(Uri serverUri)
        {
            TfsTeamProjectCollection result;
            if (!teamCollectionCache.TryGetValue(serverUri, out result))
            {
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

        private IIdentityManagementService GetIdentityMangementService(Uri serverUri)
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
            string result;
            if (!emailCache.TryGetValue(key, out result))
            {
                var identity = GetIdentityMangementService(serverUri)
                    .ReadIdentity(IdentitySearchFactor.AccountName, accountName, MembershipQuery.None, ReadIdentityOptions.ExtendedProperties | ReadIdentityOptions.IncludeReadFromSource);

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
                        // Mail is supposedly fethed from AD
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

        public void Dispose()
        {
            foreach (var teamCollection in teamCollectionCache.Values)
            {
                teamCollection.Dispose();
            }
        }
    }
}
