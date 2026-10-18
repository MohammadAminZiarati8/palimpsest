#nullable enable
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Game.Combat
{
    /// <summary>
    /// Placeholder uGUI built from code (no prefabs, no art yet, DEMO-12). Shared by the fight and stage screens.
    /// Layout is anchored on a 1920×1080 reference canvas.
    /// </summary>
    public static class Ui
    {
        private static Font? _font;
        public static Font Font => _font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static readonly Color Hero = new Color(0.25f, 0.45f, 0.8f);
        public static readonly Color Enemy = new Color(0.75f, 0.3f, 0.3f);
        public static readonly Color Empty = new Color(1f, 1f, 1f, 0.06f);
        public static readonly Color Active = new Color(1f, 0.85f, 0.2f);
        public static readonly Color Picked = new Color(0.3f, 1f, 0.4f);
        public static readonly Color Dim = new Color(0.45f, 0.45f, 0.45f);

        /// <summary>A screen-space canvas under <paramref name="parent"/>; also makes sure an EventSystem exists.</summary>
        public static RectTransform Canvas(Transform parent, string name, int order = 0)
        {
            if (Object.FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return (RectTransform)go.transform;
        }

        public static Image Panel(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text Label(RectTransform parent, string name, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Button Button(RectTransform parent, string name, int size, UnityAction onClick, string text = "")
        {
            var img = Panel(parent, name, Color.white);
            var b = img.gameObject.AddComponent<Button>();
            b.onClick.AddListener(onClick);
            var t = Label(img.rectTransform, "Text", size, TextAnchor.MiddleCenter);
            t.color = Color.black;
            t.text = text;
            Stretch(t.rectTransform, 0.03f, 0.05f, 0.97f, 0.95f);
            return b;
        }

        /// <summary>Button text (the label made by <see cref="Button"/>).</summary>
        public static void SetText(Button b, string text) => b.GetComponentInChildren<Text>().text = text;

        public static RectTransform Rect(Component c) => (RectTransform)c.transform;

        public static void Place(RectTransform rt, float ax, float ay, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = Vector2.zero;
        }

        public static void Stretch(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
