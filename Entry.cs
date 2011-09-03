//
//  Entry.cs
//
//  Author:
//       Pierrick Caillon <pierrick@megami.fr>
//
//  Copyright (c) 2011 Pierrick Caillon
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace zipdirfs
{
	class Entry
	{
		public Entry(System.IO.DirectoryInfo directory)
		{
			Name = directory.Name;
			Kind = EntryKind.Directory;
			LastWriteTime = directory.LastWriteTime;
			Node = null;
			Size = 0;
		}

		public Entry(System.IO.FileInfo file, bool zipFile)
		{
			Name = file.Name;
			Kind = zipFile ? EntryKind.ZipFile : EntryKind.File;
			LastWriteTime = file.LastWriteTime;
			Node = null;
			Size = zipFile ? 0 : file.FullName.Length;
		}

		public Entry(ICSharpCode.SharpZipLib.Zip.ZipEntry zipEntry)
		{
			Name = System.IO.Path.GetFileName(zipEntry.Name.TrimEnd('/'));
			Kind = zipEntry.IsDirectory ? EntryKind.ZipDirectoryEntry : EntryKind.ZipFileEntry;
			LastWriteTime = zipEntry.DateTime;
			Node = null;
			Size = zipEntry.IsFile ? zipEntry.Size : 0;
		}

		private Entry()
		{
		}

		public string Name
		{
			get;
			private set;
		}
		public EntryKind Kind
		{
			get;
			private set;
		}
		public DateTime LastWriteTime
		{
			get;
			private set;
		}
		public Node Node
		{
			get;
			set;
		}
		public long Size
		{
			get;
			private set;
		}

		public void UpdateSize()
		{
			if ((Kind == EntryKind.Directory) || (Kind == EntryKind.ZipFile) || (Kind == EntryKind.ZipDirectoryEntry))
			{
				Size = (Node != null) ? (4096 * (long)Math.Ceiling(Node.Nodes.Count * 0.1)) : 0;
			}
		}

		public static Entry CreateZipDirectoryEntry(string name, DateTime date)
		{
			Entry e = new Entry();
			e.Name = name;
			e.LastWriteTime = date;
			e.Kind = EntryKind.ZipDirectoryEntry;
			e.Node = null;
			e.Size = 0;
			return e;
		}

		public override string ToString()
		{
			return string.Format("{{Entry: Name={0}, Kind={1}, Size={2}}}", Name, Kind, Size);
		}
	}
}
