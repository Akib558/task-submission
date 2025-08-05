using System.Text;

public interface ISubtitleProvider
{
    string Language { get; }
    Task<string> DownloadSubtitleAsync(string movieId, CancellationToken token);
}

public class SubtitleProviderA : ISubtitleProvider
{
    public string Language => "English";
    public async Task<string> DownloadSubtitleAsync(string movieId, CancellationToken token)
    {
        await Task.Delay(1000, token); 
        return $"[English Subtitle for {movieId}]";
    }
}

public class SubtitleProviderB : ISubtitleProvider
{
    public string Language => "Spanish";
    public async Task<string> DownloadSubtitleAsync(string movieId, CancellationToken token)
    {
        await Task.Delay(800, token); 
        return $"[Spanish Subtitle for {movieId}]";
    }
}

public class SubtitleProviderC : ISubtitleProvider
{
    public string Language => "French";
    public async Task<string> DownloadSubtitleAsync(string movieId, CancellationToken token)
    {
        await Task.Delay(1200, token); 
        return $"[French Subtitle for {movieId}]";
    }
}

public class SubtitleDownloader
{
    private readonly IEnumerable<ISubtitleProvider> _providers;
    private readonly int _maxParallel;

    public SubtitleDownloader(IEnumerable<ISubtitleProvider> providers, int maxParallel = 2)
    {
        _providers = providers;
        _maxParallel = maxParallel;
    }

    public async Task<List<(string language, string content)>> DownloadAllAsync(string movieId, CancellationToken token)
    {
        var results = new List<(string, string)>();
        var semaphore = new SemaphoreSlim(_maxParallel);

        var tasks = _providers.Select(async provider =>
        {
            await semaphore.WaitAsync(token);
            try
            {
                var content = await provider.DownloadSubtitleAsync(movieId, token);
                lock (results)
                {
                    results.Add((provider.Language, content));
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return results;
    }
}

public static class SubtitleMerger
{
    public static string Merge(List<(string language, string content)> subtitles)
    {
        var sb = new StringBuilder();
        foreach (var (lang, content) in subtitles)
        {
            sb.AppendLine($"--- {lang} ---");
            sb.AppendLine(content);
            sb.AppendLine();
        }

        return sb.ToString();
    }
}

public class TestRunner
{
    public static async Task RunAsync()
    {
        string movieId = "MOV123";
        var providers = new List<ISubtitleProvider>
        {
            new SubtitleProviderA(),
            new SubtitleProviderB(),
            new SubtitleProviderC()
        };

        var downloader = new SubtitleDownloader(providers, maxParallel: 2);
        var cts = new CancellationTokenSource();

        try
        {
            var subtitles = await downloader.DownloadAllAsync(movieId, cts.Token);
            string merged = SubtitleMerger.Merge(subtitles);

            // Save or display the result
            Console.WriteLine("Subtitles downloaded and merged successfully:\n");
            Console.WriteLine(merged);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Download was cancelled.");
        }
    }
}

public class Program
{
    public static async Task Main()
    {
        await TestRunner.RunAsync();
    }
}
