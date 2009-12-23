// transmission-remote-dotnet
// http://code.google.com/p/transmission-remote-dotnet/
// Copyright (C) 2009 Alan F
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Text;
using Jayrock.Json;
using System.Windows.Forms;
using TransmissionRemoteDotnet.Commands;
using System.Threading;
using System.Collections;

namespace TransmissionRemoteDotnet.Commmands
{
    public class UpdateFilesCommand : ICommand
    {
        private bool first;
        private FileListViewItem[] newItems;

        public UpdateFilesCommand(JsonObject response)
        {
            Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(Program.Settings.Locale);
            Program.DaemonDescriptor.ResetFailCount();
            MainWindow form = Program.Form;
            JsonObject arguments = (JsonObject)response[ProtocolConstants.KEY_ARGUMENTS];
            JsonArray torrents = (JsonArray)arguments[ProtocolConstants.KEY_TORRENTS];
            if (torrents.Count != 1)
            {
                return;
            }
            JsonObject torrent = (JsonObject)torrents[0];
            int id = Toolbox.ToInt(torrent[ProtocolConstants.FIELD_ID]);
            Torrent t = null;
            form.Invoke(new MethodInvoker(delegate()
            {
                ListView torrentListView = form.torrentListView;
                lock (torrentListView)
                {
                    if (torrentListView.SelectedItems.Count == 1)
                    {
                        t = (Torrent)torrentListView.SelectedItems[0].Tag;
                    }
                }
            }));
            if (t == null || t.Id != id)
            {
                return;
            }
            JsonArray files = (JsonArray)torrent[ProtocolConstants.FIELD_FILES];
            if (files == null)
            {
                return;
            }
            JsonArray priorities = (JsonArray)torrent[ProtocolConstants.FIELD_PRIORITIES];
            JsonArray wanted = (JsonArray)torrent[ProtocolConstants.FIELD_WANTED];
            first = form.filesListView.Items.Count == 0;
            bool havepriority = (priorities != null && wanted != null);
            //uiUpdateBatch = new List<ICommand>();
#if !MONO
            ImageList imgList = Program.Form.fileIconImageList;
            /*int mainWindowHandle = 0;
            Program.Form.Invoke(new MethodInvoker(delegate()
            {
                mainWindowHandle = Program.Form.Handle.ToInt32();
            }));*/
#endif
            if (first)
                newItems = new FileListViewItem[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                JsonObject file = (JsonObject)files[i];
                long bytesCompleted = Toolbox.ToLong(file[ProtocolConstants.FIELD_BYTESCOMPLETED]);
                long length = Toolbox.ToLong(file[ProtocolConstants.FIELD_LENGTH]);
                if (first)
                {
                    FileListViewItem fileItem = new FileListViewItem(file, imgList, i, wanted, priorities);
                    newItems[i] = fileItem;
                    //string name = (string)file[ProtocolConstants.FIELD_NAME];
#if !MONO
                    //UpdateFilesCreateSubCommand subCommand = new UpdateFilesCreateSubCommand(name, length, Toolbox.ToBool(wanted[i]), (JsonNumber)priorities[i], bytesCompleted, imgList, mainWindowHandle);
#else
                    //UpdateFilesCreateSubCommand subCommand = new UpdateFilesCreateSubCommand(name, length, Toolbox.ToBool(wanted[i]), (JsonNumber)priorities[i], bytesCompleted, null, -1);
#endif
                    //uiUpdateBatch.Add((ICommand)subCommand);
                }
                else
                {
                    Program.Form.Invoke(new MethodInvoker(delegate(){
                        FileListViewItem item = (FileListViewItem)form.filesListView.Items[i];
                        item.Update(file, wanted, priorities);
                    }));

                    /*lock (form.FileItems)
                    {
                        if (i < form.FileItems.Count)
                            if (havepriority)
                                uiUpdateBatch.Add(new UpdateFilesUpdateSubCommand(form.FileItems[i], Toolbox.ToBool(wanted[i]), (JsonNumber)priorities[i], bytesCompleted));
                            else
                                uiUpdateBatch.Add(new UpdateFilesUpdateSubCommand(form.FileItems[i], bytesCompleted));
                    }*/
                }
            }
        }

        public void Execute()
        {
            MainWindow form = Program.Form;
            lock (form.filesListView)
            {
                form.filesListView.SuspendLayout();
                IComparer tmp = form.filesListView.ListViewItemSorter;
                form.filesListView.ListViewItemSorter = null;
                if (first)
                {
                    form.filesListView.Enabled = true;
                    foreach (FileListViewItem item in newItems)
                    {
                        form.filesListView.Items.Add(item);
                    }
                }
                form.filesListView.ListViewItemSorter = tmp;
                form.filesListView.Sort();
                Toolbox.StripeListView(form.filesListView);
                form.filesListView.ResumeLayout();
            }
            form.filesTimer.Enabled = true;
        }
    }
}
