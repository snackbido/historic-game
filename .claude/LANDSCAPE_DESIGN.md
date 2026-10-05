# LANDSCAPE_DESIGN.md --- Thiết kế Landscape 2.5D Pixel Art

## 1. Mục tiêu

Thiết kế một landscape có thể dùng trực tiếp làm **visual target /
level-design reference** cho game `Prehistoric Tribe`.

Game là survival + city-builder + base management thời tiền sử. Thế giới
sử dụng:

-   Unity + C#
-   Góc nhìn 2.5D
-   Model 3D low-poly
-   Pixel-art presentation
-   Camera nhìn nghiêng từ trên xuống
-   Nhân vật di chuyển trên mặt đất theo mặt phẳng XZ
-   Có thể zoom camera
-   Landscape phải đủ rõ để người chơi nhận biết địa hình, tài nguyên và
    vùng xây dựng.

Landscape không chỉ là một background đẹp. Nó phải được thiết kế như một
**playable map**, trong đó địa hình ảnh hưởng trực tiếp đến việc di
chuyển, xây dựng, thu thập tài nguyên và mở rộng làng.

------------------------------------------------------------------------

# 2. Art Direction

## 2.1 Phong cách tổng thể

Phong cách mục tiêu:

> **3D low-poly world + pixel-art visual treatment + 2.5D isometric-like
> camera**

Không làm world hoàn toàn 2D.

Các object chính vẫn là:

-   Terrain 3D
-   Rock 3D
-   Tree 3D
-   Building 3D
-   Character 3D
-   Animal 3D
-   Water surface 3D

Pixel art được dùng như **ngôn ngữ hình ảnh**, thông qua:

-   texture pixelated
-   palette giới hạn
-   silhouette rõ
-   ít chi tiết nhỏ
-   shading theo mảng
-   outline hoặc contrast nhẹ nếu cần
-   độ phân giải texture thấp
-   tránh texture photorealistic.

## 2.2 Palette

Palette cơ bản:

### Ground

-   Grass Dark
-   Grass Mid
-   Grass Light
-   Dirt
-   Dry Dirt
-   Sand
-   Mud

### Rock

-   Dark Stone
-   Stone
-   Light Stone
-   Moss Stone

### Vegetation

-   Dark Green
-   Forest Green
-   Light Green
-   Yellow Green

### Water

-   Deep Water
-   Water
-   Shallow Water
-   Foam / Highlight

### Buildings

-   Wood Dark
-   Wood
-   Straw
-   Bone
-   Stone

### UI / Gameplay Highlight

-   Selection
-   Valid Placement
-   Invalid Placement
-   Warning
-   Resource Highlight

Không cần sử dụng quá nhiều màu. Mục tiêu là tạo một world dễ đọc ngay
cả khi camera zoom out.

------------------------------------------------------------------------

# 3. Camera

## 3.1 Camera type

Sử dụng camera 3D của Unity.

Khuyến nghị prototype:

``` text
Projection:
    Perspective

Pitch:
    35° - 45°

Yaw:
    45° hoặc khoảng 45°

Follow:
    Player / Village Center

Zoom:
    Có

Movement:
    Pan theo X/Z

Rotation:
    Có thể để phiên bản sau
```

Camera cần tạo cảm giác:

``` text
       Mountain
          /\
         /  \
    Tree       Tree

          Village
       ┌─────────┐
 River │ Houses  │
 ~~~~~ │ Campfire│
 ~~~~~ └─────────┘

              Forest
```

## 3.2 Camera hierarchy

``` text
Main Camera
    └── CameraRig
          ├── FollowTarget
          └── Camera
```

`CameraRig` chịu trách nhiệm:

-   follow
-   pan
-   zoom
-   giới hạn camera
-   camera smoothing

Camera không nên phụ thuộc trực tiếp vào từng object gameplay.

------------------------------------------------------------------------

# 4. World Layout tổng thể

