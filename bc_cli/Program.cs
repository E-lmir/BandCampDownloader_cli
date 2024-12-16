using BCDownloader;
using SkiaSharp;

namespace bc_cli
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var client = new HttpClient();
            var downloader = new Downloader(client);
            const int SkiaSharpMaxQuality = 100;

            var tokenSource = new CancellationTokenSource();
            var ct = tokenSource.Token;
            try
            {
                var task = Task.Run(() =>
                {
                    while (true)
                    {
                        Console.Write(".");
                        Thread.Sleep(50); 
                        ct.ThrowIfCancellationRequested();
                    }
                }, tokenSource.Token);

                var albumInfo = await downloader.GetAlbumInfoAsync(args[0]);
                if (albumInfo is not null)
                {
                    var directoryInfo = Directory.CreateDirectory(albumInfo.Artist);
                    directoryInfo = albumInfo?.Title is null
                        ? directoryInfo
                        : Directory.CreateDirectory(Path.Combine(directoryInfo.FullName, albumInfo.Title.Title));

                    using (var image = SKBitmap.Decode(albumInfo.CoverData))
                    {
                        var returnImage = image.Encode(SKEncodedImageFormat.Jpeg, SkiaSharpMaxQuality);

                        using (var data = File.Create(Path.Combine(directoryInfo.FullName, $"{albumInfo.Title?.Title ?? albumInfo.Artist}.jpeg")))
                        {
                            returnImage.SaveTo(data);
                        }
                    }

                    await Parallel.ForEachAsync(albumInfo.TrackInfo, async (track, cts) =>
                    {
                        await File.WriteAllBytesAsync(Path.Combine(directoryInfo.FullName, $"{track.Title}.mp3"), track.Data);
                    });
                }

                tokenSource.Cancel();
            }
            catch (OperationCanceledException) { }
        }
    }
}
