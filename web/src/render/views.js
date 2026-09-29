import * as THREE from 'three';
import { AnimalState } from '../animals/Animal.js';
import { CropStage } from '../farming/FarmPlot.js';
import { headingToRotationY, lerpAngle, mesh, placeOnGround, sharedMat } from './materials.js';

// Presentation layer: mỗi view đọc trạng thái của entity logic mỗi frame, không tự đổi logic.

const SKIN = 0xc98d5e;
const FUR = 0x8a5a2b;
const WOOD = 0x6b4423;

// ─── Player ────────────────────────────────────────────────────────────────
class PlayerView {
  constructor(player) {
    this.entity = player;
    this.object3D = new THREE.Group();
    this.body = new THREE.Group();
    this.object3D.add(this.body);

    const limb = new THREE.CylinderGeometry(0.065, 0.06, 0.32, 6);
    this.legL = mesh(limb, sharedMat(SKIN));
    this.legR = mesh(limb, sharedMat(SKIN));
    this.legL.position.set(-0.09, 0.16, 0);
    this.legR.position.set(0.09, 0.16, 0);

    const tunic = mesh(new THREE.CylinderGeometry(0.19, 0.27, 0.46, 7), sharedMat(FUR));
    tunic.position.y = 0.52;

    this.armL = mesh(limb, sharedMat(SKIN));
    this.armR = mesh(limb, sharedMat(SKIN));
    this.armL.position.set(-0.25, 0.55, 0);
    this.armR.position.set(0.25, 0.55, 0);

    const head = mesh(new THREE.SphereGeometry(0.17, 10, 8), sharedMat(SKIN));
    head.position.y = 0.9;
    const hair = mesh(new THREE.SphereGeometry(0.185, 10, 6, 0, Math.PI * 2, 0, Math.PI / 2), sharedMat(0x3b2414));
    hair.position.set(0, 0.92, -0.02);

    const spear = new THREE.Group();
    const shaft = mesh(new THREE.CylinderGeometry(0.02, 0.02, 1.2, 5), sharedMat(WOOD));
    const tip = mesh(new THREE.ConeGeometry(0.05, 0.16, 5), sharedMat(0x9a9a92));
    tip.position.y = 0.68;
    spear.add(shaft, tip);
    spear.position.set(0.3, 0.62, 0.05);
    spear.rotation.x = 0.25;

    this.body.add(this.legL, this.legR, tunic, this.armL, this.armR, head, hair, spear);
    this.walkPhase = 0;
  }

  sync(dt) {
    const p = this.entity;
    placeOnGround(this.object3D, p.x, p.y);
    this.object3D.rotation.y = lerpAngle(this.object3D.rotation.y, headingToRotationY(p.heading), Math.min(1, dt * 12));

    this.walkPhase = p.moving ? this.walkPhase + dt * 11 : 0;
    const swing = Math.sin(this.walkPhase) * 0.5;
    this.legL.rotation.x = swing;
    this.legR.rotation.x = -swing;
    this.armL.rotation.x = -swing * 0.7;
    this.body.position.y = p.moving ? Math.abs(Math.sin(this.walkPhase)) * 0.05 : 0;
  }
}

// ─── Tree (ResourceNode) ───────────────────────────────────────────────────
class TreeView {
  constructor(node) {
    this.entity = node;
    this.object3D = new THREE.Group();
    this.tree = new THREE.Group();
    this.object3D.add(this.tree);

    const trunk = mesh(new THREE.CylinderGeometry(0.09, 0.14, 0.7, 6), sharedMat(WOOD));
    trunk.position.y = 0.35;
    this.tree.add(trunk);

    const layers = [
      [0.58, 0.8, 0.9, 0x2f6b34],
      [0.45, 0.7, 1.28, 0x377a3a],
      [0.3, 0.55, 1.6, 0x418a42],
    ];
    for (const [radius, height, y, color] of layers) {
      const cone = mesh(new THREE.ConeGeometry(radius, height, 7), sharedMat(color));
      cone.position.y = y;
      this.tree.add(cone);
    }
    this.tree.rotation.y = (node.x * 13.37 + node.y * 7.1) % (Math.PI * 2);

    this.lastAmount = node.amountRemaining;
    this.shake = 0;
  }

