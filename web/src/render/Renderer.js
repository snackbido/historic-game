import * as THREE from 'three';
import { eventBus, Events } from '../core/EventBus.js';
import { CONFIG } from '../data/gameData.js';
import { mesh, placeOnGround, seededRandom, sharedMat } from './materials.js';
import { buildBuildingModel, createView } from './views.js';

const SKY = 0xcfe3e6;
const CAMERA_PITCH = THREE.MathUtils.degToRad(52);
const ZOOM_MIN = 7;
const ZOOM_MAX = 24;
const FLOAT_TEXT_LIFETIME = 1.3;

export class Renderer {
  #views = new Map();
  #floaters = [];
  #time = 0;

  constructor(canvas, floaterLayer, game) {
    this.game = game;
    this.floaterLayer = floaterLayer;

    this.renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    this.renderer.shadowMap.enabled = true;
    this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
    this.renderer.toneMappingExposure = 1.05;

    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color(SKY);
    this.scene.fog = new THREE.Fog(SKY, 22, 48);

    this.camera = new THREE.PerspectiveCamera(40, 1, 0.1, 200);
    this.zoom = 13;
    this.cameraFocus = new THREE.Vector3(game.player.x, 0, -game.player.y);

    this.raycaster = new THREE.Raycaster();
    this.groundPlane = new THREE.Plane(new THREE.Vector3(0, 1, 0), 0);
    this.ndc = new THREE.Vector2();

    this.#setupLights();
    this.#setupGround();
    this.#setupDecor();
    this.#setupPlacementPreview();
    this.#setupHighlight();

    for (const entity of game.world.all()) this.#addView(entity);
    eventBus.on(Events.EntityAdded, (e) => this.#addView(e));
    eventBus.on(Events.EntityRemoved, (e) => this.#removeView(e));
    eventBus.on(Events.FloatingText, (f) => this.#spawnFloater(f));
    eventBus.on(Events.SelectionChanged, () => this.#rebuildGhost());

    window.addEventListener('resize', () => this.#resize());
    this.#resize();
  }

  // ─── Setup ───────────────────────────────────────────────────────────────
  #setupLights() {
    this.scene.add(new THREE.HemisphereLight(0xfff4de, 0x5a6b3a, 1.1));

    this.sun = new THREE.DirectionalLight(0xffe2b8, 2.2);
    this.sun.castShadow = true;
    this.sun.shadow.mapSize.set(2048, 2048);
    const s = this.sun.shadow.camera;
    s.left = -14;
    s.right = 14;
    s.top = 14;
    s.bottom = -14;
    s.near = 1;
    s.far = 50;
    this.sun.shadow.bias = -0.0005;
    this.sun.shadow.normalBias = 0.02;
    this.scene.add(this.sun, this.sun.target);
  }

  #setupGround() {
    const size = 80;
    const geo = new THREE.PlaneGeometry(size, size, 80, 80);
    geo.rotateX(-Math.PI / 2);

    // Tô màu theo đỉnh: cỏ loang lổ + khoảng đất nện quanh trại ở giữa.
    const grassA = new THREE.Color(0x6f9a45);
    const grassB = new THREE.Color(0x87aa52);
    const dirt = new THREE.Color(0xa08559);
    const pos = geo.attributes.position;
    const colors = new Float32Array(pos.count * 3);
    const c = new THREE.Color();
    for (let i = 0; i < pos.count; i++) {
      const x = pos.getX(i);
      const z = pos.getZ(i);
      const n = Math.sin(x * 0.35) * Math.cos(z * 0.3) * 0.5 + Math.sin(x * 1.3 + z * 0.9) * 0.25 + 0.5;
      c.copy(grassA).lerp(grassB, THREE.MathUtils.clamp(n, 0, 1));
      const camp = 1 - THREE.MathUtils.smoothstep(Math.hypot(x, z * 1.2), 2.5, 6.5);
      c.lerp(dirt, camp * 0.55);
      colors.set([c.r, c.g, c.b], i * 3);
    }
    geo.setAttribute('color', new THREE.BufferAttribute(colors, 3));

    const ground = mesh(geo, new THREE.MeshStandardMaterial({ vertexColors: true, roughness: 1 }), {
      cast: false,
      receive: true,
    });
    this.scene.add(ground);

    const half = CONFIG.worldHalfSize * 2;
    this.grid = new THREE.GridHelper(half * 2, half * 2, 0xfff3d6, 0xfff3d6);
    this.grid.material.transparent = true;
    this.grid.material.opacity = 0.18;
    this.grid.position.y = 0.01;
    this.grid.visible = false;
    this.scene.add(this.grid);
  }

  #setupDecor() {
    const rand = seededRandom(1987);
    const dummy = new THREE.Object3D();
    const inPlayArea = (x, z, margin) => Math.abs(x) < CONFIG.worldHalfSize + margin && Math.abs(z) < CONFIG.worldHalfSize + margin;

    // Rừng bao quanh khu vực chơi (chỉ để trang trí, không va chạm).
    const trunkGeo = new THREE.CylinderGeometry(0.12, 0.2, 1, 6);
    const crownGeo = new THREE.ConeGeometry(0.9, 2.2, 7);
    const forestCount = 220;
    const trunks = new THREE.InstancedMesh(trunkGeo, sharedMat(0x5c3a1e), forestCount);
    const crowns = new THREE.InstancedMesh(crownGeo, sharedMat(0x2c5e31), forestCount);
    trunks.castShadow = crowns.castShadow = true;
    let placed = 0;
    while (placed < forestCount) {
      const x = (rand() - 0.5) * 72;
      const z = (rand() - 0.5) * 72;
      if (inPlayArea(x, z, 1)) continue;
      const s = 0.8 + rand() * 0.9;
      dummy.position.set(x, 0.5 * s, z);
      dummy.scale.setScalar(s);
      dummy.rotation.set(0, rand() * Math.PI, 0);
      dummy.updateMatrix();
      trunks.setMatrixAt(placed, dummy.matrix);
      dummy.position.y = (1 + 1.1) * s;
      dummy.updateMatrix();
      crowns.setMatrixAt(placed, dummy.matrix);
      placed++;
    }
    this.scene.add(trunks, crowns);

    // Đá + bụi cỏ rải rác, tránh khu trại ở giữa.
    const rocks = new THREE.InstancedMesh(new THREE.DodecahedronGeometry(0.3, 0), sharedMat(0x8d8a82), 45);
    rocks.castShadow = true;
    rocks.receiveShadow = true;
    for (let i = 0; i < 45; i++) {
      let x;
      let z;
      do {
        x = (rand() - 0.5) * 34;
        z = (rand() - 0.5) * 34;
      } while (Math.hypot(x, z) < 9);
      dummy.position.set(x, 0.08, z);
      dummy.scale.set(0.6 + rand(), 0.4 + rand() * 0.5, 0.6 + rand());
      dummy.rotation.set(rand(), rand() * Math.PI, rand());
      dummy.updateMatrix();
      rocks.setMatrixAt(i, dummy.matrix);
    }
    this.scene.add(rocks);

    const tufts = new THREE.InstancedMesh(new THREE.ConeGeometry(0.07, 0.3, 3), sharedMat(0x5d8a3a), 400);
    for (let i = 0; i < 400; i++) {
      const x = (rand() - 0.5) * 40;
      const z = (rand() - 0.5) * 40;
      dummy.position.set(x, 0.12, z);
      dummy.scale.setScalar(Math.hypot(x, z) < 5 ? 0 : 0.7 + rand() * 0.8);
      dummy.rotation.set((rand() - 0.5) * 0.5, rand() * Math.PI, (rand() - 0.5) * 0.5);
      dummy.updateMatrix();
      tufts.setMatrixAt(i, dummy.matrix);
    }
    this.scene.add(tufts);
  }

  #setupPlacementPreview() {
    this.preview = new THREE.Group();
    this.previewTileMat = new THREE.MeshBasicMaterial({ color: 0x9ccf6a, transparent: true, opacity: 0.45, depthWrite: false });
    const tile = new THREE.Mesh(new THREE.PlaneGeometry(0.98, 0.98).rotateX(-Math.PI / 2), this.previewTileMat);
    tile.position.y = 0.02;
    this.preview.add(tile);
    this.ghost = null;
    this.preview.visible = false;
    this.scene.add(this.preview);
  }

  #rebuildGhost() {
    if (this.ghost) {
      this.preview.remove(this.ghost);
      this.ghost.traverse((o) => o.isMesh && o.material.dispose());
    }
    this.ghost = null;
    const selected = this.game.placer.selected;
    if (!selected) return;

    this.ghost = buildBuildingModel(selected);
    this.ghost.traverse((o) => {
      if (!o.isMesh) return;
      o.castShadow = false;
      o.material = o.material.clone();
      o.material.transparent = true;
      o.material.opacity = 0.55;
    });
    this.preview.add(this.ghost);
  }