Landscape mẫu nên có dạng một vùng đất lớn được bao quanh một phần bởi
nước.

## 4.1 Macro layout

``` text
┌─────────────────────────────────────────────────────┐
│                     MOUNTAINS                       │
│              ▲ ▲ ▲ ▲ ▲ ▲ ▲                         │
│          ROCKY / HIGH TERRAIN                       │
│                                                     │
│      FOREST                    FOREST               │
│    🌲 🌲 🌲                  🌲 🌲 🌲               │
│    🌲  🪨 🌲        OPEN LAND       🌲              │
│                  ┌──────────────┐                   │
│                  │    VILLAGE   │                   │
│                  │  CAMP / FARM │                   │
│                  └──────────────┘                   │
│                        │                            │
│                    RIVER                            │
│              ~~~~~~~~~~~~~~~~                       │
│              ~~~~~~~~~~~~~~~~      ROCK BEACH       │
│                                                     │
│                          SEA                        │
│        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~        │
│        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~        │
└─────────────────────────────────────────────────────┘
```

Đây là **conceptual layout**, không phải tile map cố định.

------------------------------------------------------------------------

# 5. Các vùng địa hình chính

Landscape nên chia thành 7 vùng chính:

1.  Central Plains
2.  Forest
3.  Mountain / Rocky Area
4.  River
5.  Lake / Water
6.  Beach / Shore
7.  Resource & Expansion Zones

------------------------------------------------------------------------

# 6. Central Plains --- vùng trung tâm

## Mục đích

Đây là vùng gameplay quan trọng nhất.

Dùng cho:

-   Village
-   Houses
-   Campfire
-   Storage
-   Farm
-   Workshop
-   Paths
-   NPC movement

## Đặc điểm

Terrain:

-   tương đối phẳng
-   ít đá lớn
-   ít cây
-   đất màu xanh/nâu
-   có các patch cỏ
-   có dirt path tự nhiên

## Quy tắc

Central Plains phải có ít nhất:

``` text
1 large buildable area
2-3 medium buildable areas
1 natural path
1 resource connection
1 water access
```

Không đặt quá nhiều decoration vào vùng này.

Mục tiêu là để người chơi có cảm giác:

> "Đây là nơi thích hợp để xây làng."

------------------------------------------------------------------------

# 7. Forest --- khu rừng

## Mục đích

Nguồn tài nguyên đầu game:

-   Wood
-   Wild food
-   Animals
-   Herbs
-   Stone nhỏ

## Composition

Không rải cây đều nhau.

Dùng:

``` text
Tree cluster
Tree cluster
Open gap
Tree cluster
Rock
Bush
Tree cluster
```

Ví dụ:

``` text
🌲 🌲    🌲
🌲 🌲 🪨 🌲

   🌿
       🌲

🌲 🌲 🌲
```

## Forest density

3 mức:

### Forest Edge

Ít cây.

Dùng để chuyển tiếp từ plains → forest.

### Normal Forest

Mật độ trung bình.

Có:

-   cây
-   bụi
-   đá
-   cỏ
-   resource nodes

### Deep Forest

Mật độ cao.

Có thể dùng làm:

-   vùng nguy hiểm
-   spawn animal
-   resource-rich zone
-   expansion zone sau này

------------------------------------------------------------------------

# 8. Mountain / Rocky Area

## Mục đích

Tạo boundary tự nhiên và resource zone.

Có:

-   large rocks
-   cliffs
-   stone nodes
-   narrow paths
-   high ground

Không nên biến toàn bộ mountain thành wall.

Nên tạo:

``` text
Mountain
████████
██    ██
██    ███
██       █
    Path
```

Có 1-2 đường đi xuyên qua mountain.

## Gameplay

Mountain có thể cung cấp:

-   Stone
-   Ore trong future
-   Rare resources
-   defensive position

Tech tree về sau có thể mở:

-   mining
-   quarry
-   advanced stone structures

------------------------------------------------------------------------

# 9. River

River là một landmark lớn của map.

