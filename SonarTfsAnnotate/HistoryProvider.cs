/*
 * SonarQube :: SCM :: TFVC :: Plugin
 * Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
 *
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.TeamFoundation.VersionControl.Client;
using System.Linq;
namespace SonarSource.TfsAnnotate
{
    class HistoryProvider : IDisposable
    {
        private const int PREFETCH_SIZE = 10;

        private readonly List<Changeset> changesets = new List<Changeset>();
        private readonly List<string> filenames = new List<string>();
        private readonly List<ManualResetEvent> manualResetEvents = new List<ManualResetEvent>();

        private int current = -1;

        public HistoryProvider(VersionControlServer server, string path, VersionSpec version)
        {
            FetchChangesets(server, path, version);

            for (int i = 0; i < PREFETCH_SIZE && i < this.changesets.Count; i++)
            {
                Prefetch(i);
            }
        }

        private void FetchChangesets(VersionControlServer server, string path, VersionSpec version)
        {
            var changesets = server.QueryHistory(path, version, 0, RecursionType.None, null, null, version, int.MaxValue, true, false, true, false);
            foreach (Changeset changeset in changesets)
            {
                if (changeset.Changes.Length != 1)
                {
                    throw new InvalidOperationException("Expected exactly 1 change, but got " + changeset.Changes.Length + " for ChangesetId " + changeset.ChangesetId);
                }
				//Find changetype. If changetype is mergeset then get changeset for mergeset and add to list. If changetype is Add/Edit then add directly to list. 
                if ((changeset.Changes[0].ChangeType.HasFlag(ChangeType.Merge) || changeset.Changes[0].ChangeType.HasFlag(ChangeType.Edit) || changeset.Changes[0].ChangeType.HasFlag(ChangeType.Add)) && !changeset.Changes[0].ChangeType.HasFlag(ChangeType.Branch))
                {
                    var t = GetMergedChangesets(changeset, server);
                    if (t != null && t.Count > 0)
                    {
                        foreach (var a in t)
                        {
                            this.changesets.Add(a);
                            filenames.Add(null);
                            manualResetEvents.Add(null);
                        }
                    }
                    else {
                        this.changesets.Add(changeset);

                        filenames.Add(null);
                        manualResetEvents.Add(null);
                    }
                }
                // If changetype is branch then find changeset for branchset in parent branch and add to list. If changeset in parent branch is mergeset then search for changeset against mergeset and add to list.
                else if (changeset.Changes[0].ChangeType.HasFlag(ChangeType.Branch))
                {
                    var branchchangeset = GetMergedChangesets(changeset, server);
                    var t = GetMergedChangesets(branchchangeset[0], server);
                    if (t != null && t.Count > 0)
                    {
                        foreach (var a in t)
                        {
                            this.changesets.Add(a);
                            filenames.Add(null);
                            manualResetEvents.Add(null);
                        }
                    }
                    else {
                        this.changesets.Add(changeset);

                        filenames.Add(null);
                        manualResetEvents.Add(null);
                    }
                }
            }
        }
        /// <summary>
        /// Function to find merge changesets.
        /// </summary>
        /// <param name="changeset"></param>
        /// <param name="versionControlServer"></param>
        /// <returns></returns>
        public static List<Changeset> GetMergedChangesets(Changeset changeset, VersionControlServer versionControlServer)
        {
            // remember the already covered changeset id's
            Dictionary<int, bool> alreadyCoveredChangesets = new Dictionary<int, bool>();

            // initialize list of parent changesets
            List<Changeset> parentChangesets = new List<Changeset>();

            // go through each change inside the changeset
            foreach (Change change in changeset.Changes)
            {
                // query for the items' history
                var queryResults = versionControlServer.QueryMergesExtended(
                                        new ItemSpec(change.Item.ServerItem, RecursionType.Full),
                                        new ChangesetVersionSpec(changeset.ChangesetId),
                                        null,
                                        null).ToList();
                var queryResults1 = queryResults.OrderByDescending(t=>t.SourceChangeset.ChangesetId);
                
                // go through each changeset in the history
                foreach (var result in queryResults1)
                {
                    // only if the target-change is the given changeset, we have a hit
                    if (result.TargetChangeset.ChangesetId == changeset.ChangesetId)
                    {
                        // if that hit has already been processed elsewhere, then just skip it
                        if (!alreadyCoveredChangesets.ContainsKey(result.SourceChangeset.ChangesetId))
                        {
                            // otherwise add it
                            alreadyCoveredChangesets.Add(result.SourceChangeset.ChangesetId, true);
                            parentChangesets.Add(versionControlServer.GetChangeset(result.SourceChangeset.ChangesetId));
                        }
                    }
                }
            }

            return parentChangesets;
        }
        public bool Next()
        {
            while (true)
            {
                if (current - 1 >= 0)
                {
                    Dispose(current - 1);
                }

                current++;
                if (current >= changesets.Count)
                {
                    return false;
                }

                if (current + PREFETCH_SIZE < changesets.Count)
                {
                    Prefetch(current + PREFETCH_SIZE);
                }

                manualResetEvents[current].WaitOne();

                if (!File.Exists(filenames[current]))
                {
                 // The download was not successful. Move on to the next file.
                    continue;
                }

                return true;
            }
        }

        public Changeset Changeset()
        {
            ThrowIfNoElement();
            return changesets[current];
        }

        public string Filename()
        {
            ThrowIfNoElement();
            return filenames[current];
        }

        public void Dispose()
        {
            for (int i = 0; i < changesets.Count; i++)
            {
                Dispose(i);
            }
        }

        private void Dispose(int i)
        {
            changesets[i] = null;
            if (filenames[i] != null)
            {
                File.Delete(filenames[i]);
                filenames[i] = null;
            }
            if (manualResetEvents[i] != null)
            {
                manualResetEvents[i].WaitOne();
                manualResetEvents[i].Dispose();
                manualResetEvents[i] = null;
            }
        }

        private void ThrowIfNoElement()
        {
            if (current >= changesets.Count)
            {
                throw new InvalidOperationException("No more elements");
            }
        }

        private void Prefetch(int i)
        {
            Item item = changesets[i].Changes[0].Item;
            filenames[i] = Path.GetTempFileName();
            manualResetEvents[i] = new ManualResetEvent(false);
            Prefetcher prefetcher = new Prefetcher(item, filenames[i], manualResetEvents[i]);
            ThreadPool.QueueUserWorkItem(prefetcher.Prefetch);
        }

        private sealed class Prefetcher
        {
            private readonly Item item;
            private readonly string filename;
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
                    item.DownloadFile(filename);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                }
                finally
                {
                    manualResetEvent.Set();
                }
            }
        }
    }
}
