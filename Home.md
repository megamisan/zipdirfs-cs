# Introduction #

No introduction yet.

# Details #

## Dependancies ##

  * Mono.Fuse
  * SharpZipLib

## Installation ##

Put zipdirfs.exe somewhere. Put Mono.Fuse.dll in the same directory. Put libMonoFuseHelper.so in the same directory or in /usr/lib.

If Mono support is not integrated with the system, create a script to run zipdirfs.exe with mono and script parameters.

## Usage ##

To mount a directory, exec `zipdirfs.exe <sourcedir> <mountpoint> [fuse options]`.
To use it in fstab / mtab write a line like that :
`<path_to_zipdirfs.exe>#<sourcedir> <mountpoint> fuse <options> 0 0`

## Note on fuse ##

If you want to be able to mount a fuse fs as a user, you need to be in the fuse group.
In the current state, the user\_allow\_others must be defined in fuse.conf if you want to mount as a user.