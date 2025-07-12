using System.Collections.Concurrent;

public class Document
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Content { get; set; }
    public int AuthorId { get; set; }
}

public record DocumentSnapshot(
    int Id, 
    string Name, 
    string Content, 
    int AuthorId
    );

public class Users
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class UserDocument
{
    public int Id { get; set; }
    public Users User { get; set; }
    public Document Document { get; set; }
    public SaverService DocumentSaver { get; set; }
}

public static class DocumentProvider
{
    public static List<Document> Documents { get; set; } = new();
}

public class DocumentQueueManager
{
    public readonly ConcurrentQueue<DocumentSnapshot> _queue = new();

    public void Enqueue(DocumentSnapshot document)
    {
        _queue.Enqueue(document);
    }
    
    public bool TryDequeue(out DocumentSnapshot snapshot)
    {
        return _queue.TryDequeue(out snapshot);
    }
}

public class DocumentRepository
{
    public readonly object _lock = new();

    public bool SaveToDb(DocumentSnapshot document)
    {
        lock (_lock)
        {
            var dbDoc = DocumentProvider.Documents.Where(x => x.Id == document.Id).FirstOrDefault();
            if (dbDoc == null)
            {
                return false;
            }
            dbDoc.Name = document.Name;
            dbDoc.Content = document.Content;
            dbDoc.AuthorId = document.AuthorId;
            Log.Logs.Add($"Document {document.Id}. Document Content: {document.Content}");
            return true;       
        }
    }
}

public class DocumentWorker
{
    private readonly DocumentRepository _repository;
    private readonly DocumentQueueManager _queue;
    private readonly CancellationToken _token;

    public DocumentWorker(DocumentRepository repository, DocumentQueueManager queue, CancellationToken token)
    {
        _repository = repository;
        _queue = queue;
        _token = token;
    }

    public void Start()
    {
        Task.Run(() => Execute(), _token);
    }

    public void Execute()
    {
        while (!_token.IsCancellationRequested)
        {
            if(_queue.TryDequeue(out var document))
            {
                _repository.SaveToDb(document);
            }
            else
            {
                Thread.Sleep(10);
            }
        }
    }
}



public class SaverService
{
    private DocumentQueueManager _documentQueueManager;
    private DocumentSnapshot _document;
    private readonly int _interval;

    public SaverService(DocumentQueueManager documentQueueManager, DocumentSnapshot document, int interval = 1000)
    {
        _documentQueueManager = documentQueueManager;
        _document = document;
        _interval = interval;
    }
 
    public void AutoSave()
    {
        while (true)
        {
            Thread.Sleep(_interval);
            var snapshot = _document with { Content = $"[AutoSave]    {DateTime.Now:HH:mm:ss.fff}" };
            _documentQueueManager.Enqueue(snapshot);
        }
    }

    public void ManualSave()
    {
        while (true)
        {
            Thread.Sleep(1000);
            var snapshot = _document with { Content = $"[ManualSave]  {DateTime.Now:HH:mm:ss.fff}" };
            _documentQueueManager.Enqueue(snapshot);
        }
    }

}

public static class Log
{
    public static List<string> Logs { get; set; } = new();
}

public class Test
{
    public void Run(int userCount)
    {
        var tokenSource = new CancellationTokenSource();
        var users = new List<Users>();
        var queueManager = new DocumentQueueManager();
        var repository = new DocumentRepository();
        var worker = new DocumentWorker(repository,queueManager, tokenSource.Token);
        worker.Start();

        for (int i = 0; i < userCount; i++)
        {
            users.Add(new Users { Id = i, Name = $"User{i}" });
            var document = new Document { Id = i };
            DocumentProvider.Documents.Add(document);
            var documentSnapshot = new DocumentSnapshot(i, $"", $"", i);
            var saver = new SaverService(queueManager, documentSnapshot);
            Task.Run(saver.AutoSave);
            Task.Run(saver.ManualSave);
        }

        Thread.Sleep(10000);
        tokenSource.Cancel();

        foreach (var log in Log.Logs)
        {
            Console.WriteLine(log);
        }
    }
}



public class Program
{
    public static void Main(string[] args)
    {
        new Test().Run(1);
    }
}