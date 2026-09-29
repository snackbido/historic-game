import { eventBus, Events, floatText, notify } from '../core/EventBus.js';
import { formatAmounts } from '../data/gameData.js';
import { FarmPlotState } from './FarmPlot.js';

export class FarmManager {
  selectedCrop = null;

  constructor({ resources, tech }) {
    this.resources = resources;
    this.tech = tech;
  }

  selectCrop(crop) {
    if (crop && !this.tech.isCropUnlocked(crop)) return;
    this.selectedCrop = this.selectedCrop === crop ? null : crop;
    eventBus.emit(Events.SelectionChanged);
  }

  tryInteract(plot) {
    switch (plot.state) {
      case FarmPlotState.Empty:
        if (!this.selectedCrop) {
          notify('Chọn hạt giống ở bảng bên phải trước khi gieo');
          return false;
        }
        if (!plot.plant(this.selectedCrop)) return false;
        notify(`Đã gieo ${this.selectedCrop.displayName}`);
        return true;

      case FarmPlotState.ReadyToHarvest: {
        const yields = plot.crop.harvestYield;
        if (!plot.harvest(this.resources)) return false;
        floatText(plot.x, plot.y, formatAmounts(yields, '+'));
        return true;
      }

      case FarmPlotState.Withered:
        plot.clearWithered();
        notify('Đã dọn cây héo');
        return true;

      default:
        return false;
    }
  }
}
