# Daily Driver — 3D Logistics Simulation Game

Daily Driver is a 3D logistics simulation game developed with Unity. The game simulates package delivery in an urban environment, where players can plan delivery routes, navigate between destinations, and compare manual driving performance with an AI-assisted route.

The project focuses on the implementation of **A* pathfinding**, **Finite State Machine (FSM)**, NPC traffic behavior, route optimization, and interactive minimap systems.

## Features

- 3D logistics and package delivery simulation
- Manual package delivery mode
- AI-assisted delivery mode
- A* pathfinding system
- AI route recommendation
- Route distance calculation
- Manual vs AI route comparison
- Package delivery progress tracking
- Delivery timer
- Dynamic minimap
- Interactive minimap
- NPC traffic system
- NPC automatic spawning
- NPC obstacle avoidance
- Traffic light system
- Vehicle movement and third-person camera
- Pause and resume system
- Loading screen
- Level progression system
- AI mode unlock system
- Delivery notifications
- Game Over and completion states

## Gameplay

The player controls a delivery vehicle and is required to deliver packages to several destinations within the available time.

Each delivery destination is represented as a node in the navigation network. The player can select a package destination manually, while the AI mode can determine a recommended delivery sequence.

The game also records the player's traveled distance and compares it with the calculated optimal AI route.

## Game Modes

### Manual Mode

In Manual Mode, the player decides which package destination to visit.

The player can:

- Select delivery destinations
- Navigate manually
- Follow the generated path
- Monitor remaining delivery time
- Track traveled distance
- View package delivery progress

Package destinations can be selected using keyboard input or by interacting with the minimap.

### AI Mode

AI Mode provides an automatically recommended delivery route.

The system calculates a delivery sequence based on the available navigation nodes and path distances. The AI then assigns the next destination and uses the pathfinding system to navigate toward it.

AI Mode is unlocked after the required level progression has been completed.

## Pathfinding

Daily Driver implements a node-based pathfinding system for vehicle navigation.

Each `Node` contains a list of neighboring nodes and uses:

```text
fCost = gCost + hCost
```

where:

- `gCost` represents the accumulated path cost.
- `hCost` represents the estimated cost to the target.
- `fCost` represents the total path cost.

The pathfinding system also checks whether road segments are blocked by obstacles and can recalculate the route when necessary.

The system supports:

- Nearest-node detection
- Target-node selection
- Path calculation
- Obstacle detection
- Path recalculation
- Wrong-node detection
- Route visualization

The pathfinding system is implemented in `Pathfinding.cs`.

## NPC System

NPC vehicles use node-based navigation to move through the environment.

NPC movement includes:

- Waypoint navigation
- Random direction selection
- Straight/left/right movement preferences
- Obstacle detection
- Traffic congestion detection
- Safe neighbor selection
- Path avoidance
- Anti-loop movement behavior

The main NPC movement logic is implemented through `NPCNodeMovement`.

NPC vehicles are controlled using a Finite State Machine.

## Finite State Machine

NPC vehicles use three primary states:

```text
Move
  ↓
Stop
  ↓
Avoid
```

### Move

The NPC follows its navigation direction while checking for obstacles, stop zones, and traffic lights.

### Stop

The NPC stops when the road is blocked or a traffic light requires the vehicle to stop.

### Avoid

When an obstacle is detected, the NPC attempts to avoid it by selecting an available direction.

The FSM implementation is handled by `CarFSM.cs`.

## Traffic Light System

The game includes a traffic light system with three states:

```text
Red
Yellow
Green
```

Traffic lights automatically transition between states based on their configured durations.

NPC vehicles detect traffic lights and stop when approaching a red light.

The player vehicle can also be checked against traffic light rules through the game ruler/checking system.

## Delivery System

The delivery system manages:

- Package destinations
- Delivery progress
- Remaining time
- Current target
- Delivered packages
- Player route order
- AI route order
- Distance comparison

The game starts with a configurable number of packages and a delivery time limit.

After reaching a destination, the package is marked as delivered and the remaining delivery time is updated.

## Route Comparison

One of the main purposes of the game is to compare the player's delivery route with the AI-recommended route.

The system records:

```text
Player Route
        ↓
Total Distance
        ↓
AI Optimal Route
        ↓
Distance Comparison
```

