import { ANIMALS, BUILDINGS, CONFIG, CROPS, RESOURCE_TYPES, TECHS, WORLD_LAYOUT } from '../data/gameData.js';
import { Animal } from '../animals/Animal.js';
import { TamingSystem } from '../animals/TamingSystem.js';
import { BuildingPlacer } from '../building/BuildingPlacer.js';
import { FarmManager } from '../farming/FarmManager.js';
import { FarmPlot } from '../farming/FarmPlot.js';
import { PlayerController } from '../player/PlayerController.js';
import { PlayerInteraction } from '../player/PlayerInteraction.js';
import { ResourceManager } from '../resources/ResourceManager.js';
import { ResourceNode } from '../resources/ResourceNode.js';
import { TechManager } from '../tech/TechManager.js';
import { eventBus, Events, notify } from './EventBus.js';
import { SaveSystem } from './SaveSystem.js';
import { World } from './World.js';

const SAVE_VERSION = 1;

/** Dựng và điều phối mọi hệ thống — tương đương GameManager.cs + GameplaySceneBuilder.cs. */
export class GameManager {
  constructor() {
    this.data = { resourceTypes: RESOURCE_TYPES, buildings: BUILDINGS, crops: CROPS, animals: ANIMALS, techs: TECHS };
    this.cropsById = new Map(CROPS.map((c) => [c.id, c]));
    this.animalsById = new Map(ANIMALS.map((a) => [a.id, a]));

    this.world = new World();
    this.resources = new ResourceManager(RESOURCE_TYPES);
    this.tech = new TechManager({
      resources: this.resources,
      techs: TECHS,
      knowledgeTypeId: CONFIG.knowledgeTypeId,
      interval: CONFIG.knowledgeGenerationInterval,
    });
    this.placer = new BuildingPlacer({
      world: this.world,
      resources: this.resources,
      tech: this.tech,
      buildings: BUILDINGS,
      cellSize: CONFIG.cellSize,
    });
    this.farm = new FarmManager({ resources: this.resources, tech: this.tech });
    this.taming = new TamingSystem({ resources: this.resources });

    this.player = this.world.add(new PlayerController({ ...CONFIG.player, ...WORLD_LAYOUT.playerStart }));
    this.interaction = new PlayerInteraction({
      player: this.player,
      world: this.world,
      resources: this.resources,
      farm: this.farm,
      taming: this.taming,
      interactRadius: CONFIG.player.interactRadius,
    });
    this.saveSystem = new SaveSystem(CONFIG.saveKey);

    this.#spawnInitialWorld();
  }

  #spawnInitialWorld() {
    for (const tree of WORLD_LAYOUT.trees) this.world.add(new ResourceNode(tree));
    for (const plot of WORLD_LAYOUT.farmPlots) this.world.add(new FarmPlot(plot));
    for (const { id, x, y } of WORLD_LAYOUT.animals) {
      this.world.add(new Animal({ data: this.animalsById.get(id), x, y }));
    }
  }

  /** @param pickGround (pointer) => {x, y} | null — do tầng Presentation cung cấp (raycast). */
  update(dt, input, pickGround) {
    if (input.wasPressed('F5')) this.saveGame();
    if (input.wasPressed('F9')) this.loadGame();

    this.player.move(dt, input.moveVector(), this.world.ofKind('building'), CONFIG.worldHalfSize);
    this.interaction.update(input);
    this.#updatePlacement(input, pickGround);
    this.world.update(dt);
    this.tech.update(dt);
  }

  #updatePlacement(input, pickGround) {
    if (input.wasPressed('Escape') || input.consumeClick(2)) this.placer.cancel();

    if (!this.placer.selected) {
      this.placer.hoverCell = null;
      return;
    }

    const point = input.pointer.inside ? pickGround(input.pointer) : null;
    this.placer.hoverCell = point ? this.placer.worldToCell(point.x, point.y) : null;
    if (this.placer.hoverCell && input.consumeClick(0)) this.placer.tryPlace(this.placer.hoverCell);
  }

  saveGame() {
    const data = {
      version: SAVE_VERSION,
      savedAt: new Date().toISOString(),
      player: this.player.getSaveData(),
      resources: this.resources.getSaveData(),
      tech: this.tech.getSaveData(),
      buildings: this.placer.getSaveData(),
      farmPlots: this.world.ofKind('farmPlot').map((p) => p.getSaveData()),
      animals: this.world.ofKind('animal').map((a) => a.getSaveData()),
      resourceNodes: this.world.ofKind('resourceNode').map((n) => n.getSaveData()),
    };
    notify(this.saveSystem.save(data) ? 'Đã lưu game' : 'Lưu game thất bại (trình duyệt chặn localStorage?)');
  }

  loadGame() {
    const data = this.saveSystem.load();
    if (!data || data.version !== SAVE_VERSION) {
      notify('Chưa có bản lưu nào');
      return;
    }

    this.placer.cancel();
    this.tech.loadFromSaveData(data.tech);
    this.resources.loadFromSaveData(data.resources);
    this.player.loadFromSaveData(data.player);
    this.placer.loadFromSaveData(data.buildings);

    // Ô đất cố định theo bố cục bản đồ → khớp theo thứ tự.
    this.world.ofKind('farmPlot').forEach((plot, i) => plot.loadFromSaveData(data.farmPlots?.[i], this.cropsById));

    this.world.removeKind('animal');
    for (const saved of data.animals ?? []) {
      const animal = Animal.fromSaveData(saved, this.animalsById);
      if (animal) this.world.add(animal);
    }

    this.world.removeKind('resourceNode');
    for (const saved of data.resourceNodes ?? []) this.world.add(new ResourceNode(saved));

    if (this.farm.selectedCrop && !this.tech.isCropUnlocked(this.farm.selectedCrop)) this.farm.selectCrop(null);

    eventBus.emit(Events.GameLoaded);
    notify('Đã tải game');
  }
}
