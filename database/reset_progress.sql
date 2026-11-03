
-- ============================================================
--  reset_progress.sql  -  start over for testing
--  Locks every achievement, un-collects every item and puts
--  progress back to chapter 1. Does NOT delete any rows.
-- ============================================================

USE StoryGameDB;

UPDATE Achievements SET IsUnlocked = FALSE, UnlockedAt = NULL WHERE AchievementID > 0;
UPDATE Collectibles SET IsCollected = FALSE, CollectedAt = NULL WHERE CollectibleID > 0;
UPDATE GameProgress SET CurrentChapter = 1, LastCheckpoint = NULL, PlaytimeMinutes = 0, LastSaved = NULL WHERE ProgressID = 1;
