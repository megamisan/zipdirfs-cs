//
//  ZipFileReference.cs
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
	public class ZipFileReference
	{
		public string Path
		{
			get;
			set;
		}
		public ICSharpCode.SharpZipLib.Zip.ZipFile ZipFile
		{
			get;
			set;
		}
		public int RefCount
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Format("{{{0} Refs={1}}}", Path, RefCount);
		}
	}
}
