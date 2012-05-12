using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace EventEvaluationLib
{
    public class FilePathHelpers
    {
        public static string GetPathRelativeTo(string path, string relativeTo)
        {
            Uri pathAsUri = new Uri(path);
            Uri relativeToUri = new Uri(relativeTo);
            return Uri.UnescapeDataString(relativeToUri.MakeRelativeUri(pathAsUri).ToString());
        }

        public static IEnumerable<string> GetDirectoriesByMaskedPath(string mask)
        {
            /* if (Path.IsPathRooted(mask))
                 mask = "\\" + mask;*/
            foreach (string dir in GetDirectoryPathsRecurcively("", mask))
            {
                yield return dir;
            }
        }

        public static IEnumerable<string> GetFilesByMaskedPath(string mask)
        {
            /* if (Path.IsPathRooted(mask))
                 mask = "\\" + mask;*/
            int fileAndDirseparatorPos = Math.Max(mask.LastIndexOf(Path.DirectorySeparatorChar),
                                                  mask.LastIndexOf(Path.AltDirectorySeparatorChar));
            string directoryPart = mask.Substring(0, fileAndDirseparatorPos);
            string filePart = mask.Substring(fileAndDirseparatorPos + 1);


            foreach (string dir in GetDirectoryPathsRecurcively("", directoryPart))
            {
                foreach (string file in Directory.GetFiles(dir, filePart))
                {
                    yield return file;
                }

            }
        }

        private static IEnumerable<string> GetDirectoryPathsRecurcively(string begining, string pathTailwithMask)
        {
            if (!String.IsNullOrEmpty(begining) && !Directory.Exists(begining))
                yield break;


            string[] possibleDirectories;
            string[] parts = pathTailwithMask.Split(
                new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
            {
                yield return begining;
                yield break;
            }
            if (!parts[0].Contains("*") && !parts[0].Contains("?"))
            {
                possibleDirectories = new string[1];
                possibleDirectories[0] = begining + parts[0];
            }
            else if (String.IsNullOrEmpty(begining))
            {
                possibleDirectories = new DirectoryInfo(".").GetDirectories(parts[0]).Select(d => d.Name).ToArray();
                foreach (string possibleDirectory in possibleDirectories)
                {
                    foreach (string directory in GetDirectoryPathsRecurcively(possibleDirectory + Path.DirectorySeparatorChar, String.Join(new string(Path.DirectorySeparatorChar, 1), parts.ToArray())))
                    {
                        yield return directory;
                    }
                }
            }
            else
            {
                try
                {
                    possibleDirectories = Directory.GetDirectories(begining, parts[0]);
                }
                catch (Exception ex)
                {
                    yield break;
                }

            }
            foreach (string possibleDirectory in possibleDirectories)
            {
                foreach (string directory in GetDirectoryPathsRecurcively(possibleDirectory + Path.DirectorySeparatorChar,
                                                                        String.Join(
                                                                            new string(Path.DirectorySeparatorChar, 1),
                                                                            parts.Skip(1).ToArray())))
                {
                    yield return directory;
                }
            }
        }

        public static IEnumerable<string> FindDirectoriesOnFixedDisks(string p)
        {
            return Path.GetPathRoot(p) == Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture)
                       ? DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed).Select(
                           d => d.Name.TrimEnd('\\') + p)
                       : new List<string>() { p };
        }
    }
}