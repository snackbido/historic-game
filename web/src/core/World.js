import { eventBus, Events } from './EventBus.js';

/**
 * Danh sách thực thể trong màn chơi. Tầng Presentation lắng nghe EntityAdded/EntityRemoved
 * để tạo/xóa mesh tương ứng — logic không biết gì về Three.js.
 */
export class World {
  #entities = new Set();

  add(entity) {
    this.#entities.add(entity);
    eventBus.emit(Events.EntityAdded, entity);
    return entity;
  }

  remove(entity) {
    if (this.#entities.delete(entity)) eventBus.emit(Events.EntityRemoved, entity);
  }

  removeKind(kind) {
    for (const entity of this.ofKind(kind)) this.remove(entity);
  }

  all() {
    return this.#entities;
  }

  ofKind(kind) {
    return [...this.#entities].filter((e) => e.kind === kind);
  }

  update(dt) {
    for (const entity of [...this.#entities]) entity.update?.(dt, this);
  }
}
