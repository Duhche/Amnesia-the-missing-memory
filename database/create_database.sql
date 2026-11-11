-- part 1: final

-- make the database
CREATE DATABASE IF NOT EXISTS StoryGameDB;

USE StoryGameDB;


-- 1. SETTINGS (volume, graphics, language). only ever has one row
CREATE TABLE IF NOT EXISTS Settings (
    SettingsID INT PRIMARY KEY DEFAULT 1,
    MasterVolume DECIMAL(3,2) NOT NULL DEFAULT 1.00,
    MusicVolume DECIMAL(3,2) NOT NULL DEFAULT 0.80,
    GraphicsQuality VARCHAR(20) NOT NULL DEFAULT 'High',
    Language VARCHAR(20) NOT NULL DEFAULT 'English',

    CONSTRAINT chk_settings_id
        CHECK (SettingsID = 1),

    CONSTRAINT chk_master_volume
        CHECK (MasterVolume BETWEEN 0 AND 1),

    CONSTRAINT chk_music_volume
        CHECK (MusicVolume BETWEEN 0 AND 1),

    CONSTRAINT chk_graphics_quality
        CHECK (GraphicsQuality IN ('Low', 'Medium', 'High', 'Ultra'))
);


-- 2. GAME PROGRESS (where the player is). only ever has one row
CREATE TABLE IF NOT EXISTS GameProgress (
    ProgressID INT PRIMARY KEY DEFAULT 1,
    CurrentChapter INT NOT NULL DEFAULT 1,
    LastCheckpoint VARCHAR(100) DEFAULT NULL,
    PlaytimeMinutes INT UNSIGNED NOT NULL DEFAULT 0,
    LastSaved DATETIME DEFAULT NULL,

    CONSTRAINT chk_progress_id
        CHECK (ProgressID = 1),

    CONSTRAINT chk_current_chapter
        CHECK (CurrentChapter >= 1)
);


-- 3. ACHIEVEMENTS
--    the game unlocks these by name, so the name has to match
--    what's typed in Unity exactly (like Victim > Achievement On Kill).
--    IsUnlocked and UnlockedAt aren't used anymore, each save has its own in SaveAchievements
CREATE TABLE IF NOT EXISTS Achievements (
    AchievementID INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL UNIQUE,
    Description TEXT,
    IsUnlocked BOOLEAN NOT NULL DEFAULT FALSE,
    UnlockedAt DATETIME DEFAULT NULL
);


-- 4. PLAY SESSIONS (one row per time you play). not used yet
CREATE TABLE IF NOT EXISTS PlaySessions (
    SessionID INT AUTO_INCREMENT PRIMARY KEY,
    StartTime DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    EndTime DATETIME DEFAULT NULL,
    DurationMinutes INT UNSIGNED NOT NULL DEFAULT 0,
    ChaptersCompleted INT UNSIGNED NOT NULL DEFAULT 0
);


-- 5. COLLECTIBLES (items you can find)
--    IsCollected and CollectedAt aren't used anymore, each save has its own in SaveCollectibles
CREATE TABLE IF NOT EXISTS Collectibles (
    CollectibleID INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description TEXT,
    ItemType VARCHAR(30) NOT NULL DEFAULT 'Object',
    ChapterNumber INT NOT NULL,
    IsCollected BOOLEAN NOT NULL DEFAULT FALSE,
    CollectedAt DATETIME DEFAULT NULL,

    CONSTRAINT chk_collectible_type
        CHECK (ItemType IN ('Paper', 'Photograph', 'Object', 'Other')),

    CONSTRAINT chk_collectible_chapter
        CHECK (ChapterNumber >= 1)
);


-- 6. SAVE SLOTS (the 3 saves in the main menu and which level each one is on)
CREATE TABLE IF NOT EXISTS SaveSlots (
    SlotNumber INT PRIMARY KEY,
    CurrentLevel VARCHAR(50) NOT NULL DEFAULT 'Level1(night)',
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    LastPlayed DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT chk_slot_number
        CHECK (SlotNumber BETWEEN 1 AND 3)
);


-- 7. SAVE ACHIEVEMENTS (which achievements each save has unlocked)
--    a new save has no rows here, so everything starts locked.
--    deleting a save deletes its rows too (ON DELETE CASCADE)
CREATE TABLE IF NOT EXISTS SaveAchievements (
    SlotNumber INT NOT NULL,
    AchievementID INT NOT NULL,
    UnlockedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (SlotNumber, AchievementID),

    CONSTRAINT fk_save_achievements_slot
        FOREIGN KEY (SlotNumber) REFERENCES SaveSlots(SlotNumber) ON DELETE CASCADE,

    CONSTRAINT fk_save_achievements_achievement
        FOREIGN KEY (AchievementID) REFERENCES Achievements(AchievementID) ON DELETE CASCADE
);


-- 8. SAVE COLLECTIBLES (which collectibles each save has found)
--    works the same as SaveAchievements: new save = nothing found, delete save = rows go too
CREATE TABLE IF NOT EXISTS SaveCollectibles (
    SlotNumber INT NOT NULL,
    CollectibleID INT NOT NULL,
    CollectedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (SlotNumber, CollectibleID),

    CONSTRAINT fk_save_collectibles_slot
        FOREIGN KEY (SlotNumber) REFERENCES SaveSlots(SlotNumber) ON DELETE CASCADE,

    CONSTRAINT fk_save_collectibles_collectible
        FOREIGN KEY (CollectibleID) REFERENCES Collectibles(CollectibleID) ON DELETE CASCADE
);


-- default settings row
INSERT IGNORE INTO Settings (SettingsID)
VALUES (1);


-- starting progress row
INSERT IGNORE INTO GameProgress (ProgressID)
VALUES (1);


-- the real achievements
INSERT IGNORE INTO Achievements (Name, Description)
VALUES
    ('Night 1 Complete', 'You finished the first night.'),
    ('First Inspection', 'You inspected a body for the first time.'),
    ('Day 1 Cleared', 'You made it through the first day.'),
    ('Night 2 Complete', 'You finished the second night.'),
    ('Cold Steel', 'Kill someone with the knife.'),
    ('Day 2 Cleared', 'You made it through the second day.');


-- part 2: placeholder (test data, replace later)

-- achievements we got rid of (deletes them if they're still in the database)
DELETE FROM Achievements WHERE Name IN ('First Steps');


-- test achievements
INSERT IGNORE INTO Achievements (Name, Description)
VALUES
    ('Collector', 'Collect your first collectible.'),
    ('Story Complete', 'Complete the main story.');


-- old copies of the example collectibles from when this file used to make doubles.
-- they took ids 4-6, so the Mystery Items couldn't get theirs
DELETE FROM Collectibles
WHERE CollectibleID IN (4, 5, 6) AND Name IN ('Torn Letter', 'Old Photograph', 'Strange Key');

-- test collectibles
-- the number at the start goes in PickupItem > Collectible Id in Unity
INSERT IGNORE INTO Collectibles
    (CollectibleID, Name, Description, ItemType, ChapterNumber)
VALUES
    (1, 'Torn Letter', 'A mysterious piece of paper.', 'Paper', 1),
    (2, 'Old Photograph', 'An old photograph found in the environment.', 'Photograph', 2),
    (3, 'Strange Key', 'An unusual key discovered during the story.', 'Object', 3),
    (4, 'Mystery Item', 'Day 1 collectible - decide what it is later.', 'Other', 1),
    (5, 'Mystery Item 2', 'Day 2 collectible - decide what it is later.', 'Other', 2);
