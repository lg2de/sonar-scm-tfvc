/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) Lukas Grützmacher.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */

using PowerArgs;

namespace SonarSource.TfsAnnotate
{
    public class Options
    {
        [ArgDescription("The URI of the Azure DevOps (TFS) collection to be accessed.")]
        [ArgShortcut("-c")]
        public string CollectionUri { get; set; }

        [ArgDescription("The user name to be used to logon to Azure DevOps (TFS) server.")]
        [ArgShortcut("-u")]
        public string UserName { get; set; }

        [ArgDescription("The password to be used to logon to Azure DevOps (TFS) server.")]
        [ArgShortcut("-p")]
        public string Password { get; set; }

        [ArgDescription("The token to logon to Azure DevOps (TFS) server.")]
        [ArgShortcut("-PAT")]
        public string PersonalAccessToken { get; set; }

        [ArgDescription("Path to local file to annotate. If absent one or more files can be annotated interactively.")]
        [ArgExistingFile]
        [ArgShortcut("-f")]
        public string FileName { get; set; }
    }
}