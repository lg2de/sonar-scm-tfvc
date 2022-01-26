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

                if (args.Length != 0)
                {
                    Console.Error.WriteLine(
                        "This program is only expected to be called by the SonarQube TFS SCM plugin.");
                    return 1;
                }

                Console.WriteLine("Enter your credentials - username, password, PAT (separate rows):");
                Console.Out.Flush();
                string username = Console.ReadLine();
                string password = Console.ReadLine();
                string pat = Console.ReadLine();

                VssCredentials credentials;

                if (!string.IsNullOrEmpty(pat))
                {
                    credentials = new VssCredentials(new VssBasicCredential(new NetworkCredential("", pat)));
                }
                else if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
                {
                    credentials = new VssCredentials(new WindowsCredential(new NetworkCredential(username, password)));
                }
                else
                {
                    credentials = new VssCredentials(true);
                }

                Console.WriteLine("Enter the Collection URI:");
                Console.Out.Flush();
                string serverUriString = Console.ReadLine();

                if (!string.IsNullOrEmpty(serverUriString))
                {
                    if (!SetServerUri(serverUriString))
                    {
                        return 1;
                    }
                }

                using (var foundationServiceProvider = new FoundationServiceProvider(credentials))
                {
                    var cache = new AccountCache(foundationServiceProvider);
                    if (serverUri != null)
                    {
                        if (!UpdateWorkspaceCache(foundationServiceProvider))
                        {
                            return 1;
                        }
                    }

                    Console.Out.WriteLine("Enter the paths to annotate:");
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

                            if (!Workstation.Current.IsMapped(path))
                            {
                                FailOnFile(path, "The file is not in a mapped TFS workspace.");
                                continue;
                            }

                            var workspaceInfo = Workstation.Current.GetLocalWorkspaceInfo(path);
                            var version = new WorkspaceVersionSpec(workspaceInfo);

                            if (serverUri == null || workspaceInfo.ServerUri.AbsoluteUri != serverUri.AbsoluteUri)
                            {
                                serverUri = workspaceInfo.ServerUri;
                                if (!UpdateWorkspaceCache(foundationServiceProvider))
                                {
                                    return 1;
                                }
                            }

                            var versionControlServer = foundationServiceProvider.GetVersionControlServer(serverUri);

                            var annotatedFile = new FileAnnotator(versionControlServer).Annotate(path, version);
                            if (annotatedFile == null)
                            {
                                FailOnFile(path, "The file is not yet checked-in.");
                                continue;
                            }

                            if (annotatedFile.IsBinary())
                            {
                                FailOnFile(path, "The file is a binary.");
                                continue;
                            }

                            bool failed = false;
                            for (int i = 0; !failed && i < annotatedFile.Lines(); i++)
                            {
                                var state = annotatedFile.State(i);
                                if (state != AnnotationState.Committed)
                                {
                                    FailOnFile(path, $"Line {(i + 1)} has not yet been checked-in ({state}).");
                                    failed = true;
                                }
                            }

                            if (failed)
                            {
                                continue;
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
                        catch (Exception e)
                        {
                            FailOnFile(path, e.Message);
                        }
                    }

                    Console.Out.Flush();
                }

                return 0;
            }
            catch (Exception e)
            {
                FailOnProject(
                    $"Unable to annotate the project. Exception: '{e.Message}'.{Environment.NewLine}{e.StackTrace}");
                return 1;
            }
        }

        private static bool UpdateWorkspaceCache(IFoundationServiceProvider foundationServiceProvider)
        {
            var versionControlServer = foundationServiceProvider.GetVersionControlServer(serverUri);
            Workstation.Current.EnsureUpdateWorkspaceInfoCache(versionControlServer,
                versionControlServer.AuthorizedUser);
            return true;
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
