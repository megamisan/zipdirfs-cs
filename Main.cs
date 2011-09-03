//
//  Main.cs
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
using System.Collections.Generic;
using System.IO;

namespace zipdirfs
{
	class MainClass : FileSystem
	{
		private Walker walker;
		private IDictionary<string, ZipFileReference> zipFiles;
		private IDictionary<string, OpenedZipEntry> zipEntries;
		private OpenedZipEntry[] zipEntryCache;

		public static void Main(string[] args)
		{
			using (FileSystem fs = new MainClass(args))
			{
				// fs.EnableFuseDebugOutput = true;
				fs.AllowAccessToOthers = true;
				fs.EnableKernelCache = true;
				fs.EnableKernelPermissionChecking = true;
				fs.DefaultUmask = NativeConvert.FromOctalPermissionString("222");
				fs.Name = "Zip browsing File system.";
				fs.Start();
			}
		}

		public string Source
		{
			get;
			set;
		}

		public MainClass(string[] args) : base(args)
		{
			Source = null;
			if (args.Length > 0)
			{
				if (Directory.Exists(args[0]))
				{
					Source = args[0];
				}
				walker = new Walker(Source);
				zipFiles = new Dictionary<string, ZipFileReference>();
				zipEntries = new Dictionary<string, OpenedZipEntry>();
				zipEntryCache = new OpenedZipEntry[10];
			}
		}

