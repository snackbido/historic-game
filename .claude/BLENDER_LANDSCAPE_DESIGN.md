# PREHISTORIC TRIBE --- BLENDER LANDSCAPE DESIGN SPECIFICATION

## 0. Purpose

This document is a **scene-generation specification for Claude + Blender
MCP**.

Goal: create a playable-looking 2.5D prehistoric survival/city-builder
landscape in Blender as a graybox/low-poly environment.

The scene must be understandable immediately from an isometric camera
and must be structured so that the generated objects can later be
replaced by real low-poly assets in Unity.

This document is intentionally concrete. Claude should use the
dimensions, coordinates, naming conventions, collections, and placement
rules below instead of inventing a different layout.

------------------------------------------------------------------------

# 1. Project Context

Source project specification:

-   Genre: Survival + City-Builder + Base Management
-   Setting: prehistoric tribe
-   Engine target: Unity
-   World representation: 2.5D
-   World plane: XZ
-   Y axis: vertical
-   Camera: perspective, angled from above
-   Visual style: 3D low-poly, flat shading
-   Environment palette: natural earth colors
-   Main gameplay systems: resources, building, farming, animals,
    technology, combat, disasters
-   Initial resources: wood, stone, food, water
-   Future systems include farming, animal husbandry, technology,
    combat, disasters

Architecture requirement:

-   Data / Logic / Presentation are separated.
-   Resource nodes are represented separately from visual terrain.
-   The environment should therefore be designed so terrain, decoration,
    and gameplay resource nodes can be separated into different Blender
    collections.

The original project architecture explicitly uses the XZ ground plane
with Y upward and is moving from the earlier 2D presentation to 2.5D 3D
models.

------------------------------------------------------------------------

# 2. Scene Goal

Create one complete coastal prehistoric landscape named:

`PT_Landscape_01`

The map represents a natural valley connected to:

1.  mountain range
2.  forest
3.  central grassland/plain
4.  river
5.  smaller streams
6.  rocky ground
7.  dry land
8.  beach
9.  sea

The player starting settlement is located in the central plain.

The river originates from the mountain area and flows through the plain
toward the sea.

The landscape must feel continuous rather than being divided into
rectangular biome blocks.

------------------------------------------------------------------------

# 3. World Coordinate System

Use Blender coordinates exactly as follows:

``` text
X = East / West
Y = Height
Z = North / South
```

Ground plane:

``` text
X: -100 to +100
Z: -100 to +100
Y: approximately 0
```

Total playable landscape size:

``` text
200 x 200 meters
```

Recommended base grid:

``` text
1 Blender unit = 1 meter
```

Do not rotate the entire world after creation.

North:

``` text
+Z
```

South:

``` text
-Z
```

East:

``` text
+X
```

West:

``` text
-X
```

------------------------------------------------------------------------

# 4. Overall Landscape Layout

Use this conceptual arrangement:

``` text
                         NORTH
                           +Z

       ┌───────────────────────────────────┐
       │       MOUNTAIN RANGE              │
       │   ⛰ ⛰ ⛰ ⛰ ⛰ ⛰                   │
       │   rocky slopes + forest edge      │
       │          ↓ stream                 │
       │        ↓                          │
       │   FOREST            FOREST        │
       │  🌲🌲🌲             🌲🌲           │
       │        \             /             │
       │         \           /              │
       │          CENTRAL PLAIN             │
       │             🏕 START               │
       │              │                     │
       │              │ river               │
       │              ↓                     │
       │       ROCKY      DRY LAND          │
       │       GROUND      /                │
       │              \   /                 │
       │               \ /                  │
       │             BEACH                  │
       │─────────────────────────────────── │
       │              SEA                  │
       └───────────────────────────────────┘

                           -Z
```

This is a spatial guideline, not a literal texture map.

Biome boundaries must be irregular.

------------------------------------------------------------------------

# 5. Major Biome Zones

