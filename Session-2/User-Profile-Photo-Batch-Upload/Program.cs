namespace User_Profile_Photo_Batch_Upload;

public class UploadedPhoto
{
    public string FileName { get; set; } = null!;
    public byte[] Content { get; set; } = null!;
}

public class UserUploadRequest
{
    public int UserId { get; set; }
    public List<UploadedPhoto> Photos { get; set; } = new();
}

public class TestRunner
{
    private readonly IImageProcessor _imageProcessor;
    
    public TestRunner(IImageProcessor imageProcessor)
    {
        _imageProcessor = imageProcessor;
    }
    public List<UserUploadRequest> Generate(int userCount, string sampleImageFolder)
    {
        var imageFiles = Directory.GetFiles(sampleImageFolder, "*.jpg");
        if (imageFiles.Length == 0)
            throw new Exception("No images found in the sample image folder.");

        var userUploadRequests = new List<UserUploadRequest>();

        for (int i = 0; i < userCount; i++)
        {
            var user = new UserUploadRequest { UserId = i };

            var selectedImages = imageFiles
                .OrderBy(_ => Guid.NewGuid())
                .Take(Random.Shared.Next(1, 7));

            foreach (var imagePath in selectedImages)
            {
                var uploadedPhoto = new UploadedPhoto
                {
                    FileName = Path.GetFileName(imagePath),
                    Content = File.ReadAllBytes(imagePath)
                };

                user.Photos.Add(uploadedPhoto);
            }

            userUploadRequests.Add(user);
        }

        return userUploadRequests;
    }

    
    public async Task RunTest(int userCount = 10000, string imageFolder = "photo-collections", int worker = 1000, int workerCapacity = 1000, CancellationToken cancellationToken = default)
    {
        var sampleFolder = Path.Combine(Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName, imageFolder);
        var usersUploadRequests = Generate(userCount: userCount, sampleImageFolder: sampleFolder);

        usersUploadRequests.ForEach(request =>
        {
            Console.WriteLine($"[User {request.UserId}]\t uploaded\t {request.Photos.Count}\t images.");
        });

        Console.WriteLine($"Generated {usersUploadRequests.Count} users with uploaded images.\n");
        
        using var semaphore = new SemaphoreSlim(worker);
        
        
        var tasks = usersUploadRequests.Select(async request =>
        {
            await semaphore.WaitAsync();
            try
            {
                await _imageProcessor.ProcessUserImagesAsync(request, workerCapacity, cancellationToken);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[User {request.UserId}] Error processing images: {e.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        Console.WriteLine("==== All users photos have been uploaded and processed. ====");
    }
}


public interface IImageService
{
    Task<bool> UploadImageAsync(int userId, UploadedPhoto photo, CancellationToken cancellationToken = default);
    Task<bool> RenameImageAsync(int userId, UploadedPhoto photo, CancellationToken cancellationToken = default);
    Task<bool> ProcessImageAsync(int userId, UploadedPhoto photo, CancellationToken cancellationToken = default);
}


public interface IFileService
{
    Task<bool> UploadFileAsync(int userId, UploadedPhoto photo, CancellationToken cancellationToken = default);
    Task<bool> CompressFileAsync(int userId, UploadedPhoto photo, CancellationToken cancellationToken = default);
    Task<bool> RenameFileAsync(int userId, UploadedPhoto photo, CancellationToken cancellationToken = default);
}

public interface IImageProcessor
{
    Task ProcessUserImagesAsync(UserUploadRequest request, int maxDegreeOfParallelism, CancellationToken cancellationToken = default);
}


public class FileService : IFileService
{
    public async Task<bool> UploadFileAsync(int userId, UploadedPhoto photo, CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"[User {userId}] Starting upload of {photo.FileName} on thread {Environment.CurrentManagedThreadId}");
            await Task.Delay(Random.Shared.Next(200, 1000), cancellationToken);;
            Console.WriteLine($"[User {userId}] Finished upload of {photo.FileName}");
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[User {userId}] Error uploading {photo.FileName}: {e.Message}");
            return false;
        }
    }
    
    public async Task<bool> CompressFileAsync(int userId, UploadedPhoto photo, CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"[User {userId}] Starting processing of {photo.FileName} on thread {Environment.CurrentManagedThreadId}");
            await Task.Delay(Random.Shared.Next(200, 1000), cancellationToken);
            Console.WriteLine($"[User {userId}] Finished processing {photo.FileName}");
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[User {userId}] Error processing {photo.FileName}: {e.Message}");
            return false;
        }
    }
    
    public async Task<bool> RenameFileAsync(int userId, UploadedPhoto photo, CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine(
                $"[User {userId}] Starting rename of {photo.FileName} on thread {Environment.CurrentManagedThreadId}");
            await Task.Delay(Random.Shared.Next(200, 1000), cancellationToken);
            Console.WriteLine($"[User {userId}] Finished rename of {photo.FileName}");
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[User {userId}] Error renaming {photo.FileName}: {e.Message}");
            return false;
        }
    }
}

public class ImageService : IImageService
{
    private readonly IFileService _fileService;
    
    public ImageService(IFileService fileService)
    {
        _fileService = fileService;
    }
    
    public async Task<bool> UploadImageAsync(int userId, UploadedPhoto photo, CancellationToken cancellationToken = default)
    {
        await _fileService.UploadFileAsync(userId, photo, cancellationToken);
        return true;
    }

    public async Task<bool> RenameImageAsync(int userId, UploadedPhoto photo, CancellationToken cancellationToken = default)
    {
        await _fileService.RenameFileAsync(userId, photo, cancellationToken);
        return true;
    }

    public async Task<bool> ProcessImageAsync(int userId, UploadedPhoto photo, CancellationToken cancellationToken = default)
    {
        await _fileService.CompressFileAsync(userId, photo, cancellationToken);
        return true;
    }
}

public class ImageProcessor : IImageProcessor
{

    private readonly IImageService _imageService;

    public ImageProcessor(IImageService imageService)
    {
        _imageService = imageService;
    }

    public async Task ProcessUserImagesAsync(UserUploadRequest request, int maxDegreeOfParallelism, CancellationToken cancellationToken = default)
    {
        using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);

        var tasks = request.Photos.Select(async photo =>
        {
            await semaphore.WaitAsync();
            
            var success = await _imageService.RenameImageAsync(request.UserId, photo, cancellationToken);

            if (success)
            {
                success = await _imageService.ProcessImageAsync(request.UserId, photo, cancellationToken);
            }
            if (success)
            {
                success = await _imageService.UploadImageAsync(request.UserId, photo, cancellationToken);
            }
            
            semaphore.Release();
            return success;
        });
        
        await Task.WhenAll(tasks);
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        using var cts = new CancellationTokenSource();

        // Start a background task to cancel after 3 seconds
        _ = Task.Run(() =>
        {
            Thread.Sleep(3000);
            cts.Cancel();
            Console.WriteLine("Cancellation requested! -----------------------------------------------------------------------------");
        });

        var testRunner = new TestRunner(new ImageProcessor(new ImageService(new FileService())));

        try
        {
            await testRunner.RunTest(
                userCount: 20,
                imageFolder: "photo-collections",
                worker: 5,
                workerCapacity: 3,
                cancellationToken: cts.Token
            );
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("The operation was cancelled.");
        }
        
    }

}