  sync(dt, time) {
    const n = this.entity;
    placeOnGround(this.object3D, n.x, n.y);

    if (n.amountRemaining !== this.lastAmount) {
      this.lastAmount = n.amountRemaining;
      this.shake = 0.35;
    }
    this.shake = Math.max(0, this.shake - dt);
    this.object3D.rotation.z = Math.sin(time * 45) * this.shake * 0.3;

    const s = 0.7 + 0.3 * (n.amountRemaining / n.maxAmount);
    this.tree.scale.setScalar(s);
  }
}

// ─── Farm plot ─────────────────────────────────────────────────────────────
const CROP_SPOTS = [-0.3, 0, 0.3];
const CROP_GEO = {
  seed: new THREE.SphereGeometry(0.04, 5, 4),
  leaf: new THREE.ConeGeometry(0.045, 0.22, 4),
  bush: new THREE.IcosahedronGeometry(0.14, 0),
  berry: new THREE.SphereGeometry(0.035, 6, 4),
  withered: new THREE.IcosahedronGeometry(0.12, 0),
};

class FarmPlotView {
  constructor(plot) {
    this.entity = plot;
    this.object3D = new THREE.Group();

    const soil = mesh(new THREE.BoxGeometry(0.96, 0.08, 0.96), sharedMat(0x5a3d22), { cast: false, receive: true });
    soil.position.y = 0.04;
    this.object3D.add(soil);

    const furrow = new THREE.BoxGeometry(0.86, 0.03, 0.1);
    for (const z of CROP_SPOTS) {
      const f = mesh(furrow, sharedMat(0x47301a), { cast: false, receive: true });
      f.position.set(0, 0.09, z);
      this.object3D.add(f);
    }

    this.cropGroup = new THREE.Group();
    this.cropGroup.position.y = 0.1;
    this.object3D.add(this.cropGroup);
    this.stageKey = null;
  }

  sync(dt, time) {
    const plot = this.entity;
    placeOnGround(this.object3D, plot.x, plot.y);

    const key = plot.crop ? `${plot.crop.id}:${plot.stage}` : 'empty';
    if (key !== this.stageKey) {
      this.stageKey = key;
      this.#rebuildCrop(plot);
    }

    if (plot.stage === CropStage.Sprouting) {
      this.cropGroup.scale.setScalar(0.5 + 0.5 * Math.min(1, plot.stageTimer / plot.crop.timeToMature));
    } else if (plot.stage === CropStage.Mature && plot.crop) {
      this.cropGroup.scale.setScalar(1 + Math.sin(time * 3) * 0.03);
    } else {
      this.cropGroup.scale.setScalar(1);
    }
  }

  #rebuildCrop(plot) {
    this.cropGroup.clear();
    if (!plot.crop) return;

    const v = plot.crop.visual;
    for (const z of CROP_SPOTS) {
      for (const x of [-0.25, 0.25]) this.cropGroup.add(this.#buildPlant(plot.stage, v, x, z));
    }
  }

  #buildPlant(stage, v, x, z) {
    const g = new THREE.Group();
    g.position.set(x, 0, z);

    switch (stage) {
      case CropStage.Seed: {
        const seed = mesh(CROP_GEO.seed, sharedMat(0x3a2412), { cast: false });
        seed.position.y = 0.01;
        g.add(seed);
        break;
      }
      case CropStage.Sprouting: {
        for (const [dx, lean] of [[-0.03, 0.4], [0.03, -0.4]]) {
          const leaf = mesh(CROP_GEO.leaf, sharedMat(v.sprout));
          leaf.position.set(dx, 0.1, 0);
          leaf.rotation.z = lean;
          g.add(leaf);
        }
        break;
      }
      case CropStage.Mature: {
        const bush = mesh(CROP_GEO.bush, sharedMat(v.leaves));
        bush.position.y = 0.13;
        g.add(bush);
        for (let i = 0; i < 4; i++) {
          const a = (i / 4) * Math.PI * 2 + x * 5;
          const berry = mesh(CROP_GEO.berry, sharedMat(v.fruit, { emissive: 0x2a0008 }));
          berry.position.set(Math.cos(a) * 0.12, 0.13 + (i % 2) * 0.06, Math.sin(a) * 0.12);
          g.add(berry);
        }
        break;
      }
      case CropStage.Withered: {
        const bush = mesh(CROP_GEO.withered, sharedMat(v.withered));
        bush.scale.set(1.1, 0.55, 1.1);
        bush.position.y = 0.06;
        g.add(bush);
        break;
      }
    }
    return g;
  }
}

