// Pub/sub đơn giản — tương đương EventBus.cs. Các hệ thống không phụ thuộc cứng vào nhau.

export const Events = Object.freeze({
  ResourceChanged: 'resource-changed', // { typeId, amount, delta }
  BuildingPlaced: 'building-placed', // Building
  TechUnlocked: 'tech-unlocked', // tech data
  SelectionChanged: 'selection-changed', // chọn công trình / hạt giống
  Notification: 'notification', // string
  FloatingText: 'floating-text', // { x, y, text, tone }
  EntityAdded: 'entity-added',
  EntityRemoved: 'entity-removed',
  GameLoaded: 'game-loaded',
});

class EventBus {
  #listeners = new Map();

  on(event, handler) {
    if (!this.#listeners.has(event)) this.#listeners.set(event, new Set());
    this.#listeners.get(event).add(handler);
    return () => this.off(event, handler);
  }

  off(event, handler) {
    this.#listeners.get(event)?.delete(handler);
  }

  emit(event, payload) {
    for (const handler of [...(this.#listeners.get(event) ?? [])]) handler(payload);
  }
}

export const eventBus = new EventBus();

export const notify = (message) => eventBus.emit(Events.Notification, message);

export const floatText = (x, y, text, tone = 'good') =>
  eventBus.emit(Events.FloatingText, { x, y, text, tone });
