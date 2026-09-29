export class ResourceNode {
  kind = 'resourceNode';
  interactExtent = 0.5;

  constructor({ typeId, x, y, amount = 10, yieldPerHit = 1, maxAmount = amount }) {
    this.typeId = typeId;
    this.x = x;
    this.y = y;
    this.amountRemaining = amount;
    this.maxAmount = maxAmount;
    this.yieldPerHit = yieldPerHit;
  }

  get depleted() {
    return this.amountRemaining <= 0;
  }

  /** Trả về số tài nguyên thực nhận được. */
  harvest(resourceManager) {
    const amount = Math.min(this.yieldPerHit, this.amountRemaining);
    if (amount <= 0) return 0;

    this.amountRemaining -= amount;
    resourceManager.add(this.typeId, amount);
    return amount;
  }

  getSaveData() {
    const { typeId, x, y, amountRemaining, maxAmount, yieldPerHit } = this;
    return { typeId, x, y, amount: amountRemaining, maxAmount, yieldPerHit };
  }
}
