using UnityEngine;
using UnityEngine.UI;

namespace HongikIngan
{
    /// <summary>Behavior for the independently editable controls in Main.unity.</summary>
    public sealed class MainMenuScreen : MonoBehaviour
    {
        public Button startButton, exitButton;
        Font menuFont;
        bool starting;

        void Start()
        {
            menuFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 24);
            foreach (var label in GetComponentsInChildren<Text>(true)) label.font = menuFont;
            startButton.onClick.AddListener(StartNewGame);
            exitButton.onClick.AddListener(ExitGame);
        }

        public void StartNewGame()
        {
            if (starting) return;
            starting = true;
            startButton.interactable = exitButton.interactable = false;
            PrototypeSession.Ensure().Restart();
        }

        public void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void OnDestroy()
        {
            if (startButton != null) startButton.onClick.RemoveListener(StartNewGame);
            if (exitButton != null) exitButton.onClick.RemoveListener(ExitGame);
            if (menuFont != null) Destroy(menuFont);
        }
    }
}