## Shape

Không làm sông thẳng.

Nên có:

``` text
Mountain
    \
     \
      \____
           \____
                \
                 \~~~~ Sea
```

River cần có:

-   bends
-   shallow sections
-   rocks
-   riverbank
-   small islands nếu cần.

## Gameplay

River cung cấp:

-   water
-   fishing
-   farming access
-   natural boundary

## Crossing

Ban đầu:

-   không thể đi qua ở mọi vị trí.

Có thể có:

-   shallow crossing
-   bridge sau khi unlock technology.

------------------------------------------------------------------------

# 10. Lake

Lake là vùng nước tĩnh.

Có thể đặt:

-   gần forest
-   gần village
-   gần mountain

Lake không nên chiếm quá nhiều diện tích.

Mục tiêu:

> tạo landmark và gameplay resource, không làm map bị chia cắt.

------------------------------------------------------------------------

# 11. Beach / Shore

Vùng chuyển tiếp:

``` text
Grass
  ↓
Dirt
  ↓
Sand
  ↓
Shallow Water
  ↓
Deep Water
```

Không chuyển màu đột ngột.

Nên sử dụng transition tiles:

``` text
Grass
Grass/Dirt
Dirt
Dirt/Sand
Sand
Wet Sand
Shallow Water
Water
```

------------------------------------------------------------------------

# 12. Terrain Layering

Landscape nên có nhiều layer.

``` text
Layer 0 — Base Terrain
Layer 1 — Ground Variation
Layer 2 — Terrain Transition
Layer 3 — Rocks
Layer 4 — Vegetation
Layer 5 — Resource Nodes
Layer 6 — Buildings
Layer 7 — Characters
Layer 8 — Effects
```

Không nên đặt tất cả object thành một mesh duy nhất.

Lý do:

-   dễ chỉnh sửa
-   dễ thay asset
-   dễ spawn/despawn
-   dễ save/load
-   dễ tối ưu
-   dễ thêm gameplay.

------------------------------------------------------------------------

# 13. Terrain Tile System

Nếu sử dụng grid:

``` text
Grid
 └── Terrain Cell
       ├── Ground Type
       ├── Height
       ├── Walkable
       ├── Buildable
       ├── Water
       ├── ResourceAllowed
       └── Biome
```

Ví dụ:

``` text
TerrainCell
{
    groundType = Grass
    height = 0
    walkable = true
    buildable = true
    water = false
}
```

## Ground types

``` text
Grass
Dirt
ForestFloor
Rock
Sand
ShallowWater
DeepWater
Mud
```

------------------------------------------------------------------------

# 14. Height Design

Không nên làm terrain quá phẳng.

Dùng khoảng:

``` text
Level 0 = Water
Level 1 = Shore
Level 2 = Plains
Level 3 = Hills
Level 4 = Mountain
```

Ví dụ:

``` text
          Mountain
             ███
          ███████
       ███████████

          Hill
        █████
      ███████

Plains
────────────────────

Shore
════════════════════

Water
~~~~~~~~~~~~~~~~~~~~
```

Height difference phải đủ lớn để camera 2.5D tạo chiều sâu nhưng không
che gameplay.

------------------------------------------------------------------------

# 15. Buildable Area

Đây là một trong những phần quan trọng nhất.

Landscape đẹp nhưng nếu không có đủ vùng xây dựng thì không phù hợp
city-builder.

## Buildable zone

Mỗi zone có thể được định nghĩa:

``` text
BuildZone
    id
    bounds
    terrainType
    maxSlope
    allowedBuildingTypes
```

## Phân bố

Map mẫu:

``` text
             Mountain
          ███████████

     Forest        Forest

        ┌─────────────┐
        │ BUILD ZONE  │
        │   VILLAGE   │
        └─────────────┘

     Farm Zone     Farm Zone

~~~~~~~~~~~~ River ~~~~~~~~~~~~

            Beach
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
              Sea
```

------------------------------------------------------------------------

