using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrganiseSongs
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // 0. Sanity checks
                if (args.Length != 2)
                    throw new ArgumentException("Wrong nubmer of parameters");
                // 1. Set source dir
                var sourceDir = new DirectoryInfo(args[0]);
                if (!sourceDir.Exists)
                    throw new ArgumentException($"Source directory does not exist: {sourceDir.FullName}");
                Console.WriteLine($"Will read all files from {sourceDir.FullName}");

                // 2. Set target dir
                var targetPath = new DirectoryInfo(args[1]);
                Console.WriteLine($"Will output to {targetPath.FullName}");
                if (!targetPath.Exists)
                {
                    Console.WriteLine($"{targetPath.FullName} does not exist, attempting to create...");
                    targetPath.Create();
                    Console.WriteLine($"{targetPath.FullName} created");
                }

                // 3. Iterate all files in source
                var allFiles = sourceDir.GetFiles("*", SearchOption.AllDirectories);
                Console.WriteLine($"Found {allFiles.Length} files.");
                var counter = 0;
                var linePosition = Console.CursorTop;
                var errorLinePosition = linePosition + 1;
                var sw = new Stopwatch();
                sw.Start();
                foreach (var file in allFiles)
                {
                    ClearConsoleLine(linePosition);
                    Console.WriteLine($"[{++counter}/{allFiles.Length}], time elapsed: {sw.Elapsed}, remaining: {TimeSpan.FromMilliseconds((((double)counter) / allFiles.Length) * sw.ElapsedMilliseconds)}");

                    try
                    {
                        string album;
                        uint year;
                        string artist;
                        using (var tagFile = TagLib.File.Create(file.FullName))
                        {
                            artist = GetArtist(tagFile.Tag);
                            year = tagFile.Tag.Year;
                            album = tagFile.Tag.Album ?? "Unknown";
                        }

                        var pathToMove = GetOrCreateDirectoryPath(targetPath.FullName, artist, year.ToString(), album);
                        File.Move(file.FullName,Path.Combine(pathToMove,file.Name));
                    }
                    catch (Exception e)
                    {
                        Console.SetCursorPosition(0, errorLinePosition);
                        Console.WriteLine($"{file.FullName}: Cannot process: {e.Message}");
                        errorLinePosition = Console.CursorTop + 1;
                        Console.SetCursorPosition(0, linePosition);
                    }
                }

                Console.WriteLine("Finished, press any key to exit");
                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error: {e}");
                Console.Error.WriteLine("Usage: OrganiseSongs <sourceDirectory> <targetDirectory>");
                Console.Error.WriteLine("Include full paths, i.e. OrganiseSongs C:\\MySongs C:\\Library");
                Environment.Exit(-1);
            }
        }

        private static string GetArtist(TagLib.Tag tag)
        {
#pragma warning disable 618
            return tag.JoinedAlbumArtists ?? tag.JoinedArtists ??
                   tag.JoinedPerformers ?? tag.JoinedComposers ?? "Unknown";
#pragma warning restore 618
        }

        private static void ClearConsoleLine(int linePosition)
        {
            Console.SetCursorPosition(0, linePosition);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, linePosition);
        }

        private static string GetOrCreateDirectoryPath(string root, string artist, string year, string albumTitle)
        {
            var path = $@"{root}\{SanitizeForPath(artist)}\{SanitizeForPath(year)} - {SanitizeForPath(albumTitle)}";
            Directory.CreateDirectory(path);
            return path;
        }

        private static string SanitizeForPath(string str) => Path.GetInvalidFileNameChars()
            .Aggregate(str, (current, c) => current.Replace(c, '-'));
    }
}
