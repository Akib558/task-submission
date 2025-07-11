using System.Collections.Concurrent;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Quantity { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class OrderRequest
{
    public int CustomerId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class OrderResponse
{
    public bool Success { get; set; }
    public OrderRequest RequestBody { get; set; }
    public string? Error { get; set; }
}



public static class Inventory
{ 
    public static List<Product> Products { get; set; } 
    public static List<Order> Orders { get; set; }
    public static List<Customer> Customers { get; set; } 
    
    static Inventory()
    {
        Random random = new Random();
        Products = new List<Product>();
        Orders = new List<Order>();
        Customers = new List<Customer>();
        
        for (int i = 0; i < 100; i++)
        {
            Products.Add(new Product { Id = i, Name = $"Product {i}", Quantity = random.Next(0, 1000000) });       
        }
        
        for (int i = 0; i < 100000; i++)
        {
            Orders.Add(new Order { Id = i, CustomerId = random.Next(0, 100), ProductId = random.Next(0, 100), Quantity = random.Next(0, 10) });       
        }

        for (int i = 0; i < 1000; i++)
        {
            Customers.Add(new Customer { Id = i, Name = $"Customer {i}" });      
        }
    }
    
}

public class DbLevelOrderService
{
    private static readonly Mutex _mutex = new Mutex(); //db level lock
    public OrderResponse PlaceOrderInDb(OrderRequest orderRequest)
    {
        _mutex.WaitOne();
        var product = Inventory.Products.FirstOrDefault(x => x.Id == orderRequest.ProductId);
        if (product == null)
        {
            _mutex.ReleaseMutex();
            return new OrderResponse()
            {
                Success = false,
                RequestBody = orderRequest,
                Error = "Product not found"
            };
        }

        if (product.Quantity < orderRequest.Quantity)
        {
            _mutex.ReleaseMutex();
            
            return new OrderResponse()
            {
                Success = false,
                RequestBody = orderRequest,
                Error = "Not enough quantity"
            };
        }
        
        product.Quantity -= orderRequest.Quantity;
        
        _mutex.ReleaseMutex();
        
        return new OrderResponse()
        {
            Success = true,
            RequestBody = orderRequest,
            Error = null
        };
        
    }
}

public class InventoryService
{
    private readonly Mutex _mutex = new Mutex() // server level lock
    SemaphoreSlim semaphore = new SemaphoreSlim(1, 1000);

    public OrderResponse PlaceOrder(OrderRequest orderRequest)
    {
        semaphore.Wait();
        _mutex.WaitOne();
        
        var dbLevelOrderService = new DbLevelOrderService();
        var response = dbLevelOrderService.PlaceOrderInDb(orderRequest);
        
        _mutex.ReleaseMutex();
        semaphore.Release();
        
        return response;
        
        
    }
}

public class Program
{

    public static void PlaceOrder()
    {
        
        var totalProductBefore = Inventory.Products.Sum(x => x.Quantity);
        Console.WriteLine($"Total products before: {totalProductBefore}");

        var orderResponses = new ConcurrentBag<OrderResponse>();

        var allOrderRequests = new List<OrderRequest>();

        for (int i = 1; i < 1000; i++)
        {
            for (int j = i; j < i * 10 && j < Inventory.Orders.Count; j++)
            {
                allOrderRequests.Add(new OrderRequest
                {
                    CustomerId = Inventory.Orders[j].CustomerId,
                    ProductId = Inventory.Orders[j].ProductId,
                    Quantity = Inventory.Orders[j].Quantity
                });
            }
        }
        
        // Simulating multiple servers
        var servers = new List<InventoryService>();

        for (int i = 0; i < 20; i++)
        {
            servers.Add(new InventoryService());
        }
        
        // simulating multi request
        Parallel.ForEach(allOrderRequests, orderRequest =>
        {
            Random rnd = new Random();
            var randomService = servers[rnd.Next(servers.Count)];
            var response = randomService.PlaceOrder(orderRequest);
            orderResponses.Add(response);
            
        });

        var totalProductAfter = Inventory.Products.Sum(x => x.Quantity);

        // Defensive: filter out any nulls
        var successfulOrders = orderResponses.Where(x => x != null && x.Success && x.RequestBody != null).ToList();
        var failedOrders = orderResponses.Where(x => x != null && !x.Success).ToList();

        var successOrdersProductSum = successfulOrders.Sum(x => x.RequestBody.Quantity);

        Console.WriteLine($"Total products after: {totalProductAfter}");
        Console.WriteLine($"Total success order product count: {successOrdersProductSum}");
        Console.WriteLine($"Total success order product count should be: {totalProductBefore - totalProductAfter}");
        Console.WriteLine($"Product count mismatch: {successOrdersProductSum - (totalProductBefore - totalProductAfter)}");
        Console.WriteLine($"Total successful orders: {successfulOrders.Count}");
        Console.WriteLine($"Total failed orders: {failedOrders.Count}");
    }

    public static void Main(string[] args)
    {
        PlaceOrder();
    }
}


/*
 * Without distributed lock:
 *
 * Total products before: 52075923
   Total products after: 33670312
   Total success order product count: 18405611
   Total success order product count should be: 18405611
   Product count mismatch: 0
   Total successful orders: 4133221
   Total failed orders: 362279
   
 *
 *
 *
 * Without distributed lock:
 *
 *
 * Total products before: 50879882
   Total products after: 32070297
   Total success order product count: 18811604
   Total success order product count should be: 18809585
   Product count mismatch: 2019
   Total successful orders: 4193685
   Total failed orders: 301815
   
 *
 *
 *
 * Without server level lock:
 *
 * Total products before: 50199996
   Total products after: 31782833
   Total success order product count: 18445406
   Total success order product count should be: 18417163
   Product count mismatch: 28243
   Total successful orders: 4165886
   Total failed orders: 329614
 * 
 */