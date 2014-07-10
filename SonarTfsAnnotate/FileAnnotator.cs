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
using System.Text;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;

namespace SonarTfsAnnotate
{
    class FileAnnotator
    {
        private const int UNKNOWN = -1;
        private const int LOCAL = 0;

        private readonly VersionControlServer server;

        public FileAnnotator(VersionControlServer server)
        {
            this.server = server;
        }

        public void Annotate(string path, VersionSpec version)
        {
            Item item = server.GetItem(path); // will throw if not found

            var options = new DiffOptions();
            options.Flags = DiffOptionFlags.EnablePreambleHandling;

            Changeset currentChangeset = null;
            Item current = item;
            String currentPath = path;

            string[] data = File.ReadAllLines(currentPath, Encoding.GetEncoding(current.Encoding));
            int lines = data.Length;
            int[] revisions = new int[lines];
            int[] mappings = new int[lines];
            for (int line = 0; line < lines; line++)
            {
                revisions[line] = UNKNOWN;
                mappings[line] = line;
            }

            var history = server.QueryHistory(path, version, 0, RecursionType.None, null, null, version, int.MaxValue, true, false, true, false);
            using (var historyProvider = new HistoryProvider(server, item.ItemId, (IEnumerable<Changeset>)history))
            {
                Dictionary<int, int> diff = null;
                while (historyProvider.Next())
                {
                    Changeset previousChangeset = historyProvider.Changeset();

                    string previousPath = historyProvider.Filename();
                    Item previous = previousChangeset.Changes[0].Item;

                    diff = Mapping(Difference.DiffFiles(currentPath, current.Encoding, previousPath, previous.Encoding, options));

                    bool complete = true;
                    for (int i = 0; i < revisions.Length; i++)
                    {
                        if (revisions[i] == UNKNOWN)
                        {
                            int line = mappings[i];
                            if (!diff.ContainsKey(line))
                            {
                                int changesetId = currentChangeset != null ? currentChangeset.ChangesetId : LOCAL;
                                revisions[i] = changesetId;
                            }
                            else
                            {
                                mappings[i] = diff[line];
                                complete = false;
                            }
                        }
                    }

                    currentChangeset = previousChangeset;
                    current = previous;
                    currentPath = previousPath;

                    if (complete)
                    {
                        break;
                    }
                }

                if (diff != null)
                {
                    for (int i = 0; i < revisions.Length; i++)
                    {
                        if (revisions[i] == UNKNOWN)
                        {
                            int line = mappings[i];
                            if (diff.ContainsValue(line))
                            {
                                int changesetId = currentChangeset != null ? currentChangeset.ChangesetId : LOCAL;
                                revisions[i] = changesetId;
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < lines; i++)
            {
                Console.Write(revisions[i]);
                Console.Write(' ');
                Console.WriteLine(data[i]);
            }
        }

        private static Dictionary<int, int> Mapping(DiffSegment diffSegment)
        {
            var result = new Dictionary<int, int>();

            while (diffSegment != null)
            {
                int originalLine = diffSegment.OriginalStart;
                int modifiedLine = diffSegment.ModifiedStart;
                for (int i = 0; i < diffSegment.OriginalLength; i++)
                {
                    result.Add(originalLine, modifiedLine);
                    originalLine++;
                    modifiedLine++;
                }

                diffSegment = diffSegment.Next;
            }

            return result;
        }
    }
}
