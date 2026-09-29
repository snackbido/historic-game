import * as THREE from 'three';

const cache = new Map();

/** Material low-poly dùng chung (cache theo màu) — đừng sửa màu trên material trả về. */
export function sharedMat(color, { roughness = 0.9, emissive = 0x000000 } = {}) {
  const key = `${color}|${roughness}|${emissive}`;
  if (!cache.has(key)) {
    cache.set(key, new THREE.MeshStandardMaterial({ color, roughness, emissive, flatShading: true }));
  }
  return cache.get(key);
}

export function mesh(geometry, material, { cast = true, receive = false } = {}) {
  const m = new THREE.Mesh(geometry, material);
  m.castShadow = cast;
  m.receiveShadow = receive;
  return m;
}

/** Logic chạy trên mặt phẳng 2D (x, y) như bản Unity; scene 3D dùng (x, h, -y). */
export function placeOnGround(object3D, x, y, h = 0) {
  object3D.position.set(x, h, -y);
}

/** Heading logic (radian, 0 = hướng +x) → rotation.y cho model nhìn về +z. */
export function headingToRotationY(heading) {
  return Math.atan2(Math.cos(heading), -Math.sin(heading));
}

export function lerpAngle(a, b, t) {
  let d = ((b - a + Math.PI) % (Math.PI * 2)) - Math.PI;
  if (d < -Math.PI) d += Math.PI * 2;
  return a + d * t;
}

/** Hàm random tất định để trang trí bản đồ giống nhau mỗi lần tải. */
export function seededRandom(seed) {
  let s = seed >>> 0;
  return () => {
    s = (s + 0x6d2b79f5) >>> 0;
    let t = s;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
