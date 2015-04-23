# Introduction #

Because I could not run the C# version on the distribution provided mono version on Ubuntu 12.04, I started a C++ version.

# Details #

## Dependancies ##

  * fuse 2.7 or earlier
  * fusekit 0.6.3 with symlink patch ([issue #1](https://code.google.com/p/zipdirfs/issues/detail?id=#1))
  * libzip 10 or earlier

## Installation ##

Extract the program's archive (zipdirfs-0.1.tar.gz) in some folder. Enter the created folder (zipdirfs-0.1) and run `./configure && make && sudo make install`. You may not use sudo to run as root if you are not root or sudo is not configured.

## Usage ##

To mount a directory, execute `zipdirfs <sourcedir> <mountpoint> [fuse options]`.
To use it in fstab, write a line like that :
`<sourcedir> <mountpoint> fuse.zipdirfs <options> 0 2`

Recommended fuse and mount options are `ro,allow_other,defaults_permissions`.