# 16. Natural Paths

Không tạo đường bằng cách vẽ texture đơn giản.

Đường nên hình thành từ:

-   terrain variation
-   dirt
-   removed grass
-   stones
-   footprints
-   NPC traffic

Có thể bắt đầu bằng procedural path đơn giản.

Ví dụ:

``` text
Village
   │
   │
  ╱
 ╱
Forest

Village
   │
   └──────── Farm

Village
      ╲
       ╲
       River
```

------------------------------------------------------------------------

# 17. Resource Distribution

Không spawn tài nguyên ngẫu nhiên hoàn toàn.

Mỗi biome có resource profile.

## Forest

``` text
Wood       High
Food       Medium
Stone      Low
Animal     Medium
```

## Mountain

``` text
Wood       Low
Food       Low
Stone      High
Ore        Future
Animal     Low
```

## River

``` text
Water      High
Food       Medium
Wood       Medium
Stone      Low
```

## Plains

``` text
Wood       Low
Food       Medium
Farm       Excellent
Building   Excellent
```

------------------------------------------------------------------------

# 18. Resource Node Visuals

Resource phải nhìn thấy được từ camera zoom out.

### Wood

-   tree cluster
-   stump
-   fallen log

### Stone

-   rock cluster
-   large boulder

### Food

-   berry bush
-   wild plant
-   animal

Không dùng icon 2D lớn gắn cố định lên world trừ khi cần gameplay
highlight.

------------------------------------------------------------------------

# 19. Vegetation Design

Dùng 3 kích thước:

``` text
Small
Medium
Large
```

Ví dụ:

``` text
small bush
small bush
medium tree
large tree
rock
grass
flower
```

Không lặp cùng một prefab quá nhiều.

Có thể tạo variation:

``` text
Tree_A
Tree_B
Tree_C
Tree_D
```

Khác nhau về:

-   rotation
-   scale nhỏ
-   shape
-   color variation

------------------------------------------------------------------------

# 20. Rock Design

Rock là phần quan trọng để phá sự đồng đều của terrain.

Các loại:

``` text
Pebble
SmallRock
MediumRock
LargeRock
RockCluster
Cliff
```

Placement:

-   gần mountain
-   riverbank
-   beach
-   forest
-   random decoration trên plains nhưng mật độ thấp.

------------------------------------------------------------------------

# 21. Water Design

Water không cần simulation vật lý phức tạp ở MVP.

Có thể dùng:

``` text
Plane / Quad
+
Pixel texture
+
Scrolling UV
+
Simple shader
```

Visual:

-   pixelated wave
-   2-3 tone màu
-   shoreline highlight
-   foam ở cạnh.

Không nên dùng realistic water shader.

------------------------------------------------------------------------

# 22. Weather / Disaster Visual Zones

Theo SPEC, game có:

-   flood
-   wildfire
-   earthquake
-   storm
-   drought
-   harsh winter

Landscape nên có khả năng nhận disaster overlay.

Ví dụ:

``` text
Normal Terrain
      ↓
Warning
      ↓
Disaster Area
      ↓
Aftermath
```

### Flood

River water expands vào:

-   farmland
-   roads
-   low terrain
-   buildings gần river.

### Wildfire

Forest trở thành:

``` text
Normal
  ↓
Dry
  ↓
Burning
  ↓
Burned
```

### Drought

Water giảm:

``` text
Lake
River
Farm
```

------------------------------------------------------------------------

# 23. Pixel Art Asset Rules

Mỗi asset nên có silhouette rõ.

Ví dụ Tree:

``` text
       ███
     ███████
   ███████████
      █████
       ███
       ███
```

Không cần texture quá chi tiết.

Ưu tiên:

1.  silhouette
2.  color block
3.  readable shadow
4.  small details

------------------------------------------------------------------------

# 24. Pixel Resolution

Khuyến nghị bắt đầu:

``` text
Texture:
16x16
32x32
64x64
```

Object lớn:

