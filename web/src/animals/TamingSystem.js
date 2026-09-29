import { floatText, notify } from '../core/EventBus.js';
import { formatAmounts } from '../data/gameData.js';

// Tương đương TamingSystem.cs, kèm cùng bộ thông báo (EventBus.RaiseNotification).
export class TamingSystem {
  constructor({ resources }) {
    this.resources = resources;
  }

  tryInteract(animal) {
    if (!animal.isTamed) return this.#tryFeed(animal);
    return animal.productReady ? this.#collectProduct(animal) : this.#tryFeed(animal);
  }

  #tryFeed(animal) {
    const { data } = animal;
    if (!this.resources.spendAll(data.feedCost)) {
      notify(`Không đủ tài nguyên để cho ${data.displayName} ăn (cần ${formatAmounts(data.feedCost)})`);
      return false;
    }

    const wasWild = !animal.isTamed;
    animal.feed();
    floatText(animal.x, animal.y, formatAmounts(data.feedCost, '-'), 'bad');

    if (wasWild && animal.isTamed) notify(`Đã thuần hóa ${data.displayName}!`);
    else if (wasWild) notify(`Đã cho ăn (${animal.tamingProgress}/${data.feedingsToTame})`);
    else notify(`Đã cho ${data.displayName} ăn`);
    return true;
  }

  #collectProduct(animal) {
    const { products, displayName } = animal.data;
    if (!animal.collectProduct(this.resources)) return false;

    const summary = formatAmounts(products, '+');
    floatText(animal.x, animal.y, summary);
    notify(`Thu hoạch từ ${displayName}: ${summary}`);
    return true;
  }
}