// ─── Animal ────────────────────────────────────────────────────────────────
class AnimalView {
  constructor(animal) {
    this.entity = animal;
    this.object3D = new THREE.Group();
    this.body = new THREE.Group();
    this.object3D.add(this.body);

    const { visual } = animal.data;
    this.wildColor = new THREE.Color(visual.body);
    this.tamedColor = this.wildColor.clone().multiply(new THREE.Color(...visual.tamedTint));
    this.hide = new THREE.MeshStandardMaterial({ color: this.wildColor, roughness: 0.95, flatShading: true });

    const torso = mesh(new THREE.SphereGeometry(0.3, 9, 7), this.hide);
    torso.scale.set(0.85, 0.78, 1.3);
    torso.position.y = 0.38;

    const ridge = mesh(new THREE.BoxGeometry(0.08, 0.1, 0.55), sharedMat(0x3a2616));
    ridge.position.set(0, 0.6, -0.02);

    const head = mesh(new THREE.BoxGeometry(0.28, 0.26, 0.26), this.hide);
    head.position.set(0, 0.42, 0.4);

    const snout = mesh(new THREE.CylinderGeometry(0.075, 0.085, 0.1, 8), sharedMat(0x9c6b5a));
    snout.rotation.x = Math.PI / 2;
    snout.position.set(0, 0.38, 0.57);

    const tuskGeo = new THREE.ConeGeometry(0.02, 0.1, 4);
    const tusks = [-0.09, 0.09].map((x) => {
      const t = mesh(tuskGeo, sharedMat(0xeee6d0));
      t.position.set(x, 0.36, 0.56);
      t.rotation.x = -0.6;
      return t;
    });

    const earGeo = new THREE.ConeGeometry(0.05, 0.1, 4);
    const ears = [-0.1, 0.1].map((x) => {
      const e = mesh(earGeo, this.hide);
      e.position.set(x, 0.59, 0.36);
      return e;
    });

    const legGeo = new THREE.CylinderGeometry(0.045, 0.04, 0.24, 5);
    this.legs = [
      [-0.13, 0.2], [0.13, 0.2], [-0.13, -0.22], [0.13, -0.22],
    ].map(([x, z]) => {
      const leg = mesh(legGeo, sharedMat(0x3a2616));
      leg.position.set(x, 0.12, z);
      return leg;
    });

    // Vòng cổ + biểu tượng sản phẩm: phân biệt rõ con đã thuần / có sản phẩm (UX gap ở PROGRESS.md M3).
    this.collar = mesh(new THREE.TorusGeometry(0.19, 0.03, 6, 16), sharedMat(0xd9b36c));
    this.collar.position.set(0, 0.42, 0.27);

    this.productIcon = mesh(new THREE.OctahedronGeometry(0.1, 0), sharedMat(0xffd23f, { emissive: 0x8a6a00 }));
    this.productIcon.position.y = 1.0;

    this.body.add(torso, ridge, head, snout, ...tusks, ...ears, ...this.legs, this.collar, this.productIcon);
    this.walkPhase = 0;
  }

