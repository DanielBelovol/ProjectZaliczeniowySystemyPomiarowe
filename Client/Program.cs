using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5100");
var app = builder.Build();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/api/telemetry")
    {
        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var apiKey) || apiKey != "TajneHaslo")
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("401 Unauthorized");
            return;
        }
    }
    await next(context);
});

app.MapPost("/api/telemetry", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    
    try {
        var payload = JsonSerializer.Deserialize<JsonElement>(body);
        float distance = payload.GetProperty("distance").GetSingle();
        float temperature = payload.GetProperty("temperature").GetSingle();
        
        bool isDoorOpen = distance > 100.0f; 
        bool isTempBad = temperature < 2.0f || temperature > 8.0f; 

        Console.WriteLine($"[TELEMETRY] Temp: {temperature:F1}°C, Distance: {distance:F1}cm");

        if (VaultState.Current == SystemState.CriticalIncident || VaultState.Current == SystemState.ColdChainBreach)
            return Results.Ok(new { status = "System locked. Manual reset required." });

        DateTime now = DateTime.Now;

        if (isDoorOpen && isTempBad)
        {
            var newState = VaultState.IsOutOfOffice(now) ? SystemState.ColdChainBreach : SystemState.CriticalIncident; 
            VaultState.LogIncident("Both sensors triggered simultaneously", newState);
            if (newState == SystemState.ColdChainBreach) Console.WriteLine("[CRITICAL ANOMALY] COLD CHAIN BREACH");
        }
        else if (isDoorOpen || isTempBad)
        {
            string reason = isDoorOpen ? "Door open" : "Temperature out of range";

            if (VaultState.Current == SystemState.Normal)
            {
                VaultState.AnomalyStartTime = now;
                VaultState.AnomalyEndTime = null;
                VaultState.LogIncident(reason, SystemState.SecurityEvent); 
            }
            else if (VaultState.Current == SystemState.SecurityEvent)
            {
                if (VaultState.AnomalyStartTime.HasValue && (now - VaultState.AnomalyStartTime.Value).TotalSeconds > 60)
                {
                    var newState = VaultState.IsOutOfOffice(now) ? SystemState.ColdChainBreach : SystemState.CriticalIncident; 
                    VaultState.LogIncident($"{reason} (persisted > 60s)", newState);
                    if (newState == SystemState.ColdChainBreach) Console.WriteLine("[CRITICAL ANOMALY] COLD CHAIN BREACH");
                }
                VaultState.AnomalyEndTime = null; 
            }
        }
        else
        {
            if (VaultState.Current == SystemState.SecurityEvent)
            {
                if (!VaultState.AnomalyEndTime.HasValue) VaultState.AnomalyEndTime = now; 
                
                else if ((now - VaultState.AnomalyEndTime.Value).TotalSeconds > 60)
                {
                    VaultState.Current = SystemState.Normal;
                    VaultState.AnomalyStartTime = null;
                    VaultState.AnomalyEndTime = null;
                    Console.WriteLine("[INFO] Security event cleared (TTL expired).");
                }
            }
        }

        return Results.Ok(new { status = VaultState.Current.ToString() });
    }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

app.MapPost("/api/reset", () =>
{
    VaultState.Current = SystemState.Normal;
    VaultState.AnomalyStartTime = null;
    VaultState.AnomalyEndTime = null;
    VaultState.LogIncident("Manual system reset", SystemState.Normal);
    return Results.Ok(new { message = "System reset." });
});

Console.WriteLine("Client alarm service started (Port 5100)...");
app.Run();

enum SystemState { Normal, SecurityEvent, CriticalIncident, ColdChainBreach }

static class VaultState
{
    public static SystemState Current = SystemState.Normal;
    public static DateTime? AnomalyStartTime = null;
    public static DateTime? AnomalyEndTime = null;
    public static readonly string LogFilePath = "incidents.csv"; 

    public static void LogIncident(string triggerDetails, SystemState newState)
    {
        Current = newState;
        string timestamp = DateTime.UtcNow.ToString("O"); 
        string logLine = $"{timestamp},{newState},{triggerDetails}\n";
        File.AppendAllText(LogFilePath, logLine); 
        Console.WriteLine($"[LOG SAVED] {logLine.Trim()}");
    }

    public static bool IsOutOfOffice(DateTime localTime)
    {
        if (localTime.DayOfWeek == DayOfWeek.Saturday || localTime.DayOfWeek == DayOfWeek.Sunday) return true;
        if (localTime.Hour >= 18 || localTime.Hour < 6) return true;
        return false;
    }
}