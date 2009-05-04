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

﻿using System;
using System.Collections.Generic;
using System.Text;
using Jayrock.Json;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Collections;

namespace TransmissionRemoteDotnet.Commands
{     
    class UpdateFilesUpdateSubCommand : ICommand
    {
        private ListViewItem item;
        private long bytesCompleted;
        private string bytesCompletedStr;
        private decimal progress;

        public UpdateFilesUpdateSubCommand(ListViewItem item, long bytesCompleted)
        {
            this.item = item;
            this.bytesCompleted = bytesCompleted;
            this.bytesCompletedStr = Toolbox.GetFileSize(bytesCompleted);
            this.progress = Toolbox.CalcPercentage(bytesCompleted, (long)item.SubItems[1].Tag);
        }

        public void Execute()
        {
            item.SubItems[2].Tag = bytesCompleted;
            item.SubItems[2].Text = bytesCompletedStr;
            item.SubItems[3].Tag = progress;
            item.SubItems[3].Text = progress + "%";
        }
    }

    class UpdateFilesCreateSubCommand : ICommand
    {
        private ListViewItem item;
        private string extension;
        private static RegisteredFileType regTypes;

        public UpdateFilesCreateSubCommand(string name, long length, bool wanted,
            JsonNumber priority, long bytesCompleted, ImageList img, int mainHandle)
        {
            int fwdSlashPos = name.IndexOf('/');
            if (fwdSlashPos > 0)
            {
                name = name.Remove(0, fwdSlashPos + 1);
            }
            else
            {
                int bckSlashPos = name.IndexOf('\\');
                if (bckSlashPos > 0)
                {
                    name = name.Remove(0, bckSlashPos + 1);
                }
            }
            if (regTypes == null)
                regTypes = new RegisteredFileType();
            this.item = new ListViewItem(name);
            string[] split = name.Split('.');
            if (split.Length > 1)
            {
                string extension = split[split.Length - 1].ToLower();
                if (img.Images.ContainsKey(extension))
                {
                    this.extension = extension;
                }
                else if (regTypes.Icons.ContainsKey("." + extension))
                {
                    string fileAndParam = (regTypes.Icons["."+extension]).ToString();
                    if (!String.IsNullOrEmpty(fileAndParam))
                    {
                        //Use to store the file contains icon.
                        string fileName = "";

                        //The index of the icon in the file.
                        int iconIndex = 0;
                        string iconIndexString = "";

                        int index = fileAndParam.IndexOf(",");
                        //if fileAndParam is some thing likes that: "C:\\Program Files\\NetMeeting\\conf.exe,1".
                        if (index > 0)
                        {
                            fileName = fileAndParam.Substring(0, index);
                            iconIndexString = fileAndParam.Substring(index + 1);
                        }
                        else
                            fileName = fileAndParam;

                        if (!string.IsNullOrEmpty(iconIndexString))
                        {
                            //Get the index of icon.
                            iconIndex = int.Parse(iconIndexString);
                            if (iconIndex < 0)
                                iconIndex = 0;  //To avoid the invalid index.
                        }

                        //Gets the handle of the icon.
                        IntPtr lIcon = RegisteredFileType.ExtractIcon(mainHandle, fileName, iconIndex);

                        //The handle cannot be zero.
                        if (lIcon != IntPtr.Zero)
                        {
                            //Gets the real icon.
                            Icon icon = Icon.FromHandle(lIcon);

                            //Draw the icon to the picture box.
                            img.Images.Add(extension, icon);
                            this.extension = extension;
                        }
                    }
                }
            }
            item.Name = item.ToolTipText = name;
            item.SubItems.Add(Toolbox.GetFileSize(length));
            item.SubItems[1].Tag = length;
            item.SubItems.Add(Toolbox.GetFileSize(bytesCompleted));
            item.SubItems[2].Tag = bytesCompleted;
            decimal progress = Toolbox.CalcPercentage(bytesCompleted, length);
            item.SubItems.Add(progress + "%");
            item.SubItems[3].Tag = progress;
            item.SubItems.Add(wanted ? OtherStrings.No : OtherStrings.Yes);
            item.SubItems.Add(FormatPriority(priority));
            lock (Program.Form.FileItems)
            {
                Program.Form.FileItems.Add(item);
            }
        }

        public void Execute()
        {
            if (extension != null)
                item.ImageKey = extension;
            Program.Form.filesListView.Items.Add(item);
        }

        private string FormatPriority(JsonNumber n)
        {
            short s = n.ToInt16();
            if (s < 0)
            {
                return OtherStrings.Low;
            }
            else if (s > 0)
            {
                return OtherStrings.High;
            }
            else
            {
                return OtherStrings.Normal;
            }
        }
    }
}
