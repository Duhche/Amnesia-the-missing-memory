# Setup guide

How to hook up the player, camera, E-interactions, the kill minigame, the
achievement pop-up and the database.

## What each script does

All scripts are in `AmnesiaTheMissingMemory(game)/Assets/Scripts/`.

| Script | Goes on | What it does |
|---|---|---|
| `Player/PlayerHandler` | Player | Walk with WASD |
| `Player/FollowPlayer` | Main Camera | First person camera at the player's eyes. Looks around with the mouse |
| `Player/PlayerInteractor` | Player | Shows `[E] ...` when you look at something usable and uses it when you press E |
| `Interaction/PickupItem` | Items | `[E] Pick up X`. Saves it to the Collectibles table |
| `Interaction/InteractableObject` | Doors, notes, switches | `[E] ...`. Shows a message and runs whatever you hook up in **On Interact** |
| `Interaction/Victim` | Victim | `[E] Kill`. Starts the minigame, and on a win unlocks **Night 1 Complete** |
| `Gameplay/KillMinigame` | GameSystems | The "MASH [SPACE]!" pop-up |
| `Gameplay/AchievementManager` | GameSystems | The "Achievement Unlocked" pop-up. Also saves the achievement to the database |
| `Database/DbClient` | GameSystems | Sends requests to AmnesiaAPI, which talks to MySQL |
| `UI/MainMenu` | Menu object in the MainMenu scene | Title, START and ACHIEVEMENTS. The achievements screen shows every achievement with a trophy |
| `Core/GameState`, `Core/GameUI` | Nothing | Helpers the other scripts use. Don't add them to objects |

---

## Part 1: Unity scene (open `Assets/Level1.unity`)

### 1. Ground
1. Your **Cube** is the floor. Make sure it has a **Box Collider**. Cubes have one by default.

### 2. Player
1. Select your player object (the **Capsule**). Rename it to `Player`.
2. **Add Component → Rigidbody**. You don't need to tick Freeze Rotation because the script does it.
3. Make sure it has a **Capsule Collider**. Capsules have one by default.
4. **Add Component → Player Handler**.
5. **Add Component → Player Interactor**.
6. Leave **Camera Transform** empty on both. They find the Main Camera on their own.

### 3. Camera
1. Select **Main Camera**. It must **not** be a child of the player, so drag it out of the player if it is.
2. **Add Component → Follow Player**.
3. Drag `Player` from the Hierarchy into the **Player** slot.
4. **Offset** is the eye height. Raise or lower its Y value if the view feels too low or too high.

### 4. GameSystems (one per scene)
1. **GameObject → Create Empty**, then rename it to `GameSystems`.
2. Add **Kill Minigame**, **Achievement Manager** and **Db Client** to it.

### 5. Victim
1. **GameObject → 3D Object → Capsule**, then rename it to `Victim`.
2. Drag `materials/victim.mat` onto it.
3. **Add Component → Victim**.
4. **Prompt Text** should already say `Kill`. You can change it to anything.
5. Check that **Achievement On Kill** is `Night 1 Complete`. It must be spelled exactly like the name in the database.

### 6. Items (optional)
- **Pickup:** add a 3D object and add **Pickup Item** to it. Set **Item Name**. If you want it saved, set **Collectible Id** to a `CollectibleID` from the database (1, 2 or 3 from the placeholder data). Use 0 if you don't want it saved.
- **Object:** add **Interactable Object** to it. Set **Prompt Text** (for example `Open door`) and **Message**. Under **On Interact**, press **+**, drag in an object and pick a function, for example `GameObject.SetActive`.

Every interactable needs a **Collider**, or the player can't aim at it.

### 7. Save and play
**Ctrl+S**, then press Play:
- WASD walks, and the mouse looks around. Esc frees the cursor, and clicking grabs it again.
- Look at the victim to see `[E] Kill`. Press E, then mash Space.
- On a win, the victim falls over and the **Night 1 Complete** pop-up appears.
- If time runs out, you see "They broke free!", and you can try again after 2 seconds.

The game works even without the database. In that case the pop-up still shows, and the Console warns that the achievement wasn't saved.

---

## Part 1b: Main menu

### 1. Make the scene
1. **File → New Scene**, pick **Basic (URP)**, then click **Create**.
2. **File → Save As**, open the `Assets/Scenes` folder and name it `MainMenu`.

### 2. Add the menu
1. **GameObject → Create Empty**, then rename it to `Menu`.
2. **Add Component → Main Menu**.
3. **Add Component → Db Client**. The achievements screen needs it to load the list.
4. **Ctrl+S**.

