using System.Collections.Concurrent;

public class Emailer
{
    public async Task<bool> Emailio()
    {
        await Task.Delay(100);
        
        double number = Random.Shared.NextDouble();
        
        if (number < 0.1)
        {
            throw new Exception("Something went wrong");
        }
        
        return true;
    }
}

public class EmailResponse
{
    public string Body { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public class Customer
{
    public int Id { get; set; }
    public string Email { get; set; }
}

public class EmailService
{
    public async Task<EmailResponse> Send(string email)
    {
        try
        {
            var ioOperation = new Emailer();
            var res = await ioOperation.Emailio();
            return new EmailResponse()
            {
                Body = "Email sent",
                Success = res,
                Error = null
            };
        }
        catch (Exception e)
        {
            return new EmailResponse()
            {
                Body = "Email not sent",
                Success = false,
                Error = e.Message
            };
        }
    }


    public async Task<List<EmailResponse>> SendEmails(List<Customer> customers)
    {
        int maxConcurrent = 10;
        var semaphore = new SemaphoreSlim(maxConcurrent);
        var tasks = new List<Task>();
        var emails = new ConcurrentBag<EmailResponse>();
        
        
        foreach (var customer in customers)
        {
            await semaphore.WaitAsync();

            tasks.Add(Task.Run( async () =>
            {
                try
                {
                    var emailResponse = await Send(customer.Email);
                    emails.Add(emailResponse);
                }
                finally
                {
                    semaphore.Release();
                }
                
            }));
        }
        
        await Task.WhenAll(tasks);
        return emails.ToList();
    }
}


public class User
{
    public List<Customer> GetTodayUsers()
    {
        var customers = new List<Customer>();
        for (int i = 0; i < 100; i++)
        {
            customers.Add(new Customer { Id = i, Email = $"user{i}@{i}.com" });
        }
        return customers;
    }
}






public class Program
{
    public static async Task Main(string[] args)
    {
        var  user = new User();
        var customers = user.GetTodayUsers();
        var emailService = new EmailService();
        var emails = await emailService.SendEmails(customers);
        foreach(var email in emails)
        {
            Console.WriteLine(email.Body);
        }
        Console.WriteLine("Done");
    }
}