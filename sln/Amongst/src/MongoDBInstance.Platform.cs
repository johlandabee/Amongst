using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Amongst.Exception;
using Amongst.Helper;
#if NETSTANDARD1_6
using System.Runtime.InteropServices;

#endif

namespace Amongst
{
    public partial class MongoDBInstance
    {
        private const string BIN_WIN32 = "mongodb-win32-x86_64-2008plus-3.4.4";
        private const string BIN_LINUX = "mongodb-linux-x86_64-3.4.4";
        private const string BIN_OSX = "mongodb-osx-x86_64-3.4.4";

        //------------------------------------------------------------------------------------------------------------->

#pragma warning disable CS0162
        private string GetBinaryPath()
        {
#if NETSTANDARD1_6
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            var isOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

            var osDescription = RuntimeInformation.OSDescription.Trim();
#elif NET46
            const bool isWindows = true;
            const bool isLinux = false;
            const bool isOSX = false;

            var osDescription = $"{Environment.OSVersion}".Trim();
#endif
            string build;
            if (isWindows)
                build = BIN_WIN32;
            else if (isLinux)
                build = BIN_LINUX;
            else if (isOSX)
                build = BIN_OSX;
            else
                throw new PlatformNotSupportedException($"Platform {osDescription} is not supported.");

            var prefix = $"[{DateTime.Now}][Info]";
            _options.OutputHelper.WriteLine(
                $"{prefix}: Detected {osDescription}. Using {build} binaries.");

            string binPath;
            if (Directory.Exists(Store.BinaryPath)) {
                binPath = Store.BinaryPath;

                _options.OutputHelper.WriteLine($"{prefix}: Using cached binary path.");
            }
            else {
                var binSegment = Path.Combine("tools", $"{build}", "bin");

                var homePath = isWindows
                    ? Environment.GetEnvironmentVariable("USERPROFILE")
                    : Environment.GetEnvironmentVariable("HOME");

                var searchPaths = new List<string>
                {
                    Directory.GetCurrentDirectory(),
                    Path.Combine(homePath, ".nuget", "packages"),
                    _options.PackageDirectory
                }.Where(p => !string.IsNullOrEmpty(p));

                binPath = searchPaths.Select(path =>
                    FolderSearch.FindDownwards(path, binSegment)
                    ?? FolderSearch.FindUpwards(path, binSegment)).FirstOrDefault();
            }

            if (binPath == null)
                throw new DirectoryNotFoundException(
                    "MongoDB binarys could not be found. " +
                    "If you have a custom package path, please specify it using the PackageDirectory option. " +
                    "If you already specified a custom package directory, double check if it is correct.");

            lock (Sync) {
                Store.BinaryPath = binPath;
                Store.Save();
            }

            _options.OutputHelper.WriteLine($"[{DateTime.Now}][Info]: Full binary path: {binPath}");

            return binPath;
        }
#pragma warning restore

#if NETSTANDARD1_6
        private static void SetExecutableBit(string file)
        {
            // Workaround until dotnet core 2.0
            var p = Process.Start("chmod", $"+x {file}");
            p.WaitForExit();

            if (p.ExitCode != 0)
                throw new CouldNotSetExecutableBit($"Could not set executable bit for {file}");
        }

        private static bool IsUnix()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        }
#endif
    }
}
