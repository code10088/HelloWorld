using UnityEngine;
using UnityEngine.Rendering;
using System;

namespace UnityExtensions.Tween
{
    [Serializable, TweenAnimation("Rendering/Light Color", "Light Color")]
    public class TweenLightColor : TweenColor<Light>
    {
        public override Color current
        {
            get => target ? target.color : Color.white;
            set { if (target) target.color = value; }
        }

        public override TweenColor<Light> Clone3()
        {
            return new TweenLightColor();
        }
    }

    [Serializable, TweenAnimation("Rendering/Light Intensity", "Light Intensity")]
    public class TweenLightIntensity : TweenFloat<Light>
    {
        public override float current
        {
            get => target ? target.intensity : 1f;
            set { if (target) target.intensity = value; }
        }

        public override TweenFloat<Light> Clone3()
        {
            return new TweenLightIntensity();
        }
    }

    [Serializable, TweenAnimation("Rendering/Light Range", "Light Range")]
    public class TweenLightRange : TweenFloat<Light>
    {
        public override float current
        {
            get => target ? target.range : 10f;
            set { if (target) target.range = value; }
        }

        public override TweenFloat<Light> Clone3()
        {
            return new TweenLightRange();
        }
    }

} // namespace UnityExtensions.Tween