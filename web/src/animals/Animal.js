import { notify } from '../core/EventBus.js';
import { resolveCircleVsBuildings } from '../core/physics.js';

export const AnimalState = Object.freeze({ Wild: 'wild', Tamed: 'tamed' });

const WANDER_RADIUS = 1.5;
const WANDER_SPEED = 0.7;

// Tương đương AnimalController.cs, thêm đi lang thang quanh "nhà" cho sinh động.
export class Animal {
  kind = 'animal';
  interactExtent = 0.4;
  radius = 0.35;

  state = AnimalState.Wild;
  tamingProgress = 0;
  hunger = 100;
  productReady = false;

  hungerTimer = 0;
  reproductionTimer = 0;
  productionTimer = 0;

  heading = -Math.PI / 2;
  moving = false;
  #wanderTarget = null;
  #wanderPause = Math.random() * 3;

  constructor({ data, x, y, homeX = x, homeY = y }) {
    this.data = data;
    this.x = x;
    this.y = y;
    this.homeX = homeX;
    this.homeY = homeY;
  }

  get isTamed() {
    return this.state === AnimalState.Tamed;
  }

  get isWellFed() {
    return this.hunger >= this.data.hungerThresholdForNeeds;
  }

  update(dt, world) {
    this.#updateWander(dt, world);
    if (!this.isTamed) return;

    this.#updateHunger(dt);
    if (!this.isWellFed) return;

    this.#updateReproduction(dt, world);
    this.#updateProduction(dt);
  }

  feed() {
    if (this.isTamed) {
      this.hunger = 100;
      return;
    }
    this.tamingProgress++;
    if (this.tamingProgress >= this.data.feedingsToTame) this.#tame();
  }

  collectProduct(resourceManager) {
    if (!this.productReady) return false;

    for (const p of this.data.products) resourceManager.add(p.type, p.amount);
    this.productReady = false;
    this.productionTimer = 0;
    return true;
  }

  initializeAsTamed() {
    this.tamingProgress = this.data.feedingsToTame;
    this.#tame();
    return this;
  }

  #tame() {
    this.state = AnimalState.Tamed;
    this.hunger = 100;
    this.hungerTimer = 0;
    this.reproductionTimer = 0;
    this.productionTimer = 0;
    this.productReady = false;
  }

  #updateHunger(dt) {
    this.hungerTimer += dt;
    if (this.hungerTimer < this.data.hungerDecayInterval) return;
    this.hungerTimer = 0;
    this.hunger = Math.max(0, this.hunger - 1);
  }

  #updateReproduction(dt, world) {
    this.reproductionTimer += dt;
    if (this.reproductionTimer < this.data.reproductionInterval) return;
    this.reproductionTimer = 0;

    const population = world.ofKind('animal').filter((a) => a.data === this.data).length;
    if (population >= this.data.maxPopulation) return;

    const angle = Math.random() * Math.PI * 2;
    const r = 0.6 + Math.random() * 0.6;
    const x = this.x + Math.cos(angle) * r;
    const y = this.y + Math.sin(angle) * r;
    world.add(new Animal({ data: this.data, x, y }).initializeAsTamed());
    // Bản Unity im lặng ở đây và gây nhầm lẫn khi test tay (PROGRESS.md, M3) — báo rõ cho người chơi.
    notify(`${this.data.displayName} vừa sinh con (đã thuần sẵn)!`);
  }

  #updateProduction(dt) {
    if (this.productReady) return;
    this.productionTimer += dt;
    if (this.productionTimer >= this.data.productionInterval) this.productReady = true;
  }

  #updateWander(dt, world) {
    this.moving = false;
    if (this.#wanderPause > 0) {
      this.#wanderPause -= dt;
      return;
    }
    if (!this.#wanderTarget) {
      const angle = Math.random() * Math.PI * 2;
      const r = Math.random() * WANDER_RADIUS;
      this.#wanderTarget = { x: this.homeX + Math.cos(angle) * r, y: this.homeY + Math.sin(angle) * r };
    }

    const dx = this.#wanderTarget.x - this.x;
    const dy = this.#wanderTarget.y - this.y;
    const dist = Math.hypot(dx, dy);
    const step = WANDER_SPEED * dt;
    if (dist <= step) {
      this.#stopWandering();
      return;
    }

    const prevX = this.x;
    const prevY = this.y;
    this.x += (dx / dist) * step;
    this.y += (dy / dist) * step;
    this.heading = Math.atan2(dy, dx);
    this.moving = true;

    resolveCircleVsBuildings(this, this.radius, world.ofKind('building'));
    if (Math.hypot(this.x - prevX, this.y - prevY) < step * 0.3) this.#stopWandering(); // bị kẹt
  }

  #stopWandering() {
    this.#wanderTarget = null;
    this.#wanderPause = 2 + Math.random() * 4;
  }

  getSaveData() {
    const { x, y, homeX, homeY, state, tamingProgress, hunger, productReady } = this;
    const { hungerTimer, reproductionTimer, productionTimer } = this;
    return {
      dataId: this.data.id,
      x, y, homeX, homeY, state, tamingProgress, hunger, productReady,
      hungerTimer, reproductionTimer, productionTimer,
    };
  }

  static fromSaveData(saved, animalsById) {
    const data = animalsById.get(saved.dataId);
    if (!data) return null;
    const animal = new Animal({ data, x: saved.x, y: saved.y, homeX: saved.homeX, homeY: saved.homeY });
    Object.assign(animal, {
      state: saved.state,
      tamingProgress: saved.tamingProgress,
      hunger: saved.hunger,
      productReady: saved.productReady,
      hungerTimer: saved.hungerTimer,
      reproductionTimer: saved.reproductionTimer,
      productionTimer: saved.productionTimer,
    });
    return animal;
  }
}
