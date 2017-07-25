using System;
using System.Diagnostics;
using System.IO;
using Amongst.Exception;

namespace Amongst
{
    public partial class MongoDBInstance
    {
        private const int DEFAULT_TIMEOUT = 5000;

        //------------------------------------------------------------------------------------------------------------->

        /// <summary>
        /// Import data using native mongoimport functionality.
        /// Failure timeout is 5000ms.
        /// </summary>
        /// <param name="database">Database name</param>
        /// <param name="collection">Collection name</param>
        /// <param name="filePath">File to be imported.</param>
        /// <param name="dropCollection">Drop already existing collections?</param>
        public void Import(string database, string collection, string filePath, bool dropCollection)
        {
            Import(database, collection, filePath, dropCollection, DEFAULT_TIMEOUT);
        }

        /// <summary>
        /// Import data using native mongoimport functionality.
        /// Existing collections will be dropped.
        /// </summary>
        /// <param name="database">Database name</param>
        /// <param name="collection">Collection name</param>
        /// <param name="filePath">File to be imported.</param>
        /// <param name="timeout">Failure timeout</param>
        public void Import(string database, string collection, string filePath, int timeout)
        {
            Import(database, collection, filePath, true, timeout);
        }

        /// <summary>
        /// Import data using native mongoimport functionality.
        /// </summary>
        /// <param name="database">Database name</param>
        /// <param name="collection">Collection name</param>
        /// <param name="filePath">File to be imported.</param>
        /// <param name="dropCollection">Drop already existing collections?</param>
        /// <param name="timeout">Failure timeout</param>
        public void Import(string database, string collection, string filePath, bool dropCollection,
            int timeout)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Could not import file {filePath}.");
            }

            var fullPath = Path.Combine(_binaryPath, "mongoimport");

#if NETSTANDARD1_6
            if (IsUnix())
            {
                SetExecutableBit(fullPath);
            }
#endif
            // mongoimport wants a UNIX path.
            var unixFilePath = filePath.Replace("\\", "/");

            var drop = dropCollection ? "--drop" : null;
            var logVerbosity = _options.Verbosity == LogVerbosity.Quiet
                ? "--quiet"
                : _options.Verbosity == LogVerbosity.Verbose
                    ? "--verbose"
                    : null;

            var args = new[]
            {
                $"--host {_connection.IP}:{_connection.Port}",
                $"--db {database}",
                $"--collection {collection}",
                $"--file {unixFilePath}",
                $"{drop}",
                $"{logVerbosity}"
            };

            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fullPath,
                    Arguments = string.Join(" ", args),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            p.OutputDataReceived += OnOutputDataReceived;
            p.ErrorDataReceived += OnErrorDataReceived;

            p.Start();

            var exited = p.WaitForExit(timeout);
            if (!exited)
            {
                throw new TimeoutException(
                    $"Mongoimport failed to import {unixFilePath} to {database}/{collection} after {timeout} milliseconds.");
            }

            if (p.ExitCode != 0)
            {
                throw new ExitCodeException(
                    $"Mongoimport failed to import {unixFilePath} to {database}/{collection}. Exit code {p.ExitCode}.");
            }

            if (_options.Verbosity > LogVerbosity.Normal)
            {
                _options.OutputHelper.WriteLine($"Successfully imported {unixFilePath} to {database}/{collection}");
            }
        }

        /// <summary>
        /// Export data using native mongoexport functionality.
        /// Failure timeout is 5000ms.
        /// </summary>
        /// <param name="database">Database name</param>
        /// <param name="collection">Collection name</param>
        /// <param name="filePath">Export destination</param>
        public void Export(string database, string collection, string filePath)
        {
            Export(database, collection, filePath, DEFAULT_TIMEOUT);
        }

        /// <summary>
        /// Export data using native mongoexport functionality.
        /// </summary>
        /// <param name="database">Database name</param>
        /// <param name="collection">Collection name</param>
        /// <param name="filePath">Export destination</param>
        /// <param name="timeout">Failure timeout</param>
        public void Export(string database, string collection, string filePath, int timeout)
        {
            var fullPath = Path.Combine(_binaryPath, "mongoexport");

#if NETSTANDARD1_6
            if (IsUnix())
            {
                SetExecutableBit(fullPath);
            }
#endif
            // mongoexpot wants a UNIX path.
            var unixFilePath = filePath.Replace("\\", "/");

            var logVerbosity = _options.Verbosity == LogVerbosity.Quiet
                ? "--quiet"
                : _options.Verbosity == LogVerbosity.Verbose
                    ? "--verbose"
                    : null;

            var args = new[]
            {
                $"--host {_connection.IP}:{_connection.Port}",
                $"--db {database}",
                $"--collection {collection}",
                $"--out {unixFilePath}",
                $"{logVerbosity}"
            };

            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fullPath,
                    Arguments = string.Join(" ", args),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            p.OutputDataReceived += OnOutputDataReceived;
            p.ErrorDataReceived += OnErrorDataReceived;

            p.Start();

            var exited = p.WaitForExit(timeout);
            if (!exited)
            {
                throw new TimeoutException(
                    $"Mongoexport failed to export {database}/{collection} to {unixFilePath} after {timeout} milliseconds.");
            }

            if (p.ExitCode != 0)
            {
                throw new ExitCodeException(
                    $"Mongoexport failed to export {database}/{collection} to {unixFilePath}. Exit code {p.ExitCode}.");
            }

            if (_options.Verbosity > LogVerbosity.Normal)
            {
                _options.OutputHelper.WriteLine($"Successfully exported {database}/{collection} to {unixFilePath}");
            }
        }
    }
}