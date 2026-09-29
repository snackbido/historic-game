import { eventBus, Events } from '../core/EventBus.js';

export class ResourceManager {
  #amounts = new Map();

  constructor(resourceTypes) {
    for (const type of resourceTypes) this.#amounts.set(type.id, 0);
  }

  get(typeId) {
    return this.#amounts.get(typeId) ?? 0;
  }

  add(typeId, amount) {
    if (amount <= 0) return;
    this.#set(typeId, this.get(typeId) + amount);
  }

  canAfford(costs) {
    return costs.every((c) => this.get(c.type) >= c.amount);
  }

  spendAll(costs) {
    if (!this.canAfford(costs)) return false;
    for (const c of costs) this.#set(c.type, this.get(c.type) - c.amount);
    return true;
  }

  #set(typeId, value) {
    const delta = value - this.get(typeId);
    this.#amounts.set(typeId, value);
    eventBus.emit(Events.ResourceChanged, { typeId, amount: value, delta });
  }

  getSaveData() {
    return Object.fromEntries(this.#amounts);
  }

  loadFromSaveData(data) {
    for (const typeId of this.#amounts.keys()) this.#set(typeId, data?.[typeId] ?? 0);
  }
}
