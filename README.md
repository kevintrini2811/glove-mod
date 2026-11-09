# Glove Mod 1.4.2

## Dependencies

- Game/runtime: PvZ Replanted (Windows/IL2CPP), MelonLoader 0.7.3, and .NET 6.
- Required mods: `BloomEngine.dll`.
- Optional integrations: `DebugMod`.
- Building: .NET SDK with `net6.0` support and the game/MelonLoader reference assemblies declared in `LawnGlove.csproj`.

Mod for PvZ Replanted 1.5.1469_Steam, Unity 6000.0.52f1 and MelonLoader 0.7.3.
Installed as Mods/LawnGlove.dll. If the game is running, the new version will load after a full restart.

## Change 1.4.2

The mod is now called **Glove Mod** in the mod menu. Like Plant Library, the registry uses the native BloomEngine interface `ModMenuService.CreateEntry(...).AddIcon(...).AddDescription(...).Register()`. The square icon combines the original Zen Garden glove with the same bright Replanted lawn as Plant Library. BloomEngine draws frames and places the description; additional UI backgrounds are not created.

Description: "Move plants and zombies. Hammer for 3x3 fields. Change tools by right-clicking on the shovel."

BloomEngine is already installed and is explicitly stated as a dependency. Assembly name `LawnGlove`, DLL file name and public interfaces are preserved for Fusion mod and existing extensions. The previous DLL and affected sources are under `backup/1.4.1-before-mod-menu/`. Icon and build are checked offline; Game and test programs will not start.

## Change 1.4.1

The tool button shows the complete native hammer including the handle and connecting part. The icon is rendered offline from the resting pose of the same spine skeleton as the hammer cursor. The previous icon only contained the single atlas piece `hammer_1` (hammer head). Loading display and tool behavior are retained. Previous DLL and affected sources are under `backup/1.4.0-before-hammer-icon/`. Release build without game start; The user is responsible for play testing.

## Controls

- Right click on the unlocked shovel: Change tools cyclically: shovel, plant glove, hammer, zombie glove.
- Controller: Hold the native shovel button (Xbox: B; on other controllers, the game-mapped button) for 1.5 seconds to switch to the next tool. A light circle behind the native key icon fills in a clockwise direction. A long button press changes exactly once per hold. A plant picked up when pressed is returned without a cooldown.
- Controller: With the glove selected, B (or the native shovel button) picks up the plant when pressed. The next short press of the button places the plant at the desired target field when it is released. An additional activation click is not necessary. When the button is pressed briefly, the shovel also digs when released. No scooping action is triggered during gauntlet cooldown. In active offset mode, the native pick/drop confirmation button also works; the native cancel key returns the plant. Stick, control pad, seed selection and other game actions retain their native assignment.
- Left click on the ready glove: activate offset mode.
- Click on the plant: The plant is picked up and appears opaque on the glove cursor. A separate, faded plant preview snaps onto the valid target field. The real plant is hidden at the source field; Production, attacks, and targeting are disabled while worn.
- Click on a free field suitable for this plant: the same plant instance is placed there. Health and other condition remain intact. Exactly one successful move starts a ten second cooldown.
- The glove icon charges from bottom to top without a number display. Only the sprite pixels are brightened; the toolbox remains unchanged.
- Invalid targets leave the plant on the gauntlet. Right-clicking, switching to another tool, pausing, changing levels or saving cancels carrying and returns the plant to its source location. Clicking on the source field also returns it without a cooldown.
- You can switch to the shovel at any time during the cooldown. It remains usable immediately. Meanwhile, the glove continues to charge separately; When paused, the reloading time is stopped.

Normal terrain rules continue to apply. Occupied fields are only used by a registered compatible extension; without an extension they remain blocked. A water lily/pot with other plants on it cannot be removed individually. Corncob cannons require two free spaces. The Zen garden remains unchanged.

## Changes 1.1.0