## 5.1 Mountain Range

Approximate bounding area:

``` text
X = -80 to +45
Z = +45 to +100
```

Height:

``` text
Y = 2 to 18
```

The mountain range occupies the northern part of the map.

Do not make it a straight wall.

Create 4--7 major mountain masses with gaps between them.

Recommended peaks:

``` text
Peak_01: (-60, 0, 82), height 17
Peak_02: (-35, 0, 91), height 22
Peak_03: (-5, 0, 78), height 16
Peak_04: (22, 0, 88), height 19
Peak_05: (45, 0, 67), height 13
```

Important:

-   Mountain silhouettes should overlap.
-   Use large angular low-poly rocks.
-   Add smaller rocks around mountain bases.
-   Do not cover every surface with vegetation.
-   Highest areas should contain mostly rock.

Mountain transitions:

`text mountain peak → rocky slope → sparse trees → forest edge → grassland`

------------------------------------------------------------------------

# 6. Forest

Create two major forest clusters.

## Western Forest

Approximate area:

``` text
X = -95 to -35
Z = 5 to +60
```

## Eastern Forest

Approximate area:

``` text
X = +35 to +85
Z = +20 to +65
```

Forest density:

``` text
interior: 70–85%
edge: 25–50%
```

Use clustered placement.

Do NOT place trees using a uniform grid.

Tree types:

``` text
Tree_Large
Tree_Medium
Tree_Small
Tree_Dead
Bush
Bush_Berry
Grass_Tall
Grass_Short
```

Suggested distribution:

``` text
Large tree    15%
Medium tree   40%
Small tree    25%
Dead tree      5%
Bush          15%
```

Trees should have random rotation around Y.

Do not rotate trees around X/Z unless the model itself requires it.

------------------------------------------------------------------------

# 7. Central Plain

This is the most important gameplay area.

Approximate area:

``` text
X = -45 to +45
Z = -20 to +45
```

Average height:

``` text
Y = 0
```

Allowed variation:

``` text
Y = -0.4 to +0.8
```

The central plain should be the flattest area on the map.

Purpose:

-   starting settlement
-   building
-   farming
-   animal husbandry
-   expansion

Do not place large rocks or dense trees in the central starting area.

------------------------------------------------------------------------

# 8. Starting Settlement Area

Center:

``` text
X = 0
Y = 0
Z = +5
```

Clear radius:

``` text
12 meters
```

Inside this radius:

-   no large rocks
-   no dense trees
-   no steep terrain
-   no water
-   no mountain
-   no cliffs

Optional placeholder objects:

``` text
Start_Campfire
Start_Shelter
Start_Storage
Start_FarmPlot
```

These are only visual placeholders.

The actual Unity building system will later control construction.

------------------------------------------------------------------------

# 9. River System

The river is a major visual and gameplay feature.

The river MUST have a believable source.

Flow:

``` text
Mountain
    ↓
small stream
    ↓
stream junction
    ↓
main river
    ↓
south
    ↓
sea
```

## Main River

Approximate centerline:

``` text
(-5, +70)
(-12, +50)
(-4, +35)
(+4, +20)
(+8, 0)
(+12, -25)
(+18, -48)
(+25, -65)
(+30, -82)
```

Coordinates are X/Z pairs.

River width:

``` text
mountain stream: 1.0–2.0 m
upper river:      2.0–3.0 m
central river:    3.0–5.0 m
lower river:      4.0–7.0 m
```

The river should widen toward the sea.

River bank:

``` text
grass
soil
small rocks
pebbles
occasional reeds
```

Avoid perfectly smooth edges.

------------------------------------------------------------------------

# 10. Small Streams

Create 2--3 smaller streams.

Stream A:

``` text
from western mountain
→ forest
→ joins main river
```

Stream B:

``` text
from eastern mountain
→ eastern forest
→ joins main river
```

Stream C:

``` text
short seasonal stream
→ rocky ground
→ main river
```

