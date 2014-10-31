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
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;

    public class HistoryProvider : IDisposable
    {
        private const int PREFETCH_SIZE = 10;

        private readonly VersionControlServer server;
        private readonly List<Changeset> changesets = new List<Changeset>();
        private readonly List<string> filenames = new List<string>();
        private readonly List<ManualResetEvent> manualResetEvents = new List<ManualResetEvent>();

        private int current = -1;

        public HistoryProvider(VersionControlServer server, IEnumerable<Changeset> changesets)
        {
            this.server = server;
            
            foreach (Changeset changeset in changesets)
            {
                if (changeset.Changes.Length != 1)
                {
                    throw new InvalidOperationException("Expected exactly 1 change, but got " + changeset.Changes.Length + " for ChangesetId " + changeset.ChangesetId);
                }

                if ((changeset.Changes[0].ChangeType & ChangeType.Edit) != 0)
                {
                    this.changesets.Add(changeset);
                    this.filenames.Add(null);
                    this.manualResetEvents.Add(null);
                }
            }

            for (int i = 0; i < PREFETCH_SIZE && i < this.changesets.Count; i++)
            {
                this.Prefetch(i);
            }
        }

        public bool Next()
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

            if (this.current + PREFETCH_SIZE < this.changesets.Count)
            {
                this.Prefetch(this.current + PREFETCH_SIZE);
            }

            this.manualResetEvents[this.current].WaitOne();

            return true;
        }

        public Changeset Changeset()
        {
            this.ThrowIfNoElement();
            return this.changesets[this.current];
        }

        public string FileName()
        {
            this.ThrowIfNoElement();
            return this.filenames[this.current];
        }

        public void Dispose()
        {
            for (int i = 0; i < this.changesets.Count; i++)
            {
                this.Dispose(i);
                GC.SuppressFinalize(this);
            }
        }

        private void Dispose(int i)
        {
            this.changesets[i] = null;
            if (this.filenames[i] != null)
            {
                File.Delete(this.filenames[i]);
                this.filenames[i] = null;
            }
            
            this.manualResetEvents[i] = null;
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
            Item item = this.changesets[i].Changes[0].Item;
            this.filenames[i] = Path.GetTempFileName();
            this.manualResetEvents[i] = new ManualResetEvent(false);
            Prefetcher prefetcher = new Prefetcher(item, this.filenames[i], this.manualResetEvents[i]);
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
                this.item.DownloadFile(this.filename);
                this.manualResetEvent.Set();
            }
        }
    }
}
