using UnityEngine;
using UnityEngine.UI;

namespace HongikIngan
{
    /// <summary>Uses the prototype's OS font fallback; assign a font to keep a designer's choice.</summary>
    [ExecuteAlways]
    public sealed class BattleUiFont : MonoBehaviour
    {
        public Font font;
        static Font fallback;
        void OnEnable() => Apply();
        void OnValidate() => Apply();
        void Apply()
        {
            if (font == null && fallback == null)
                fallback = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 20);
            foreach (var label in GetComponentsInChildren<Text>(true)) label.font = font != null ? font : fallback;
        }
    }
}
