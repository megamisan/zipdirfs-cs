//
//  ZipFileGenerator.cs
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
using ICSharpCode.SharpZipLib.Zip;

namespace zipdirfs
{
	static class ZipFileGenerator
	{
		public static Node Generate(System.IO.FileInfo fileInfo)
		{
			ZipFile file = new ZipFile(fileInfo.FullName);
			Node node = new Node();
			IDictionary<string, IList<Entry>> waiting = new Dictionary<string, IList<Entry>>();
			foreach (ZipEntry zipEntry in file)
			{
				Entry entry = ParseEntry(zipEntry, node);
				if (entry != null)
				{
					string name = zipEntry.Name.TrimEnd('/');
					string path = name.Substring(0, name.LastIndexOf('/'));
					if (!waiting.ContainsKey(path))
						waiting[path] = new List<Entry>();
					waiting[path].Add(entry);
				}
				if (zipEntry.IsDirectory && waiting.ContainsKey(zipEntry.Name.TrimEnd('/')))
				{
					Node dir = GetDirectory(zipEntry.Name.TrimEnd('/'), node);
					if (dir != null)
					{
						foreach (var dirEntry in waiting[zipEntry.Name.TrimEnd('/')])
						{
							dir.Nodes.Add(dirEntry);
						}
						waiting.Remove(zipEntry.Name.TrimEnd('/'));
					}
				}
			}
			FinalizeEntries(node, waiting, fileInfo);
			return node;
		}

		private static void FinalizeEntries(Node node, IDictionary<string, IList<Entry>> waiting, System.IO.FileInfo fileInfo)
		{
			int lastCount = 0;
			int count = 0;
			do
			{
				lastCount = count;
				count = 0;
				foreach (var waitingInfo in waiting.ToArray())
				{
					Node dir = GetDirectory(waitingInfo.Key, node);
					if ((dir == null))
					{
						dir = GetDirectory(waitingInfo.Key.Substring(0, Math.Max(0, waitingInfo.Key.LastIndexOf('/'))), node);
						if (dir != null)
						{
							Entry dirEntry = Entry.CreateZipDirectoryEntry(waitingInfo.Key.Substring(waitingInfo.Key.LastIndexOf('/') + 1), fileInfo.LastWriteTime);
							dirEntry.Node = new Node();
							dir.Nodes.Add(dirEntry);
							dir = dirEntry.Node;
						}
					}
					if (dir != null)
					{
						foreach (var dirEntry in waitingInfo.Value)
						{
							dir.Nodes.Add(dirEntry);
						}
						waiting.Remove(waitingInfo.Key);
					}

					
					else
					{
						count += waitingInfo.Value.Count;
					}
				}
			}
			while ((count > 0) && (lastCount != count));
		}

		private static void UpdateSizes(Node node)
		{
			foreach (var entry in node.Nodes)
			{
				if (entry.Kind == EntryKind.ZipDirectoryEntry)
				{
					entry.UpdateSize();
					UpdateSizes(entry.Node);
				}
			}
		}

		private static Entry ParseEntry(ICSharpCode.SharpZipLib.Zip.ZipEntry entry, Node root)
		{
			if ((!entry.IsFile) && (!entry.IsDirectory))
				return null;
			string name = entry.Name.TrimEnd('/');
			Node dir = GetDirectory(name.Substring(0, Math.Max(0, name.LastIndexOf('/'))), root);
			Entry newEntry = new Entry(entry);
			if (newEntry.Kind == EntryKind.ZipDirectoryEntry)
				newEntry.Node = new Node();
			if (dir == null)
				return newEntry;
			dir.Nodes.Add(newEntry);
			return null;
		}

		private static Node GetDirectory(string path, Node root)
		{
			if (path == string.Empty)
				return root;
			string[] pathComponents = path.Split('/');
			for (int i = 0; i < pathComponents.Length; i++)
			{
				bool found = false;
				foreach (var entry in root.Nodes)
				{
					if (entry.Name == pathComponents[0])
					{
						found = true;
						if (entry.Kind != EntryKind.ZipDirectoryEntry)
							return null;
						root = entry.Node;
					}
				}
				if (!found)
					return null;
			}
			return root;
		}
	}
}
