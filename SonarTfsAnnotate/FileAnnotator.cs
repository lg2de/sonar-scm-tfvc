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
        private readonly VersionControlServer server;

        public FileAnnotator(VersionControlServer server)
        {
            this.server = server;
        }

        public IAnnotatedFile Annotate(string path, VersionSpec version)
        {
            var options = new DiffOptions();
            options.Flags = DiffOptionFlags.EnablePreambleHandling;

            AnnotatedFile annotatedFile = null;
            
            Changeset currentChangeset = null;
            String currentPath = null;
            int currentEncoding = 0;

            PendingChange[] pendingChanges = server.GetWorkspace(path).GetPendingChanges(path);
            foreach (PendingChange pendingChange in pendingChanges) // TODO Expect at most 1 change?
            {
                if ((pendingChange.ChangeType & ChangeType.Edit) != 0)
                {
                    annotatedFile = new AnnotatedFile(path, pendingChange.Encoding);
                    currentPath = path;
                    currentEncoding = pendingChange.Encoding;
                }
            }

            var history = server.QueryHistory(path, version, 0, RecursionType.None, null, null, version, int.MaxValue, true, false, true, false);
            using (var historyProvider = new HistoryProvider(server, (IEnumerable<Changeset>)history))
            {
                bool done = false;
                Dictionary<int, int> diff = null;

                while (!done && historyProvider.Next())
                {
                    Changeset previousChangeset = historyProvider.Changeset();

                    string previousPath = historyProvider.Filename();
                    int previousEncoding = previousChangeset.Changes[0].Item.Encoding;

                    if (annotatedFile == null)
                    {
                        annotatedFile = new AnnotatedFile(previousPath, previousEncoding);
                    }
                    else
                    {
                        diff = DiffMapping(Difference.DiffFiles(currentPath, currentEncoding, previousPath, previousEncoding, options));
                        done = annotatedFile.ApplyDiff(currentChangeset, diff);
                    }

                    currentChangeset = previousChangeset;
                    currentEncoding = previousEncoding;
                    currentPath = previousPath;
                }

                if (diff != null)
                {
                    annotatedFile.ApplyLastDiff(currentChangeset, diff);
                }
            }

            return annotatedFile;
        }

        private static Dictionary<int, int> DiffMapping(DiffSegment diffSegment)
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


        private class AnnotatedFile : IAnnotatedFile
        {
            private const int UNKNOWN = -1;
            private const int LOCAL = 0;

            private readonly string[] data;
            private readonly int lines;
            private readonly int[] revisions;
            private readonly int[] mappings;
            private readonly IDictionary<int, Changeset> changesets = new Dictionary<int, Changeset>();

            public AnnotatedFile(string path, int encoding)
            {
                data = File.ReadAllLines(path, Encoding.GetEncoding(encoding));
                lines = data.Length;
                revisions = new int[lines];
                mappings = new int[lines];
                for (int i = 0; i < lines; i++)
                {
                    revisions[i] = UNKNOWN;
                    mappings[i] = i;
                }
            }

            public bool ApplyDiff(Changeset changeset, Dictionary<int, int> diff)
            {
                bool done = true;

                for (int i = 0; i < revisions.Length; i++)
                {
                    if (revisions[i] == UNKNOWN)
                    {
                        int line = mappings[i];
                        if (!diff.ContainsKey(line))
                        {
                            Associate(i, changeset);
                        }
                        else
                        {
                            mappings[i] = diff[line];
                            done = false;
                        }
                    }
                }

                return done;
            }

            public void ApplyLastDiff(Changeset changeset, Dictionary<int, int> diff)
            {
                for (int i = 0; i < revisions.Length; i++)
                {
                    if (revisions[i] == UNKNOWN)
                    {
                        int line = mappings[i];
                        if (diff.ContainsValue(line))
                        {
                            Associate(i, changeset);
                        }
                    }
                }
            }

            private void Associate(int line, Changeset changeset)
            {
                int changesetId = changeset != null ? changeset.ChangesetId : LOCAL;
                revisions[line] = changesetId;
                if (!changesets.ContainsKey(changesetId))
                {
                    changesets.Add(changesetId, changeset);
                }
            }

            public int Lines()
            {
                return lines;
            }

            public string Data(int line)
            {
                return data[line];
            }

            public AnnotationState State(int line)
            {
                switch (revisions[line])
                {
                    case UNKNOWN:
                        return AnnotationState.UNKNOWN;
                    case LOCAL:
                        return AnnotationState.LOCAL;
                    default:
                        return AnnotationState.COMMITTED;
                }
            }

            public Changeset Changeset(int line)
            {
                return changesets[revisions[line]];
            }
        }
    }

    public interface IAnnotatedFile
    {
        int Lines();

        string Data(int line);

        AnnotationState State(int line);

        Changeset Changeset(int line);
    }

    public enum AnnotationState
    {
        UNKNOWN,
        LOCAL,
        COMMITTED
    }
}
