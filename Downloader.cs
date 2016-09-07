using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NadekoUpdater.json;

namespace NadekoUpdater
{
    public class Downloader
    {
        private readonly GithubReleaseModel _releaseModel;

        private readonly string _sha1 = "";

        private const string Release = "../release.zip";

        public Downloader(GithubReleaseModel model)
        {
            _releaseModel = model;
            var match = Regex.Match(model.PatchNotes, @"SHA1: ([a-zA-Z0-9]*)");
            if (match.Success)
            {
                _sha1 = match.Value;
            }
        } 

        public async Task<ZipArchive> Download()
        {
            using (var httpClient = new HttpClient())
            {

                var nbTry = 0;
                do
                {
                    if (nbTry == 3)
                    {
                        throw new DownloadException("Can't download zip file, corrupted download after 3 attempts");
                    }
                    using (var fileStream = new FileStream(Release, FileMode.Create, FileAccess.Write))
                    using (var httpStream = await httpClient.GetStreamAsync(_releaseModel.Assets[0].DownloadLink))
                    {
                        httpStream.CopyTo(fileStream);
                        fileStream.Flush();
                    }
                    nbTry++;
                } while (!VerifyDownload());
                return new ZipArchive(new FileStream(Release, FileMode.Open, FileAccess.Read));
            }
        }

        private bool VerifyDownload()
        {
            using (var stream = new FileStream(Release, FileMode.Open, FileAccess.Read))
            using (var sha = SHA1.Create())
            {
                return _sha1 == "" || BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).Equals(_sha1, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