The game calculates the distance traveled by the player and compares it with the distance calculated through the pathfinding network.

This allows the player to see the difference between their selected route and the AI recommendation.

## Minimap

Daily Driver includes an interactive minimap system.

The minimap provides:

- Player position
- Delivery destination icons
- Target destination highlighting
- Delivered destination indicators
- Expandable minimap
- Player-following camera
- Minimap zoom
- Interactive destination selection

The minimap can switch between a small mode and a larger view.

## Progression System

The game includes a level progression system.

Player progress is stored using Unity `PlayerPrefs`, including whether each level has achieved its optimal result.

The project currently checks progression across five levels.

The AI mode can remain locked until the required level progression has been completed.

## Controls

| Input | Function |
|---|---|
| `W / S` | Move forward / backward |
| `A / D` | Steer |
| `Mouse` | Camera control |
| `Mouse Scroll` | Camera zoom |
| `1 - 9` | Select package destination |
| `M` | Toggle minimap |
| `H` | Switch game mode when unlocked |
| `ESC` | Pause / Resume |
| `R` | Reset progression |

> Some controls may depend on the current game state and level progression.

## Technology Stack

- **Unity** — Game engine
- **C#** — Programming language
- **A* Pathfinding** — Route and navigation system
- **Finite State Machine (FSM)** — NPC behavior
- **Unity Physics** — Collision and obstacle detection
- **TextMesh Pro** — User interface text
- **Unity UI** — Game interface
- **PlayerPrefs** — Local progression storage
- **Blender** — 3D asset creation
- **Git & GitHub** — Version control

## Project Structure

```text
Daily-Driver/
│
├── Assets/
│   ├── Material/
│   ├── Object/
│   ├── Prefabs/
│   ├── Scenes/
│   ├── Script/
│   ├── Settings/
│   ├── UI/
│   ├── TextMesh Pro/
│   └── ...
│
├── Packages/
│
├── ProjectSettings/
│
├── .gitignore
│
└── README.md
```

## Main Scripts

| Script | Description |
|---|---|
| `Pathfinding.cs` | Handles node-based pathfinding and route calculation |
| `Node.cs` | Defines navigation nodes and their connections |
| `DeliveryManualAI.cs` | Manages package delivery, game modes, route comparison, and delivery progress |
| `NPCNodeMovement.cs` | Controls NPC navigation between nodes |
| `CarFSM.cs` | Controls NPC Move, Stop, and Avoid states |
| `NPCSpawnerDespawner.cs` | Handles NPC spawning and spawn-point validation |
| `TrafficLight.cs` | Controls traffic light states |
| `GameRulerChecker.cs` | Checks traffic rules and node-related game conditions |
| `InteractableMinimap.cs` | Controls minimap interaction, zoom, and display modes |
| `MinimapIcon.cs` | Controls delivery and player icons on the minimap |
| `MinimapFollow.cs` | Controls minimap camera following |
| `CarController.cs` | Handles player vehicle movement |
| `CameraController.cs` | Handles third-person camera movement and collision |
| `NotifikasiPaket.cs` | Displays package delivery notifications |
| `ProgresManager.cs` | Stores and manages level progression |
| `AIUnlockManager.cs` | Handles AI mode unlocking |
| `MainMenu.cs` | Handles scene navigation, pause, resume, and exit |

## AI & Simulation Architecture

```text
                    Daily Driver
                         │
          ┌──────────────┴──────────────┐
          │                             │
      Player Mode                   AI Mode
          │                             │
   Manual Destination          Route Recommendation
          │                             │
          └──────────────┬──────────────┘
                         │
                    Pathfinding
                         │
                    Node Network
                         │
          ┌──────────────┴──────────────┐
          │                             │
      Player Vehicle                NPC Vehicles
          │                             │
    Car Controller                  Car FSM
                                        │
                              ┌─────────┼─────────┐
                              │         │         │
                            Move      Stop      Avoid
                              │         │         │
                              └─────────┼─────────┘
                                        │
                                Traffic / Obstacles
```

## License

This repository is intended for portfolio and educational purposes.

Please check the license of any third-party assets included in the Unity project before redistributing the project or its assets.

## Author

**Harsa Archive**

Full-Stack Developer | Game Developer | 3D Designer

GitHub: https://github.com/harsarcv