Streams should be narrower than the main river.

------------------------------------------------------------------------

# 11. River Rocks and Pebbles

Place rocks near the river.

Distribution:

``` text
large rock: 10%
medium rock: 30%
small rock: 35%
pebble cluster: 25%
```

Higher density near:

-   bends
-   waterfalls
-   stream junctions
-   shallow water
-   river banks

Lower density near the starting settlement.

------------------------------------------------------------------------

# 12. Waterfalls

Create 2 small waterfalls in the mountain area.

Waterfall 01:

``` text
near (-20, +62)
```

Waterfall 02:

``` text
near (+15, +58)
```

Use simple geometry:

``` text
water plane
+
vertical water strip
+
white/bright foam geometry
+
rocks
```

Do not create complex fluid simulation.

This is a low-poly visual placeholder.

------------------------------------------------------------------------

# 13. Rocky Ground

Create a rocky zone south-east of the central plain.

Approximate area:

``` text
X = +35 to +85
Z = -20 to -65
```

Characteristics:

-   exposed soil
-   medium rocks
-   large rocks
-   pebbles
-   sparse vegetation

Purpose:

``` text
stone resource area
```

Resource nodes should be visually separate from decorative rocks.

Use:

``` text
Rock_Decorative
StoneNode_Large
StoneNode_Medium
StoneNode_Small
```

------------------------------------------------------------------------

# 14. Dry Land

Create a dry region south-west of the central plain.

Approximate area:

``` text
X = -80 to -25
Z = -25 to -70
```

Characteristics:

-   yellow-brown ground
-   sparse grass
-   sparse trees
-   small rocks
-   cracked/rough soil appearance
-   lower vegetation density

This area should contrast strongly with the central green plain.

Do not make it a desert.

It is dry prehistoric grassland.

------------------------------------------------------------------------

# 15. Beach

Create a coastal transition on the southern edge.

Approximate area:

``` text
Z = -80 to -100
```

Beach width:

``` text
5–15 meters
```

Beach shape must be irregular.

Use:

``` text
grassland
→ dry/sandy soil
→ sand
→ shallow water
→ deep sea
```

Do not create a perfectly straight coastline.

------------------------------------------------------------------------

# 16. Sea

Sea occupies the southern boundary.

Approximate area:

``` text
Z < -90
```

Use a large water plane.

Add:

-   shallow water
-   deep water
-   small rocks
-   2--4 small offshore rock/island formations

No detailed ocean simulation is required.

Use a simple low-poly water material.

------------------------------------------------------------------------

# 17. Offshore Rocks / Small Islands

Create 3--5 small rocky formations.

Example locations:

``` text
(-55, -92)
(+5, -95)
(+55, -90)
(+75, -72)
```

Each formation:

``` text
2–6 rocks
+
small vegetation
```

Do not create large islands.

------------------------------------------------------------------------

# 18. Terrain Transitions

Biome boundaries MUST NOT be straight.

Required transition rules:

``` text
Mountain → Rocky slope → Forest → Plain

Forest → Forest edge → Grassland

Plain → Dry grassland

Plain → River bank → River

Dry land → Sandy land → Beach

Beach → Shallow water → Deep sea
```

Use irregular patches.

The landscape should look hand-authored even if generated procedurally.

------------------------------------------------------------------------

# 19. Ground Materials

Create these materials:

``` text
MAT_Ground_Plain
MAT_Ground_Forest
MAT_Ground_Mountain
MAT_Ground_Rock
MAT_Ground_Dry
MAT_Ground_Sand
MAT_Water_River
MAT_Water_Sea
MAT_Rock
MAT_Wood
MAT_Grass
```

Style:

-   low-poly
-   flat shading
-   rough materials
-   no metallic look except future ore objects
-   avoid photorealism
-   avoid realistic PBR complexity

Suggested palette:

``` text
Plain      = muted natural green
Forest     = dark natural green
Dry        = yellow-brown
Rock       = grey
Mountain   = dark grey / brown-grey
Sand       = pale warm beige
River      = muted blue
Sea        = darker blue
Wood       = brown
```

Do not use neon colors.

------------------------------------------------------------------------

# 20. Ground Geometry Strategy

Do NOT make the entire world one perfectly flat plane.

Use a low-poly terrain mesh with broad elevation changes.

Suggested height levels:

``` text
Sea              Y = -0.5
Beach            Y = 0
Plain            Y = 0 to +0.8
Forest           Y = 0 to +2
Rocky ground     Y = +0.5 to +3
Mountain slope   Y = +2 to +10
Mountain peaks   Y = +10 to +22
```

The central starting zone remains mostly flat.

------------------------------------------------------------------------

# 21. Low-Poly Modeling Rules

All generated environment geometry should follow these rules:

-   Shade Flat.
-   Use simple meshes.
-   Avoid excessive subdivision.
-   Use bevels only when visually useful.
-   Use irregular silhouettes.
-   Prefer 6--20 sided forms for rocks.
-   Trees can use simplified low-poly trunks and crowns.
-   Do not generate high-poly photorealistic assets.

Target visual:

``` text
stylized
low-poly
readable
natural
game-ready
```

------------------------------------------------------------------------

# 22. Rock Design

Create at least 4 rock shapes:

``` text
Rock_Small
Rock_Medium
Rock_Large
Rock_Flat
```

Each should have:

-   irregular silhouette
-   flat shading
-   6--12 major faces
-   slightly different proportions

Create multiple instances with different scales.

Never duplicate a rock at identical scale and rotation repeatedly.

------------------------------------------------------------------------

# 23. Tree Design

Create simplified low-poly trees.

Tree structure:

``` text
Trunk
+
1–3 foliage masses
```

Avoid realistic branch simulation.

Create at least:

``` text
Tree_Small
Tree_Medium
Tree_Large
Tree_Dead
```

Each should have a different silhouette.

------------------------------------------------------------------------

# 24. Grass and Small Vegetation

Use small clusters instead of individual grass blades.

Create:

``` text
GrassCluster_Small
GrassCluster_Medium
GrassCluster_Large
Bush_Small
Bush_Berry
Reed_River
```

Place denser grass:

-   forest edges
-   river banks
-   plains

Place less grass:

-   rocky area
-   dry land
-   mountain slopes
-   beach

------------------------------------------------------------------------

# 25. Resource Node Placement

Decorative objects and resource nodes must be separate.

Collections:

``` text
ENV_Decoration
RESOURCE_Nodes
```

Example wood nodes:

``` text
Resource_Wood_001
Resource_Wood_002
...
```

Stone nodes:

``` text
Resource_Stone_001
Resource_Stone_002
...
```

Recommended starting distribution:

``` text
Wood:
  mostly forest

Stone:
  mostly mountain / rocky ground

Water:
  river / streams

Food:
  forest edge / river / future farm
```

Do not put resource nodes inside the starting settlement clear radius.

------------------------------------------------------------------------

# 26. Collection Structure in Blender

Create these collections exactly:

``` text
PT_Landscape_01
│
├── TERRAIN
│   ├── Terrain_Plain
│   ├── Terrain_Forest
│   ├── Terrain_Dry
│   ├── Terrain_Rocky
│   ├── Terrain_Mountain
│   ├── Terrain_Beach
│   └── Terrain_Water
│
├── ENV_Decoration
│   ├── Trees
│   ├── Bushes
│   ├── Grass
│   ├── Rocks
│   └── Pebbles
│
├── RESOURCE_Nodes
│   ├── Wood
│   ├── Stone
│   └── Water
│
├── WATER_FEATURES
│   ├── River
│   ├── Streams
│   ├── Waterfalls
│   └── Sea
│
├── START_AREA
│
├── LANDMARKS
│
└── DEBUG
```

