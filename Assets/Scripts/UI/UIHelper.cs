using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace IdleGame.UI
{
    public static class UIHelper
    {
        public static GameObject MakeText(Transform parent, string text, int fontSize,
            TextAnchor alignment = TextAnchor.MiddleLeft,
            Vector2 offsetMin = default, Vector2 offsetMax = default,
            Color? color = null,
            Vector2? anchorMin = null, Vector2? anchorMax = null)
        {
            GameObject obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.AddComponent<RectTransform>();

            if (anchorMin.HasValue && anchorMax.HasValue)
            {
                rt.anchorMin = anchorMin.Value;
                rt.anchorMax = anchorMax.Value;
                rt.offsetMin = offsetMin;
                rt.offsetMax = offsetMax;
            }
            else
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = offsetMin;
                rt.offsetMax = offsetMax;
            }

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color ?? Color.white;
            tmp.alignment = alignment switch
            {
                TextAnchor.UpperCenter => TextAlignmentOptions.Top,
                TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
                TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
                TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
                _ => TextAlignmentOptions.Left
            };
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            return obj;
        }

        public static GameObject MakeButton(Transform parent, string label, int fontSize,
            Color? bgColor = null)
        {
            GameObject obj = new GameObject("Button");
            obj.transform.SetParent(parent, false);

            obj.AddComponent<RectTransform>();

            Image img = obj.AddComponent<Image>();
            img.color = bgColor ?? new Color(0.25f, 0.25f, 0.3f, 1f);

            Button btn = obj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            btn.colors = cb;
            btn.targetGraphic = img;

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(obj.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(4, 4);
            textRt.offsetMax = new Vector2(-4, -4);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Midline;
            tmp.raycastTarget = false;

            return obj;
        }

        public static GameObject MakeScrollView(Transform parent, out Transform content)
        {
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(parent, false);
            scrollObj.AddComponent<RectTransform>();

            Image bg = scrollObj.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.8f);

            ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();

            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform vpRt = viewport.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            // Content
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewport.transform, false);
            RectTransform contentRt = contentObj.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRt;
            scrollRect.viewport = vpRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 30;

            content = contentObj.transform;
            return scrollObj;
        }
    }
}
