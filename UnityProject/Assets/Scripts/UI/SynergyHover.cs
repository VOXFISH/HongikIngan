using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace HongikIngan
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class SynergyHover : MonoBehaviour
    {
        public SynergyPopup popup;
        TMP_Text label;
        readonly List<RaycastResult> hits = new List<RaycastResult>();
        void Awake() => label = GetComponent<TMP_Text>();
        void Update()
        {
            if (popup == null || Mouse.current == null || EventSystem.current == null) return;
            Inspect(Mouse.current.position.ReadValue());
        }
        public void Inspect(Vector2 point)
        {
            if (label == null) label = GetComponent<TMP_Text>();
            if (SceneTransitionOverlay.Current != null && SceneTransitionOverlay.Current.IsBusy) { popup.Hide(this); return; }
            hits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position = point}, hits);
            if (hits.Count == 0 || hits[0].gameObject != gameObject) { popup.Hide(this); return; }
            var canvas = label.canvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            int link = TMP_TextUtilities.FindIntersectingLink(label, point, camera);
            if (link < 0) { popup.Hide(this); return; }
            popup.Show(this, label.textInfo.linkInfo[link].GetLinkID(), point, camera);
        }
        void OnDisable() { if (popup != null) popup.Hide(this); }
    }
}
