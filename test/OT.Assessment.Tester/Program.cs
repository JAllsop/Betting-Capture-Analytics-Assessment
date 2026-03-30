// See https://aka.ms/new-console-template for more information

using OT.Assessment.Shared;
using System.Collections.Concurrent;

var bg = new BogusGenerator();
var total = bg.Generate();

var resultsDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data_audit");
Directory.CreateDirectory(resultsDir);
var sentFilePath = Path.Combine(resultsDir, "sent_wagers_audit.json");

var sentAudit = new ConcurrentBag<CasinoWager>();

Console.WriteLine($"Generated {total.Count} wagers and saved to {sentFilePath}");

var scenario = Scenario.Create("hello_world_scenario", async context =>
    {
        var wager = total[(int)context.InvocationNumber];
        sentAudit.Add(wager);

        var body = JsonSerializer.Serialize(wager);
        using var httpClient = new HttpClient();
        var request =
            Http.CreateRequest("POST", "https://localhost:7120/api/Player/CasinoWager")
                .WithHeader("Accept", "application/json")
                .WithBody(new StringContent($"{body}", Encoding.UTF8, "application/json"));

        var response = await Http.Send(httpClient, request);

        if (response.StatusCode == "OK") return Response.Ok();
        return Response.Fail(body, response.StatusCode, response.Message, response.SizeBytes);
    })
    .WithoutWarmUp()
    .WithLoadSimulations(
        Simulation.IterationsForInject(rate: 500,
            interval: TimeSpan.FromSeconds(2),
            iterations: 7000)
    );

NBomberRunner
    .RegisterScenarios(scenario)
    .WithWorkerPlugins(new HttpMetricsPlugin(new[] { HttpVersion.Version1 }))
    .WithoutReports()
    .Run();

var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
File.WriteAllText(sentFilePath, JsonSerializer.Serialize(sentAudit, jsonOptions));