//
//  AbstractEntries.cs
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
using Mono.Fuse;
using Mono.Unix.Native;
using System.Collections;
using System.Collections.Generic;

namespace zipdirfs
{
	abstract class AbstractEntries : IEnumerable<DirectoryEntry>, IEnumerable
	{
		#region IEnumerable[DirectoryEntry] implementation
		public abstract IEnumerator<DirectoryEntry> GetEnumerator();
		#endregion

		internal DirectoryEntry MakeDirectoryEntry(Entry entry)
		{
			DirectoryEntry dirEntry = new DirectoryEntry(entry.Name);
			Helpers.SetStatInfo(entry, ref dirEntry.Stat);
			return dirEntry;
		}

		internal DirectoryEntry MakeDirectoryEntry(string name, DateTime writeTime, long size)
		{
			DirectoryEntry dirEntry = new DirectoryEntry(name);
			dirEntry.Stat.st_mode = NativeConvert.FromOctalPermissionString("0666") | FilePermissions.S_IFDIR;
			dirEntry.Stat.st_size = size;
			dirEntry.Stat.st_mtime = NativeConvert.FromDateTime(writeTime);
			return dirEntry;
		}

		#region IEnumerable implementation
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		#endregion
	}
}

