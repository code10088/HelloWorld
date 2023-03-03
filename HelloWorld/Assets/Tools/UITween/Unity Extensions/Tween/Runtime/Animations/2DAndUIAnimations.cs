#define USE_TEXT_MESH_PRO
#define USE_UGUI

using UnityEngine;
using System;

#if USE_TEXT_MESH_PRO
using TMPro;
#endif

#if USE_UGUI
using UnityEngine.UI;
#endif

namespace UnityExtensions.Tween
{
#if USE_UGUI

    [Serializable, TweenAnimation("2D and UI/Canvas Group Alpha", "Canvas Group Alpha")]
    public class TweenCanvasGroupAlpha : TweenFloat<CanvasGroup>
    {
        public override float current
        {
            get => target ? target.alpha : 1f;
            set { if (target) target.alpha = value; }
        }

        public override TweenFloat<CanvasGroup> Clone3()
        {
            return new TweenCanvasGroupAlpha();
        }
    }

    [Serializable, TweenAnimation("2D and UI/Graphic Color", "Graphic Color")]
    public class TweenGraphicColor : TweenColor<Graphic>
    {
        public override Color current
        {
            get => target ? target.color : Color.white;
            set { if (target) target.color = value; }
        }

        public override TweenColor<Graphic> Clone3()
        {
            return new TweenGraphicColor();
        }
    }

    [Serializable, TweenAnimation("2D and UI/Image Fill Amount", "Image Fill Amount")]
    public class TweenImageFillAmount : TweenFloat<Image>
    {
        public override float current
        {
            get => target ? target.fillAmount : 1;
            set { if (target) target.fillAmount = value; }
        }

        public override TweenFloat<Image> Clone3()
        {
            return new TweenImageFillAmount();
        }
    }


    [Serializable, TweenAnimation("2D and UI/Grid Layout Group Cell Size", "Grid Layout Group Cell Size")]
    public class TweenGridLayoutGroupCellSize : TweenVector2<GridLayoutGroup>
    {
        public override Vector2 current
        {
            get => target ? target.cellSize : default;
            set { if (target) target.cellSize = value; }
        }

        public override TweenVector2<GridLayoutGroup> Clone3()
        {
            return new TweenGridLayoutGroupCellSize();
        }
    }

    [Serializable, TweenAnimation("2D and UI/Grid Layout Group Spacing", "Grid Layout Group Spacing")]
    public class TweenGridLayoutGroupSpacing : TweenVector2<GridLayoutGroup>
    {
        public override Vector2 current
        {
            get => target ? target.spacing : default;
            set { if (target) target.spacing = value; }
        }

        public override TweenVector2<GridLayoutGroup> Clone3()
        {
            return new TweenGridLayoutGroupSpacing();
        }
    }

#endif

    [Serializable, TweenAnimation("2D and UI/Sprite Color", "Sprite Color")]
    public class TweenSpriteColor : TweenColor<SpriteRenderer>
    {
        public override Color current
        {
            get => target ? target.color : Color.white;
            set { if (target) target.color = value; }
        }

        public override TweenColor<SpriteRenderer> Clone3()
        {
            return new TweenSpriteColor();
        }
    }


#if USE_TEXT_MESH_PRO

    [Serializable, TweenAnimation("2D and UI/Text Mesh Pro Font Size", "Text Mesh Pro Font Size")]
    public class TextMeshProFontSize : TweenFloat<TMP_Text>
    {
        public override float current
        {
            get => target ? target.fontSize : 1f;
            set { if (target) target.fontSize = value; }
        }

        public override TweenFloat<TMP_Text> Clone3()
        {
            return new TextMeshProFontSize();
        }
    }

#endif

} // namespace UnityExtensions.Tween