﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jayrock.Json;
using System.Windows.Forms;

namespace TransmissionClientNew.Commmands
{
    class TorrentGetCommand : TransmissionCommand
    {
        private JsonObject response;
        private Boolean beginLoop;

        public TorrentGetCommand(JsonObject response, Boolean beginLoop)
        {
            this.response = response;
            this.beginLoop = beginLoop;
            Program.failCount = 0;
        }

        public void Execute()
        {
            if (!Program.Connected)
            {
                return;
            }
            long totalUpload = 0;
            long totalDownload = 0;
            int totalTorrents = 0;
            int totalSeeding = 0;
            int totalDownloading = 0;
            long totalSize = 0;
            long totalDownloadedSize = 0;
            JsonObject arguments = (JsonObject)response["arguments"];
            JsonArray torrents = (JsonArray)arguments["torrents"];
            Program.updateSerial++;
            Program.form.SuspendListView();
            foreach (JsonObject torrent in torrents)
            {
                int id = ((JsonNumber)torrent["id"]).ToInt32();
                totalUpload += ((JsonNumber)torrent["rateUpload"]).ToInt64();
                totalDownload += ((JsonNumber)torrent["rateDownload"]).ToInt64();
                totalSize += ((JsonNumber)torrent["totalSize"]).ToInt64();
                totalDownloadedSize += ((JsonNumber)torrent["haveValid"]).ToInt64();
                totalTorrents++;
                short status = ((JsonNumber)torrent["status"]).ToInt16();
                if (status == (short)TorrentStatus.Downloading)
                {
                    totalDownloading++;
                }
                else if (status == (short)TorrentStatus.Seeding)
                {
                    totalSeeding++;
                }
                if (Program.torrentIndex.ContainsKey(id))
                {
                    Program.torrentIndex[id].Update(torrent);
                }
                else
                {
                    lock (Program.updateLock)
                    {
                        Program.torrentIndex[id] = new Torrent(torrent);
                    }
                }
            }
            Program.form.ResumeListView();
            Program.form.UpdateStatus("Connected | "+ Toolbox.GetFileSize(totalDownload) + "/s down, " + Toolbox.GetFileSize(totalUpload) + "/s up | "+totalTorrents+" torrents: "+totalDownloading+" downloading, "+totalSeeding+" seeding | "+Toolbox.GetFileSize(totalDownloadedSize)+" / "+Toolbox.GetFileSize(totalSize));
            Queue<int> removeQueue = new Queue<int>();
            foreach (KeyValuePair<int, Torrent> pair in Program.torrentIndex)
            {
                Torrent t = pair.Value;
                if (t.updateSerial != Program.updateSerial)
                {
                    t.Remove();
                    removeQueue.Enqueue(pair.Key);
                }
            }
            lock (Program.updateLock)
            {
                foreach (int id in removeQueue)
                {
                    Program.torrentIndex.Remove(id);
                }
            }
            if (beginLoop)
            {
                BeginLoop();
            }
        }

        private delegate void BeginLoopDelegate();
        private void BeginLoop()
        {
            Form1 form = Program.form;
            if (form.InvokeRequired)
            {
                form.Invoke(new BeginLoopDelegate(this.BeginLoop));
            }
            else
            {
                form.ConnectButton.Enabled = form.RefreshTimer.Enabled = true;
            }
        }
    }
}
