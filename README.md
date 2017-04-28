# Amongst - A handy MongoDB (Integration) Testing Tool!
![Build status](https://ci.appveyor.com/api/projects/status/github/Johlandabee/Amongst?branch=develop&svg=true)  
---
Amongst provides isolated MongoDB instances on **Windows**, **Linux** and **OSX** within your C# unit tests.  
It does so by wrapping around current **MongoDB 3.4.4** binarys while targeting **.NET 4.6** and **.NET Standard 1.6**.  
Amongst is build with **Dotnet Core 1.1**. 

The Project is inspired by [Mongo2Go](https://github.com/Mongo2Go/Mongo2Go) which comes with a few issues.  

Amongst is created to resolve those issues while [keeping it simple](https://en.wikipedia.org/wiki/You_aren%27t_gonna_need_it).
Also with [XUnit](https://xunit.github.io/), Dotnet Core's default testing framekwork in mind. Nevertheless, it dosen't mean you are restricted to it.

---
### Current features are:
 - Availability on all three major platforms: Windows, Linux and OSX.
 - Creating a disposable isolated MongoDB instace without any setup procedre.
 - Pass Mongo's ouput to your unit tests so it will show up in your ci build logs or write them on disk.
 - Data in- and export.

**Note:** The **inMemory** storage engine **won't** be supported since it is only available in **MongoDB Enterprise**. 

>If you would like to see a new *feature*, *imporvement* or are having an *issue* with Amongst, please participate on the [issue tracker](https://github.com/Johlandabee/Amongst/issues).

### Included MongoDB builds:
- [mongodb-win32-x86_64-2008plus-3.4.4](http://downloads.mongodb.org/win32/mongodb-win32-x86_64-2008plus-3.4.4.zip)
- [mongodb-linux-x86_64-3.4.4](http://downloads.mongodb.org/linux/mongodb-linux-x86_64-3.4.4.tgz)
- [mongodb-osx-x86_64-3.4.4](http://downloads.mongodb.org/osx/mongodb-osx-x86_64-3.4.4.tgz)
