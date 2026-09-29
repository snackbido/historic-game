const MOVE_KEYS = {
  up: ['KeyW', 'ArrowUp'],
  down: ['KeyS', 'ArrowDown'],
  left: ['KeyA', 'ArrowLeft'],
  right: ['KeyD', 'ArrowRight'],
};

// F5/F9 là phím lưu/tải như bản Unity — chặn trình duyệt reload trang.
const PREVENT_DEFAULT = new Set(['F5', 'F9', 'Space', 'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight']);

const isTyping = (el) => el instanceof HTMLInputElement || el instanceof HTMLTextAreaElement;

/**
 * Gom input bàn phím + chuột thành trạng thái theo frame.
 * Sự kiện chuột chỉ gắn lên canvas, nên click lên panel UI không bao giờ lọt xuống game
 * (tương đương EventSystem.IsPointerOverGameObject() bên Unity).
 */
export class Input {
  #down = new Set();
  #pressed = new Set();
  #clicks = new Set();

  pointer = { ndcX: 0, ndcY: 0, inside: false };
  wheel = 0;

  constructor(canvas) {
    window.addEventListener('keydown', (e) => {
      if (isTyping(e.target)) return;
      if (PREVENT_DEFAULT.has(e.code)) e.preventDefault();
      if (!e.repeat) this.#pressed.add(e.code);
      this.#down.add(e.code);
    });
    window.addEventListener('keyup', (e) => this.#down.delete(e.code));
    window.addEventListener('blur', () => this.#down.clear());

    canvas.addEventListener('pointermove', (e) => this.#updatePointer(e, canvas));
    canvas.addEventListener('pointerleave', () => (this.pointer.inside = false));
    canvas.addEventListener('pointerdown', (e) => {
      this.#updatePointer(e, canvas);
      this.#clicks.add(e.button);
    });
    canvas.addEventListener('contextmenu', (e) => e.preventDefault());
    canvas.addEventListener(
      'wheel',
      (e) => {
        e.preventDefault();
        this.wheel += Math.sign(e.deltaY);
      },
      { passive: false },
    );
  }

  #updatePointer(e, canvas) {
    const rect = canvas.getBoundingClientRect();
    this.pointer.ndcX = ((e.clientX - rect.left) / rect.width) * 2 - 1;
    this.pointer.ndcY = -(((e.clientY - rect.top) / rect.height) * 2 - 1);
    this.pointer.inside = true;
  }

  isDown(code) {
    return this.#down.has(code);
  }

  wasPressed(code) {
    return this.#pressed.has(code);
  }

  consumeClick(button) {
    const had = this.#clicks.has(button);
    this.#clicks.delete(button);
    return had;
  }

  moveVector() {
    const any = (codes) => codes.some((c) => this.#down.has(c));
    let x = (any(MOVE_KEYS.right) ? 1 : 0) - (any(MOVE_KEYS.left) ? 1 : 0);
    let y = (any(MOVE_KEYS.up) ? 1 : 0) - (any(MOVE_KEYS.down) ? 1 : 0);
    const len = Math.hypot(x, y);
    if (len > 0) {
      x /= len;
      y /= len;
    }
    return { x, y };
  }

  endFrame() {
    this.#pressed.clear();
    this.#clicks.clear();
    this.wheel = 0;
  }
}
