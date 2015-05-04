/*
 * SonarQube TFS Annotate Command Line Tool
 * Copyright (C) 2014 SonarSource
 * dev@sonar.codehaus.org
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
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;

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

            using (var cache = new TfsCache(new TfsClientCredentials(true)))
            {
                string path;
                while ((path = Console.ReadLine()) != null)
                {
                    if (!File.Exists(path))
                    {
                        Console.Error.WriteLine("The given file does not exist: " + path);
                        return 2;
                    }

                    if (!Workstation.Current.IsMapped(path))
                    {
                        Console.Error.WriteLine("The given file is not in a mapped TFS workspace: " + path);
                        return 3;
                    }

                    WorkspaceInfo workspaceInfo = Workstation.Current.GetLocalWorkspaceInfo(path);
                    Uri serverUri = workspaceInfo.ServerUri;
                    WorkspaceVersionSpec version = new WorkspaceVersionSpec(workspaceInfo);
                    var versionControlServer = cache.GetVersionControlServer(serverUri);

                    IAnnotatedFile annotatedFile = new FileAnnotator(versionControlServer).Annotate(path, version);
                    if (annotatedFile == null)
                    {
                        Console.Error.WriteLine("The given file has not yet been checked-in: " + path);
                        return 4;
                    }

                    if (annotatedFile.IsBinary())
                    {
                        Console.Error.WriteLine("The given file is a binary one: " + path);
                        return 5;
                    }

                    Console.WriteLine(path);
                    Console.WriteLine(annotatedFile.Lines());
                    for (int i = 0; i < annotatedFile.Lines(); i++)
                    {
                        switch (annotatedFile.State(i))
                        {
                            case AnnotationState.UNKNOWN:
                                Console.Write("unknown ");
                                break;
                            case AnnotationState.LOCAL:
                                Console.Write("local ");
                                break;
                            case AnnotationState.COMMITTED:
                                Changeset changeset = annotatedFile.Changeset(i);
                                Console.Write(changeset.ChangesetId);
                                Console.Write(' ');
                                Console.Write(cache.GetEmailOrAccountName(serverUri, changeset.Owner));
                                Console.Write(' ');
                                Console.Write(ToUnixTimestampInMs(changeset.CreationDate));
                                Console.Write(' ');
                                break;
                            default:
                                throw new InvalidOperationException("Unsupported annotation state: " + annotatedFile.State(i));
                        }

                        Console.WriteLine(annotatedFile.Data(i));
                    }

                    Console.Out.Flush();
                }
            }

            return 0;
        }

        private static long ToUnixTimestampInMs(DateTime dateTime)
        {
            var timespan = dateTime - Epoch;
            return Convert.ToInt64(timespan.TotalMilliseconds);
        }
    }
}
