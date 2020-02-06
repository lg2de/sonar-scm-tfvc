/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace SonarSource.TfsAnnotate
{
    internal class HistoryProvider : IDisposable
    {
        private const int PrefetchSize = 10;

        private readonly List<Changeset> changesets = new List<Changeset>();
        private readonly List<string> fileNames = new List<string>();
        private readonly List<ManualResetEvent> manualResetEvents = new List<ManualResetEvent>();

        private int current = -1;

        public HistoryProvider(VersionControlServer server, string path, VersionSpec version)
        {
            this.FetchChangesets(server, path, version);

            for (int i = 0; i < PrefetchSize && i < this.changesets.Count; i++)
            {
                this.Prefetch(i);
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < this.changesets.Count; i++)
            {
                this.Dispose(i);
            }
        }

        private void FetchChangesets(VersionControlServer server, string path, VersionSpec version)
        {
            var history = server.QueryHistory(path, version, 0, RecursionType.None, null, null, version, int.MaxValue,
                true, false, true, false);
            foreach (Changeset changeset in history)
            {
                if (changeset.Changes.Length != 1)
                {
                    throw new InvalidOperationException("Expected exactly 1 change, but got " +
                                                        changeset.Changes.Length + " for ChangesetId " +
                                                        changeset.ChangesetId);
                }

                this.changesets.Add(changeset);
                this.fileNames.Add(null);
                this.manualResetEvents.Add(null);

                var change = changeset.Changes[0];
                if (change.ChangeType.HasFlag(ChangeType.Branch))
                {
                    var branchHistoryTree = server.GetBranchHistory(
                        new[] {new ItemSpec(change.Item.ServerItem, RecursionType.None)},
                        new ChangesetVersionSpec(changeset.ChangesetId));
                    if (branchHistoryTree == null || branchHistoryTree.Length == 0 || branchHistoryTree[0].Length == 0)
                    {
                        continue;
                    }

                    var item = branchHistoryTree[0][0].GetRequestedItem().Relative.BranchFromItem;
                    if (item != null)
                    {
                        this.FetchChangesets(server, item.ServerItem, new ChangesetVersionSpec(item.ChangesetId));
                    }
                }
            }
        }

        public bool Next()
        {
            while (true)
            {
                if (this.current - 1 >= 0)
                {
                    this.Dispose(this.current - 1);
                }

                this.current++;
                if (this.current >= this.changesets.Count)
                {
                    return false;
                }

                if (this.current + PrefetchSize < this.changesets.Count)
                {
                    this.Prefetch(this.current + PrefetchSize);
                }

                this.manualResetEvents[this.current].WaitOne();

                if (!File.Exists(this.fileNames[this.current]))
                {
                    // The download was not successful. Move on to the next file.
                    continue;
                }

                return true;
            }
        }

        public Changeset Changeset()
        {
            this.ThrowIfNoElement();
            return this.changesets[this.current];
        }

        public string Filename()
        {
            this.ThrowIfNoElement();
            return this.fileNames[this.current];
        }

        private void Dispose(int i)
        {
            this.changesets[i] = null;
            if (this.fileNames[i] != null)
            {
                File.Delete(this.fileNames[i]);
                this.fileNames[i] = null;
            }

            if (this.manualResetEvents[i] != null)
            {
                this.manualResetEvents[i].WaitOne();
                this.manualResetEvents[i].Dispose();
                this.manualResetEvents[i] = null;
            }
        }

        private void ThrowIfNoElement()
        {
            if (this.current >= this.changesets.Count)
            {
                throw new InvalidOperationException("No more elements");
            }
        }

        private void Prefetch(int i)
        {
            var item = this.changesets[i].Changes[0].Item;
            this.fileNames[i] = Path.GetTempFileName();
            this.manualResetEvents[i] = new ManualResetEvent(false);
            var prefetcher = new Prefetcher(item, this.fileNames[i], this.manualResetEvents[i]);
            ThreadPool.QueueUserWorkItem(prefetcher.Prefetch);
        }

        private sealed class Prefetcher
        {
            private readonly string filename;
            private readonly Item item;
            private readonly ManualResetEvent manualResetEvent;

            public Prefetcher(Item item, string filename, ManualResetEvent manualResetEvent)
            {
                this.item = item;
                this.filename = filename;
                this.manualResetEvent = manualResetEvent;
            }

            public void Prefetch(object o)
            {
                try
                {
                    this.item.DownloadFile(this.filename);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                }
                finally
                {
                    this.manualResetEvent.Set();
                }
            }
        }
    }
}