------------------------------------------------------------------------

# 27. Object Naming

Use PascalCase.

Examples:

``` text
TerrainPlain_Main
TerrainForest_West
TerrainForest_East
TerrainDry_SouthWest
TerrainRocky_SouthEast
MountainPeak_01
MountainPeak_02
River_Main
Stream_West
Stream_East
Waterfall_01
Tree_Large_001
Rock_Medium_001
StoneNode_Large_001
StartArea_Center
```

Never use:

``` text
Cube.001
Plane.034
Object123
Thing
Test
```

for final scene objects.

Temporary objects may use a `TMP_` prefix and should be removed before
completion.

------------------------------------------------------------------------

# 28. Camera

Create:

``` text
Camera_GamePreview
```

Camera type:

``` text
Perspective
```

Recommended starting position:

``` text
X = 115
Y = 125
Z = 115
```

Point camera toward:

``` text
X = 0
Y = 0
Z = 0
```

Approximate view:

``` text
45–55 degree downward angle
```

The entire landscape should fit into the frame.

Create a second optional camera:

``` text
Camera_StartArea
```

focused on the central settlement area.

------------------------------------------------------------------------

# 29. Lighting

Create:

``` text
Sun_Main
World_Light
```

Use one large directional sun.

Lighting should create:

-   readable terrain
-   readable low-poly facets
-   soft but visible shadows
-   clear separation between mountains, forest and plain

Avoid dramatic cinematic lighting.

This is a gameplay environment, not a cinematic scene.

------------------------------------------------------------------------

# 30. Atmospheric Depth

Use subtle atmospheric depth.

Far mountains should be slightly less visually dominant than foreground
terrain.

Do not use heavy fog.

The player should be able to understand the map layout from the main
camera.

------------------------------------------------------------------------

# 31. Gameplay Readability

The landscape must visually communicate:

``` text
Where can I build?
        ↓
Central plain

Where is wood?
        ↓
Forest

Where is stone?
        ↓
Mountain / rocky area

Where is water?
        ↓
River / streams

Where can farming happen?
        ↓
Flat plain near water

Where is the sea?
        ↓
South

Where is dangerous terrain?
        ↓
Mountain / dense forest / rocky area
```

------------------------------------------------------------------------

# 32. Starting Area Rules

The starting area must satisfy:

``` text
flat
near river
near forest
near stone
not inside forest
not inside mountain
not directly on beach
```

This creates immediate access to the four fundamental resources:

``` text
wood
stone
food
water
```

without making the entire map equally accessible.

------------------------------------------------------------------------

# 33. Landmark Placement

Create visually memorable landmarks:

``` text
LANDMARK_MountainPeak
LANDMARK_Waterfall
LANDMARK_LargeRock
LANDMARK_OldTree
LANDMARK_RiverBend
```

At least:

``` text
1 major mountain silhouette
2 waterfalls
1 large ancient tree
1 large rock formation
1 major river bend
```

These help players orient themselves.

------------------------------------------------------------------------

# 34. Variation Rules

Use deterministic variation.

Do not rely on completely random placement.

Recommended:

``` text
Seed = 20261002
```

If procedural generation is used, the same seed should reproduce the
same layout.

Variation can include:

``` text
scale
rotation Y
small position offset
tree type
rock type
grass density
```

Do not vary:

``` text
core gameplay landmarks
starting area
main river path
major mountain positions
```

------------------------------------------------------------------------

# 35. Blender MCP Execution Order

Claude should execute the scene in this order:

## Step 1 --- Clean scene

Remove default objects unless needed.

Create root collection:

``` text
PT_Landscape_01
```

## Step 2 --- Create materials

Create all `MAT_*` materials.

## Step 3 --- Create terrain base

Create:

-   plain
-   forest ground
-   dry ground
-   rocky ground
-   beach
-   mountain masses
-   sea

