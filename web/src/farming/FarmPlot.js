export const FarmPlotState = Object.freeze({
  Empty: 'empty',
  Growing: 'growing',
  ReadyToHarvest: 'ready',
  Withered: 'withered',
});

export const CropStage = Object.freeze({
  Seed: 'seed',
  Sprouting: 'sprouting',
  Mature: 'mature',
  Withered: 'withered',
});

// State machine enum + switch, giống FarmPlot.cs.
export class FarmPlot {
  kind = 'farmPlot';
  interactExtent = 0.5;

  crop = null;
  stage = CropStage.Seed;
  stageTimer = 0;
  state = FarmPlotState.Empty;

  constructor({ x, y }) {
    this.x = x;
    this.y = y;
  }

  update(dt) {
    const canWither = this.state === FarmPlotState.ReadyToHarvest && this.crop.witherTime > 0;
    if (this.state !== FarmPlotState.Growing && !canWither) return;

    this.stageTimer += dt;
    this.#advanceStage();
  }

  plant(crop) {
    if (this.state !== FarmPlotState.Empty || !crop) return false;

    this.crop = crop;
    this.stage = CropStage.Seed;
    this.stageTimer = 0;
    this.state = FarmPlotState.Growing;
    return true;
  }

  harvest(resourceManager) {
    if (this.state !== FarmPlotState.ReadyToHarvest) return false;

    for (const y of this.crop.harvestYield) resourceManager.add(y.type, y.amount);
    this.#reset();
    return true;
  }

  clearWithered() {
    if (this.state !== FarmPlotState.Withered) return;
    this.#reset();
  }

  /** 0..1 từ lúc gieo tới lúc chín — dùng cho UI. */
  get growthProgress() {
    if (!this.crop) return 0;
    const total = this.crop.timeToSprout + this.crop.timeToMature;
    if (this.stage === CropStage.Seed) return this.stageTimer / total;
    if (this.stage === CropStage.Sprouting) return (this.crop.timeToSprout + this.stageTimer) / total;
    return 1;
  }

  #advanceStage() {
    switch (this.stage) {
      case CropStage.Seed:
        if (this.stageTimer >= this.crop.timeToSprout) this.#setStage(CropStage.Sprouting);
        break;

      case CropStage.Sprouting:
        if (this.stageTimer >= this.crop.timeToMature) {
          this.#setStage(CropStage.Mature);
          this.state = FarmPlotState.ReadyToHarvest;
        }
        break;

      case CropStage.Mature:
        if (this.crop.witherTime > 0 && this.stageTimer >= this.crop.witherTime) {
          this.#setStage(CropStage.Withered);
          this.state = FarmPlotState.Withered;
        }
        break;
    }
  }

  #setStage(stage) {
    this.stage = stage;
    this.stageTimer = 0;
  }

  #reset() {
    this.crop = null;
    this.stage = CropStage.Seed;
    this.stageTimer = 0;
    this.state = FarmPlotState.Empty;
  }

  getSaveData() {
    return { cropId: this.crop?.id ?? null, stage: this.stage, stageTimer: this.stageTimer, state: this.state };
  }

  loadFromSaveData(data, cropsById) {
    const crop = data?.cropId ? cropsById.get(data.cropId) : null;
    if (!crop) {
      this.#reset();
      return;
    }
    this.crop = crop;
    this.stage = data.stage;
    this.stageTimer = data.stageTimer;
    this.state = data.state;
  }
}
