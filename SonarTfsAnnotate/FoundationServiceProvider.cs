/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) Lukas Grützmacher.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */

using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Services.Common;

namespace SonarSource.TfsAnnotate
{
    /// <summary>
    ///     Default implementation for <see cref="IFoundationServiceProvider"/> using real TFS API.
    /// </summary>
    internal sealed class FoundationServiceProvider : IFoundationServiceProvider, IDisposable
    {
        private readonly VssCredentials credentials;
        private readonly IDictionary<Uri, TfsTeamProjectCollection> teamCollectionCache =
            new Dictionary<Uri, TfsTeamProjectCollection>();

        public FoundationServiceProvider(VssCredentials credentials)
        {
            this.credentials = credentials;
        }

        public void Dispose()
        {
            foreach (var teamCollection in this.teamCollectionCache.Values)
            {
                teamCollection.Dispose();
            }
        }

        public IIdentityManagementService GetIdentityService(Uri serverUri)
        {
            return this.GetTeamProjectCollection(serverUri).GetService<IIdentityManagementService>();
        }

        public VersionControlServer GetVersionControlServer(Uri serverUri)
        {
            return this.GetTeamProjectCollection(serverUri).GetService<VersionControlServer>();
        }

        private TfsTeamProjectCollection GetTeamProjectCollection(Uri serverUri)
        {
            if (!this.teamCollectionCache.TryGetValue(serverUri, out var result))
            {
                // create new connection, validate and store
                result = new TfsTeamProjectCollection(serverUri, this.credentials);
                result.EnsureAuthenticated();
                this.teamCollectionCache[serverUri] = result;
            }

            return result;
        }
    }
}