## Step 4 --- Create river

Create main river.

Then streams.

Then waterfalls.

## Step 5 --- Create environmental assets

Create simple low-poly:

-   trees
-   bushes
-   rocks
-   pebbles
-   grass

## Step 6 --- Populate environment

Use deterministic placement.

## Step 7 --- Create resource nodes

Create separate resource node objects.

## Step 8 --- Create starting area

Create clear area and optional placeholder camp.

## Step 9 --- Create landmarks

Add mountain peak, ancient tree, waterfall, large rock.

## Step 10 --- Camera and lighting

Create gameplay preview camera and lighting.

## Step 11 --- Validation

Check:

``` text
[ ] map approximately 200x200
[ ] central area is buildable
[ ] river reaches sea
[ ] mountains are north
[ ] sea is south
[ ] forest exists west/east
[ ] rocky area exists south-east
[ ] dry area exists south-west
[ ] beach separates land and sea
[ ] resource nodes are separate from decoration
[ ] all final objects use correct naming
[ ] no unnecessary high-poly geometry
[ ] no default Cube/Camera/Light left unintentionally
```

------------------------------------------------------------------------

# 36. Blender MCP Validation Requirements

After generation, Claude should inspect the Blender scene and report:

``` text
Scene name
Object count
Collection count
Material count
Terrain objects
Water objects
Resource nodes
Starting area
Main camera
Lighting objects
```

Also verify that:

``` text
River_Main
```

physically connects the mountain region to the sea region.

Verify that:

``` text
StartArea_Center
```

is inside the central plain.

Verify that no major mountain intersects the starting area.

------------------------------------------------------------------------

# 37. Important Constraint

Do NOT turn this into a photorealistic environment.

Do NOT create:

-   realistic vegetation simulation
-   realistic fluid simulation
-   high-resolution terrain sculpt
-   cinematic environment
-   complex particle systems

The target is:

``` text
LOW-POLY
+
2.5D
+
GAMEPLAY READABILITY
+
PREHISTORIC
+
CITY BUILDER
```

The scene is a **production-oriented graybox/visual prototype**, not a
final art asset.

------------------------------------------------------------------------

# 38. Expected Final Result

The final Blender scene should communicate this at a glance:

``` text
                MOUNTAINS
            ⛰️ ⛰️ ⛰️ ⛰️
           🌲    🪨    🌲
              ↓
            STREAM
              ↓
       FOREST        FOREST
      🌲🌲🌲          🌲🌲
          \          /
           \        /
          CENTRAL PLAIN
             🏕️
             🟩
              \
               🟦
                \
             🪨 ROCKY
                  \
                   🟨 DRY
                     \
                    BEACH
──────────────────────────
            SEA
```

The map should look like a natural prehistoric valley that a player
could immediately imagine building a tribe settlement in.

------------------------------------------------------------------------

# 39. Relationship to Unity

The Blender scene is a visual prototype.

When exporting to Unity:

``` text
Terrain
Environment
Resource Nodes
Water
Landmarks
```

should remain logically separable.

Do not merge the entire world into one mesh.

This preserves the project's Data / Logic / Presentation separation and
makes later replacement with Unity prefabs easier.

------------------------------------------------------------------------

# 40. Claude Instruction

When Claude reads this specification through Blender MCP:

1.  Follow the coordinate system exactly.
2.  Follow the major biome locations.
3.  Follow the river path.
4.  Keep the starting area clear and flat.
5.  Use low-poly geometry.
6.  Use deterministic placement.
7.  Keep resource nodes separate from decoration.
8.  Create collections and names exactly as specified where practical.
9.  Validate the scene after generation.
10. If Blender MCP cannot perform a requested operation, approximate it
    with simple geometry rather than inventing a completely different
    landscape.
11. Do not redesign the gameplay layout unless explicitly instructed.
12. Prefer simple, readable geometry over visual complexity.

END OF SPECIFICATION
