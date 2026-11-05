using TMPro;
using UnityEngine;

namespace HongikIngan
{
    /// <summary>Optional TMP font override; otherwise preserve each label's authored font.</summary>
    [ExecuteAlways]
    public sealed class BattleUiFont : MonoBehaviour
    {
        public TMP_FontAsset font;
        static TMP_FontAsset defaultFont;
        public static TMP_FontAsset DefaultFont => defaultFont != null
            ? defaultFont
            : defaultFont = Resources.Load<TMP_FontAsset>("GmarketSansTTFMedium SDF");

        void OnEnable() => Apply();
        void OnValidate() => Apply();
        void Apply()
        {
            foreach (var label in GetComponentsInChildren<TMP_Text>(true))
                if (font != null || label.font == null)
                    label.font = font != null ? font : DefaultFont;
        }
    }
}
