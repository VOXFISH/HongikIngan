using TMPro;
using UnityEngine;

namespace HongikIngan
{
    public sealed class SynergyPopup : MonoBehaviour
    {
        public RectTransform panel, bounds;
        public TMP_Text label;
        public float maximumHeight = 580;
        Object owner;
        public bool IsOpen => panel.gameObject.activeSelf;
        public void Show(Object source, string name, Vector2 screenPoint, Camera camera)
        {
            owner = source;
            label.text = SynergyText.Popup(PrototypeSession.Ensure().Run, name);
            float height = Mathf.Clamp(label.GetPreferredValues(label.text, label.rectTransform.rect.width, 0).y + 40, 120, maximumHeight);
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - 40);
            panel.gameObject.SetActive(true);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds, screenPoint, camera, out var point);
            panel.localPosition = point + new Vector2(24, -18);
            Clamp(panel, bounds);
        }
        public void Hide(Object source)
        {
            if (owner != source) return;
            owner = null; panel.gameObject.SetActive(false);
        }
        public static void Clamp(RectTransform rect, RectTransform bounds)
        {
            // Clamp the rotated corners, not the unrotated width/height.
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            float left = float.MaxValue, right = float.MinValue, bottom = float.MaxValue, top = float.MinValue;
            foreach (var corner in corners)
            {
                var p = bounds.InverseTransformPoint(corner);
                left = Mathf.Min(left,p.x); right = Mathf.Max(right,p.x);
                bottom = Mathf.Min(bottom,p.y); top = Mathf.Max(top,p.y);
            }
            var area = bounds.rect;
            float x = left < area.xMin + 12 ? area.xMin + 12 - left : right > area.xMax - 12 ? area.xMax - 12 - right : 0;
            float y = bottom < area.yMin + 12 ? area.yMin + 12 - bottom : top > area.yMax - 12 ? area.yMax - 12 - top : 0;
            rect.position += bounds.TransformVector(new Vector3(x,y,0));
        }
    }
}