``` text
128x128
```

Không nên tạo texture 1024x1024 cho mọi object nếu art direction là
pixel art.

Unity texture import:

``` text
Filter Mode:
Point

Compression:
None hoặc phù hợp pixel texture

Generate Mip Maps:
tùy asset
```

------------------------------------------------------------------------

# 25. Lighting

Lighting nên đơn giản.

Mục tiêu:

-   đọc terrain rõ
-   tạo chiều sâu
-   không realistic quá.

Prototype:

``` text
Directional Light
+
Ambient Light
+
Soft Shadows
```

Sun direction:

``` text
Top-left → Bottom-right
```

Giữ hướng ánh sáng ổn định để pixel-art asset không bị thay đổi cảm giác
quá nhiều.

------------------------------------------------------------------------

# 26. Shadows

Ưu tiên shadow mềm và dễ đọc.

Quan trọng nhất:

-   Building shadow
-   Tree shadow
-   Character shadow
-   Large rock shadow

Không cần shadow quá chi tiết cho grass nhỏ.

------------------------------------------------------------------------

# 27. Object Placement Rules

Không đặt object hoàn toàn random.

Dùng rule:

``` text
Biome
    ↓
Density
    ↓
Allowed Objects
    ↓
Spacing
    ↓
Gameplay Validation
```

Ví dụ:

``` text
Forest:
    Tree = 70%
    Bush = 15%
    Rock = 10%
    Special resource = 5%
```

Các tỷ lệ trên chỉ là starting point và cần điều chỉnh khi prototype.

------------------------------------------------------------------------

# 28. Unity Scene Structure

Scene đề xuất:

``` text
Gameplay
│
├── GameSystems
│   ├── GameManager
│   ├── ResourceManager
│   ├── BuildingSystem
│   ├── FarmingSystem
│   ├── AnimalSystem
│   ├── TechManager
│   └── DisasterManager
│
├── World
│   ├── Terrain
│   ├── Water
│   ├── Environment
│   ├── ResourceNodes
│   └── SpawnPoints
│
├── Village
│   ├── Buildings
│   ├── NPCs
│   └── Animals
│
├── Camera
│   └── CameraRig
│
└── UI
```

------------------------------------------------------------------------

# 29. Prefab Structure

Mỗi environment object nên là prefab.

``` text
Prefabs/
├── Environment/
│   ├── Trees/
│   ├── Rocks/
│   ├── Bushes/
│   ├── Terrain/
│   ├── Water/
│   └── Props/
│
├── Buildings/
├── Resources/
├── Animals/
├── Enemies/
└── Player/
```

Điều này phù hợp với kiến trúc hiện tại đang tách `Prefabs`, `Models`,
`Materials` và hệ thống gameplay khỏi presentation.

------------------------------------------------------------------------

# 30. Data-driven Landscape

Có thể tạo:

``` text
BiomeData : ScriptableObject
```

Ví dụ:

``` text
BiomeData
    biomeName
    groundMaterial
    treePrefabs
    rockPrefabs
    bushPrefabs
    resourceTypes
    resourceDensity
    animalTypes
    walkable
    buildable
```

Các biome:

``` text
PlainsBiome
ForestBiome
MountainBiome
RiverBiome
BeachBiome
```

Không hard-code biome trong logic.

------------------------------------------------------------------------

# 31. Map Generation

MVP không cần procedural generation ngay.

Nên làm:

``` text
Phase 1
Hand-crafted map

Phase 2
Reusable terrain modules

Phase 3
Seed-based procedural generation
```

Lý do:

Prototype cần kiểm soát:

-   gameplay
-   camera
-   movement
-   building
-   resource
-   save/load

trước khi đầu tư procedural generation.

------------------------------------------------------------------------

# 32. Landscape Prototype

Prototype đầu tiên chỉ cần:

``` text
1 x Plains
1 x Forest
1 x River
1 x Mountain
1 x Beach
1 x Water
```

Và:

``` text
1 Player
1 Wood resource
1 Tent
1 Campfire
1 Resource UI
```

Điều này khớp với MVP hiện tại: nhân vật di chuyển, thu thập gỗ, đặt
lều, UI tài nguyên và save/load.

------------------------------------------------------------------------

# 33. Recommended Prototype Map

Kích thước logic ban đầu:

``` text
64 x 64 cells
```

Hoặc nếu muốn test camera rộng hơn:

``` text
96 x 96 cells
```

Không nên bắt đầu bằng map quá lớn.

### Suggested layout

``` text
                    NORTH
                      ↑

        ┌──────────────────────────────┐
        │        MOUNTAIN              │
        │      ▲ ▲ ▲ ▲ ▲              │
        │    ▲ ▲ ROCK ▲ ▲             │
        │                              │
        │ FOREST            FOREST     │
        │ 🌲🌲🌲            🌲🌲🌲      │
        │ 🌲🌲                🌲        │
        │                              │
        │         VILLAGE              │
        │      ┌────────────┐          │
        │      │  BUILDING  │          │
        │      │    AREA    │          │
        │      └────────────┘          │
        │          FARM                │
        │                              │
        │     ~~~~~ RIVER ~~~~~        │
        │          ~~~~~               │
        │                              │
        │             BEACH            │
        │     ~~~~~~~~~~~~~~~~~        │
        │            SEA               │
        └──────────────────────────────┘

                      ↓
                    SOUTH
```

------------------------------------------------------------------------

# 34. Gameplay Readability

Mọi khu vực cần trả lời được 3 câu hỏi:

### 1. Tôi đang ở đâu?

Nhờ:

-   terrain color
-   landmarks
-   biome
-   camera.

### 2. Tôi có thể làm gì ở đây?

Nhờ:

-   resource nodes
-   buildable area
-   interactable objects.

### 3. Tôi có thể đi đâu?

Nhờ:

-   natural paths
-   river crossing
-   mountain gaps
-   terrain readability.

------------------------------------------------------------------------

# 35. Zoom Levels

Nên thiết kế landscape theo 3 zoom level.

## Zoom Near

Người chơi thấy:

-   character
-   building
-   trees
-   rocks
-   resource.

## Zoom Medium

Người chơi thấy:

-   village
-   forest
-   farms
-   river.

## Zoom Far

Người chơi thấy:

-   mountain
-   forest
-   river
-   coast
-   village position.

Nếu một landmark biến mất hoàn toàn ở zoom far thì nó có thể cần
silhouette hoặc scale phù hợp hơn.

------------------------------------------------------------------------

# 36. Mobile Consideration

SPEC định hướng hỗ trợ mobile ở giai đoạn sau.

Landscape cần tránh:

-   quá nhiều object nhỏ
-   quá nhiều particle
-   shader phức tạp
-   quá nhiều realtime shadow
-   terrain quá lớn trong một scene.

Ưu tiên:

``` text
Low-poly mesh
Simple material
Baked/static lighting khi phù hợp
Object pooling
LOD cho object lớn
Distance culling
Chunk-based loading nếu map lớn
```

------------------------------------------------------------------------

# 37. Performance Strategy

Khi map lớn:

``` text
World
 ├── Chunk 0
 ├── Chunk 1
 ├── Chunk 2
 ├── Chunk 3
 └── ...
```

Mỗi chunk quản lý:

-   terrain
-   vegetation
-   resource nodes
-   props.

Có thể unload decoration ở xa camera.

Gameplay data không nên phụ thuộc việc object presentation đang active
hay inactive.

------------------------------------------------------------------------

# 38. Separation of Logic and Presentation

Landscape phải tuân thủ kiến trúc:

``` text
DATA
 ↓
LOGIC
 ↓
PRESENTATION
```

Ví dụ:

``` text
ResourceNodeData
      ↓
ResourceNode
      ↓
Tree / Rock / Bush prefab
```