		protected override Errno OnReadDirectory(string directory, OpenedPathInfo info, out IEnumerable<Mono.Fuse.DirectoryEntry> paths)
		{
			try
			{
				if (directory == "/")
				{
					paths = new DirectoryEntries(walker.RootNode, System.IO.Directory.GetLastWriteTime(Source));
				}

				else
				{
					paths = null;
					try
					{
						Entry dirEntry = walker.SearchEntry(directory.Substring(1));
						if (dirEntry == null)
							return Errno.ENOENT;
						if ((dirEntry.Kind == EntryKind.File) || (dirEntry.Kind == EntryKind.ZipFileEntry))
							return Errno.ENOTDIR;
						paths = new DirectoryEntries(dirEntry.Node, dirEntry.LastWriteTime);
					}
					catch (DirectoryNotFoundException)
					{
						return Errno.ENOTDIR;
					}
					catch (IOException)
					{
						return Errno.EIO;
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}
			return 0;
		}

		protected override Errno OnGetFileSystemStatus(string path, out Statvfs buf)
		{
			buf = new Statvfs();
			buf.f_flag = MountFlags.ST_RDONLY | MountFlags.ST_NOSUID | MountFlags.ST_NOEXEC | MountFlags.ST_NODIRATIME | MountFlags.ST_NODEV | MountFlags.ST_NOATIME;
			buf.f_namemax = 0xFFFFFFFFuL;
			return 0;
		}

		protected override Errno OnGetPathStatus(string path, out Stat stat)
		{
			stat = new Stat();
			if (path == "/")
			{
				DirectoryInfo di = new DirectoryInfo(Source);
				stat.st_mode = FilePermissions.S_IFDIR | NativeConvert.FromOctalPermissionString("0666");
				stat.st_size = (4096 * (long)Math.Ceiling(walker.RootNode.Nodes.Count * 0.1));
				stat.st_mtime = NativeConvert.FromDateTime(di.LastWriteTime);
				stat.st_nlink = 2 + (ulong)Helpers.GetEntriesCount(walker.RootNode, new EntryKind[] { EntryKind.Directory, EntryKind.ZipFile, EntryKind.ZipDirectoryEntry });
			}

			else
			{
				try
				{
					Entry fsEntry = walker.SearchEntry(path.Substring(1));
					if (fsEntry == null)
						return Errno.ENOENT;
					Helpers.SetStatInfo(fsEntry, ref stat);
				}
				catch (DirectoryNotFoundException)
				{
					return Errno.ENOTDIR;
				}
				catch (IOException)
				{
					return Errno.EIO;
				}
			}
			return 0;
		}

		protected override Errno OnReadSymbolicLink(string link, out string target)
		{
			target = string.Empty;
			try
			{
				Entry lnEntry = walker.SearchEntry(link.Substring(1));
				if (lnEntry == null)
					return Errno.ENOENT;
				if (lnEntry.Kind != EntryKind.File)
					return Errno.EINVAL;
				target = Path.Combine(Source, link.Substring(1));
			}
			catch (DirectoryNotFoundException)
			{
				return Errno.ENOTDIR;
			}
			catch (IOException)
			{
				return Errno.EIO;
			}
			return 0;
		}

		protected override Errno OnAccessPath(string path, AccessModes mode)
		{
			if ((mode & AccessModes.W_OK) != 0)
				return Errno.EROFS;
			try
			{
				Entry fsEntry = walker.SearchEntry(path.Substring(1));
				if (fsEntry == null)
					return Errno.ENOENT;
				if (((mode & AccessModes.X_OK) != 0) && (fsEntry.Kind == EntryKind.ZipFileEntry))
					return Errno.EACCES;
			}
			catch (DirectoryNotFoundException)
			{
				return Errno.ENOTDIR;
			}
			catch (IOException)
			{
				return Errno.EIO;
			}
			return 0;
		}

		protected override Errno OnOpenHandle(string file, OpenedPathInfo info)
		{
			if ((info.OpenFlags & (OpenFlags.O_CREAT | OpenFlags.O_APPEND | OpenFlags.O_WRONLY)) != 0)
				return Errno.EROFS;
			try
			{
				Entry fsEntry = walker.SearchEntry(file.Substring(1));
				if (fsEntry == null)
					return Errno.ENOENT;
				if (fsEntry.Kind != EntryKind.ZipFileEntry)
					return Errno.EIO;
				if (!IsZipEntryOpened(file))
				{
					if (!IsZipEntryCached(file))
					{
						Entry zip = walker.SearchEntry(file.Substring(1), EntryKind.ZipFile);
						string fileEntryPath = file.Substring(file.LastIndexOf(zip.Name + '/') + zip.Name.Length + 1);
						string zipPath = file.Substring(0, file.LastIndexOf(zip.Name + '/') + zip.Name.Length);
						ZipFileReference zipRef = null;
						if (!IsZipFileOpened(zipPath))
						{
							zipRef = CreateZipFileReference(zipPath);
						}

						else
						{
							zipRef = LockZipFileReference(zipPath);
						}
						CreateOpenedZipEntry(zipRef, file, fileEntryPath);
					}

					else
						LockOpenedZipEntryFromCache(file);
				}

				else
					LockOpenedZipEntry(file);
			}
			catch (DirectoryNotFoundException)
			{
				return Errno.ENOTDIR;
			}
			catch (IOException)
			{
				return Errno.EIO;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}
			return 0;
		}

		protected override Errno OnGetHandleStatus(string file, OpenedPathInfo info, out Stat buf)
		{
			return OnGetPathStatus(file, out buf);
		}

		protected override Errno OnReadHandle(string file, OpenedPathInfo info, byte[] buf, long offset, out int bytesWritten)
		{
			OpenedZipEntry entry = null;
			lock (zipEntries)
			{
				entry = zipEntries[file];
			}
			lock (entry)
			{
				if (entry.Position < offset + buf.LongLength)
					ReadToPosition(entry, offset + buf.LongLength);
			}
			long maxRead = Math.Min(entry.Data.LongLength - offset, buf.LongLength);
			Array.Copy(entry.Data, offset, buf, 0, maxRead);
			bytesWritten = (int)maxRead;
			return 0;
		}

		protected override Errno OnReleaseHandle(string file, OpenedPathInfo info)
		{
			try
			{
				Entry fsEntry = walker.SearchEntry(file.Substring(1));
				if (fsEntry == null)
					return Errno.ENOENT;
				if (fsEntry.Kind != EntryKind.ZipFileEntry)
					return Errno.EIO;
				ReleaseOpenedZipEntry(file);
			}
			catch (DirectoryNotFoundException)
			{
				return Errno.ENOTDIR;
			}
			catch (IOException)
			{
				return Errno.EIO;
			}
			return 0;
		}

		protected bool IsZipEntryOpened(string path)
		{
			lock (zipEntries)
			{
				return zipEntries.ContainsKey(path);
			}
		}

		protected bool IsZipEntryCached(string path)
		{
			lock (zipEntryCache)
			{
				for (int i = 0; i < zipEntryCache.Length; i++)
				{
					if ((zipEntryCache[i] != null) && (zipEntryCache[i].Path == path))
					{
						zipEntryCache[i].LastAccess = DateTime.Now;
						return true;
					}
				}
			}
			return false;
		}

		protected bool IsZipFileOpened(string path)
		{
			lock (zipFiles)
			{
				return zipFiles.ContainsKey(path);
			}
		}

		protected OpenedZipEntry LockOpenedZipEntry(string path)
		{
			OpenedZipEntry zipEntry = null;
			lock (zipEntries)
			{
				zipEntry = zipEntries[path];
			}
			lock (zipEntry)
			{
				zipEntry.OpenCount++;
			}
			return zipEntry;
		}

		protected OpenedZipEntry LockOpenedZipEntryFromCache(string path)
		{
			OpenedZipEntry zipEntry = null;
			lock (zipEntryCache)
			{
				int cacheIndex = -1;
				for (int i = 0; i < zipEntryCache.Length; i++)
				{
					if ((zipEntryCache[i] != null) && (zipEntryCache[i].Path == path))
					{
						cacheIndex = i;
						break;
					}
				}
				zipEntry = zipEntryCache[cacheIndex];
				lock (zipEntries)
				{
					zipEntries.Add(path, zipEntry);
				}
				zipEntryCache[cacheIndex] = null;
			}
			lock (zipEntry)
			{
				zipEntry.OpenCount++;
			}
			return zipEntry;
		}

		protected void ReleaseOpenedZipEntry(string path)
		{
			OpenedZipEntry zipEntry = null;
			lock (zipEntries)
			{
				zipEntry = zipEntries[path];
			}
			lock (zipEntry)
			{
				zipEntry.OpenCount--;
			}
			lock (zipEntry)
			{
				if (zipEntry.OpenCount == 0)
				{
					lock (zipEntryCache)
					{
						int lastAccessed = -1;
						DateTime lastAccess = DateTime.Now;
						int emptyIndex = -1;
						for (int i = 0; i < zipEntryCache.Length; i++)
						{
							if (zipEntryCache[i] == null)
							{
								emptyIndex = i;
								break;
							}
							if (zipEntryCache[i].LastAccess < lastAccess)
							{
								lastAccessed = i;
								lastAccess = zipEntryCache[i].LastAccess;
							}
						}
						if (emptyIndex < 0)
						{
							if (zipEntryCache[lastAccessed].Stream != null)
								zipEntryCache[lastAccessed].Stream.Close();
							zipEntryCache[lastAccessed].Stream = null;
							zipEntryCache[lastAccessed].Data = null;
							if (zipEntryCache[lastAccessed].ZipFile != null)
							{
								ReleaseZipFileReference(zipEntryCache[lastAccessed].ZipFile.Path);
							}
							zipEntryCache[lastAccessed].ZipFile = null;
							zipEntryCache[lastAccessed] = null;
							emptyIndex = lastAccessed;
						}
						zipEntryCache[emptyIndex] = zipEntry;
						lock (zipEntries)
						{
							zipEntries.Remove(path);
						}
						zipEntry.LastAccess = DateTime.Now;
					}
				}
			}
		}

		protected void ReleaseZipFileReference(string path)
		{
			lock (zipFiles)
			{
				zipFiles[path].RefCount--;
				if (zipFiles[path].RefCount == 0)
				{
					zipFiles[path].ZipFile.Close();
					zipFiles.Remove(path);
				}
			}
		}

		protected ZipFileReference LockZipFileReference(string path)
		{
			lock (zipFiles)
			{
				zipFiles[path].RefCount++;
				return zipFiles[path];
			}
		}

		protected ZipFileReference CreateZipFileReference(string path)
		{
			ICSharpCode.SharpZipLib.Zip.ZipFile zipFile = new ICSharpCode.SharpZipLib.Zip.ZipFile(System.IO.Path.Combine(Source, path.Substring(1)));
			lock (zipFiles)
			{
				ZipFileReference zipFileRef = new ZipFileReference();
				zipFiles.Add(path, zipFileRef);
				zipFileRef.Path = path;
				zipFileRef.RefCount++;
				zipFileRef.ZipFile = zipFile;
				return zipFileRef;
			}
		}

		protected OpenedZipEntry CreateOpenedZipEntry(ZipFileReference zipFile, string fullPath, string path)
		{
			OpenedZipEntry entry = new OpenedZipEntry();
			entry.Path = fullPath;
			entry.ZipFile = zipFile;
			entry.ZipEntry = zipFile.ZipFile.GetEntry(path);
			entry.Stream = zipFile.ZipFile.GetInputStream(entry.ZipEntry);
			entry.Data = new byte[entry.ZipEntry.Size];
			entry.OpenCount++;
			entry.LastAccess = DateTime.Now;
			lock (zipEntries)
			{
				zipEntries.Add(fullPath, entry);
			}
			return entry;
		}

		void ReadToPosition(OpenedZipEntry entry, long position)
		{
			position = Math.Min(entry.Data.LongLength, position);
			if (entry.Position == entry.Data.LongLength)
				return;
			while (position > entry.Position)
			{
				int read = entry.Stream.Read(entry.Data, (int)entry.Position, (int)(position - entry.Position));
				entry.Position += read;
			}
			if (entry.Position == entry.Data.LongLength)
			{
				entry.Stream.Close();
				entry.Stream = null;
				entry.ZipEntry = null;
				ReleaseZipFileReference(entry.ZipFile.Path);
				entry.ZipFile = null;
			}
		}
	}
}
