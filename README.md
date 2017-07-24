# Amongst - MongoDB (Integration) Testing!
[![Codacy Badge](https://api.codacy.com/project/badge/Grade/f3dbbcc1c0a94916985614f630e0f826)](https://www.codacy.com/app/Johlandabee/Amongst?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=Johlandabee/Amongst&amp;utm_campaign=Badge_Grade)
[![Windows build status](https://ci.appveyor.com/api/projects/status/github/Johlandabee/Amongst?branch=develop&svg=true)](https://ci.appveyor.com/project/Johlandabee/amongst)
[![Linux build Status](https://travis-ci.org/Johlandabee/Amongst.svg?branch=develop)](https://travis-ci.org/Johlandabee/Amongst)

Amongst provides isolated MongoDB instances on **Windows**, **Linux** and **OSX** within your C# unit tests.  
It does so by wrapping around current **MongoDB 3.4.4** binaries while targeting **.NET 4.6** and **.NET Standard 1.6**.  
Amongst is built with **Dotnet Core 1.1**. 

<p align="center">
  <img alt="Logo" src="https://cdn.rawgit.com/Johlandabee/Amongst/develop/logo.svg" />
</p>

The Project is inspired by [Mongo2Go](https://github.com/Mongo2Go/Mongo2Go). Its goal is [to keep it simple](https://en.wikipedia.org/wiki/You_aren%27t_gonna_need_it).  

Amongst is being created with [XUnit](https://xunit.github.io/), Dotnet Core's default testing framework in mind. Nevertheless, it doesn't mean you are restricted to it.

## Current features
 - Availability on all three major platforms: Windows, Linux and OSX.
 - Creating a disposable isolated MongoDB instance without any setup procedure.
 - Pass Mongo's output to your unit tests so it will show up in your ci build logs or write them on disk.
 - Data import and export.

**Note:** The **inMemory** storage engine **won't** be supported since it is only available in **MongoDB Enterprise**. 

>If you would like to see a new *feature*, *improvement* or are having an *issue* with Amongst, please participate on the [issue tracker](https://github.com/Johlandabee/Amongst/issues).

## Included MongoDB builds 
(only *mongod*, *mongoexport* and *mongoimport*):
- [mongodb-win32-x86_64-2008plus-3.4.4](http://downloads.mongodb.org/win32/mongodb-win32-x86_64-2008plus-3.4.4.zip)
- [mongodb-linux-x86_64-3.4.4](http://downloads.mongodb.org/linux/mongodb-linux-x86_64-3.4.4.tgz)
- [mongodb-osx-x86_64-3.4.4](http://downloads.mongodb.org/osx/mongodb-osx-x86_64-3.4.4.tgz)
