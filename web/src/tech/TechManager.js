import { eventBus, Events, notify } from '../core/EventBus.js';

export class TechManager {
  #unlockedTechIds = new Set();
  #unlockedBuildingIds = new Set();
  #unlockedCropIds = new Set();
  #knowledgeTimer = 0;

  constructor({ resources, techs, knowledgeTypeId, interval }) {
    this.resources = resources;
    this.techs = techs;
    this.techsById = new Map(techs.map((t) => [t.id, t]));
    this.knowledgeTypeId = knowledgeTypeId;
    this.interval = interval;
  }

  /** 0..1 tới điểm tri thức kế tiếp — dùng cho thanh tiến độ trên HUD. */
  get knowledgeProgress() {
    return this.#knowledgeTimer / this.interval;
  }

  update(dt) {
    this.#knowledgeTimer += dt;
    if (this.#knowledgeTimer < this.interval) return;
    this.#knowledgeTimer = 0;
    this.resources.add(this.knowledgeTypeId, 1);
  }

  isUnlocked(tech) {
    return this.#unlockedTechIds.has(tech.id);
  }

  prerequisitesMet(tech) {
    return tech.prerequisites.every((id) => this.#unlockedTechIds.has(id));
  }

  canUnlock(tech) {
    return !this.isUnlocked(tech) && this.prerequisitesMet(tech) && this.resources.canAfford(tech.cost);
  }

  tryUnlock(tech) {
    if (!this.canUnlock(tech)) return false;
    if (!this.resources.spendAll(tech.cost)) return false;

    this.#apply(tech);
    notify(`Đã nghiên cứu: ${tech.displayName}`);
    eventBus.emit(Events.TechUnlocked, tech);
    return true;
  }

  isBuildingUnlocked(building) {
    return building.unlockedByDefault || this.#unlockedBuildingIds.has(building.id);
  }

  isCropUnlocked(crop) {
    return crop.unlockedByDefault || this.#unlockedCropIds.has(crop.id);
  }

  #apply(tech) {
    this.#unlockedTechIds.add(tech.id);
    for (const id of tech.unlockedBuildingIds) this.#unlockedBuildingIds.add(id);
    for (const id of tech.unlockedCropIds) this.#unlockedCropIds.add(id);
  }

  getSaveData() {
    return { unlockedTechIds: [...this.#unlockedTechIds], knowledgeTimer: this.#knowledgeTimer };
  }

  loadFromSaveData(data) {
    this.#unlockedTechIds.clear();
    this.#unlockedBuildingIds.clear();
    this.#unlockedCropIds.clear();
    for (const id of data?.unlockedTechIds ?? []) {
      const tech = this.techsById.get(id);
      if (tech) this.#apply(tech);
    }
    this.#knowledgeTimer = data?.knowledgeTimer ?? 0;
  }
}
