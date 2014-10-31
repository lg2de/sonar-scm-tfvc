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

namespace SonarSource.TfsAnnotate
{
    using Microsoft.TeamFoundation;
    using Microsoft.TeamFoundation.Client;
    using Microsoft.TeamFoundation.VersionControl.Client;
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;

    public class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("Expected exactly three argument, the file to annotate. " + args.Length + " given.");
                return 1;
            }

            string path = args[0];
            string user = args[1];
            string password = args[2];

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
            TfsClientCredentials credentials = new TfsClientCredentials(true);
            credentials.AllowInteractive = false;
            
            using (TfsTeamProjectCollection collection = new TfsTeamProjectCollection(serverUri, credentials))
            {
                collection.Credentials = new NetworkCredential(user, password);

                try
                {
                    collection.Authenticate();
                }
                catch (TeamFoundationServerUnauthorizedException)
                {
                    Console.Error.WriteLine("The user {0} can't access to the server {1} with the password {2}", user, serverUri.OriginalString, password);
                    return 4;
                }                

                VersionControlServer server = collection.GetService<VersionControlServer>();

                IAnnotatedFile annotatedFile = new FileAnnotator(server).Annotate(path, version);
                
                if (annotatedFile == null)
                {
                    Console.Error.WriteLine("The given file has not yet been checked-in: " + path);
                    return 5;
                }

                if (annotatedFile.IsBinary())
                {
                    Console.Error.WriteLine("The given file is a binary one: " + path);
                    return 6;
                }

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
                            Console.Write(changeset.Owner);
                            Console.Write(' ');
                            Console.Write(changeset.CreationDate.ToString("MM\\/dd\\/yyyy", CultureInfo.InvariantCulture));
                            Console.Write(' ');
                            break;
                        default:
                            throw new InvalidOperationException("Unsupported annotation state: " + annotatedFile.State(i));
                    }

                    Console.WriteLine(annotatedFile.Data(i));
                }
            }

            return 0;
        }
    }
}
