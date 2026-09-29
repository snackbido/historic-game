// Data layer — tương đương các ScriptableObject trong Assets/_Data/.
// Thêm loại tài nguyên / công trình / cây / vật nuôi / công nghệ mới chỉ cần sửa file này,
// không phải sửa logic (nguyên tắc data-driven trong ARCHITECTURE.md § 2.1).

export const RESOURCE_TYPES = [
  { id: 'wood', displayName: 'Gỗ', icon: '🌲' },
  { id: 'food', displayName: 'Thức ăn', icon: '🍖' },
  { id: 'knowledge', displayName: 'Tri thức', icon: '💡' },
];

export const BUILDINGS = [
  {
    id: 'hut',
    displayName: 'Lều trại',
    model: 'hut',
    costs: [{ type: 'wood', amount: 10 }],
    unlockedByDefault: true,
  },
  {
    id: 'storage',
    displayName: 'Kho chứa',
    model: 'storage',
    costs: [{ type: 'wood', amount: 20 }],
    unlockedByDefault: false,
  },
];

export const CROPS = [
  {
    id: 'berry',
    displayName: 'Cây mọng',
    unlockedByDefault: false,
    timeToSprout: 5,
    timeToMature: 10,
    witherTime: 20,
    harvestYield: [{ type: 'food', amount: 3 }],
    visual: { sprout: 0x6fae3a, leaves: 0x3f8a36, fruit: 0xc4193a, withered: 0x6e5e4a },
  },
];

export const ANIMALS = [
  {
    id: 'wild_boar',
    displayName: 'Heo rừng',
    hungerDecayInterval: 20,
    hungerThresholdForNeeds: 50,
    feedCost: [{ type: 'food', amount: 3 }],
    feedingsToTame: 2,
    reproductionInterval: 45,
    products: [{ type: 'food', amount: 2 }],
    productionInterval: 15,
    // Bản Unity không giới hạn — thêm trần để đàn heo không sinh sản vô hạn làm nặng trình duyệt.
    maxPopulation: 8,
    visual: { body: 0x8a5a33, tamedTint: [0.6, 0.9, 0.6] },
  },
];

export const TECHS = [
  {
    id: 'tech_farming',
    displayName: 'Nông nghiệp',
    description: 'Thuần hóa cây dại, dựng kho để tích trữ.',
    cost: [{ type: 'knowledge', amount: 5 }],
    prerequisites: [],
    unlockedBuildingIds: ['storage'],
    unlockedCropIds: ['berry'],
  },
];

export const CONFIG = {
  player: { moveSpeed: 4, radius: 0.45, interactRadius: 1.2 },
  knowledgeTypeId: 'knowledge',
  knowledgeGenerationInterval: 3,
  cellSize: 1,
  worldHalfSize: 16,
  saveKey: 'prehistoric-tribe/save-v1',
};

// Bố cục bản đồ ban đầu — cùng tọa độ với GameplaySceneBuilder.cs, thêm vài cây/ô đất cho đỡ trống.
export const WORLD_LAYOUT = {
  playerStart: { x: 0, y: 0 },
  trees: [
    { x: 2, y: 1 }, { x: 4.5, y: 2.5 }, { x: -1.5, y: 3 }, { x: 6, y: -0.5 },
    { x: -5, y: 2 }, { x: 1, y: 4.5 }, { x: 7, y: 3.5 }, { x: -6.5, y: -3.5 },
  ].map((t) => ({ ...t, typeId: 'wood', amount: 10, yieldPerHit: 1 })),
  farmPlots: [
    { x: -2, y: -1.5 }, { x: -3.2, y: -1.5 },
    { x: -2, y: -2.7 }, { x: -3.2, y: -2.7 },
  ],
  animals: [{ id: 'wild_boar', x: 3, y: -1.5 }],
};

const resourcesById = new Map(RESOURCE_TYPES.map((r) => [r.id, r]));
export const getResourceType = (id) => resourcesById.get(id);

/** "+3 🍖 Thức ăn, +1 🪵 Gỗ" */
export function formatAmounts(amounts, sign = '') {
  return amounts
    .map(({ type, amount }) => {
      const r = getResourceType(type);
      return `${sign}${amount} ${r?.icon ?? ''} ${r?.displayName ?? type}`.replace(/\s+/g, ' ');
    })
    .join(', ');
}