Không để tree prefab tự quyết định gameplay resource bằng cách
hard-code.

------------------------------------------------------------------------

# 39. Interaction

Theo kiến trúc hiện tại, interaction không nên phụ thuộc hoàn toàn vào
collider.

Landscape object có thể đăng ký:

``` text
InteractableRegistry
```

Ví dụ:

``` text
Tree
 ├── Transform
 ├── ResourceNode
 └── Interactable
```

Player tìm object gần nhất dựa trên khoảng cách trên mặt đất.

Điều này phù hợp với quyết định kiến trúc 2.5D hiện tại.

------------------------------------------------------------------------

# 40. Final Visual Target

Landscape hoàn chỉnh cần tạo cảm giác:

> Một vùng đất tiền sử rộng lớn, tự nhiên và có chiều sâu, nơi người
> chơi bắt đầu với một nhóm nhỏ người sống sót ở vùng đồng bằng gần
> nước, xung quanh là rừng cung cấp gỗ, núi cung cấp đá, sông tạo ranh
> giới và nguồn nước, còn vùng đất trống tạo không gian để phát triển
> thành làng.

Visual priority:

``` text
1. Readability
2. Gameplay
3. Composition
4. Pixel-art style
5. Detail
```

Không đảo ngược thứ tự này.

------------------------------------------------------------------------

# 41. Checklist triển khai

## Terrain

-   [ ] Central Plains
-   [ ] Forest
-   [ ] Mountain
-   [ ] River
-   [ ] Beach
-   [ ] Water
-   [ ] Terrain transitions
-   [ ] Height levels

## Gameplay

-   [ ] Walkable area
-   [ ] Buildable area
-   [ ] Resource zones
-   [ ] Natural paths
-   [ ] River crossing
-   [ ] Expansion zones

## Art

-   [ ] Pixel palette
-   [ ] Tree variants
-   [ ] Rock variants
-   [ ] Bush variants
-   [ ] Ground tiles
-   [ ] Water material
-   [ ] Building style
-   [ ] Character scale

## Unity

-   [ ] Terrain scene
-   [ ] CameraRig
-   [ ] Environment prefabs
-   [ ] BiomeData
-   [ ] Resource nodes
-   [ ] InteractableRegistry
-   [ ] Culling
-   [ ] LOD
-   [ ] Mobile performance test

------------------------------------------------------------------------

# 42. Relationship with Existing Architecture

Landscape design này không thay đổi gameplay architecture hiện tại.

`ARCHITECTURE.md` đã xác định world 2.5D nằm trên mặt phẳng XZ, Y là
chiều cao; logic/data được giữ nguyên và presentation chuyển sang model
3D. Landscape vì vậy tập trung vào presentation, terrain và level design
trong khi vẫn tương thích với các hệ thống `Building`, `Resources`,
`Farming`, `Animals`, `Tech`, `Combat` và `Disaster`.

`SPEC.md` xác định world là 2.5D low-poly, camera nhìn nghiêng từ trên
xuống, có rừng, sông, núi đá và các vùng đất hoang sơ. Thiết kế này
chuyển các yêu cầu đó thành một layout landscape cụ thể có thể dùng làm
reference khi dựng scene Unity.

------------------------------------------------------------------------

# 43. Next Design Step

Sau tài liệu này, nên tạo tiếp các design document riêng:

``` text
LANDSCAPE_DESIGN.md
        │
        ├── TERRAIN_TILE_SPEC.md
        ├── BIOME_SPEC.md
        ├── ENVIRONMENT_ASSET_SPEC.md
        ├── CAMERA_SPEC.md
        ├── BUILDABLE_ZONE_SPEC.md
        └── UNITY_SCENE_IMPLEMENTATION.md
```

Sau đó mới dùng các document này làm input để tạo:

``` text
Unity Scene
    ↓
Terrain
    ↓
Biome
    ↓
Environment Prefabs
    ↓
Resource Nodes
    ↓
Village
    ↓
Playable Prototype
```
