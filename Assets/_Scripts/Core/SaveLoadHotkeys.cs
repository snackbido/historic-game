using UnityEngine;

namespace PrehistoricTribe
{
    public class SaveLoadHotkeys : MonoBehaviour
    {
        [SerializeField] private KeyCode saveKey = KeyCode.F5;
        [SerializeField] private KeyCode loadKey = KeyCode.F9;

        private void Update()
        {
            if (GameMenuUI.IsOpen) return; // đang mở menu: không nhận lệnh trong game
            if (Input.GetKeyDown(saveKey))
                GameManager.Instance.SaveGame();
            else if (Input.GetKeyDown(loadKey))
                GameManager.Instance.LoadGame();
        }
    }
}
