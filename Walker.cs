//
//  Walker.cs
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
	class Walker
	{
		private string rootPath;
		private Node rootNode;
		private DateTime lastWriteTime;

		public Walker(string root)
		{
			rootPath = root;
		}

		public Node RootNode
		{
			get
			{
				if (rootNode == null)
					InitRoot();
				else if (System.IO.Directory.GetLastWriteTime(rootPath) > lastWriteTime)
				{
					UpdateRoot();
				}
				return rootNode;
			}
		}

		public Entry SearchEntry(string path)
		{
			if (rootNode == null)
				InitRoot();
			else if (System.IO.Directory.GetLastWriteTime(rootPath) > lastWriteTime)
			{
				UpdateRoot();
			}
			return SearchEntry(path, rootNode, rootPath, null);
		}

		public Entry SearchEntry(string path, EntryKind stopAt)
		{
			if (rootNode == null)
				InitRoot();
			else if (System.IO.Directory.GetLastWriteTime(rootPath) > lastWriteTime)
			{
				UpdateRoot();
			}
			return SearchEntry(path, rootNode, rootPath, stopAt);
		}

		public static Entry SearchEntry(string path, Node parent)
		{
			return SearchEntry(path, parent, null, null);
		}

		private static Entry SearchEntry(string path, Node parent, string rootPath, EntryKind? stopAt)
		{
			Entry result = null;
			string[] pathComponents = path.Split(System.IO.Path.DirectorySeparatorChar);
			Node currentNode = parent;
			for (int i = 0; i < pathComponents.Length; i++)
			{
				result = null;
				foreach (var entry in currentNode.Nodes)
				{
					if (entry.Name == pathComponents[i])
					{
						result = entry;
						break;
					}
				}
				if (result == null)
				{
					break;
				}
				if (((result.Kind == EntryKind.File) || (result.Kind == EntryKind.ZipFileEntry)) && (i + 1 < pathComponents.Length))
				{
					throw new System.IO.DirectoryNotFoundException();
				}
				if (result.Node == null)
				{
					if (rootPath == null)
					{
						throw new System.IO.IOException();
					}
					string elementPath = System.IO.Path.Combine(rootPath, string.Join(new string(System.IO.Path.DirectorySeparatorChar, 1), pathComponents, 0, i + 1));
					if (result.Kind == EntryKind.Directory)
					{
						result.Node = DirectoryGenerator.Generate(new System.IO.DirectoryInfo(elementPath), false);
						result.UpdateSize();
					}
					if (result.Kind == EntryKind.ZipFile)
					{
						result.Node = ZipFileGenerator.Generate(new System.IO.FileInfo(elementPath));
						result.UpdateSize();
					}
				}



				else if (rootPath != null)
				{
					string elementPath = System.IO.Path.Combine(rootPath, string.Join(new string(System.IO.Path.DirectorySeparatorChar, 1), pathComponents, 0, i + 1));
					if (result.Kind == EntryKind.Directory)
					{
						if (result.LastWriteTime < System.IO.Directory.GetLastWriteTime(elementPath))
						{
							System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(elementPath);
							DirectoryGenerator.Update(di, result.Node);
							Entry newEntry = new Entry(di);
							newEntry.Node = result.Node;
							newEntry.UpdateSize();
							currentNode.Nodes.Remove(result);
							currentNode.Nodes.Add(newEntry);
							result = newEntry;
						}
					}
					if (result.Kind == EntryKind.File)
					{
						if (result.LastWriteTime < System.IO.File.GetLastWriteTime(elementPath))
						{
							System.IO.FileInfo fi = new System.IO.FileInfo(elementPath);
							Entry newEntry = new Entry(fi, false);
							currentNode.Nodes.Remove(result);
							currentNode.Nodes.Add(newEntry);
							result = newEntry;
						}
					}
				}
				if (stopAt.HasValue && (result.Kind == stopAt.Value))
					break;
				currentNode = result.Node;
			}
			if ((result != null) && stopAt.HasValue && (result.Kind != stopAt.Value))
				return null;
			return result;
		}

		private void InitRoot()
		{
			System.IO.DirectoryInfo root = new System.IO.DirectoryInfo(rootPath);
			rootNode = DirectoryGenerator.Generate(root, false);
			lastWriteTime = root.LastWriteTime;
		}

		private void UpdateRoot()
		{
			System.IO.DirectoryInfo root = new System.IO.DirectoryInfo(rootPath);
			DirectoryGenerator.Update(root, rootNode);
			lastWriteTime = root.LastWriteTime;
		}

		public override string ToString()
		{
			return string.Format("{{Walker: Path={0}}}", rootPath);
		}
	}
}