  sync(dt, time) {
    const a = this.entity;
    placeOnGround(this.object3D, a.x, a.y);
    this.object3D.rotation.y = lerpAngle(this.object3D.rotation.y, headingToRotationY(a.heading), Math.min(1, dt * 6));

    this.walkPhase = a.moving ? this.walkPhase + dt * 14 : 0;
    this.legs.forEach((leg, i) => (leg.rotation.x = Math.sin(this.walkPhase + (i % 2) * Math.PI) * 0.5));

    const tamed = a.state === AnimalState.Tamed;
    this.hide.color.copy(tamed ? this.tamedColor : this.wildColor);
    this.collar.visible = tamed;
    this.productIcon.visible = tamed && a.productReady;
    if (this.productIcon.visible) {
      this.productIcon.rotation.y = time * 2.5;
      this.productIcon.position.y = 1.0 + Math.sin(time * 4) * 0.06;
    }
  }

  dispose() {
    this.hide.dispose();
  }
}

// ─── Buildings ─────────────────────────────────────────────────────────────
export function buildBuildingModel(data) {
  const g = new THREE.Group();

  if (data.model === 'hut') {
    const cover = mesh(new THREE.ConeGeometry(0.46, 1.1, 9), sharedMat(0xb08556));
    cover.position.y = 0.55;
    const band = mesh(new THREE.CylinderGeometry(0.26, 0.3, 0.08, 9), sharedMat(0x7a4f2c));
    band.position.y = 0.55;
    const door = mesh(new THREE.BoxGeometry(0.22, 0.36, 0.05), sharedMat(0x2b1a0e));
    door.position.set(0, 0.18, 0.37);
    door.rotation.x = -0.4;
    g.add(cover, band, door);

    const poleGeo = new THREE.CylinderGeometry(0.018, 0.018, 0.45, 4);
    for (let i = 0; i < 4; i++) {
      const pole = mesh(poleGeo, sharedMat(WOOD));
      const a = (i / 4) * Math.PI * 2 + 0.4;
      pole.position.set(Math.cos(a) * 0.05, 1.18, Math.sin(a) * 0.05);
      pole.rotation.set(Math.sin(a) * 0.35, 0, -Math.cos(a) * 0.35);
      g.add(pole);
    }
  } else if (data.model === 'storage') {
    const base = mesh(new THREE.BoxGeometry(0.84, 0.5, 0.84), sharedMat(0x8b5e34));
    base.position.y = 0.25;
    const roof = mesh(new THREE.ConeGeometry(0.72, 0.45, 4), sharedMat(0xc9a35a));
    roof.position.y = 0.72;
    roof.rotation.y = Math.PI / 4;
    const door = mesh(new THREE.BoxGeometry(0.26, 0.34, 0.03), sharedMat(0x3a2414));
    door.position.set(0, 0.17, 0.43);
    g.add(base, roof, door);

    const logGeo = new THREE.CylinderGeometry(0.05, 0.05, 0.5, 6);
    [[-0.07, 0.05], [0.07, 0.05], [0, 0.14]].forEach(([dy, y]) => {
      const log = mesh(logGeo, sharedMat(WOOD));
      log.rotation.z = Math.PI / 2;
      log.position.set(0.52, y, dy);
      log.rotation.y = Math.PI / 2;
      g.add(log);
    });
  } else {
    g.add(mesh(new THREE.BoxGeometry(0.8, 0.6, 0.8), sharedMat(0x888888)));
  }
  return g;
}

class BuildingView {
  constructor(building) {
    this.entity = building;
    this.object3D = buildBuildingModel(building.data);
    this.age = 0;
  }

  sync(dt) {
    const b = this.entity;
    placeOnGround(this.object3D, b.x, b.y);
    // Hiệu ứng "bật" lên khi vừa xây xong.
    this.age = Math.min(1, this.age + dt / 0.35);
    const t = this.age - 1;
    const s = 1 + 2.7 * t * t * t + 1.7 * t * t; // easeOutBack
    this.object3D.scale.setScalar(s);
  }
}

const VIEW_TYPES = {
  player: PlayerView,
  resourceNode: TreeView,
  farmPlot: FarmPlotView,
  animal: AnimalView,
  building: BuildingView,
};

export function createView(entity) {
  const ViewType = VIEW_TYPES[entity.kind];
  return ViewType ? new ViewType(entity) : null;
}
