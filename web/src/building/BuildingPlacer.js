import { eventBus, Events, notify } from '../core/EventBus.js';

export class Building {
  kind = 'building';

  constructor(data, cell, center, cellSize) {
    this.data = data;
    this.cell = cell;
    this.x = center.x;
    this.y = center.y;
    this.halfSize = cellSize * 0.45;
  }
}

const cellKey = ({ cx, cy }) => `${cx},${cy}`;
const BLOCKING_KINDS = new Set(['player', 'animal', 'resourceNode', 'farmPlot']);

// Đặt công trình theo grid (Decision Log 2026-09-21), registry nhiều BuildingData như M4.
export class BuildingPlacer {
  selected = null;
  hoverCell = null;
  #occupied = new Map();

  constructor({ world, resources, tech, buildings, cellSize }) {
    this.world = world;
    this.resources = resources;
    this.tech = tech;
    this.buildings = buildings;
    this.buildingsById = new Map(buildings.map((b) => [b.id, b]));
    this.cellSize = cellSize;
  }

  select(data) {
    if (!this.tech.isBuildingUnlocked(data)) return;
    this.selected = this.selected === data ? null : data;
    eventBus.emit(Events.SelectionChanged);
  }

  cancel() {
    if (!this.selected) return;
    this.selected = null;
    this.hoverCell = null;
    eventBus.emit(Events.SelectionChanged);
  }

  worldToCell(x, y) {
    return { cx: Math.floor(x / this.cellSize), cy: Math.floor(y / this.cellSize) };
  }

  cellCenter({ cx, cy }) {
    return { x: (cx + 0.5) * this.cellSize, y: (cy + 0.5) * this.cellSize };
  }

  /** { ok, reason } — dùng cho cả preview (xanh/đỏ) lẫn lúc đặt thật. */
  checkPlacement(cell) {
    if (!this.selected) return { ok: false, reason: '' };
    if (this.#occupied.has(cellKey(cell))) return { ok: false, reason: 'Ô này đã có công trình' };
    if (this.#isBlocked(cell)) return { ok: false, reason: 'Vướng vật cản' };
    if (!this.resources.canAfford(this.selected.costs)) return { ok: false, reason: 'Không đủ tài nguyên' };
    return { ok: true, reason: '' };
  }

  tryPlace(cell) {
    const check = this.checkPlacement(cell);
    if (!check.ok) {
      if (check.reason) notify(check.reason);
      return false;
    }
    if (!this.resources.spendAll(this.selected.costs)) return false;

    this.#place(this.selected, cell);
    notify(`Đã xây ${this.selected.displayName}`);
    return true;
  }

  #isBlocked(cell) {
    const c = this.cellCenter(cell);
    const half = this.cellSize / 2;
    for (const e of this.world.all()) {
      if (!BLOCKING_KINDS.has(e.kind)) continue;
      const r = e.radius ?? e.interactExtent ?? 0.4;
      const nx = Math.max(c.x - half, Math.min(e.x, c.x + half));
      const ny = Math.max(c.y - half, Math.min(e.y, c.y + half));
      if (Math.hypot(e.x - nx, e.y - ny) < r * 0.9) return true;
    }
    return false;
  }

  #place(data, cell) {
    const building = new Building(data, cell, this.cellCenter(cell), this.cellSize);
    this.#occupied.set(cellKey(cell), building);
    this.world.add(building);
    eventBus.emit(Events.BuildingPlaced, building);
    return building;
  }

  getSaveData() {
    return [...this.#occupied.values()].map((b) => ({ buildingId: b.data.id, cellX: b.cell.cx, cellY: b.cell.cy }));
  }

  loadFromSaveData(list) {
    for (const building of this.#occupied.values()) this.world.remove(building);
    this.#occupied.clear();

    for (const entry of list ?? []) {
      const data = this.buildingsById.get(entry.buildingId);
      if (data) this.#place(data, { cx: entry.cellX, cy: entry.cellY });
    }
  }
}
