import './style.css';
import { GameManager } from './core/GameManager.js';
import { Input } from './core/Input.js';
import { Renderer } from './render/Renderer.js';
import { HUD } from './ui/HUD.js';

const canvas = document.getElementById('game-canvas');
const input = new Input(canvas);
const game = new GameManager();
const renderer = new Renderer(canvas, document.getElementById('floaters'), game);
const hud = new HUD(document.getElementById('hud'), game);

// Mở devtools console gõ `game` để debug trạng thái (giống RunCommand qua Unity MCP).
window.game = game;

let last = performance.now();
function frame(now) {
  const dt = Math.min((now - last) / 1000, 0.1);
  last = now;

  game.update(dt, input, (pointer) => renderer.pickGround(pointer));
  renderer.update(dt, input);
  hud.update(dt);
  input.endFrame();

  requestAnimationFrame(frame);
}
requestAnimationFrame(frame);
