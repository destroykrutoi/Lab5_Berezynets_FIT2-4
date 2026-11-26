using System.Collections.Concurrent;
using System.Text;
using System.Text.Json.Nodes;

// Локальний заглушковий REST API для перевірки синхронізації
Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
var app = builder.Build();

var routes = new ConcurrentDictionary<int, JsonObject>();

void Log(string text) => Console.WriteLine($"{DateTime.Now:HH:mm:ss}  {text}");

// Стратегія вирішення конфліктів: перемагає останній запис (last write wins) за полем UpdatedAt
bool IsNewer(JsonObject incoming, JsonObject? stored) =>
    stored is null ||
    DateTime.Parse(incoming["UpdatedAt"]!.ToString()) >= DateTime.Parse(stored["UpdatedAt"]!.ToString());

app.MapGet("/api/routes", () =>
{
    Log($"GET    /api/routes → {routes.Count} записів");
    return Results.Ok(routes.Values.OrderBy(r => (int)r["Id"]!));
});

app.MapPost("/api/routes", (JsonObject route) =>
{
    var id = (int)route["Id"]!;
    routes[id] = route;
    Log($"POST   /api/routes  Id={id} «{route["Name"]}» → 201 Created");
    return Results.Created($"/api/routes/{id}", route);
});

app.MapPut("/api/routes/{id:int}", (int id, JsonObject route) =>
{
    routes.TryGetValue(id, out var stored);
    if (!IsNewer(route, stored))
    {
        Log($"PUT    /api/routes/{id} → застаріла версія, залишено серверну (last write wins)");
        return Results.Ok(new { applied = false });
    }
    routes[id] = route;
    Log($"PUT    /api/routes/{id} «{route["Name"]}» → 200 OK");
    return Results.Ok(new { applied = true });
});

app.MapDelete("/api/routes/{id:int}", (int id) =>
{
    var removed = routes.TryRemove(id, out _);
    Log($"DELETE /api/routes/{id} → {(removed ? "204 No Content" : "404 Not Found")}");
    return removed ? Results.NoContent() : Results.NotFound();
});

Log("RailwayErp.StubApi запущено: http://localhost:5085");
app.Run("http://localhost:5085");
