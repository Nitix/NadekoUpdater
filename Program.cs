using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Threading;
using System.IO;
using NadekoUpdater.json;


namespace NadekoUpdater
{
    public class Program
    {
        private static Version LastUpdate { get; set; }

        public static void Main(string[] args)
        {
            MainAsync().Wait();
            WriteLine("***PROGRAM ENDED***", ConsoleColor.Red);
            Console.ReadKey();
        }

        private static async Task MainAsync()
        {
            WriteLine("***NADEKOBOT UPDATER***", ConsoleColor.Green);

            while (true)
            {
                if (!File.Exists("../version.txt"))
                {
                    File.WriteAllText("../version.txt", "");
                    LastUpdate = Version.DefaultVersion;
                }
                else
                {
                    var ver = File.ReadAllText("../version.txt");
                    LastUpdate = ver == "" ? Version.DefaultVersion : new Version(File.ReadAllText("../version.txt"));
                }
                WriteLine("........................................");
                Console.WriteLine("Current version release: " + LastUpdate);
                WriteLine("PICK AN OPTION: (type 1-3)", ConsoleColor.Magenta);
                WriteLine("1. Check for newest stable release.", ConsoleColor.Magenta);
                WriteLine("2. Check for any newest release.", ConsoleColor.Magenta);
                WriteLine("3. Exit", ConsoleColor.Magenta);
                var input = Console.ReadLine().Trim();
                if (string.IsNullOrWhiteSpace(input))
                    continue;
                if (input == "3")
                    break;
                try
                {
                    switch (input)
                    {
                        case "1":
                        {
                            WriteLine("Getting data...");
                            var data = await GetReleaseData("https://api.github.com/repos/Kwoth/NadekoBot/releases/latest");
                            if (ConfirmReleaseUpdate(data))
                                await Update(data);
                            break;
                        }
                        case "2":
                        {
                            WriteLine("Getting data...");
                            var data = await GetReleaseData("https://api.github.com/repos/Kwoth/NadekoBot/releases", true);
                            if (ConfirmReleaseUpdate(data))
                                await Update(data);
                            break;
                        }
                        default:
                            WriteLine("Unknown option, please try again");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unable to sync. {ex.Message}");
                }
            }
        }

        private static bool ConfirmReleaseUpdate(GithubReleaseModel data)
        {
            if (new Version(data.VersionName).CompareTo(LastUpdate) <= 0)
            {
                WriteLine("You already have an up-to-date version!", ConsoleColor.Red);
                return false;
            }
            WriteLine(
                "Newer version found!\n\nAre you sure you want to update? (y or n)\n\nYour current version will be backed up to NadekoBot_old folder. Always check the github release page to see if credentials or config files need updating",
                ConsoleColor.Magenta);
            return Console.ReadLine().ToLower() == "y" || Console.ReadLine().ToLower() == "yes";
        }

        private static async Task Update(GithubReleaseModel data)
        {
            WriteLine("........................................");
            try
            {
                var cancelSource = new CancellationTokenSource();
                var cancelToken = cancelSource.Token;
                var waitTask = Task.Run(async () => await Waiter(cancelToken));
                Console.WriteLine("Downloading. Be patient. There is no need to open an issue if it takes long.");
                var downloader = new Downloader(data);
                var arch = await downloader.Download();
                cancelSource.Cancel();
                await waitTask;
                if (Directory.Exists("../NadekoBot"))
                {
                    WriteLine("Backing up old version...", ConsoleColor.DarkYellow);
                    if (Directory.Exists("../NadekobBot_old"))
                    {
                        Directory.Delete("../NadekoBot_old", true);
                    }
                    DirectoryCopy(@"../NadekoBot", @"../NadekoBot_old", true);
                }
                WriteLine("Saving...", ConsoleColor.Green);
                arch.ExtractToDirectory(@"../NadekoBot_new");
                DirectoryCopy(@"../NadekoBot_new", @"../NadekoBot", true);

                File.WriteAllText("../version.txt", data.VersionName);
                Directory.Delete(@"../NadekoBot_new", true);
                arch.Dispose();
                WriteLine("Done!");
                
            }
            catch (Exception ex)
            {
                WriteLine(ex.ToString());
            }
        }

        private static async Task<GithubReleaseModel> GetReleaseData(string link, bool prerelease = false)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/vnd.github.v3+json");
                client.DefaultRequestHeaders.TryAddWithoutValidation("user-agent",
                    "Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0)");

                var response = await client.GetStringAsync(link);
                var release = JsonConvert.DeserializeObject<GithubReleaseModel>(!prerelease ? response : Newtonsoft.Json.Linq.JArray.Parse(response)[0].ToString());
                Console.WriteLine(
                    $"\tReleased At: {release.PublishedAt}\n\tVersion: {release.VersionName}\n\tLink: {release.Assets[0].DownloadLink}");
                return release;
            }
        }

        private static void Write(string text, ConsoleColor clr = ConsoleColor.White)
        {
            var oldClr = Console.ForegroundColor;
            Console.ForegroundColor = clr;
            Console.Write(text);
            Console.ForegroundColor = oldClr;
        }

        private static void WriteLine(string text, ConsoleColor clr = ConsoleColor.White)
        {
            Write(text + Environment.NewLine, clr);
        }

        private static async Task Waiter(CancellationToken cancel)
        {
            try
            {
                while (!cancel.IsCancellationRequested)
                {
                    Write("|");
                    Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                    await Task.Delay(333, cancel);
                    Write("/");
                    Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                    await Task.Delay(333, cancel);
                    Write("-");
                    Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                    await Task.Delay(333, cancel);
                    Write("\\");
                    Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                    await Task.Delay(333, cancel);
                }
            }
            catch (OperationCanceledException)
            {
                WriteLine("Download complete.");
            }
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}