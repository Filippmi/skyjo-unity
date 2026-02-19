# Skyjo — Unity version

A simple single-player Skyjo card game you can run in the Unity Editor.

## Requirements

- **Unity 2022.3** or newer (other 2022.x versions should work; adjust `ProjectSettings/ProjectVersion.txt` if needed).

## How to run

1. **Open the project in Unity**
   - Open Unity Hub → **Add** → choose the `SkyjoUnity` folder (the one that contains `Assets` and `ProjectSettings`).
   - Open the project.

2. **Create or open a scene**
   - If Unity asks for a scene, use **File → New Scene** (e.g. Basic 2D or Empty).
   - Save the scene (e.g. `Assets/Scenes/Main.unity`).

3. **Setup the Skyjo UI**
   - In the menu bar, click **Skyjo → Setup Scene**.
   - This creates the Canvas, score text, draw/discard piles, 3×4 card grid, message text, and action buttons, and wires them to `SkyjoGameManager`.

4. **Press Play**
   - Click the **Play** button in the Editor. Use the buttons and grid as in the web version:
     - **Draw from deck** or **Take discard** to start your turn.
     - If you drew: **Swap** with a grid card (click the card) or **Discard drawn** then click a face-down card to flip it.
     - If you took the discard: click a grid card to replace it.
   - Round ends when all 12 cards are face up; use **New round** to play again.

## Project structure

- **Assets/Scripts/**
  - `SkyjoGame.cs` — Game state and rules (deck, grid, phases, scoring). No Unity dependencies.
  - `SkyjoGameManager.cs` — MonoBehaviour that owns the game and drives the UI (assign refs in Inspector or use Setup Scene).
  - `CardSlotView.cs` — One card slot in the grid; shows value/face-down/removed and reports clicks.
- **Assets/Editor/**
  - `SkyjoSetupScene.cs` — Menu **Skyjo → Setup Scene** builds the full UI hierarchy and assigns references.

## Rules (short)

- **Goal:** Lowest score. You have a 3×4 grid of 12 cards (values -2 to 12).
- **Turn:** Draw from deck or take the top discard. Then either swap with a grid card or (if you drew) discard the card and flip one face-down card.
- **Column bonus:** Three identical numbers in a column are removed and no longer count.
- **Round:** Ends when all 12 cards are face up. Your score is the sum of face-up values (removed = 0).
