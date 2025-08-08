using System.Collections.Concurrent;
using System.Diagnostics;

public class UserRequest
{
    public string Name { get; set; }
    public string Resolution { get; set; }
    public string InputFilePath { get; set; }
}

public class UserResponse
{
    public string Name { get; set; }
    public string Resolution { get; set; }
    public string VideoUrl { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
}

public class FileInfo
{
    public string UniqueId { get; set; }
    public string Filename { get; set; }
    public string Resolution { get; set; }
    public string PreConvertUrl { get; set; }
}

public class VideoUploader
{
    static SemaphoreSlim semaphore = new SemaphoreSlim(100);
    static string Now() => DateTime.Now.ToString("HH:mm:ss.fff");

    public static async Task<(bool IsSuccessful, string fileUrl, string message)> UploadVideo(FileInfo file, string path)
    {
        try
        {
            await semaphore.WaitAsync();

            Console.WriteLine($"[{Now()}] Uploading: {file.Filename}");
            await Task.Delay(1000);

            string simulatedUrl = $"simulated://uploaded/{file.Filename}";

            Console.WriteLine($"[{Now()}] Upload complete: {file.Filename}");
            return (true, simulatedUrl, "Upload simulated successfully");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[{Now()}] Upload failed: {file.Filename} - {e.Message}");
            return (false, null, e.Message);
        }
        finally
        {
            semaphore.Release();
        }
    }
}

public static class VideoConverter
{
    static SemaphoreSlim semaphore = new SemaphoreSlim(100);
    static string Now() => DateTime.Now.ToString("HH:mm:ss.fff");

    public static async Task<(bool IsSuccessful, string OutputPath, string Message)> ConvertVideo(FileInfo file, string inputPath)
    {
        try
        {
            await semaphore.WaitAsync();

            Console.WriteLine($"[{Now()}] Converting: {file.Filename} to {file.Resolution}");
            int delay = 5000;

            await Task.Delay(delay);
            string simulatedOutput = $"simulated://converted/{file.UniqueId}-{file.Resolution}.mp4";

            Console.WriteLine($"[{Now()}] Conversion complete: {file.Filename} (delay: {delay}ms)");
            return (true, simulatedOutput, "Conversion simulated successfully");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[{Now()}] Conversion failed: {file.Filename} - {e.Message}");
            return (false, null, e.Message);
        }
        finally
        {
            semaphore.Release();
        }
    }
}

// Test Runner
public static class Test
{
    public static async Task Run()
    {
        var users = new List<UserRequest>();

        for (int i = 0; i < 30; i++)
        {
            users.Add(new UserRequest
            {
                Name = $"User-{i + 1}",
                InputFilePath = $"Users/{i + 1}/Video.mp4",
                Resolution = "1080p"
            });
        }

        var results = new ConcurrentBag<UserResponse>();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 1000
        };

        await Parallel.ForEachAsync(users, options, async (user, _) =>
        {
            Console.WriteLine($"\n[{Now()}] Processing: {user.Name} | {user.Resolution}");

            var file = new FileInfo
            {
                UniqueId = Guid.NewGuid().ToString(),
                Filename = $"{user.Name}-{user.Resolution}.mp4",
                Resolution = user.Resolution
            };

            var upload = await VideoUploader.UploadVideo(file, user.InputFilePath);
            if (!upload.IsSuccessful)
            {
                results.Add(new UserResponse { Name = user.Name, Resolution = user.Resolution, Success = false, Message = "Initial upload failed" });
                return;
            }

            var convert = await VideoConverter.ConvertVideo(file, upload.fileUrl);
            if (!convert.IsSuccessful)
            {
                results.Add(new UserResponse { Name = user.Name, Resolution = user.Resolution, Success = false, Message = "Conversion failed" });
                return;
            }

            var uploadConverted = await VideoUploader.UploadVideo(file, convert.OutputPath);
            if (!uploadConverted.IsSuccessful)
            {
                results.Add(new UserResponse { Name = user.Name, Resolution = user.Resolution, Success = false, Message = "Converted upload failed" });
                return;
            }

            results.Add(new UserResponse
            {
                Name = user.Name,
                Resolution = user.Resolution,
                Success = true,
                VideoUrl = uploadConverted.fileUrl,
                Message = "Completed"
            });

            Console.WriteLine($"[{Now()}] Done: {user.Name} ({user.Resolution})\n");
        });

        Console.WriteLine("\n=== FINAL RESULTS ===");
        foreach (var r in results.OrderBy(r => r.Name))
        {
            Console.WriteLine($"[{(r.Success ? "Success" : "Fail")}] {r.Name} - {r.Resolution} - {r.Message} - {r.VideoUrl}");
        }
    }

    static string Now() => DateTime.Now.ToString("HH:mm:ss.fff");
}

public class Program
{
    public static async Task Main(string[] args)
    {
        var stopwatch = Stopwatch.StartNew();

        await Test.Run();

        stopwatch.Stop();
        Console.WriteLine($"Total execution time: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
    }
}
