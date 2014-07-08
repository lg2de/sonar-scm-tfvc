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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;

namespace SonarTfsAnnotate
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Expected exactly one argument, the file to annotate. " + args.Length + " given.");
                return 1;
            }

            String path = args[0];
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

            Uri serverUri = Workstation.Current.GetLocalWorkspaceInfo(path).ServerUri;
            TfsClientCredentials credentials = new TfsClientCredentials(true);
            using (TfsTeamProjectCollection collection = new TfsTeamProjectCollection(serverUri, credentials))
            {
                VersionControlServer server = collection.GetService<VersionControlServer>();

                String currentPath = @"c:\Users\Vagrant\Desktop\tfs-out\A.txt";
                String previousPath = @"c:\Users\Vagrant\Desktop\tfs-out\B.txt";

                Item item = server.GetItem(path); // will throw if not found

                var options = new DiffOptions();
                options.Flags = DiffOptionFlags.EnablePreambleHandling;

                Changeset currentChangeset = null;
                Item current = null;
                int[] revisions = null;
                int[] mappings = null;

                var itemHistory = server.QueryHistory(path, VersionSpec.Latest, 0, RecursionType.None, null, null, null, int.MaxValue, /* populate Changeset.Changes? */ true, false, true, false); // FIXME stop at local revision
                Mapping diff = null;
                foreach (Changeset previousChangeset in itemHistory)
                {
                    Console.WriteLine("Analyzing changeset " + previousChangeset.ChangesetId);
                    if (current == null)
                    {
                        currentChangeset = previousChangeset;

                        current = server.GetItem(item.ItemId, previousChangeset.ChangesetId);
                        current.DownloadFile(currentPath);

                        int lines = File.ReadLines(currentPath).Count();
                        revisions = new int[lines];
                        mappings = new int[lines];
                        for (int line = 0; line < lines; line++)
                        {
                            revisions[line] = 0;
                            mappings[line] = line;
                        }

                        continue;
                    }

                    if (previousChangeset.Changes.Length != 1)
                    {
                        throw new InvalidOperationException("Expected exactly 1 change, but got " + previousChangeset.Changes.Length + " for ChangesetId " + previousChangeset.ChangesetId);
                    }

                    if ((previousChangeset.Changes[0].ChangeType & ChangeType.Edit) != 0)
                    {
                        // File was edited
                        Item previous = server.GetItem(item.ItemId, previousChangeset.ChangesetId);
                        previous.DownloadFile(previousPath);

                        diff = new Mapping(Difference.DiffFiles(currentPath, current.Encoding, previousPath, previous.Encoding, options));

                        bool complete = true;
                        for (int i = 0; i < revisions.Length; i++)
                        {
                            if (revisions[i] == 0)
                            {
                                int line = mappings[i];
                                if (!diff.ContainsKey(line))
                                {
                                    Console.WriteLine("  - line " + (i + 1) + " was last touched in revision " + currentChangeset.ChangesetId);
                                    revisions[i] = currentChangeset.ChangesetId;
                                }
                                else
                                {
                                    mappings[i] = diff.NewLine(line);
                                    complete = false;
                                }
                            }
                        }

                        // Swap current and previous paths
                        String tmp = previousPath;
                        previousPath = currentPath;
                        currentPath = tmp;

                        currentChangeset = previousChangeset;
                        current = previous;

                        if (complete)
                        {
                            break;
                        }
                    }
                }

                Console.WriteLine("Final conclusions...");

                if (diff != null)
                {
                    for (int i = 0; i < revisions.Length; i++)
                    {
                        if (revisions[i] == 0)
                        {
                            int line = mappings[i];
                            if (diff.ContainsValue(line))
                            {
                                Console.WriteLine("  - line " + (i + 1) + " was last touched in revision " + currentChangeset.ChangesetId);
                                revisions[i] = currentChangeset.ChangesetId;
                            }
                            else
                            {
                                Console.WriteLine("  - line " + (i + 1) + " is local");
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Press a key to continue...");
            Console.ReadLine();

            return 0;
        }

        private class Mapping
        {
            private Dictionary<int, int> m_mapping = new Dictionary<int, int>();

            public Mapping(DiffSegment diffSegment)
            {
                while (diffSegment != null)
                {
                    int originalLine = diffSegment.OriginalStart;
                    int modifiedLine = diffSegment.ModifiedStart;
                    for (int i = 0; i < diffSegment.OriginalLength; i++)
                    {
                        m_mapping.Add(originalLine, modifiedLine);
                        originalLine++;
                        modifiedLine++;
                    }

                    diffSegment = diffSegment.Next;
                }
            }

            public bool ContainsKey(int line)
            {
                return m_mapping.ContainsKey(line);
            }

            public bool ContainsValue(int line)
            {
                return m_mapping.ContainsValue(line);
            }

            public int NewLine(int line)
            {
                return m_mapping[line];
            }
        }
    }
}
