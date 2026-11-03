using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// set your real password with user-secrets (see SETUP_GUIDE.md), not in appsettings.json
string connStr = builder.Configuration.GetConnectionString("StoryGameDB")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:StoryGameDB");

// ---- Progress ----
app.MapGet("/api/progress", async () =>
{
    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new MySqlCommand(
        "SELECT CurrentChapter, LastCheckpoint, PlaytimeMinutes FROM GameProgress WHERE ProgressID = 1", conn);
    using var r = await cmd.ExecuteReaderAsync();
    if (!await r.ReadAsync()) return Results.NotFound();
    return Results.Ok(new
    {
        currentChapter = r.GetInt32(0),
        lastCheckpoint = r.IsDBNull(1) ? "" : r.GetString(1),
        playtimeMinutes = r.GetInt32(2)
    });
});

app.MapPut("/api/progress", async (ProgressDto dto) =>
{
    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new MySqlCommand(
        @"UPDATE GameProgress
          SET CurrentChapter = @c, LastCheckpoint = @cp, PlaytimeMinutes = @p, LastSaved = NOW()
          WHERE ProgressID = 1", conn);
    cmd.Parameters.AddWithValue("@c", dto.CurrentChapter);
    cmd.Parameters.AddWithValue("@cp", dto.LastCheckpoint);
    cmd.Parameters.AddWithValue("@p", dto.PlaytimeMinutes);
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok();
});

// ---- Collectibles ----
app.MapGet("/api/collectibles", async () =>
{
    var list = new List<object>();
    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new MySqlCommand(
        "SELECT CollectibleID, Name, ChapterNumber, IsCollected FROM Collectibles", conn);
    using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
        list.Add(new { id = r.GetInt32(0), name = r.GetString(1), chapter = r.GetInt32(2), isCollected = r.GetBoolean(3) });
    return list;
});

app.MapPost("/api/collectibles/{id:int}/collect", async (int id) =>
{
    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new MySqlCommand(
        "UPDATE Collectibles SET IsCollected = TRUE, CollectedAt = NOW() WHERE CollectibleID = @id AND IsCollected = FALSE", conn);
    cmd.Parameters.AddWithValue("@id", id);
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok();
});

// ---- Achievements ----
app.MapGet("/api/achievements", async () =>
{
    var list = new List<object>();
    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new MySqlCommand(
        "SELECT AchievementID, Name, Description, IsUnlocked, UnlockedAt FROM Achievements", conn);
    using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
        list.Add(new
        {
            id = r.GetInt32(0),
            name = r.GetString(1),
            description = r.IsDBNull(2) ? "" : r.GetString(2),
            isUnlocked = r.GetBoolean(3),
            unlockedAt = r.IsDBNull(4) ? (DateTime?)null : r.GetDateTime(4)
        });
    return list;
});

// unlock by name, e.g. { "name": "Night 1 Complete" }
app.MapPost("/api/achievements/unlock", async (UnlockDto dto) =>
{
    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();

    using var find = new MySqlCommand("SELECT IsUnlocked FROM Achievements WHERE Name = @n", conn);
    find.Parameters.AddWithValue("@n", dto.Name);
    object? existing = await find.ExecuteScalarAsync();
    if (existing == null) return Results.NotFound($"No achievement named '{dto.Name}'");

    using var cmd = new MySqlCommand(
        "UPDATE Achievements SET IsUnlocked = TRUE, UnlockedAt = NOW() WHERE Name = @n AND IsUnlocked = FALSE", conn);
    cmd.Parameters.AddWithValue("@n", dto.Name);
    int changed = await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { name = dto.Name, newlyUnlocked = changed > 0 });
});

app.Run();

record ProgressDto(int CurrentChapter, string LastCheckpoint, int PlaytimeMinutes);
record UnlockDto(string Name);