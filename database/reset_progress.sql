-- for testing: deletes all saves (and their achievements and collectibles) and resets progress

USE StoryGameDB;

DELETE FROM SaveSlots WHERE SlotNumber > 0;
UPDATE GameProgress SET CurrentChapter = 1, LastCheckpoint = NULL, PlaytimeMinutes = 0, LastSaved = NULL WHERE ProgressID = 1;