Number/box overlay replaced by two sprite-like icon layers (dark icon and vertically padded colored icon). New native plant preview while wearing. Wearing state is resolved before cursor change, board serialization and board disposal so that no recorded plant is stored invisibly. The native NotOnGround rule takes recorded plants from field queries; your update/postupdate calls pause during this time.

## New tools in 1.4.0

- Hammer: The original Replanted hammer appears in the toolbox and at the cursor. After 30 seconds of active playing time he is ready, even for the first time after the start of the level. Left click on the ready icon activates it. A field click hits all living zombies in this field and the eight neighboring fields. The area at the edge of the field is cut off. Armor and shields do not protect; the native body damage routine triggers death with animation and normal death events. The hammer uses the native Foley samples Swing, Bonk and, depending on the shield, PlasticHit or ShieldHit. Each hit starts a new 30 second loading time, even on an empty field.
- Zombie Glove: The native glove asset is given a brown tint in the toolbox and cursor. Left click on the ready icon activates it. Click directly on a living zombie, then click on the target field. The exact same instance is moved; Health, armor and zombie variant are retained. Semi-transparent field preview uses the same exported replanted portraits, native preview anchor, and foot position as Fusion debug mode. Other zombies are allowed to stand on the target space. Native water/terrain crossings continue to apply after drop-off.
- The zombie remains in the board pool while being carried, continues to count towards the wave, is hidden at the source location, and pauses its native update/postupdate calls. Normal damage tracking skips it. Abort, tool change, pause, save and level change return it to the unchanged source location. Clicking on its source field also cancels. Only successful relocation starts a 20 second cooldown.
- Zombie Glove: Pick up uses SOUND_TAP, Drop uses the same terrain-dependent Foley selection as the Plant Glove. Invalid fields do not consume any charge.
- All three charging times continue to run independently, even when the shovel is selected. Pause pauses loading times. Controller: short tool button press uses the selected tool, hold for 1.5 seconds switches once; Confirmation and cancellation use the same paths as with the mouse.

## Build and test status 1.4.0

Release build against the installed MelonLoader/IL2CPP assemblies: without warnings and errors. Existing condition tests and the old integration test sequence have been adapted to the four-tool cycle. Tests were not carried out at the user's request. No game started. Display, audio, hit area, special zombies and terrain changes must be checked by the user in the game.

Previous DLL and sources: `backup/1.3.2-before-tools/`. `Mods/LawnGlove.dll` contains the new version. The Fusion mod retains its version and its existing plant-glove interface.

## Building

../.tools/dotnet/dotnet build LawnGlove.csproj -c Release
../.tools/dotnet/dotnet run --project tests/StateTests.csproj -c Release

Set game path with -p:GameDir=... if necessary. Requires the existing MelonLoader/Il2CppAssemblies.

## Uninstalling

Close game and remove Mods/LawnGlove.dll. The mod does not change profile definitions. Successfully relocated plants are saved by the game in the level as normal.

## Fix 1.1.1

The global Board.ClearCursor hook added in 1.1.0 has been removed: native cursor reinitializations are not allowed to cancel offset mode. Offset mode is now only activated after native cursor initialization. An actual change to a different cursor is handled the next time you click on the field and in the update; Plants that have been picked up are reset before further native use of the source field. Activation and recording are logged separately.

The direct hook to Board.SerializeBoardData with an IL2CPP ref write buffer has been replaced with a hook to the parameterless SerializationService.Serialize method. This avoids the BoolArray/Writer error encountered in the 1.1.0 protocol, while still resetting plants before saving begins.

Release build and health tests passed. No game launch or gameplay test performed. The files of the current game status were not changed during installation.

## Fix 1.1.2

