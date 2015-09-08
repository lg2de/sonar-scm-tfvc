/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;

namespace SonarSource.TfsAnnotate
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
            var options = new DiffOptions
            {
                Flags = DiffOptionFlags.EnablePreambleHandling | DiffOptionFlags.IgnoreLeadingAndTrailingWhiteSpace | DiffOptionFlags.IgnoreEndOfLineDifference
            };

            PendingChange[] pendingChanges = server.GetWorkspace(path).GetPendingChanges(path);
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

            using (var historyProvider = new HistoryProvider(server, path, version))
            {
                bool done = false;

                while (!done && historyProvider.Next())
                {
                    Changeset previousChangeset = historyProvider.Changeset();

                    string previousPath = historyProvider.Filename();
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
                    isBinary = true;
                }
                else
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
            }

            public void Apply(Changeset changeset)
            {
                for (int i = 0; i < revisions.Length; i++)
                {
                    if (revisions[i] == UNKNOWN)
                    {
                        Associate(i, changeset);
                    }
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

            private void Associate(int line, Changeset changeset)
            {
                int changesetId = changeset != null ? changeset.ChangesetId : LOCAL;
                revisions[line] = changesetId;
                if (!changesets.ContainsKey(changesetId))
                {
                    changesets.Add(changesetId, changeset);
                }
            }

            public bool IsBinary()
            {
                return isBinary;
            }

            public int Lines()
            {
                ThrowIfBinaryFile();
                return lines;
            }

            public string Data(int line)
            {
                ThrowIfBinaryFile();
                return data[line];
            }

            public AnnotationState State(int line)
            {
                ThrowIfBinaryFile();
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
                ThrowIfBinaryFile();
                return changesets[revisions[line]];
            }

            private void ThrowIfBinaryFile()
            {
                if (IsBinary())
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
