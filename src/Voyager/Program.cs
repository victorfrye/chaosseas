using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using VictorFrye.ChaosSeas.Extensions.ServiceDefaults;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddHttpClient("Voyager", static client =>
{
    client.BaseAddress = new Uri("https+http://harbor-api");
    client.Timeout = TimeSpan.FromSeconds(30);
});

IHost host = builder.Build();
await host.StartAsync();

IHttpClientFactory factory = host.Services.GetRequiredService<IHttpClientFactory>();
HttpClient client = factory.CreateClient("Voyager");

// Parse arguments
string scenario = args.Length > 0 ? args[0] : "calm-seas";
int requestCount = args.Length > 1 && int.TryParse(args[1], out int count) ? count : 10;

string path = scenario switch
{
    "calm-seas" => "/voyage/calm-seas",
    "rough-seas-faults" => "/voyage/rough-seas/faults",
    "rough-seas-latency" => "/voyage/rough-seas/latency",
    "rough-seas-outcomes" => "/voyage/rough-seas/outcomes",
    "game-day" => "/voyage/game-day",
    _ => "/voyage/calm-seas"
};

Console.WriteLine();
Console.WriteLine("🏴‍☠️ ═══════════════════════════════════════════");
Console.WriteLine($"   VOYAGER — Chaos Seas Navigator");
Console.WriteLine("═══════════════════════════════════════════════");
Console.WriteLine($"   Scenario:  {scenario}");
Console.WriteLine($"   Voyages:   {requestCount}");
Console.WriteLine($"   Target:    {path}");
Console.WriteLine("═══════════════════════════════════════════════");
Console.WriteLine();

int successes = 0;
int failures = 0;
List<long> latencies = [];
Stopwatch totalStopwatch = Stopwatch.StartNew();

for (int i = 1; i <= requestCount; i++)
{
    Stopwatch sw = Stopwatch.StartNew();

    try
    {
        HttpResponseMessage response = await client.GetAsync(path);
        sw.Stop();
        latencies.Add(sw.ElapsedMilliseconds);

        if (response.IsSuccessStatusCode)
        {
            successes++;
            Console.WriteLine($"  ✅ Voyage {i,3}: {sw.ElapsedMilliseconds,5}ms — Ship returned safely!");
        }
        else
        {
            failures++;
            Console.WriteLine($"  ❌ Voyage {i,3}: {sw.ElapsedMilliseconds,5}ms — HTTP {(int)response.StatusCode} — Ship lost at sea!");
        }
    }
    catch (Exception ex)
    {
        sw.Stop();
        latencies.Add(sw.ElapsedMilliseconds);
        failures++;
        Console.WriteLine($"  💀 Voyage {i,3}: {sw.ElapsedMilliseconds,5}ms — {ex.GetType().Name}: {ex.Message[..Math.Min(60, ex.Message.Length)]}");
    }
}

totalStopwatch.Stop();

// Summary
Console.WriteLine();
Console.WriteLine("🏴‍☠️ ═══════════════════════════════════════════");
Console.WriteLine("   VOYAGE SUMMARY");
Console.WriteLine("═══════════════════════════════════════════════");
Console.WriteLine($"   Total voyages:    {requestCount}");
Console.WriteLine($"   Ships returned:   {successes} ✅");
Console.WriteLine($"   Ships lost:       {failures} ❌");
Console.WriteLine($"   Success rate:     {(requestCount > 0 ? (double)successes / requestCount * 100 : 0):F1}%");

if (latencies.Count > 0)
{
    latencies.Sort();
    Console.WriteLine($"   Avg latency:      {latencies.Average():F0}ms");
    Console.WriteLine($"   Min latency:      {latencies[0]}ms");
    Console.WriteLine($"   Max latency:      {latencies[^1]}ms");
    int p95Index = (int)Math.Ceiling(latencies.Count * 0.95) - 1;
    Console.WriteLine($"   P95 latency:      {latencies[Math.Max(0, p95Index)]}ms");
}

Console.WriteLine($"   Total time:       {totalStopwatch.ElapsedMilliseconds}ms");
Console.WriteLine("═══════════════════════════════════════════════");
Console.WriteLine();

await host.StopAsync();
