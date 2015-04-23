A FUSE filesystem written in C#.
It mirrors a filesystem directory and any of its content.
If a Zip file is encountered, its content is exposed directly in the filesystem.
The filesystem is readonly but synchronized with the source changes.

I recently made a C++ version of this tool, source is available at http://svn.megami.fr/svn/zipdirfs/. Releases available in this project.