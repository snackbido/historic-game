// Tương đương SaveSystem.cs — lưu JSON vào localStorage thay cho Application.persistentDataPath.
export class SaveSystem {
  constructor(key) {
    this.key = key;
  }

  save(data) {
    try {
      localStorage.setItem(this.key, JSON.stringify(data));
      return true;
    } catch {
      return false;
    }
  }

  load() {
    try {
      const raw = localStorage.getItem(this.key);
      return raw ? JSON.parse(raw) : null;
    } catch {
      return null;
    }
  }
}
