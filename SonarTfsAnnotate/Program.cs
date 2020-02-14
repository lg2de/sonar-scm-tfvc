/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */

using System;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Services.Common;
using PowerArgs;

namespace SonarSource.TfsAnnotate
{
    internal static class Program
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1);
        private static Uri serverUri;

        private static int Main(string[] args)
        {
            try
            {
                Console.InputEncoding = Encoding.UTF8;
                Console.OutputEncoding = Encoding.UTF8;

                var options = Args.Parse<Options>(args);

                VssCredentials credentials;
                if (!string.IsNullOrEmpty(options.PersonalAccessToken))
                {
                    Console.WriteLine($"Connecting using PAT: {options.PersonalAccessToken.Mask()}...");
                    var credential = new NetworkCredential(userName: "", password: options.PersonalAccessToken);
                    credentials = new VssCredentials(new VssBasicCredential(credential));
                }
                else if (!string.IsNullOrEmpty(options.UserName) || !string.IsNullOrEmpty(options.Password))
                {
                    Console.WriteLine($"Connecting using user/password: {options.UserName}/{options.Password.Mask()}...");
                    var credential = new NetworkCredential(options.UserName, options.Password);
                    credentials = new VssCredentials(new WindowsCredential(credential));
                }
                else
                {
                    Console.WriteLine("Connecting using default credentials...");
                    credentials = new VssCredentials(useDefaultCredentials: true);
                }

                if (!string.IsNullOrEmpty(options.CollectionUri))
                {
                    if (!SetServerUri(options.CollectionUri))
                    {
                        return 1;
                    }
                }

                using (var foundationServiceProvider = new FoundationServiceProvider(credentials))
                {
                    var cache = new AccountCache(foundationServiceProvider);
                    if (serverUri != null)
                    {
                        UpdateWorkspaceCache(foundationServiceProvider);
                    }

                    if (!string.IsNullOrEmpty(options.FileName))
                    {
                        AnnotateFile(options.FileName, foundationServiceProvider, cache);
                        Console.Out.Flush();
                        return 0;
                    }

                    Console.Out.WriteLine("Enter the local paths to annotate:");
                    Console.Out.Flush();

                    while (true)
                    {
                        var path = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(path))
                        {
                            break;
                        }

                        try
                        {
                            Console.Out.Flush();
                            Console.WriteLine(path);

                            if (!File.Exists(path))
                            {
                                FailOnFile(path, "The file does not exist.");
                                continue;
                            }

                            AnnotateFile(path, foundationServiceProvider, cache);
                        }
                        catch (Exception e)
                        {
                            FailOnFile(path, e.Message);
                        }
                    }
                }

                Console.Out.Flush();
                return 0;
            }
            catch (ArgException ex)
            {
                FailOnProject(ex.Message);
                Console.WriteLine(ArgUsage.GenerateUsageFromTemplate<Options>());
                Console.Out.Flush();
                return 1;
            }
            catch (Exception e)
            {
                FailOnProject(e.Message);
                Console.Out.Flush();
                return 1;
            }
        }

        private static void AnnotateFile(
            string localPath,
            FoundationServiceProvider foundationServiceProvider,
            AccountCache cache)
        {
            if (!Workstation.Current.IsMapped(localPath))
            {
                FailOnFile(localPath, "The file is not in a mapped TFS workspace.");
                return;
            }

            var workspaceInfo = Workstation.Current.GetLocalWorkspaceInfo(localPath);
            var version = new WorkspaceVersionSpec(workspaceInfo);

            if (serverUri == null || workspaceInfo.ServerUri.AbsoluteUri != serverUri.AbsoluteUri)
            {
                serverUri = workspaceInfo.ServerUri;
                UpdateWorkspaceCache(foundationServiceProvider);
            }

            var versionControlServer = foundationServiceProvider.GetVersionControlServer(serverUri);

            var annotatedFile = new FileAnnotator(versionControlServer).Annotate(localPath, version);
            if (annotatedFile == null)
            {
                FailOnFile(localPath, "The file is not yet checked-in.");
                return;
            }

            if (annotatedFile.IsBinary())
            {
                FailOnFile(localPath, "The file is a binary.");
                return;
            }

            bool failed = false;
            for (int i = 0; !failed && i < annotatedFile.Lines(); i++)
            {
                var state = annotatedFile.State(i);
                if (state == AnnotationState.Committed)
                {
                    // ok
                    continue;
                }

                FailOnFile(localPath, $"line {i + 1} has not yet been checked-in ({state}): {localPath}");
                failed = true;
            }

            if (failed)
            {
                return;
            }

            Console.WriteLine(annotatedFile.Lines());
            for (int i = 0; i < annotatedFile.Lines(); i++)
            {
                var changeSet = annotatedFile.Changeset(i);
                Console.Write(changeSet.ChangesetId);
                Console.Write('\t');
                Console.Write(cache.BuildUserName(serverUri, changeSet.Owner));
                Console.Write('\t');
                Console.Write(ToUnixTimestampInMs(changeSet.CreationDate));
                Console.Write('\t');
                Console.WriteLine(annotatedFile.Data(i));
            }
        }

        private static void UpdateWorkspaceCache(IFoundationServiceProvider foundationServiceProvider)
        {
            var versionControlServer = foundationServiceProvider.GetVersionControlServer(serverUri);
            Workstation.Current.EnsureUpdateWorkspaceInfoCache(versionControlServer,
                versionControlServer.AuthorizedUser);
        }

        private static bool SetServerUri(string serverUriString)
        {
            try
            {
                serverUri = new Uri(serverUriString);
            }
            catch (UriFormatException e)
            {
                FailOnProject(
                    $"Unable to set server URI to '{serverUriString}'. Please check the configuration. Exception: '{e.Message}'.");
                return false;
            }

            return true;
        }

        private static long ToUnixTimestampInMs(DateTime dateTime)
        {
            var timespan = dateTime - Epoch;
            return Convert.ToInt64(timespan.TotalMilliseconds);
        }

        private static void FailOnFile(string fileName, string reason)
        {
            Console.Out.WriteLine("AnnotationFailedOnFile");
            Console.Error.WriteLine($"Unable to annotate the file {fileName}: {reason}");
        }

        private static void FailOnProject(string message)
        {
            Console.Out.WriteLine("AnnotationFailedOnProject");
            Console.Error.WriteLine(message);
        }
    }
}
