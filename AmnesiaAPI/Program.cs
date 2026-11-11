using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// the real password is saved with dotnet user-secrets, not in appsettings.json
string connStr = builder.Configuration.GetConnectionString("StoryGameDB")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:StoryGameDB");

// ---- update the database ----
// runs database/create_database.sql every time the API starts.
// it only adds what's missing so it's fine to run it again and again.
string sqlFile = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "database", "create_database.sql"));
if (File.Exists(sqlFile))
{
    // connect without a database picked, the script makes StoryGameDB itself
    var serverOnly = new MySqlConnectionStringBuilder(connStr) { Database = "" };
    using var setupConn = new MySqlConnection(serverOnly.ConnectionString);
    await setupConn.OpenAsync();
    using var setupCmd = new MySqlCommand(await File.ReadAllTextAsync(sqlFile), setupConn);
    await setupCmd.ExecuteNonQueryAsync();
    app.Logger.LogInformation("Database updated from {File}", sqlFile);
}
else
{
    app.Logger.LogWarning("Couldn't find {File}, database not updated", sqlFile);
}

// ---- progress ----
// get the saved chapter, checkpoint and playtime
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

// save the chapter, checkpoint and playtime
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

// ---- collectibles (per save) ----
// list every collectible and whether this save has found it
app.MapGet("/api/saves/{slot:int}/collectibles", async (int slot) =>
{
    var list = new List<object>();
    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new MySqlCommand(
        @"SELECT c.CollectibleID, c.Name, c.ChapterNumber, sc.CollectedAt
          FROM Collectibles c
          LEFT JOIN SaveCollectibles sc ON sc.CollectibleID = c.CollectibleID AND sc.SlotNumber = @s
          ORDER BY c.CollectibleID", conn);
    cmd.Parameters.AddWithValue("@s", slot);
    using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
        list.Add(new
        {
            id = r.GetInt32(0),
            name = r.GetString(1),
            chapter = r.GetInt32(2),
            isCollected = !r.IsDBNull(3),
            collectedAt = r.IsDBNull(3) ? (DateTime?)null : r.GetDateTime(3)
        });
    return list;
});

// mark one as found for a save
app.MapPost("/api/saves/{slot:int}/collectibles/{id:int}/collect", async (int slot, int id) =>
{
    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();

    // make sure the save and the collectible both exist
    using var find = new MySqlCommand(
        @"SELECT (SELECT COUNT(*) FROM SaveSlots WHERE SlotNumber = @s),
                 (SELECT COUNT(*) FROM Collectibles WHERE CollectibleID = @id)", conn);
    find.Parameters.AddWithValue("@s", slot);
    find.Parameters.AddWithValue("@id", id);
    using (var r = await find.ExecuteReaderAsync())
    {
        await r.ReadAsync();
        if (Convert.ToInt32(r.GetValue(0)) == 0) return Results.NotFound($"No save in slot {slot}");
        if (Convert.ToInt32(r.GetValue(1)) == 0) return Results.NotFound($"No collectible with id {id}");
    }

    // IGNORE so picking it up twice keeps the first date
    using var cmd = new MySqlCommand(
        "INSERT IGNORE INTO SaveCollectibles (SlotNumber, CollectibleID) VALUES (@s, @id)", conn);
    cmd.Parameters.AddWithValue("@s", slot);
    cmd.Parameters.AddWithValue("@id", id);
    int changed = await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { id, newlyCollected = changed > 0 });
});

// ---- save slots (CRUD) ----
// the 3 save slots in the main menu. each save has its own achievements and collectibles.

// READ: list the saves that exist (empty slots just aren't in the list)
app.MapGet("/api/saves", async () =>
{
    var list = new List<object>();
    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new MySqlCommand(
        @"SELECT s.SlotNumber, s.CurrentLevel, s.CreatedAt, s.LastPlayed,
                 (SELECT COUNT(*) FROM SaveAchievements a WHERE a.SlotNumber = s.SlotNumber),
                 (SELECT COUNT(*) FROM SaveCollectibles c WHERE c.SlotNumber = s.SlotNumber)
          FROM SaveSlots s
          ORDER BY s.SlotNumber", conn);
    using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
        list.Add(new
        {
            slot = r.GetInt32(0),
            currentLevel = r.GetString(1),
            createdAt = r.GetDateTime(2),
            lastPlayed = r.GetDateTime(3),
            achievementsUnlocked = Convert.ToInt32(r.GetValue(4)),
            collectiblesFound = Convert.ToInt32(r.GetValue(5))
        });
    return list;
});

// CREATE: make a new save in an empty slot. it starts on night 1 with no achievements or collectibles
app.MapPost("/api/saves/{slot:int}", async (int slot) =>
{
    if (slot < 1 || slot > 3) return Results.BadRequest("Slot has to be 1, 2 or 3");

    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();

    // don't overwrite a save by accident, delete it first
    using var find = new MySqlCommand("SELECT COUNT(*) FROM SaveSlots WHERE SlotNumber = @s", conn);
    find.Parameters.AddWithValue("@s", slot);
    if (Convert.ToInt32(await find.ExecuteScalarAsync()) > 0) return Results.Conflict($"Slot {slot} already has a save");

    using var cmd = new MySqlCommand("INSERT INTO SaveSlots (SlotNumber) VALUES (@s)", conn);
    cmd.Parameters.AddWithValue("@s", slot);
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { slot, currentLevel = "Level1(night)" });
});

// UPDATE: remember which level the save is on, like { "currentLevel": "Level1(day)" }
app.MapPut("/api/saves/{slot:int}", async (int slot, SaveUpdateDto dto) =>
{
    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new MySqlCommand(
        "UPDATE SaveSlots SET CurrentLevel = @l, LastPlayed = NOW() WHERE SlotNumber = @s", conn);
    cmd.Parameters.AddWithValue("@l", dto.CurrentLevel);
    cmd.Parameters.AddWithValue("@s", slot);
    int changed = await cmd.ExecuteNonQueryAsync();
    return changed > 0 ? Results.Ok() : Results.NotFound($"No save in slot {slot}");
});

// DELETE: remove a save. its achievements and collectibles get deleted with it (ON DELETE CASCADE)
app.MapDelete("/api/saves/{slot:int}", async (int slot) =>
{
    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new MySqlCommand("DELETE FROM SaveSlots WHERE SlotNumber = @s", conn);
    cmd.Parameters.AddWithValue("@s", slot);
    int changed = await cmd.ExecuteNonQueryAsync();
    return changed > 0 ? Results.Ok() : Results.NotFound($"No save in slot {slot}");
});

// ---- achievements (per save) ----
// list every achievement and whether this save has unlocked it
app.MapGet("/api/saves/{slot:int}/achievements", async (int slot) =>
{
    var list = new List<object>();
    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new MySqlCommand(
        @"SELECT a.AchievementID, a.Name, a.Description, sa.UnlockedAt
          FROM Achievements a
          LEFT JOIN SaveAchievements sa ON sa.AchievementID = a.AchievementID AND sa.SlotNumber = @s
          ORDER BY a.AchievementID", conn);
    cmd.Parameters.AddWithValue("@s", slot);
    using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
        list.Add(new
        {
            id = r.GetInt32(0),
            name = r.GetString(1),
            description = r.IsDBNull(2) ? "" : r.GetString(2),
            isUnlocked = !r.IsDBNull(3),
            unlockedAt = r.IsDBNull(3) ? (DateTime?)null : r.GetDateTime(3)
        });
    return list;
});

// unlock one for a save by its name, like { "name": "Night 1 Complete" }
app.MapPost("/api/saves/{slot:int}/achievements/unlock", async (int slot, UnlockDto dto) =>
{
    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();

    // make sure the achievement exists
    using var find = new MySqlCommand("SELECT AchievementID FROM Achievements WHERE Name = @n", conn);
    find.Parameters.AddWithValue("@n", dto.Name);
    object? id = await find.ExecuteScalarAsync();
    if (id == null) return Results.NotFound($"No achievement named '{dto.Name}'");

    // make sure the save exists
    using var findSlot = new MySqlCommand("SELECT COUNT(*) FROM SaveSlots WHERE SlotNumber = @s", conn);
    findSlot.Parameters.AddWithValue("@s", slot);
    if (Convert.ToInt32(await findSlot.ExecuteScalarAsync()) == 0) return Results.NotFound($"No save in slot {slot}");

    // IGNORE so unlocking it twice keeps the first date
    using var cmd = new MySqlCommand(
        "INSERT IGNORE INTO SaveAchievements (SlotNumber, AchievementID) VALUES (@s, @a)", conn);
    cmd.Parameters.AddWithValue("@s", slot);
    cmd.Parameters.AddWithValue("@a", id);
    int changed = await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { name = dto.Name, newlyUnlocked = changed > 0 });
});

app.Run();

record ProgressDto(int CurrentChapter, string LastCheckpoint, int PlaytimeMinutes);
record UnlockDto(string Name);
record SaveUpdateDto(string CurrentLevel);