//
//  DirectoriesEntries.cs
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
using System.IO;

namespace zipdirfs
{
	class DirectoryEntries : AbstractEntries
	{
		private Node node;
		private DateTime writeTime;

		internal DirectoryEntries(Node node, DateTime writeTime)
		{
			this.node = node;
			this.writeTime = writeTime;
		}

		#region implemented abstract members of zipdirfs.AbstractEntries
		public override System.Collections.Generic.IEnumerator<Mono.Fuse.DirectoryEntry> GetEnumerator()
		{
			yield return MakeDirectoryEntry(".", writeTime, (4096 * (long)Math.Ceiling(node.Nodes.Count * 0.1)));
			yield return MakeDirectoryEntry("..", DateTime.Now, 0);
			if (node != null)
			{
				foreach (var entry in node.Nodes)
				{
					yield return MakeDirectoryEntry(entry);
				}
			}
		}
		
		#endregion
	}
}

