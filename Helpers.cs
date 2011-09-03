//
//  Helpers.cs
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
using Mono.Unix.Native;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using System.Collections.Generic;

namespace zipdirfs
{
	static class Helpers
	{
		static internal void SetStatInfo(Entry entry, ref Stat stat)
		{
			stat.st_mode = NativeConvert.FromOctalPermissionString("0444");
			switch (entry.Kind)
			{
			case EntryKind.Directory:
			case EntryKind.ZipDirectoryEntry:
			case EntryKind.ZipFile:
				stat.st_mode |= FilePermissions.S_IFDIR | NativeConvert.FromOctalPermissionString("0222");
				stat.st_nlink = 2 + ((entry.Node != null) ? (ulong)GetEntriesCount(entry.Node, new EntryKind[] { EntryKind.Directory, EntryKind.ZipFile, EntryKind.ZipDirectoryEntry }) : 0);
				break;
			case EntryKind.File:
				stat.st_mode |= FilePermissions.S_IFLNK | NativeConvert.FromOctalPermissionString("0777");
				stat.st_nlink = 1;
				break;
			case EntryKind.ZipFileEntry:
				stat.st_mode |= FilePermissions.S_IFREG;
				stat.st_nlink = 1;
				break;
			}
			stat.st_size = entry.Size;
			stat.st_mtime = NativeConvert.FromDateTime(entry.LastWriteTime);
		}

		static internal int GetEntriesCount(Node node, EntryKind[] kinds)
		{
			int count = 0;
			foreach (var entry in node.Nodes)
			{
				foreach (var kind in kinds)
				{
					if (entry.Kind == kind)
					{
						count++;
						break;
					}
				}
			}
			return count;
		}
	}
}

