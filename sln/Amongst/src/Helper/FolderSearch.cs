using System.IO;

namespace Amongst.Helper
{
    public class FolderSearch
    {
        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pattern"></param>
        /// <param name="maxRecursion"></param>
        /// <returns></returns>
        public static string FindDownwards(string path, string pattern, int maxRecursion = 6)
        {
            for (var i = 0; i <= maxRecursion; i++) {
                try {
                    return Directory.GetDirectories(path, pattern)[0];
                }
                catch (DirectoryNotFoundException) {
                    var sections = path.Split(Path.DirectorySeparatorChar);
                    var depth = sections.Length;

                    if (i == maxRecursion || depth <= 1)
                        return null;

                    path = string.Join(Path.DirectorySeparatorChar + "", sections, 0, depth - 1);
                }
            }

            return null;
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pattern"></param>
        /// <param name="maxRecursion"></param>
        /// <returns></returns>
        public static string FindUpwards(string path, string pattern, int maxRecursion = 6)
        {
            return FindUpwards(path, pattern, 0, maxRecursion);
        }

        private static string FindUpwards(string path, string pattern, int depth, int maxRecursion)
        {
            try {
                return Directory.GetDirectories(path, pattern)[0];
            }
            catch (DirectoryNotFoundException) {
                if (depth > maxRecursion) return null;

                foreach (var subDir in Directory.GetDirectories(path)) {
                    var result = FindUpwards(subDir, pattern, depth + 1, maxRecursion);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }
    }
}