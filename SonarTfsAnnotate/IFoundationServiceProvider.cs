/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) Lukas Grützmacher.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */

using System;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace SonarSource.TfsAnnotate
{
    /// <summary>
    ///     Provides access to Team Foundation API.
    /// </summary>
    internal interface IFoundationServiceProvider
    {
        IIdentityManagementService GetIdentityService(Uri serverUri);

        VersionControlServer GetVersionControlServer(Uri serverUri);
    }
}