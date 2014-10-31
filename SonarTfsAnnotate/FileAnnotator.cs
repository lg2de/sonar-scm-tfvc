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
    using Microsoft.TeamFoundation.VersionControl.Client;
    using Microsoft.TeamFoundation.VersionControl.Common;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public class FileAnnotator
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

            PendingChange[] pendingChanges = this.server.GetWorkspace(path).GetPendingChanges(path);
            if (pendingChanges.Length >= 2)
            {
                throw new InvalidOperationException("Expected at most 1 pending change, but got " + pendingChanges.Length);
            }

            Changeset currentChangeset = null;

            AnnotatedFile annotatedFile;
            string currentPath;
            int currentEncoding;

            if (pendingChanges.Length == 1 && (pendingChanges[0].ChangeType & ChangeType.Edit) != 0)
            {
                annotatedFile = new AnnotatedFile(path, pendingChanges[0].Encoding);
                if (annotatedFile.IsBinary())
                {
                    return annotatedFile;
                }

                currentPath = path;
                currentEncoding = pendingChanges[0].Encoding;
            }
            else
            {
                annotatedFile = null;
                currentPath = null;
                currentEncoding = 0;
            }

            var history = this.server.QueryHistory(path, version, 0, RecursionType.None, null, null, version, int.MaxValue, true, false, true, false);
            using (var historyProvider = new HistoryProvider(this.server, (IEnumerable<Changeset>)history))
            {
                bool done = false;

                while (!done && historyProvider.Next())
                {
                    Changeset previousChangeset = historyProvider.Changeset();

                    string previousPath = historyProvider.FileName();
                    int previousEncoding = previousChangeset.Changes[0].Item.Encoding;

                    if (annotatedFile == null)
                    {
                        annotatedFile = new AnnotatedFile(previousPath, previousEncoding);
                        if (annotatedFile.IsBinary())
                        {
                            return annotatedFile;
                        }
                    }
                    else if (previousEncoding == -1)
                    {
                        annotatedFile.Apply(currentChangeset);
                        done = true;
                    }
                    else
                    {
                        var diff = Diff(Difference.DiffFiles(currentPath, currentEncoding, previousPath, previousEncoding, options));
                        done = annotatedFile.ApplyDiff(currentChangeset, diff);
                    }

                    currentChangeset = previousChangeset;
                    currentEncoding = previousEncoding;
                    currentPath = previousPath;
                }

                if (annotatedFile != null)
                {
                    annotatedFile.Apply(currentChangeset);
                }
            }

            return annotatedFile;
        }

        private static Dictionary<int, int> Diff(DiffSegment diffSegment)
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

        private sealed class AnnotatedFile : IAnnotatedFile
        {
            private const int UNKNOWN = -1;
            private const int LOCAL = 0;

            private readonly bool isBinary;
            private readonly string[] data;
            private readonly int lines;
            private readonly int[] revisions;
            private readonly int[] mappings;
            private readonly IDictionary<int, Changeset> changesets = new Dictionary<int, Changeset>();

            public AnnotatedFile(string path, int encoding)
            {
                if (encoding == -1)
                {
                    this.isBinary = true;
                }
                else
                {
                    this.data = File.ReadAllLines(path, Encoding.GetEncoding(encoding));
                    this.lines = this.data.Length;
                    this.revisions = new int[this.lines];
                    this.mappings = new int[this.lines];
                    for (int i = 0; i < this.lines; i++)
                    {
                        this.revisions[i] = UNKNOWN;
                        this.mappings[i] = i;
                    }
                }
            }

            public void Apply(Changeset changeset)
            {
                for (int i = 0; i < this.revisions.Length; i++)
                {
                    if (this.revisions[i] == UNKNOWN)
                    {
                        Associate(i, changeset);
                    }
                }
            }

            public bool ApplyDiff(Changeset changeset, IReadOnlyDictionary<int, int> diff)
            {
                bool done = true;

                for (int i = 0; i < revisions.Length; i++)
                {
                    if (this.revisions[i] == UNKNOWN)
                    {
                        int line = this.mappings[i];
                        if (!diff.ContainsKey(line))
                        {
                            Associate(i, changeset);
                        }
                        else
                        {
                            this.mappings[i] = diff[line];
                            done = false;
                        }
                    }
                }

                return done;
            }

            private void Associate(int line, Changeset changeset)
            {
                int changesetId = changeset != null ? changeset.ChangesetId : LOCAL;
                this.revisions[line] = changesetId;
                if (!this.changesets.ContainsKey(changesetId))
                {
                    this.changesets.Add(changesetId, changeset);
                }
            }

            public bool IsBinary()
            {
                return this.isBinary;
            }

            public int Lines()
            {
                this.ThrowIfBinaryFile();
                return this.lines;
            }

            public string Data(int line)
            {
                this.ThrowIfBinaryFile();
                return this.data[line];
            }

            public AnnotationState State(int line)
            {
                this.ThrowIfBinaryFile();
                switch (this.revisions[line])
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
                this.ThrowIfBinaryFile();
                return this.changesets[revisions[line]];
            }

            private void ThrowIfBinaryFile()
            {
                if (this.IsBinary())
                {
                    throw new InvalidOperationException("Not supported on binary files!");
                }
            }     
        }
    }

    public interface IAnnotatedFile
    {
        bool IsBinary();

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
