
-- ============================================================
--  create_database.sql
--  Safe to run again at any time (nothing gets duplicated).
--
--  The file has two parts:
--    PART 1 - FINAL:       tables + real game data (keep these)
--    PART 2 - PLACEHOLDER: example rows for testing
--                          (replace/delete for the final version)
-- ============================================================


-- ############################################################
-- ##  PART 1 - FINAL  (stays in the finished game)          ##
-- ############################################################

-- Create the database
CREATE DATABASE IF NOT EXISTS StoryGameDB;

USE StoryGameDB;


-- 1. SETTINGS
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


-- 2. GAME PROGRESS
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
--    Unity unlocks these by Name, so the Name here must match
--    the text typed in the Inspector (e.g. Victim > Achievement On Kill).
CREATE TABLE IF NOT EXISTS Achievements (
    AchievementID INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL UNIQUE,
    Description TEXT,
    IsUnlocked BOOLEAN NOT NULL DEFAULT FALSE,
    UnlockedAt DATETIME DEFAULT NULL
);


-- 4. PLAY SESSIONS
CREATE TABLE IF NOT EXISTS PlaySessions (
    SessionID INT AUTO_INCREMENT PRIMARY KEY,
    StartTime DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    EndTime DATETIME DEFAULT NULL,
    DurationMinutes INT UNSIGNED NOT NULL DEFAULT 0,
    ChaptersCompleted INT UNSIGNED NOT NULL DEFAULT 0
);


-- 5. COLLECTIBLES
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


-- Insert default settings
INSERT IGNORE INTO Settings (SettingsID)
VALUES (1);


-- Insert initial game progress
INSERT IGNORE INTO GameProgress (ProgressID)
VALUES (1);


-- Final achievements
INSERT IGNORE INTO Achievements (Name, Description)
VALUES
    ('Night 1 Complete', 'You finished the first night.');


-- ############################################################
-- ##  PART 2 - PLACEHOLDER  (example data, replace for final) ##
-- ############################################################

-- PLACEHOLDER achievements
INSERT IGNORE INTO Achievements (Name, Description)
VALUES
    ('First Steps', 'Start the game for the first time.'),
    ('Collector', 'Collect your first collectible.'),
    ('Story Complete', 'Complete the main story.');


-- PLACEHOLDER collectibles
-- CollectibleID 1, 2, 3 -> put these numbers in PickupItem > Collectible Id in Unity
INSERT IGNORE INTO Collectibles
    (CollectibleID, Name, Description, ItemType, ChapterNumber)
VALUES
    (1, 'Torn Letter', 'A mysterious piece of paper.', 'Paper', 1),
    (2, 'Old Photograph', 'An old photograph found in the environment.', 'Photograph', 2),
    (3, 'Strange Key', 'An unusual key discovered during the story.', 'Object', 3);

-- ############################################################
-- ##  END OF PLACEHOLDER                                     ##
-- ############################################################
