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
    static SemaphoreSlim semaphore = new SemaphoreSlim(10, 1000);
    static string Now() => DateTime.Now.ToString("HH:mm:ss.fff");

    public static async Task<(bool IsSuccessful, string fileUrl, string message)> UploadVideo(FileInfo file, string path)
    {
        try
        {
            await semaphore.WaitAsync();
            Console.WriteLine($"[{Now()}] ⬆ Uploading: {file.Filename}");

            await Task.Delay(Random.Shared.Next(100, 300)); // Simulate network delay

            var storageDir = Path.Combine(Directory.GetCurrentDirectory(), "../../../Uploaded");
            Directory.CreateDirectory(storageDir);

            var destPath = Path.Combine(storageDir, file.Filename);
            File.Copy(path, destPath, overwrite: true);

            Console.WriteLine($"[{Now()}] ✅ Upload completed: {file.Filename} → {destPath}");
            return (true, destPath, "Uploaded successfully");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[{Now()}] ❌ Upload failed: {file.Filename} - {e.Message}");
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
    static SemaphoreSlim semaphore = new SemaphoreSlim(5, 100);
    static string Now() => DateTime.Now.ToString("HH:mm:ss.fff");

    public static async Task<(bool IsSuccessful, string OutputPath, string Message)> ConvertVideo(FileInfo file, string inputPath)
    {
        try
        {
            // await semaphore.WaitAsync();

            var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "../../../Converted");
            Directory.CreateDirectory(outputDir);

            var outputPath = Path.Combine(outputDir, $"{file.UniqueId}-{file.Resolution}.mp4");

            string scale = file.Resolution switch
            {
                "1080p" => "1920:1080",
                "720p" => "1280:720",
                "480p" => "854:480",
                "360p" => "640:360",
                _ => "1280:720"
            };

            Console.WriteLine($"[{Now()}] 🔄 Converting: {file.Filename} to {file.Resolution}...");

            var args = $"-i \"{inputPath}\" -vf scale={scale} -c:a copy \"{outputPath}\" -y";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.StandardError.ReadToEndAsync(); // Optional: capture logs
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                Console.WriteLine($"[{Now()}] ✅ Conversion completed: {outputPath}");
                return (true, outputPath, "Converted successfully");
            }
            else
            {
                Console.WriteLine($"[{Now()}] ❌ Conversion failed: {file.Filename}");
                return (false, null, $"FFmpeg failed with exit code {process.ExitCode}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"[{Now()}] ❌ Conversion error: {file.Filename} - {e.Message}");
            return (false, null, e.Message);
        }
        finally
        {
            // semaphore.Release();
        }
    }
}

// Test
public static class Test
{
    public static async Task Run()
    {
        var sampleVideo = "../../../sample.mp4";
        if (!File.Exists(sampleVideo))
        {
            Console.WriteLine("❌ Missing sample.mp4. Please place it in the correct folder.");
            return;
        }

        var resolutions = new[] { "1080p", "720p", "480p", "360p" };
        var users = new List<UserRequest>();

        for (int i = 0; i < 10; i++)
        {
            users.Add(new UserRequest
            {
                Name = $"User-{i + 1}",
                // Resolution = resolutions[Random.Shared.Next(resolutions.Length)],
                InputFilePath = sampleVideo,
                Resolution = "720p"
            });
        }

        var results = new ConcurrentBag<UserResponse>();

        await Parallel.ForEachAsync(users, async (user, _) =>
        {
            Console.WriteLine($"\n[{Now()}] 🧑 Processing: {user.Name} | {user.Resolution}");

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

            Console.WriteLine($"[{Now()}] 🟢 Done: {user.Name} ({user.Resolution})\n");
        });

        Console.WriteLine("\n=== ✅ FINAL RESULTS ===");
        foreach (var r in results.OrderBy(r => r.Name))
        {
            Console.WriteLine($"[{(r.Success ? "✓" : "✗")}] {r.Name} - {r.Resolution} - {r.Message} - {r.VideoUrl}");
        }
    }

    static string Now() => DateTime.Now.ToString("HH:mm:ss.fff");
}

// Entry
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
