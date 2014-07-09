using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace SonarTfsAnnotate
{
    public class HistoryProvider : IDisposable
    {
        private const int PREFETCH_SIZE = 10;

        private readonly VersionControlServer server;
        private readonly int itemId;
        private List<Changeset> changesets = new List<Changeset>();
        private List<string> filenames = new List<string>();
        private List<ManualResetEvent> manualResetEvents = new List<ManualResetEvent>();

        private int current = -1;

        public HistoryProvider(VersionControlServer server, int itemId, IEnumerable<Changeset> changesets)
        {
            this.server = server;
            this.itemId = itemId;
            
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
                Prefetch(i);
            }
        }

        public bool Next()
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

            return true;
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
            manualResetEvents[i] = null;
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

        private class Prefetcher
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
                item.DownloadFile(filename);
                manualResetEvent.Set();
            }
        }
    }
}
