import { eventBus, Events } from '../core/EventBus.js';
import { formatAmounts, getResourceType } from '../data/gameData.js';

const TOAST_DURATION = 2600;
const MAX_TOASTS = 4;

const el = (tag, className, text) => {
  const node = document.createElement(tag);
  if (className) node.className = className;
  if (text !== undefined) node.textContent = text;
  return node;
};

const costLabel = (costs) =>
  costs.map(({ type, amount }) => `${getResourceType(type)?.icon ?? ''} ${amount}`).join('  ');

/**
 * HUD dạng DOM overlay — tương đương ResourceBarUI / BuildMenuUI / CropSelectionUI /
 * TechTreeUI / NotificationUI. Mỗi danh sách có container riêng (tránh lỗi "xóa nhầm nút"
 * đã gặp ở bản Unity), dựng nút một lần rồi chỉ cập nhật trạng thái.
 */
export class HUD {
  #chips = new Map();
  #buildItems = [];
  #cropItems = [];
  #techItems = [];

  constructor(root, game) {
    this.game = game;
    this.root = root;
    this.resourceBar = root.querySelector('#resource-bar');
    this.buildList = root.querySelector('#build-panel .list');
    this.cropList = root.querySelector('#crop-panel .list');
    this.techList = root.querySelector('#tech-panel .list');
    this.toasts = root.querySelector('#toasts');
    this.prompt = root.querySelector('#prompt');
    this.placementHint = root.querySelector('#placement-hint');

    this.#buildResourceBar();
    this.#buildLists();

    root.querySelector('#btn-save').addEventListener('click', () => game.saveGame());
    root.querySelector('#btn-load').addEventListener('click', () => game.loadGame());

    eventBus.on(Events.ResourceChanged, (e) => this.#onResourceChanged(e));
    eventBus.on(Events.TechUnlocked, () => this.#refreshStates());
    eventBus.on(Events.SelectionChanged, () => this.#refreshStates());
    eventBus.on(Events.GameLoaded, () => this.#refreshStates());
    eventBus.on(Events.Notification, (msg) => this.#toast(msg));

    this.#refreshStates();
  }

  // ─── Resource bar ────────────────────────────────────────────────────────
  #buildResourceBar() {
    for (const type of this.game.data.resourceTypes) {
      const chip = el('div', 'chip');
      chip.title = type.displayName;
      const icon = el('span', 'chip__icon', type.icon);
      const body = el('div', 'chip__body');
      const name = el('span', 'chip__name', type.displayName);
      const value = el('span', 'chip__value', String(this.game.resources.get(type.id)));
      body.append(name, value);
      chip.append(icon, body);

      let progress = null;
      if (type.id === this.game.tech.knowledgeTypeId) {
        progress = el('div', 'chip__progress');
        progress.append(el('div', 'chip__progress-fill'));
        chip.append(progress);
      }
      this.resourceBar.append(chip);
      this.#chips.set(type.id, { chip, value, progress });
    }
  }

  #onResourceChanged({ typeId, amount, delta }) {
    const entry = this.#chips.get(typeId);
    if (entry) {
      entry.value.textContent = String(amount);
      if (delta !== 0) {
        entry.chip.classList.remove('chip--up', 'chip--down');
        void entry.chip.offsetWidth; // restart animation
        entry.chip.classList.add(delta > 0 ? 'chip--up' : 'chip--down');
      }
    }
    this.#refreshStates();
  }

  // ─── Panels ──────────────────────────────────────────────────────────────
  #buildLists() {
    const { data, placer, farm, tech } = this.game;
    const techUnlocking = (kind, id) => tech.techs.find((t) => t[kind].includes(id));

    for (const building of data.buildings) {
      const button = this.#itemButton(building.displayName, costLabel(building.costs));
      button.addEventListener('click', () => placer.select(building));
      const lockedBy = techUnlocking('unlockedBuildingIds', building.id);
      this.buildList.append(button);
      this.#buildItems.push({ button, building, lockedBy });
    }

    for (const crop of data.crops) {
      const button = this.#itemButton(crop.displayName, `⏱ ${crop.timeToSprout + crop.timeToMature}s`);
      button.addEventListener('click', () => farm.selectCrop(crop));
      const lockedBy = techUnlocking('unlockedCropIds', crop.id);
      this.cropList.append(button);
      this.#cropItems.push({ button, crop, lockedBy });
    }

    const nameOf = (list, id) => list.find((x) => x.id === id)?.displayName ?? id;
    for (const t of data.techs) {
      const card = el('div', 'tech');
      const head = el('div', 'tech__head');
      head.append(el('span', 'tech__name', t.displayName), el('span', 'tech__cost', costLabel(t.cost)));
      const desc = el('p', 'tech__desc', t.description ?? '');
      const unlocks = [
        ...t.unlockedBuildingIds.map((id) => nameOf(data.buildings, id)),
        ...t.unlockedCropIds.map((id) => nameOf(data.crops, id)),
      ];
      const unlockText = el('p', 'tech__unlocks', `Mở khóa: ${unlocks.join(', ') || '—'}`);
      const button = el('button', 'tech__button', 'Nghiên cứu');
      button.type = 'button';
      button.addEventListener('click', () => tech.tryUnlock(t));
      card.append(head, desc, unlockText, button);
      this.techList.append(card);
      this.#techItems.push({ card, button, tech: t });
    }
  }

  #itemButton(name, meta) {
    const button = el('button', 'item');
    button.type = 'button';
    button.append(el('span', 'item__name', name), el('span', 'item__meta', meta));
    return button;
  }

  #refreshStates() {
    const { placer, farm, tech, resources } = this.game;

    for (const { button, building, lockedBy } of this.#buildItems) {
      const unlocked = tech.isBuildingUnlocked(building);
      this.#setItemState(button, {
        unlocked,
        selected: placer.selected === building,
        affordable: resources.canAfford(building.costs),
        lockedHint: lockedBy ? `🔒 Cần nghiên cứu ${lockedBy.displayName}` : '🔒 Chưa mở khóa',
      });
    }

    for (const { button, crop, lockedBy } of this.#cropItems) {
      this.#setItemState(button, {
        unlocked: tech.isCropUnlocked(crop),
        selected: farm.selectedCrop === crop,
        affordable: true,
        lockedHint: lockedBy ? `🔒 Cần nghiên cứu ${lockedBy.displayName}` : '🔒 Chưa mở khóa',
      });
    }

    for (const { card, button, tech: t } of this.#techItems) {
      const done = tech.isUnlocked(t);
      card.classList.toggle('tech--done', done);
      button.disabled = !tech.canUnlock(t);
      if (done) button.textContent = '✓ Đã nghiên cứu';
      else if (!tech.prerequisitesMet(t)) button.textContent = '🔒 Thiếu công nghệ trước';
      else button.textContent = resources.canAfford(t.cost) ? 'Nghiên cứu' : 'Chưa đủ tri thức';
    }
  }

  #setItemState(button, { unlocked, selected, affordable, lockedHint }) {
    button.disabled = !unlocked;
    button.classList.toggle('item--selected', selected);
    button.classList.toggle('item--poor', unlocked && !affordable);
    button.title = unlocked ? '' : lockedHint;
  }

  // ─── Notifications ───────────────────────────────────────────────────────
  #toast(message) {
    const toast = el('div', 'toast', message);
    this.toasts.prepend(toast);
    while (this.toasts.children.length > MAX_TOASTS) this.toasts.lastElementChild.remove();
    setTimeout(() => toast.classList.add('toast--out'), TOAST_DURATION);
    setTimeout(() => toast.remove(), TOAST_DURATION + 400);
  }

  // ─── Per-frame ───────────────────────────────────────────────────────────
  update() {
    const { interaction, placer, tech } = this.game;

    const knowledge = this.#chips.get(tech.knowledgeTypeId);
    if (knowledge?.progress) knowledge.progress.firstChild.style.width = `${Math.min(1, tech.knowledgeProgress) * 100}%`;

    const prompt = placer.selected ? null : interaction.getPrompt();
    this.prompt.classList.toggle('hidden', !prompt);
    if (prompt) {
      this.prompt.classList.toggle('prompt--disabled', !prompt.enabled);
      const { text } = prompt;
      if (this.prompt.dataset.text !== text || this.prompt.dataset.enabled !== String(prompt.enabled)) {
        this.prompt.dataset.text = text;
        this.prompt.dataset.enabled = String(prompt.enabled);
        this.prompt.replaceChildren();
        if (prompt.enabled) this.prompt.append(el('kbd', null, 'E'));
        this.prompt.append(el('span', null, text));
      }
    }

    this.placementHint.classList.toggle('hidden', !placer.selected);
    if (placer.selected) {
      const check = placer.hoverCell ? placer.checkPlacement(placer.hoverCell) : { ok: true };
      const status = check.ok ? '' : ` — ${check.reason}`;
      const text = `Đặt ${placer.selected.displayName} (${formatAmounts(placer.selected.costs)})${status}`;
      if (this.placementHint.dataset.text !== text) {
        this.placementHint.dataset.text = text;
        this.placementHint.replaceChildren(
          el('strong', null, text),
          el('span', null, 'Chuột trái: đặt · Chuột phải / Esc: hủy'),
        );
        this.placementHint.classList.toggle('placement-hint--bad', !check.ok);
      }
    }
  }
}
