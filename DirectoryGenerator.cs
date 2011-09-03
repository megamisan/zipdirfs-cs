//
//  DirectoryGenerator.cs
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
	static class DirectoryGenerator
	{
		public static Node Generate(System.IO.DirectoryInfo directory, bool recursive)
		{
			Node node = new Node();
			foreach (var subdirectory in directory.EnumerateDirectories())
			{
				Entry dirEntry = new Entry(subdirectory);
				node.Nodes.Add(dirEntry);
				if (recursive)
				{
					dirEntry.Node = Generate(subdirectory, recursive);
					dirEntry.UpdateSize();
				}
			}
			foreach (var file in directory.EnumerateFiles())
			{
				Entry fileEntry = new Entry(file, IsZipFile(file));
				node.Nodes.Add(fileEntry);
				if ((fileEntry.Kind == EntryKind.ZipFile) && recursive)
				{
					fileEntry.Node = ZipFileGenerator.Generate(file);
					fileEntry.UpdateSize();
				}
			}
			return node;
		}

		public static void Update(System.IO.DirectoryInfo directory, Node node)
		{
			IDictionary<string, Entry> entries = new Dictionary<string, Entry>(node.Nodes.Count);
			foreach (var entry in node.Nodes)
			{
				entries.Add(entry.Name, entry);
			}
			foreach (var subdirectory in directory.EnumerateDirectories())
			{
				if (entries.ContainsKey(subdirectory.Name))
				{
					entries.Remove(subdirectory.Name);
				}

				else
				{
					Entry dirEntry = new Entry(subdirectory);
					node.Nodes.Add(dirEntry);
				}
			}
			foreach (var file in directory.EnumerateFiles())
			{
				if (entries.ContainsKey(file.Name))
				{
					Entry fileEntry = entries[file.Name];
					entries.Remove(file.Name);
					if (fileEntry.LastWriteTime < file.LastWriteTime)
					{
						node.Nodes.Remove(fileEntry);
						node.Nodes.Add(new Entry(file, IsZipFile(file)));
					}
				}

				else
				{
					node.Nodes.Add(new Entry(file, IsZipFile(file)));
				}
			}
			foreach (var entryPair in entries)
			{
				node.Nodes.Remove(entryPair.Value);
			}
		}

		public static bool IsZipFile(System.IO.FileInfo file)
		{
			try
			{
				new ICSharpCode.SharpZipLib.Zip.ZipFile(file.FullName).Close();
			}
			catch (ICSharpCode.SharpZipLib.Zip.ZipException)
			{
				return false;
			}
			return true;
		}

		public static IEnumerable<System.IO.DirectoryInfo> EnumerateDirectories(this System.IO.DirectoryInfo di)
		{
			return di.GetDirectories();
		}

		public static IEnumerable<System.IO.FileInfo> EnumerateFiles(this System.IO.DirectoryInfo di)
		{
			return di.GetFiles();
		}
	}
}
