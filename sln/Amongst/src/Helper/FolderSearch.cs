using System.IO;

namespace Amongst.Helper
{
    public static class FolderSearch
    {
        private const int DEFAULT_RECURSION_DEPTH = 6;

        //------------------------------------------------------------------------------------------------------------->

        /// <summary>
        /// Searches a folder recursively downwards the hierarchy.
        /// </summary>
        /// <param name="path">Start path.</param>
        /// <param name="pattern">Search pattern. Can be a path.</param>
        /// <param name="maxRecursion">The max recursion depth.</param>
        /// <returns>A valid path or null if there are no results.</returns>
        public static string FindDownwards(string path, string pattern, int maxRecursion)
        {
            var currentDir = path;

            for (var i = 0; i <= maxRecursion; i++)
            {
                try
                {
                    return Directory.GetDirectories(currentDir, pattern)[0];
                }
                catch (DirectoryNotFoundException)
                {
                    var sections = currentDir.Split(Path.DirectorySeparatorChar);
                    var depth = sections.Length;

                    if (i == maxRecursion || depth <= 1)
                    {
                        return null;
                    }

                    currentDir = string.Join(Path.DirectorySeparatorChar + "", sections, 0, depth - 1);
                }
            }

            return null;
        }

        /// <summary>
        /// Searches a folder recursively downwards the hierarchy.
        /// Recursion depth is 6.
        /// </summary>
        /// <param name="path">Start path.</param>
        /// <param name="pattern">Search pattern. Can be a path.</param>
        /// <returns>A valid path or null if there are no results.</returns>
        public static string FindDownwards(string path, string pattern)
        {
            return FindDownwards(path, pattern, DEFAULT_RECURSION_DEPTH);
        }

        /// <summary>
        /// Searches a folder recursively upwards the hierarchy.
        /// </summary>
        /// <param name="path">Start path.</param>
        /// <param name="pattern">Serach pattern. Can be a path.</param>
        /// <param name="maxRecursion">The maximum recursion depth. 6 by default</param>
        /// <returns>A valid path or null if there are no results.</returns>
        public static string FindUpwards(string path, string pattern, int maxRecursion)
        {
            return FindUpwards(path, pattern, 0, maxRecursion);
        }

        /// <summary>
        /// Searches a folder recursively upwards the hierarchy.
        /// Recursion depth is 6.
        /// </summary>
        /// <param name="path">Start path.</param>
        /// <param name="pattern">Serach pattern. Can be a path.</param>
        /// <returns>A valid path or null if there are no results.</returns>
        public static string FindUpwards(string path, string pattern)
        {
            return FindUpwards(path, pattern, 0, DEFAULT_RECURSION_DEPTH);
        }

        private static string FindUpwards(string path, string pattern, int depth, int maxRecursion)
        {
            try
            {
                return Directory.GetDirectories(path, pattern)[0];
            }
            catch (DirectoryNotFoundException)
            {
                if (depth > maxRecursion)
                {
                    return null;
                }

                foreach (var subDir in Directory.GetDirectories(path))
                {
                    var result = FindUpwards(subDir, pattern, depth + 1, maxRecursion);

                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }
    }
}