Completely removed the global harmony hook on Plant.NotOnGround. The original occupancy check of the game remains unchanged. Only the actually ingested plant temporarily receives the native state flags mSquished=true and mIsOnBoard=false; their original values ​​are restored before dropping or on cancellation. The recovery checks the health status before removing its own carry flag so that the temporary squished flag does not incorrectly block the reset.

Release build with no warnings/errors. No gameplay test is claimed to be successful: The user expressly undertakes all game tests. The test run that had begun was ended and no further game starts were allowed. The productive save files match the backup before the test run started via SHA-256. No automatic test DLL in normal installation.

## Fix 1.1.3

If the plant recording is successful, the same SOUND_TAP sample is now used as in the native ZenGarden.MouseDownWithTool (sound ID 147). No sound if selection is invalid.

The position offset was traced using Screenshot_20261015_115218.png and the native controller implementation: ReloadedController.SetPosition writes m_objectOffset, which is added to the position of the bound ReloadedObject in UpdatePosition. The cursor position was therefore applied twice. The preview now remains bound to the native CursorObject, with a unique neutral additional offset; the additional positioning per frame has been removed. Corresponding absolute SetPosition calls after moving or canceling the real plant have also been removed, as their controller already follows their logical position.

Build without warnings/errors. No game launches or gameplay testing by the agent. Savegames confirmed unchanged during installation via SHA-256. Previous version: backup/LawnGlove-1.1.2.dll.

## Change 1.1.4

Successful weaning plays the same native planting tone as planting from the grid. The audio selection corresponds to Board.DoPlantingEffects: Plant on normal fields or for flying plants, PlantWater on water fields/zombiquarium and Ceramic in the greenhouse. Only the tones are called, no additional planting effects. Invalid targets and abandonment do not play a planting tone; an audio error does not change the already completed offset process.

Release build with no warnings/errors. No game start or gameplay test by the agent. Savegames confirmed unchanged during installation via SHA-256. Previous version: backup/LawnGlove-1.1.3.dll.

## Change 1.1.5

The plant at the glove cursor is now opaque. SetColorOverride alone is not enough for this: the native PreviewController overwrites the alpha value using its m_transparent flag. This flag is turned off for the cursor plant and turned on for the new grid preview; Their transparency corresponds to the native preview.

A separate native plant preview follows the grid and shows the position after a left click. Preview and Drop use the same targeting check including terrain, occupied spaces, and the two spaces of a corncob cannon. The position takes into account the original plant offset as well as the playing field position. The grid preview remains hidden on invalid fields and the source field (abort). Aborting, canceling, saving and changing levels both remove previews.

Release build with no warnings/errors. No tests or game starts by the agent; The user takes over the playtest. Previous version: backup/LawnGlove-1.1.4.dll.


## Change 1.2.0: Fusion interface

`OccupiedTilePlacement.CanPlace` and `OccupiedTilePlacement.TryPlace` allow extensions to check occupied target fields and adopt a placement. The fusion mod 0.1.0 uses this for Sunflower + Sunflower -> Twin Sunflower. The Glove mod itself does not contain any fusion recipes.

The preview checks the still-carried, temporarily deactivated plant. Before the commit, it will be restored as before. Only a successful commit removes the previews and starts the normal cooldown once. Invalid targets and abandonment do not change plants. Consuming extensions generate their result via native `Board.AddPlant`; its planting sound will not be played a second time.

Release build with no warnings or errors. No tests or game launches performed; Runtime check by the user. The previously installed DLL is saved in the working directory of the Fusionsmod under `backup/LawnGlove-before-fusion.dll`.


## Change 1.2.1: Query worn plant

The public read-only method `LawnGloveMod.GetCarriedPlant(Board)` returns only the currently worn, live plant of the specified board when the glove is active. The fusion mod 0.1.1 uses it to mark suitable fusion targets. Pickup, placement and cooldown remain unchanged. Release build with no warnings/errors, no game launch or playtest run.


## Change 1.2.2: Common terrain check

