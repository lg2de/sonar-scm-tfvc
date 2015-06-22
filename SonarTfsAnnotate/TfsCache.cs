/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (C) 2014 SonarSource
 * sonarqube@googlegroups.com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02
 */
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarSource.TfsAnnotate
{
    class TfsCache : IDisposable
    {
        private readonly TfsClientCredentials credentials;
        private readonly IDictionary<Uri, TfsTeamProjectCollection> teamCollectionCache = new Dictionary<Uri, TfsTeamProjectCollection>();
        private readonly IDictionary<Tuple<Uri, string>, string> emailCache = new Dictionary<Tuple<Uri, string>, string>();

        public TfsCache(TfsClientCredentials credentials)
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
