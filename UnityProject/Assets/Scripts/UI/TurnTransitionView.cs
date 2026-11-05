using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HongikIngan
{
    public sealed class TurnTransitionView : MonoBehaviour
    {
        public CanvasGroup panel;
        public TMP_Text title;
        [Min(0)] public float duration = .8f;
        public int LastShownTurn { get; private set; }
        public bool IsShowing { get; private set; }
        public IEnumerator Show(int turn)
        {
            LastShownTurn = turn; IsShowing = true;
            title.text = "Turn " + turn;
            panel.gameObject.SetActive(true); panel.alpha = 1; panel.blocksRaycasts = true;
            try { yield return new WaitForSecondsRealtime(duration); }
            finally { Hide(); }
        }
        public void Hide()
        {
            IsShowing = false;
            if (panel == null) return;
            panel.alpha = 0; panel.blocksRaycasts = false;
            panel.gameObject.SetActive(false);
        }
    }
}
