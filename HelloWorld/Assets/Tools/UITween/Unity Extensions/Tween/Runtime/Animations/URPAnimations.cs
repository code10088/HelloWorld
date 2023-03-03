// Enable the URP define if you need it
//#define URP

#if URP

using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityExtensions.Tween
{
    [Serializable, TweenAnimation("Rendering/Light 2D Outer Angle", "Light 2D Outer Angle")]
    public class TweenLight2DOuterAngle : TweenFloat<Light2D>
    {
        public override float current
        {
            get => target ? target.pointLightOuterAngle : 30f;
            set { if (target) target.pointLightOuterAngle = value; }
        }

        public override TweenFloat<Light2D> Clone3()
        {
            return new TweenLight2DOuterAngle();
        }
    }

    [Serializable, TweenAnimation("Rendering/Light 2D Outer Radius", "Light 2D Outer Radius")]
    public class TweenLight2DOuterRadius : TweenFloat<Light2D>
    {
        public override float current
        {
            get => target ? target.pointLightOuterRadius : 10f;
            set { if (target) target.pointLightOuterRadius = value; }
        }

        public override TweenFloat<Light2D> Clone3()
        {
            return new TweenLight2DOuterRadius();
        }
    }

    [Serializable, TweenAnimation("Rendering/Light 2D Intensity", "Light 2D Intensity")]
    public class TweenLight2DIntensity : TweenFloat<Light2D>
    {
        public override float current
        {
            get => target ? target.intensity : 1f;
            set { if (target) target.intensity = value; }
        }

        public override TweenFloat<Light2D> Clone3()
        {
            return new TweenLight2DIntensity();
        }
    }

    [Serializable, TweenAnimation("Rendering/Light 2D Color", "Light 2D Color")]
    public class TweenLight2DColor : TweenColor<Light2D>
    {
        public override Color current
        {
            get => target ? target.color : Color.white;
            set { if (target) target.color = value; }
        }

        public override TweenColor<Light2D> Clone3()
        {
            return new TweenLight2DColor();
        }
    }

} // namespace UnityExtensions.Tween

#endif