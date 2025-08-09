using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(_ => true);
    });
});

var app = builder.Build();

app.UseCors();


var questionBank = Enumerable.Range(1, 20)
    .Select(i => $"Question {i}")
    .ToList();
var preparedSets = new ConcurrentDictionary<string, List<string>>();
var random = new Random();

app.MapHub<ExamHub>("/examHub");

app.Run();

public class ExamHub : Hub
{
    private static List<string> _questionBank = Enumerable.Range(1, 1000)
        .Select(i => $"Question {i}")
        .ToList();

    private static ConcurrentDictionary<string, List<string>> _preparedSets = new();
    private static Random _random = new();

    public override async Task OnConnectedAsync()
    {
        string studentId = Context.ConnectionId;
        
        var set = Enumerable.Range(1, 5).Select(i => _questionBank[_random.Next(0, _questionBank.Count)]).ToList();

        _preparedSets[studentId] = set;

        await Clients.Caller.SendAsync("PreparationComplete", "Your questions are ready!");
        await base.OnConnectedAsync();
    }

    public async Task StartExam()
    {
        string studentId = Context.ConnectionId;

        if (_preparedSets.TryGetValue(studentId, out var set))
        {
            await Clients.Caller.SendAsync("ReceiveQuestions", set);
        }
        else
        {
            await Clients.Caller.SendAsync("ReceiveQuestions", new List<string> { "No questions prepared!" });
        }
    }
}