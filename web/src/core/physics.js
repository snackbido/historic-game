const clamp = (v, min, max) => Math.max(min, Math.min(max, v));

/** Đẩy một hình tròn (player/vật nuôi) ra khỏi các công trình hình vuông. */
export function resolveCircleVsBuildings(entity, radius, buildings) {
  for (const b of buildings) {
    const hs = b.halfSize;
    const nx = clamp(entity.x, b.x - hs, b.x + hs);
    const ny = clamp(entity.y, b.y - hs, b.y + hs);
    const dx = entity.x - nx;
    const dy = entity.y - ny;
    const d2 = dx * dx + dy * dy;
    if (d2 >= radius * radius) continue;

    if (d2 > 1e-8) {
      const d = Math.sqrt(d2);
      entity.x += (dx / d) * (radius - d);
      entity.y += (dy / d) * (radius - d);
      continue;
    }

    // Tâm nằm hẳn trong công trình: đẩy ra theo cạnh gần nhất.
    const left = entity.x - (b.x - hs);
    const right = b.x + hs - entity.x;
    const down = entity.y - (b.y - hs);
    const up = b.y + hs - entity.y;
    const m = Math.min(left, right, down, up);
    if (m === left) entity.x = b.x - hs - radius;
    else if (m === right) entity.x = b.x + hs + radius;
    else if (m === down) entity.y = b.y - hs - radius;
    else entity.y = b.y + hs + radius;
  }
}

export function clampToWorld(entity, halfSize) {
  entity.x = clamp(entity.x, -halfSize, halfSize);
  entity.y = clamp(entity.y, -halfSize, halfSize);
}