### 3. Add both scenes to the build
`START` can only load scenes that are on this list.
1. **File → Build Profiles** → **Scene List**.
2. Untick or remove `SampleScene`.
3. Drag `Assets/Scenes/MainMenu.unity` and `Assets/Level1.unity` from the Project window into the list.
4. Put **MainMenu at the top**. The first scene is the one the game opens with.

### 4. Test it
Open the `MainMenu` scene and press Play:
- **START** loads Level1.
- **ACHIEVEMENTS** lists every achievement. Unlocked ones get a gold trophy and the date. Locked ones are grey.
- **BACK** (or Esc) returns to the menu.

If the achievements screen says "Couldn't reach the database", start the API (Part 2, step 3).

You can change the title, the subtitle and the scene START loads on the `Main Menu` component in the Inspector.

---

## Part 2: Database (MySQL)

### SQL files in `database/`

`create_database.sql` is split into two parts by big comment banners:
- **PART 1 - FINAL** holds the tables plus the real data: the settings row, the progress row and the **Night 1 Complete** achievement. Keep this part.
- **PART 2 - PLACEHOLDER** holds the example achievements (First Steps, Collector, Story Complete) and the example collectibles (Torn Letter, Old Photograph, Strange Key). Replace these with your real ones for the final version.

`reset_progress.sql` locks all achievements again and resets progress, so you can test again.

Both files can be run more than once without making duplicates.

The top-level `create_database.sql` (outside `database/`) is an older copy. Use the one in `database/`.

### 1. Run the SQL
In **MySQL Workbench**: File → Open SQL Script → `database/create_database.sql` → ⚡ Execute.

Or in a terminal (it asks for your MySQL password):
```bash
mysql -u root -p < database/create_database.sql
```

### 2. Give the API your password (kept out of git)
```bash
dotnet user-secrets set "ConnectionStrings:StoryGameDB" "Server=localhost;Database=StoryGameDB;User ID=root;Password=YOUR_REAL_PASSWORD;" --project AmnesiaAPI
```
`appsettings.json` only has a `YOUR_PASSWORD` placeholder. Don't put your real password there.

### 3. Start the API
```bash
dotnet run --project AmnesiaAPI --launch-profile http
```
It should print `Now listening on: http://localhost:5112`. Leave it running while you play. To check it works, open http://localhost:5112/api/achievements in a browser.

### 4. Let Unity use http://localhost
**Edit → Project Settings → Player → Other Settings → Allow downloads over HTTP** → `Allowed in Development Builds`.

### 5. Check the link
Press Play. The Console should show:
`[DbClient] Connected. Chapter 1, checkpoint: `

After killing the victim, refresh http://localhost:5112/api/achievements. **Night 1 Complete** should now have `isUnlocked: true`.

If you get a red `[DbClient] ... failed` error:
- The API isn't running, or
- The **Base Url** on GameSystems → Db Client isn't `http://localhost:5112`, or
- The HTTP setting from step 4 is missing.

---

## API endpoints

| Method | URL | Body |
|---|---|---|
| GET | `/api/progress` | |
| PUT | `/api/progress` | `{ "currentChapter": 2, "lastCheckpoint": "...", "playtimeMinutes": 10 }` |
| GET | `/api/collectibles` | |
| POST | `/api/collectibles/{id}/collect` | |
| GET | `/api/achievements` | |
| POST | `/api/achievements/unlock` | `{ "name": "Night 1 Complete" }` |

## Adding more achievements later
1. Add a row to the **Final achievements** insert in PART 1 of `database/create_database.sql` and run the file again.
2. From any script, call:
   `AchievementManager.Instance.Unlock("Night 2 Complete", "You survived the second night.");`

## Placeholders to replace for the final version
Search the code for `PLACEHOLDER`:
- The death effect in `Victim.cs` just tips the victim over and turns it red. Replace it with an animation, sound and blood.
- The pop-ups (`KillMinigame`, `AchievementManager`, the `[E]` prompt) are simple OnGUI boxes. Replace them with proper UI art.
- The main menu (`MainMenu`) is plain OnGUI too: text title, flat buttons and a trophy made of rectangles. Replace it with a real menu design (background art, logo, music).
- `PickupItem` has no pickup sound and no inventory yet.
- `AchievementManager` plays no achievement sound yet.
- PART 2 of `database/create_database.sql` holds example data only.