The existing assignment of upgrade plants to their basic terrain types has been moved unchanged from `PlantMover` to `PlantPlacementRules.TerrainType`. Glove and Fusionsmod use the same rules for terrain checking native upgrades. The Glove placement itself remains unchanged. Release build with no warnings/errors; no game start or playtest executed.

## Change 1.3.0: Controller tool change

The mod now extends the separate native `GamepadCursorController` instead of exclusively processing mouse events. It uses its existing secondary tool action including player assignment and device occupancy. This means that Xbox, PlayStation, Nintendo and Steam Input controllers do not require hard-coded hardware buttons. The game must still support and map the device itself. Like the previous glove, the expansion affects player 1; Player 2 and the Zen Garden retain their native tool controls.

Short button presses are only carried out when released, so that the start of a long button press does not dig up a plant. Hold for three seconds and change the tool once, even during cooldown. Another change requires releasing and pressing again. The native shovel hold attack is suppressed in this area. The indicator uses the native hold indicator circle graphic and position with a light radial fill behind the device-specific button icon.

Pickup, Preview, Valid Targets, Fusion Interface, Drop, Tones, Cancel and Cooldown use the same existing glove paths as the mouse. Native automatic seed reselection is only bypassed upon confirmation in active offset mode. Pause, loss of focus, device switch to mouse/keyboard, disconnect, save, board switch, and controller cursor removal discard ongoing holds. After an abort, the tool button must be pressed again.

Release build with no warnings/errors. At the user's request, neither tests nor game launches were carried out. Controller and representation are not yet confirmed at runtime. Previous version: `backup/LawnGlove-before-1.3.0.dll`.

## Fix 1.3.1: Shorter hold time and direct recording

Tool changes and circle filling now take 1.5 seconds instead of three. If the glove is selected, the recording takes place when the tool button is pressed. Releasing the same key press does not perform a second action. The next short press of the button places the plant at the target field. Placement and digging remain on release so long holds do not trigger a final placement or digging action. A tool change returns a plant picked up during pressing to its source location.

If a controller reports the same input for cancel and shovel, the mod will suppress cancel for that tool button. A separately assigned cancel button continues to function. The mapping uses the native inputs instead of a hard-coded B key.

Release build with no warnings/errors. No tests and no game started; The user is responsible for the runtime check. Previous version: `backup/LawnGlove-before-1.3.1.dll`.

## Fix 1.3.2: Reset glove cursor after use

The native method `Board.ClearCursor()` returns by default in controller operation without clearing the tool cursor. This allowed the glove to remain on the mouse cursor after a successful move until another plant was selected. The mod's common cursor reset now uses `ignoreControlType: true` for player 1. The native path thus completely resets the tool type, appearance and associated cursor states for mouse and controller control. This also applies to canceling and preparing a new activation; Tool selection and cooldown of the mod remain the same regardless.

Release build with no warnings/errors. No tests and no game started; The user is responsible for the runtime check. Previous version: `backup/LawnGlove-before-1.3.2.dll`.

### Manual checking by the user

- With any available supported controller: short press, hold for just under 1.5 seconds, hold for exactly 1.5 seconds, hold for more than three seconds. Only the long press of the button changes; When you release it afterwards, no plant may be removed. The circle and button symbol must match.
- With the glove: press B and check for immediate absorption before releasing; then let go, move with the stick/directional pad, check the preview, use the tool button or the native confirmation. Check invalid field, source field, abort and fusion targets. Check ten second cooldown; Meanwhile, select and use a shovel.
- After successful relocation and after cancellation, check with mouse and controller: The glove on the pointer disappears immediately without selecting another plant. After the cooldown has expired, a new recording must continue to work.
- When holding and carrying: Pause, save, lose focus, disconnect/reconnect controller and check level change. No unwanted action after return; Plants taken must return if canceled.
- Check seed selection, plants, sun exposure, special actions, mouse control and Zen garden for unchanged operation.
