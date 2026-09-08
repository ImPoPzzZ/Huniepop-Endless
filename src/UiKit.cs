using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HuniepopEndless
{
    /// <summary>
    /// Small uGUI construction helpers. Huniepop 2's own UI is a forest of authored
    /// prefabs we can't recreate, so every panel this mod shows is built from bare
    /// <c>UnityEngine.UI</c> primitives on our own overlay canvas.
    /// </summary>
    internal static class UiKit
    {
        internal static readonly Color Ink        = new Color(0.10f, 0.06f, 0.12f, 1f);
        internal static readonly Color Panel      = new Color(0.16f, 0.11f, 0.20f, 0.98f);
        internal static readonly Color Accent     = new Color(0.96f, 0.36f, 0.62f, 1f); // huniepop pink
        internal static readonly Color AccentDim  = new Color(0.55f, 0.24f, 0.40f, 1f);
        internal static readonly Color TextLight  = new Color(0.97f, 0.95f, 0.98f, 1f);
        internal static readonly Color Warn       = new Color(1f, 0.78f, 0.32f, 1f);

        private static Font _font;
        internal static Font UiFont
        {
            get
            {
                if (_font != null) return _font;
                try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
                if (_font == null) try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
                if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 20);
                return _font;
            }
        }

        /// <summary>Full-screen overlay canvas that renders above everything HP2 draws.</summary>
        internal static Canvas MakeOverlayCanvas(string name, int sortOrder = 5000)
        {
            var go = new GameObject(name);
            UnityEngine.Object.DontDestroyOnLoad(go);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
            return canvas;
        }

        internal static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var es = new GameObject("HE_EventSystem");
            UnityEngine.Object.DontDestroyOnLoad(es);
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        internal static RectTransform Stretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        internal static Image MakeImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        internal static RectTransform Box(Transform parent, string name, Color color,
            Vector2 anchoredPos, Vector2 size, Vector2? pivot = null)
        {
            var img = MakeImage(parent, name, color);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            return rt;
        }

        internal static Text MakeText(Transform parent, string name, string content,
            int fontSize, TextAnchor align, Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = UiFont;
            t.text = content;
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = color ?? TextLight;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Text confined to a fixed box centered at <paramref name="anchoredPos"/>.</summary>
        internal static Text TextBox(Transform parent, string name, string content, Vector2 anchoredPos,
            Vector2 size, int fontSize, TextAnchor align, Color? color = null)
        {
            var t = MakeText(parent, name, content, fontSize, align, color);
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            return t;
        }

        /// <summary>A rounded-ish flat button. <paramref name="onClick"/> fires on release.</summary>
        internal static Button MakeButton(Transform parent, string label, Vector2 size,
            Vector2 anchoredPos, Action onClick, Color? bg = null, int fontSize = 26)
        {
            var img = MakeImage(parent, "btn_" + label, bg ?? Accent);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.fadeDuration = 0.06f;
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            var t = MakeText(img.transform, "label", label, fontSize, TextAnchor.MiddleCenter, Ink);
            Stretch(t.gameObject);
            return btn;
        }

        /// <summary>
        /// Dim + cheap-blur backdrop that eats all clicks. Blur is a screen grab
        /// downsampled to a tiny RenderTexture and stretched back up (bilinear),
        /// which reads as a soft blur without any custom shader. Falls back to a
        /// plain dark scrim if the grab fails.
        /// </summary>
        internal static GameObject MakeBackdrop(Transform parent, float dim = 0.55f)
        {
            var root = new GameObject("backdrop", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            Stretch(root);

            try
            {
                var shot = ScreenCapture.CaptureScreenshotAsTexture();
                var small = new RenderTexture(Mathf.Max(16, Screen.width / 12),
                                              Mathf.Max(9, Screen.height / 12), 0);
                small.filterMode = FilterMode.Bilinear;
                var prev = RenderTexture.active;
                Graphics.Blit(shot, small);
                RenderTexture.active = prev;
                UnityEngine.Object.Destroy(shot);

                var raw = new GameObject("blur", typeof(RectTransform)).AddComponent<RawImage>();
                raw.transform.SetParent(root.transform, false);
                Stretch(raw.gameObject);
                raw.texture = small;
                raw.color = Color.white;
            }
            catch (Exception e)
            {
                EndlessPlugin.Log.LogWarning("Backdrop blur unavailable, using scrim: " + e.Message);
            }

            var scrim = MakeImage(root.transform, "scrim", new Color(0f, 0f, 0f, dim));
            Stretch(scrim.gameObject);
            // Scrim is the raycast blocker — swallow every click behind the panel.
            scrim.raycastTarget = true;
            return root;
        }
    }
}
