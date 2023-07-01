using UnityEngine;
using System;

namespace UnityExtensions.Tween
{
    [Serializable, TweenAnimation("Rect Transform/Size Delta", "Rect Transform Size Delta")]
    public class TweenRectTransformSizeDelta : TweenVector2<RectTransform>
    {
        public override Vector2 current
        {
            get => target ? target.sizeDelta : default;
            set { if (target) target.sizeDelta = value; }
        }
    }

    [Serializable, TweenAnimation("Rect Transform/Anchored Position", "Rect Transform Anchored Position")]
    public class TweenRectTransformAnchoredPosition : TweenVector2<RectTransform>
    {
        public override Vector2 current
        {
            get => target ? target.anchoredPosition : default;
            set { if (target) target.anchoredPosition = value; }
        }
    }

    [Serializable, TweenAnimation("Rect Transform/Offset Max", "Rect Transform Offset Max")]
    public class TweenRectTransformOffsetMax : TweenVector2<RectTransform>
    {
        public override Vector2 current
        {
            get => target ? target.offsetMax : default;
            set { if (target) target.offsetMax = value; }
        }
    }

    [Serializable, TweenAnimation("Rect Transform/Offset Min", "Rect Transform Offset Min")]
    public class TweenRectTransformOffsetMin : TweenVector2<RectTransform>
    {
        public override Vector2 current
        {
            get => target ? target.offsetMin : default;
            set { if (target) target.offsetMin = value; }
        }
    }

    [Serializable, TweenAnimation("Rect Transform/Anchor Max", "Rect Transform Anchor Max")]
    public class TweenRectTransformAnchorMax : TweenVector2<RectTransform>
    {
        public override Vector2 current
        {
            get => target ? target.anchorMax : default;
            set { if (target) target.anchorMax = value; }
        }
    }

    [Serializable, TweenAnimation("Rect Transform/Anchor Min", "Rect Transform Anchor Min")]
    public class TweenRectTransformAnchorMin : TweenVector2<RectTransform>
    {
        public override Vector2 current
        {
            get => target ? target.anchorMin : default;
            set { if (target) target.anchorMin = value; }
        }
    }

} // namespace UnityExtensions.Tween