  #setupHighlight() {
    const geo = new THREE.RingGeometry(0.62, 0.72, 40).rotateX(-Math.PI / 2);
    this.highlightMat = new THREE.MeshBasicMaterial({ color: 0xf0b95c, transparent: true, opacity: 0.85, depthWrite: false });
    this.highlight = new THREE.Mesh(geo, this.highlightMat);
    this.highlight.position.y = 0.03;
    this.highlight.visible = false;
    this.scene.add(this.highlight);
  }

  // ─── Views ───────────────────────────────────────────────────────────────
  #addView(entity) {
    const view = createView(entity);
    if (!view) return;
    this.#views.set(entity, view);
    view.sync(0, this.#time);
    this.scene.add(view.object3D);
  }

  #removeView(entity) {
    const view = this.#views.get(entity);
    if (!view) return;
    this.#views.delete(entity);
    this.scene.remove(view.object3D);
    // Geometry bị dispose vẫn dùng lại được (Three.js tự upload lại khi cần).
    view.object3D.traverse((o) => o.isMesh && o.geometry.dispose());
    view.dispose?.();
  }

  // ─── Public ──────────────────────────────────────────────────────────────
  /** Raycast chuột xuống mặt đất → tọa độ logic {x, y}. */
  pickGround(pointer) {
    this.ndc.set(pointer.ndcX, pointer.ndcY);
    this.raycaster.setFromCamera(this.ndc, this.camera);
    const hit = this.raycaster.ray.intersectPlane(this.groundPlane, new THREE.Vector3());
    return hit ? { x: hit.x, y: -hit.z } : null;
  }

  update(dt, input) {
    this.#time += dt;
    const { player, placer, interaction } = this.game;

    // Camera bám theo nhân vật (CameraFollow.cs) + zoom bằng con lăn.
    this.zoom = THREE.MathUtils.clamp(this.zoom + input.wheel * 1.2, ZOOM_MIN, ZOOM_MAX);
    const target = new THREE.Vector3(player.x, 0, -player.y);
    this.cameraFocus.lerp(target, Math.min(1, dt * 6));
    this.camera.position.set(
      this.cameraFocus.x,
      this.cameraFocus.y + Math.sin(CAMERA_PITCH) * this.zoom,
      this.cameraFocus.z + Math.cos(CAMERA_PITCH) * this.zoom,
    );
    this.camera.lookAt(this.cameraFocus);

    this.sun.position.set(this.cameraFocus.x + 8, 16, this.cameraFocus.z + 5);
    this.sun.target.position.copy(this.cameraFocus);

    for (const view of this.#views.values()) view.sync(dt, this.#time);

    // Preview đặt công trình (xanh = đặt được, đỏ = không).
    this.grid.visible = !!placer.selected;
    this.preview.visible = !!(placer.selected && placer.hoverCell);
    if (this.preview.visible) {
      const c = placer.cellCenter(placer.hoverCell);
      placeOnGround(this.preview, c.x, c.y);
      const ok = placer.checkPlacement(placer.hoverCell).ok;
      this.previewTileMat.color.set(ok ? 0x9ccf6a : 0xe0705a);
    }

    // Vòng sáng dưới đối tượng có thể tương tác gần nhất.
    const nearest = interaction.nearest;
    this.highlight.visible = !!nearest && !placer.selected;
    if (this.highlight.visible) {
      placeOnGround(this.highlight, nearest.x, nearest.y, 0.03);
      this.highlight.scale.setScalar((nearest.interactExtent + 0.2) * (1 + Math.sin(this.#time * 5) * 0.05));
      this.highlightMat.opacity = 0.6 + Math.sin(this.#time * 5) * 0.2;
    }

    this.renderer.render(this.scene, this.camera);
    this.#updateFloaters(dt);
  }

  // ─── Floating text (+1 Gỗ ...) ───────────────────────────────────────────
  #spawnFloater({ x, y, text, tone }) {
    const el = document.createElement('div');
    el.className = `floater floater--${tone}`;
    el.textContent = text;
    this.floaterLayer.appendChild(el);
    this.#floaters.push({ el, world: new THREE.Vector3(x, 1.4, -y), age: 0 });
  }

  #updateFloaters(dt) {
    const v = new THREE.Vector3();
    const { clientWidth: w, clientHeight: h } = this.renderer.domElement;
    this.#floaters = this.#floaters.filter((f) => {
      f.age += dt;
      if (f.age >= FLOAT_TEXT_LIFETIME) {
        f.el.remove();
        return false;
      }
      const k = f.age / FLOAT_TEXT_LIFETIME;
      v.copy(f.world).setY(f.world.y + k * 0.9).project(this.camera);
      f.el.style.transform = `translate(-50%, -50%) translate(${(v.x * 0.5 + 0.5) * w}px, ${(-v.y * 0.5 + 0.5) * h}px)`;
      f.el.style.opacity = String(k < 0.7 ? 1 : 1 - (k - 0.7) / 0.3);
      return true;
    });
  }

  #resize() {
    const w = window.innerWidth;
    const h = window.innerHeight;
    this.renderer.setSize(w, h, false);
    this.camera.aspect = w / h;
    this.camera.updateProjectionMatrix();
  }
}
