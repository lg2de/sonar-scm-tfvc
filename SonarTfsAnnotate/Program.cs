/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
using System;
using System.IO;
using System.Text;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using System.Net;

namespace SonarSource.TfsAnnotate
{
    class Program
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1);

        static int Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            if (args.Length != 0)
            {
                Console.Error.WriteLine("This program is only expected to be called by the SonarQube TFS SCM plugin.");
                return 1;
            }

            Console.WriteLine("Enter your credentials");
            Console.Out.Flush();
            var username = Console.ReadLine();
            var password = Console.ReadLine();

            TfsClientCredentials credentials;
            if (!String.IsNullOrEmpty(username) || !String.IsNullOrEmpty(password))
            {
                credentials = new TfsClientCredentials(new WindowsCredential(new NetworkCredential(username, password)));
            }
            else
            {
                credentials = new TfsClientCredentials(true);
            }
            credentials.AllowInteractive = false;
            using (var cache = new TfsCache(credentials))
            {
                Console.Out.WriteLine("Enter the paths to annotate");
                Console.Out.Flush();

                string path;
                while ((path = Console.ReadLine()) != null)
                {
                    Console.Out.Flush();
                    Console.WriteLine(path);

                    if (!File.Exists(path))
                    {
                        FailOnFile("does not exist: " + path);
                        continue;
                    }

                    WorkspaceInfo workspaceInfo = Workstation.Current.GetLocalWorkspaceInfo(path);
                    Uri serverUri = workspaceInfo.ServerUri;
                    WorkspaceVersionSpec version = new WorkspaceVersionSpec(workspaceInfo);

                    VersionControlServer versionControlServer = cache.GetVersionControlServer(serverUri);

                    Workstation.Current.EnsureUpdateWorkspaceInfoCache(versionControlServer, workspaceInfo.OwnerName);

                    if (!Workstation.Current.IsMapped(path))
                    {
                        FailOnFile("is not in a mapped TFS workspace: " + path);
                        continue;
                    }

                    try
                    {
                        cache.EnsureAuthenticated(serverUri);
                    }
                    catch (Exception e)
                    {
                        FailOnFile("raised the following authentication exception: " + path + ", " + e.Message);
                        return 1;
                    }

                    IAnnotatedFile annotatedFile = new FileAnnotator(versionControlServer).Annotate(path, version);
                    if (annotatedFile == null)
                    {
                        FailOnFile("is not yet checked-in: " + path);
                        continue;
                    }

                    if (annotatedFile.IsBinary())
                    {
                        FailOnFile("is a binary one: " + path);
                        continue;
                    }

                    bool failed = false;
                    for (int i = 0; !failed && i < annotatedFile.Lines(); i++)
                    {
                        var state = annotatedFile.State(i);
                        if (state != AnnotationState.COMMITTED)
                        {
                            FailOnFile("line " + (i + 1) + " has not yet been checked-in (" + state + "): " + path);
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
                        Changeset changeset = annotatedFile.Changeset(i);
                        Console.Write(changeset.ChangesetId);
                        Console.Write('\t');
                        Console.Write(cache.GetEmailOrAccountName(serverUri, changeset.Owner));
                        Console.Write('\t');
                        Console.Write(ToUnixTimestampInMs(changeset.CreationDate));
                        Console.Write('\t');
                        Console.WriteLine(annotatedFile.Data(i));
                    }
                }
                Console.Out.Flush();
            }

            return 0;
        }

        private static long ToUnixTimestampInMs(DateTime dateTime)
        {
            var timespan = dateTime - Epoch;
            return Convert.ToInt64(timespan.TotalMilliseconds);
        }

        private static void FailOnFile(string reason)
        {
            Console.WriteLine("0");
            Console.Write("Unable to TFS annotate the following file which ");
            Console.WriteLine(reason);
        }
    }
}
