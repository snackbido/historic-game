import { clampToWorld, resolveCircleVsBuildings } from '../core/physics.js';

export class PlayerController {
  kind = 'player';
  heading = -Math.PI / 2; // nhìn xuống (về phía camera)
  moving = false;

  constructor({ moveSpeed, radius, x = 0, y = 0 }) {
    this.moveSpeed = moveSpeed;
    this.radius = radius;
    this.x = x;
    this.y = y;
  }

  move(dt, dir, buildings, worldHalfSize) {
    this.moving = dir.x !== 0 || dir.y !== 0;
    if (this.moving) {
      this.x += dir.x * this.moveSpeed * dt;
      this.y += dir.y * this.moveSpeed * dt;
      this.heading = Math.atan2(dir.y, dir.x);
    }
    resolveCircleVsBuildings(this, this.radius, buildings);
    clampToWorld(this, worldHalfSize);
  }

  getSaveData() {
    return { x: this.x, y: this.y };
  }

  loadFromSaveData(data) {
    if (!data) return;
    this.x = data.x;
    this.y = data.y;
  }
}
