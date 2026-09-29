import { floatText, notify } from '../core/EventBus.js';
import { formatAmounts, getResourceType } from '../data/gameData.js';
import { FarmPlotState } from '../farming/FarmPlot.js';

const INTERACT_KEYS = ['KeyE', 'Space'];
const INTERACTABLE_KINDS = new Set(['resourceNode', 'farmPlot', 'animal']);

// Một phím tương tác cho cả 3 loại đối tượng, chọn cái gần nhất — như PlayerInteraction.cs.
export class PlayerInteraction {
  nearest = null;

  constructor({ player, world, resources, farm, taming, interactRadius }) {
    this.player = player;
    this.world = world;
    this.resources = resources;
    this.farm = farm;
    this.taming = taming;
    this.interactRadius = interactRadius;
  }

  update(input) {
    this.nearest = this.#findNearest();
    if (this.nearest && INTERACT_KEYS.some((k) => input.wasPressed(k))) this.#interact(this.nearest);
  }

  #findNearest() {
    let best = null;
    let bestDistance = Infinity;
    for (const e of this.world.all()) {
      if (!INTERACTABLE_KINDS.has(e.kind)) continue;
      const d = Math.hypot(e.x - this.player.x, e.y - this.player.y);
      if (d <= this.interactRadius + e.interactExtent && d < bestDistance) {
        best = e;
        bestDistance = d;
      }
    }
    return best;
  }

  #interact(target) {
    switch (target.kind) {
      case 'resourceNode': {
        const got = target.harvest(this.resources);
        if (got > 0) floatText(target.x, target.y, formatAmounts([{ type: target.typeId, amount: got }], '+'));
        if (target.depleted) {
          this.world.remove(target);
          notify('Cây đã bị chặt hết');
        }
        break;
      }
      case 'farmPlot':
        this.farm.tryInteract(target);
        break;
      case 'animal':
        this.taming.tryInteract(target);
        break;
    }
  }

  /** Gợi ý hành động cho đối tượng gần nhất: { text, enabled } hoặc null. */
  getPrompt() {
    const t = this.nearest;
    if (!t) return null;

    switch (t.kind) {
      case 'resourceNode': {
        const r = getResourceType(t.typeId);
        return { text: `Chặt cây · còn ${t.amountRemaining} ${r.icon} ${r.displayName}`, enabled: true };
      }
      case 'farmPlot':
        return this.#farmPrompt(t);
      case 'animal':
        return this.#animalPrompt(t);
      default:
        return null;
    }
  }

  #farmPrompt(plot) {
    switch (plot.state) {
      case FarmPlotState.Empty:
        return this.farm.selectedCrop
          ? { text: `Gieo ${this.farm.selectedCrop.displayName}`, enabled: true }
          : { text: 'Ô đất trống — chọn hạt giống ở bảng bên phải', enabled: false };
      case FarmPlotState.Growing:
        return { text: `${plot.crop.displayName} đang lớn · ${Math.floor(plot.growthProgress * 100)}%`, enabled: false };
      case FarmPlotState.ReadyToHarvest:
        return { text: `Thu hoạch ${plot.crop.displayName} (${formatAmounts(plot.crop.harvestYield, '+')})`, enabled: true };
      case FarmPlotState.Withered:
        return { text: 'Dọn cây héo', enabled: true };
      default:
        return null;
    }
  }

  #animalPrompt(animal) {
    const { data } = animal;
    const affordable = this.resources.canAfford(data.feedCost);
    const cost = formatAmounts(data.feedCost, '-');

    if (!animal.isTamed) {
      return {
        text: `Cho ${data.displayName} hoang ăn (${cost}) · thuần hóa ${animal.tamingProgress}/${data.feedingsToTame}`,
        enabled: affordable,
      };
    }
    if (animal.productReady) {
      return { text: `Thu sản phẩm từ ${data.displayName} (${formatAmounts(data.products, '+')})`, enabled: true };
    }
    return {
      text: `Cho ${data.displayName} (đã thuần) ăn (${cost}) · no ${Math.round(animal.hunger)}%`,
      enabled: affordable,
    };
  }
}
