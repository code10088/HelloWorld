using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using TweenType = PrimeTween.TweenAnimation.TweenType;
using R = UnityEngine.Renderer;
using M = UnityEngine.Material;

namespace PrimeTween {
internal static class Utils {
    internal static bool CanHaveCycles(TweenType tweenType) {
        switch (tweenType) {
            case TweenType.Disabled:
            case TweenType.Delay:
            case TweenType.Callback:
                return false;
            default: return true;
        }
    }

    internal static bool IsShake(TweenType tweenType) {
        switch (tweenType) {
            case TweenType.ShakeLocalPosition:
            case TweenType.ShakeLocalRotation:
            case TweenType.ShakeScale:
            case TweenType.ShakeCustom:
            case TweenType.ShakeCamera:
                return true;
            default:
                return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsMaterialAnimation(TweenType tweenType) {
        switch (tweenType) {
            case TweenType.MaterialColorProperty:
            case TweenType.MaterialProperty:
            case TweenType.MaterialAlphaProperty:
            case TweenType.MaterialTextureOffset:
            case TweenType.MaterialTextureScale:
            case TweenType.MaterialPropertyVector4:
            case TweenType.MaterialColor:
            case TweenType.MaterialAlpha:
            case TweenType.MaterialMainTextureOffset:
            case TweenType.MaterialMainTextureScale:

                return true;
            default: return false;
        }
    }

    static UnityEngine.Vector4 ST(UnityEngine.Vector2 scale, UnityEngine.Vector2 offset) => new UnityEngine.Vector4(scale.x, scale.y, offset.x, offset.y);

    internal static void SetMaterialValue(TweenType tweenType, object t, long longParam, TweenAnimation.ValueWrapper value) {
        int propId = (int)longParam;
        switch (tweenType) {
            case TweenType.MaterialColorProperty:     (t as UnityEngine.Material).SetColor        (propId,                                value.color);   break;
            case TweenType.MaterialProperty:          (t as UnityEngine.Material).SetFloat        (propId,                                value.single);  break;
            case TweenType.MaterialAlphaProperty: {         var mat = t as M; mat.SetColor        (propId, mat.GetColor(propId).WithAlpha(value.single)); break; }
            case TweenType.MaterialTextureOffset:     (t as UnityEngine.Material).SetTextureOffset(propId,                                value.vector2); break;
            case TweenType.MaterialTextureScale:      (t as UnityEngine.Material).SetTextureScale (propId,                                value.vector2); break;
            case TweenType.MaterialPropertyVector4:   (t as UnityEngine.Material).SetVector       (propId,                                value.vector4); break;
            case TweenType.MaterialColor:             (t as UnityEngine.Material).color =                                                 value.color;    break;
            case TweenType.MaterialAlpha: {                 var mat = t as M; mat.color =                             mat.color.WithAlpha(value.single);  break; }
            case TweenType.MaterialMainTextureOffset: (t as UnityEngine.Material).mainTextureOffset =                                     value.vector2;  break;
            case TweenType.MaterialMainTextureScale:  (t as UnityEngine.Material).mainTextureScale =                                      value.vector2;  break;


            default: throw new Exception();
        }
    }

    readonly struct MaterialPropertyBlockSetterScope : IDisposable {
        readonly UnityEngine.Renderer renderer;
        internal readonly UnityEngine.MaterialPropertyBlock block;

        public MaterialPropertyBlockSetterScope(object target) {
            renderer = target as UnityEngine.Renderer;
            block = PrimeTweenManager.Instance.materialPropertyBlockForSetter;
            renderer.GetPropertyBlock(block);
        }
        void IDisposable.Dispose() => renderer.SetPropertyBlock(block);
    }

    internal static bool IsMaterialPropertyAnimation(TweenType tweenType) {
        switch (tweenType) {
            case TweenType.MaterialColorProperty:
            case TweenType.MaterialProperty:
            case TweenType.MaterialAlphaProperty:
            case TweenType.MaterialTextureOffset:
            case TweenType.MaterialTextureScale:
            case TweenType.MaterialPropertyVector4:

                return true;
            default: return false;
        }
    }

    static bool UNITY_2021_1_OR_NEWER {
        get {
            #if UNITY_2021_1_OR_NEWER
            return true;
            #else
            return false;
            #endif
        }
    }

    internal static bool IsValidMaterialProperty(TweenType tweenType, [NotNull] object target, int propId) {
        #if UNITY_2021_1_OR_NEWER
        if (UNITY_2021_1_OR_NEWER) {
            switch (tweenType) {
                case TweenType.MaterialColorProperty: return (target as UnityEngine.Material).HasColor(propId);
                case TweenType.MaterialProperty: return (target as UnityEngine.Material).HasFloat(propId);
                case TweenType.MaterialAlphaProperty: return (target as UnityEngine.Material).HasColor(propId);
                case TweenType.MaterialTextureOffset:
                case TweenType.MaterialTextureScale: return (target as UnityEngine.Material).HasTexture(propId);
                case TweenType.MaterialPropertyVector4: return (target as UnityEngine.Material).HasVector(propId);

                default: {
                    UnityEngine.Debug.LogError($"Invalid tween type:{tweenType}, propId:{propId}");
                    return false;
                }
            }
        }
        #endif
        return (target as UnityEngine.Material).HasProperty(propId);
    }

    static int Unpack_ST(long packed) => (int)(packed >> 32);

    internal static TweenAnimation.ValueWrapper GetAnimatedValue(object target, TweenType tweenType, long longParam) {
        switch (tweenType) {
            #if TEXT_MESH_PRO_INSTALLED
            case TweenType.TextMaxVisibleCharacters: return new TweenAnimation.ValueWrapper { single = (target as TMPro.TMP_Text).maxVisibleCharacters };
            case TweenType.TextMaxVisibleCharactersNormalized: { var text = target as TMPro.TMP_Text; return new TweenAnimation.ValueWrapper { single = text.textInfo?.characterCount is int count && count > 0 ? (float)text.maxVisibleCharacters / count : 0f };}
            #endif
            case TweenType.GlobalTimeScale: return UnityEngine.Time.timeScale.ToContainer();
            case TweenType.EulerAngles: return (target as UnityEngine.Transform).eulerAngles.ToContainer();
            case TweenType.LocalEulerAngles: return (target as UnityEngine.Transform).localEulerAngles.ToContainer();
            case TweenType.TweenTimeScale:
            case TweenType.TweenTimeScaleSequence: {
                var tweenTarget = target as ColdData;
                if (longParam != tweenTarget.id || !tweenTarget.data.isAlive) {
                    return 1f.ToContainer();
                }
                return tweenTarget.data.timeScale.ToContainer();
            }

            case TweenType.MaterialColorProperty:   return (target as UnityEngine.Material).GetColor        ((int)longParam).  ToContainer();
            case TweenType.MaterialProperty:        return (target as UnityEngine.Material).GetFloat        ((int)longParam).  ToContainer();
            case TweenType.MaterialAlphaProperty:   return (target as UnityEngine.Material).GetColor        ((int)longParam).a.ToContainer();
            case TweenType.MaterialTextureOffset:   return (target as UnityEngine.Material).GetTextureOffset((int)longParam).  ToContainer();
            case TweenType.MaterialTextureScale:    return (target as UnityEngine.Material).GetTextureScale ((int)longParam).  ToContainer();
            case TweenType.MaterialPropertyVector4: return (target as UnityEngine.Material).GetVector       ((int)longParam).  ToContainer();


            case TweenType.ShakeLocalPosition: return (target as UnityEngine.Transform).localPosition.ToContainer();
            case TweenType.ShakeLocalRotation: return (target as UnityEngine.Transform).localRotation.ToContainer();
            case TweenType.ShakeScale: return (target as UnityEngine.Transform).localScale.ToContainer();

// CODE GENERATOR BEGIN
            case TweenType.LightRange: return (target as UnityEngine.Light).range.ToContainer();
            case TweenType.LightShadowStrength: return (target as UnityEngine.Light).shadowStrength.ToContainer();
            case TweenType.LightIntensity: return (target as UnityEngine.Light).intensity.ToContainer();
            case TweenType.LightColor: return (target as UnityEngine.Light).color.ToContainer();
            case TweenType.CameraOrthographicSize: return (target as UnityEngine.Camera).orthographicSize.ToContainer();
            case TweenType.CameraBackgroundColor: return (target as UnityEngine.Camera).backgroundColor.ToContainer();
            case TweenType.CameraAspect: return (target as UnityEngine.Camera).aspect.ToContainer();
            case TweenType.CameraFarClipPlane: return (target as UnityEngine.Camera).farClipPlane.ToContainer();
            case TweenType.CameraFieldOfView: return (target as UnityEngine.Camera).fieldOfView.ToContainer();
            case TweenType.CameraNearClipPlane: return (target as UnityEngine.Camera).nearClipPlane.ToContainer();
            case TweenType.CameraPixelRect: return (target as UnityEngine.Camera).pixelRect.ToContainer();
            case TweenType.CameraRect: return (target as UnityEngine.Camera).rect.ToContainer();
            case TweenType.Position: return (target as UnityEngine.Transform).position.ToContainer();
            case TweenType.PositionX: return (target as UnityEngine.Transform).position.x.ToContainer();
            case TweenType.PositionY: return (target as UnityEngine.Transform).position.y.ToContainer();
            case TweenType.PositionZ: return (target as UnityEngine.Transform).position.z.ToContainer();
            case TweenType.LocalPosition: return (target as UnityEngine.Transform).localPosition.ToContainer();
            case TweenType.LocalPositionX: return (target as UnityEngine.Transform).localPosition.x.ToContainer();
            case TweenType.LocalPositionY: return (target as UnityEngine.Transform).localPosition.y.ToContainer();
            case TweenType.LocalPositionZ: return (target as UnityEngine.Transform).localPosition.z.ToContainer();
            case TweenType.RotationQuaternion: return (target as UnityEngine.Transform).rotation.ToContainer();
            case TweenType.LocalRotationQuaternion: return (target as UnityEngine.Transform).localRotation.ToContainer();
            case TweenType.Scale: return (target as UnityEngine.Transform).localScale.ToContainer();
            case TweenType.ScaleX: return (target as UnityEngine.Transform).localScale.x.ToContainer();
            case TweenType.ScaleY: return (target as UnityEngine.Transform).localScale.y.ToContainer();
            case TweenType.ScaleZ: return (target as UnityEngine.Transform).localScale.z.ToContainer();
            case TweenType.ColorSpriteRenderer: return (target as UnityEngine.SpriteRenderer).color.ToContainer();
            case TweenType.AlphaSpriteRenderer: return (target as UnityEngine.SpriteRenderer).color.a.ToContainer();
            #if !UNITY_2019_1_OR_NEWER || UNITY_UGUI_INSTALLED
            case TweenType.UISliderValue: return (target as UnityEngine.UI.Slider).value.ToContainer();
            case TweenType.UINormalizedPosition: return (target as UnityEngine.UI.ScrollRect).GetNormalizedPosition().ToContainer();
            case TweenType.UIHorizontalNormalizedPosition: return (target as UnityEngine.UI.ScrollRect).horizontalNormalizedPosition.ToContainer();
            case TweenType.UIVerticalNormalizedPosition: return (target as UnityEngine.UI.ScrollRect).verticalNormalizedPosition.ToContainer();
            case TweenType.UIPivotX: return (target as UnityEngine.RectTransform).pivot[0].ToContainer();
            case TweenType.UIPivotY: return (target as UnityEngine.RectTransform).pivot[1].ToContainer();
            case TweenType.UIPivot: return (target as UnityEngine.RectTransform).pivot.ToContainer();
            case TweenType.UIAnchorMax: return (target as UnityEngine.RectTransform).anchorMax.ToContainer();
            case TweenType.UIAnchorMin: return (target as UnityEngine.RectTransform).anchorMin.ToContainer();
            case TweenType.UIAnchoredPosition3D: return (target as UnityEngine.RectTransform).anchoredPosition3D.ToContainer();
            case TweenType.UIAnchoredPosition3DX: return (target as UnityEngine.RectTransform).anchoredPosition3D[0].ToContainer();
            case TweenType.UIAnchoredPosition3DY: return (target as UnityEngine.RectTransform).anchoredPosition3D[1].ToContainer();
            case TweenType.UIAnchoredPosition3DZ: return (target as UnityEngine.RectTransform).anchoredPosition3D[2].ToContainer();
            case TweenType.UIEffectDistance: return (target as UnityEngine.UI.Shadow).effectDistance.ToContainer();
            case TweenType.UIAlphaShadow: return (target as UnityEngine.UI.Shadow).effectColor.a.ToContainer();
            case TweenType.UIColorShadow: return (target as UnityEngine.UI.Shadow).effectColor.ToContainer();
            case TweenType.UIPreferredSize: return (target as UnityEngine.UI.LayoutElement).GetPreferredSize().ToContainer();
            case TweenType.UIPreferredWidth: return (target as UnityEngine.UI.LayoutElement).preferredWidth.ToContainer();
            case TweenType.UIPreferredHeight: return (target as UnityEngine.UI.LayoutElement).preferredHeight.ToContainer();
            case TweenType.UIFlexibleSize: return (target as UnityEngine.UI.LayoutElement).GetFlexibleSize().ToContainer();
            case TweenType.UIFlexibleWidth: return (target as UnityEngine.UI.LayoutElement).flexibleWidth.ToContainer();
            case TweenType.UIFlexibleHeight: return (target as UnityEngine.UI.LayoutElement).flexibleHeight.ToContainer();
            case TweenType.UIMinSize: return (target as UnityEngine.UI.LayoutElement).GetMinSize().ToContainer();
            case TweenType.UIMinWidth: return (target as UnityEngine.UI.LayoutElement).minWidth.ToContainer();
            case TweenType.UIMinHeight: return (target as UnityEngine.UI.LayoutElement).minHeight.ToContainer();
            case TweenType.UIColorGraphic: return (target as UnityEngine.UI.Graphic).color.ToContainer();
            case TweenType.UIAnchoredPosition: return (target as UnityEngine.RectTransform).anchoredPosition.ToContainer();
            case TweenType.UIAnchoredPositionX: return (target as UnityEngine.RectTransform).anchoredPosition.x.ToContainer();
            case TweenType.UIAnchoredPositionY: return (target as UnityEngine.RectTransform).anchoredPosition.y.ToContainer();
            case TweenType.UISizeDelta: return (target as UnityEngine.RectTransform).sizeDelta.ToContainer();
            case TweenType.UIAlphaCanvasGroup: return (target as UnityEngine.CanvasGroup).alpha.ToContainer();
            case TweenType.UIAlphaGraphic: return (target as UnityEngine.UI.Graphic).color.a.ToContainer();
            case TweenType.UIFillAmount: return (target as UnityEngine.UI.Image).fillAmount.ToContainer();
            case TweenType.UIOffsetMin: return (target as UnityEngine.RectTransform).offsetMin.ToContainer();
            case TweenType.UIOffsetMinX: return (target as UnityEngine.RectTransform).offsetMin[0].ToContainer();
            case TweenType.UIOffsetMinY: return (target as UnityEngine.RectTransform).offsetMin[1].ToContainer();
            case TweenType.UIOffsetMax: return (target as UnityEngine.RectTransform).offsetMax.ToContainer();
            case TweenType.UIOffsetMaxX: return (target as UnityEngine.RectTransform).offsetMax[0].ToContainer();
            case TweenType.UIOffsetMaxY: return (target as UnityEngine.RectTransform).offsetMax[1].ToContainer();
            #endif
            #if !UNITY_2019_1_OR_NEWER || PHYSICS_MODULE_INSTALLED
            case TweenType.RigidbodyMovePosition: return (target as UnityEngine.Rigidbody).position.ToContainer();
            case TweenType.RigidbodyMoveRotationQuaternion: return (target as UnityEngine.Rigidbody).rotation.ToContainer();
            #endif
            #if !UNITY_2019_1_OR_NEWER || PHYSICS2D_MODULE_INSTALLED
            case TweenType.RigidbodyMovePosition2D: return (target as UnityEngine.Rigidbody2D).position.ToContainer();
            case TweenType.RigidbodyMoveRotation2D: return (target as UnityEngine.Rigidbody2D).rotation.ToContainer();
            #endif
            case TweenType.MaterialColor: return (target as UnityEngine.Material).color.ToContainer();
            case TweenType.MaterialAlpha: return (target as UnityEngine.Material).color.a.ToContainer();
            case TweenType.MaterialMainTextureOffset: return (target as UnityEngine.Material).mainTextureOffset.ToContainer();
            case TweenType.MaterialMainTextureScale: return (target as UnityEngine.Material).mainTextureScale.ToContainer();
            #if !UNITY_2019_1_OR_NEWER || AUDIO_MODULE_INSTALLED
            case TweenType.AudioVolume: return (target as UnityEngine.AudioSource).volume.ToContainer();
            case TweenType.AudioPitch: return (target as UnityEngine.AudioSource).pitch.ToContainer();
            case TweenType.AudioPanStereo: return (target as UnityEngine.AudioSource).panStereo.ToContainer();
            #endif
            #if UI_ELEMENTS_MODULE_INSTALLED
            case TweenType.VisualElementLayout: return (target as UnityEngine.UIElements.VisualElement).GetResolvedStyleRect().ToContainer();
            case TweenType.VisualElementPosition: return (target as UnityEngine.UIElements.ITransform).position.ToContainer();
            case TweenType.VisualElementRotationQuaternion: return (target as UnityEngine.UIElements.ITransform).rotation.ToContainer();
            case TweenType.VisualElementScale: return (target as UnityEngine.UIElements.ITransform).scale.ToContainer();
            case TweenType.VisualElementSize: return (target as UnityEngine.UIElements.VisualElement).layout.size.ToContainer();
            case TweenType.VisualElementTopLeft: return (target as UnityEngine.UIElements.VisualElement).GetTopLeft().ToContainer();
            case TweenType.VisualElementColor: return (target as UnityEngine.UIElements.VisualElement).style.color.value.ToContainer();
            case TweenType.VisualElementBackgroundColor: return (target as UnityEngine.UIElements.VisualElement).style.backgroundColor.value.ToContainer();
            case TweenType.VisualElementOpacity: return (target as UnityEngine.UIElements.VisualElement).style.opacity.value.ToContainer();
            #endif
            #if TEXT_MESH_PRO_INSTALLED
            case TweenType.TextFontSize: return (target as TMPro.TMP_Text).fontSize.ToContainer();
            #endif
// CODE GENERATOR END

            default: throw new Exception(tweenType.ToString());
        }
    }

    internal static bool SetAnimatedValue(ref TweenData rt, ref UnmanagedTweenData d) {
        TweenType tweenType = d.tweenType;
        TweenAnimation.ValueWrapper startValue = d.startValue;
        float t = d.easedInterpolationFactor;

        // The order putting delta on stack matters because it touches TweenData after touching all fields of UnmanagedTweenData
        var delta = rt.endValueOrDiff;
        var target = rt.target;
        if (TweenData.IsDestroyedUnityObject(target)) {
            rt.EmergencyStop(true, ref d);
            return false;
        }

        float FloatVal() => startValue.single + delta.single * t;
        UnityEngine.Color ColorVal() => new UnityEngine.Color(
            startValue.x + delta.x * t,
            startValue.y + delta.y * t,
            startValue.z + delta.z * t,
            startValue.w + delta.w * t);
        UnityEngine.Vector2 Vector2Val() => new UnityEngine.Vector2(
            startValue.x + delta.x * t,
            startValue.y + delta.y * t);
        UnityEngine.Vector3 Vector3Val() => new UnityEngine.Vector3(
            startValue.x + delta.x * t,
            startValue.y + delta.y * t,
            startValue.z + delta.z * t);
        UnityEngine.Vector4 Vector4Val() => new UnityEngine.Vector4(
            startValue.x + delta.x * t,
            startValue.y + delta.y * t,
            startValue.z + delta.z * t,
            startValue.w + delta.w * t);
        UnityEngine.Rect RectVal() => new UnityEngine.Rect(
            startValue.x + delta.x * t,
            startValue.y + delta.y * t,
            startValue.z + delta.z * t,
            startValue.w + delta.w * t);
        UnityEngine.Quaternion QuaternionVal() => UnityEngine.Quaternion.SlerpUnclamped(startValue.quaternion, delta.quaternion, t);
        // rt.print($"SetAnimatedValue start:{d.startValue.vector3}, diff:{rt.endValueOrDiff.vector3}, t:{t}, cur:{Vector3Val()}");

        switch (tweenType) {
            #pragma warning disable CS0618 // Type or member is obsolete
            case TweenType.TweenAwaiter: Tween.TweenAwaiter.UpdateTweenAwaiter(ref rt, ref d); break;
            #pragma warning restore CS0618 // Type or member is obsolete
            case TweenType.NestedSequence:
            case TweenType.MainSequence:
            case TweenType.Delay:
                break;

            #if TEXT_MESH_PRO_INSTALLED
            case TweenType.TextMaxVisibleCharacters: (target as TMPro.TMP_Text).maxVisibleCharacters = Mathf.RoundToInt(FloatVal()); break;
            case TweenType.TextMaxVisibleCharactersNormalized: { var text = target as TMPro.TMP_Text; text.maxVisibleCharacters = Mathf.RoundToInt(Mathf.Lerp(0, text.textInfo?.characterCount ?? 0, FloatVal())); break; }
            #endif
            case TweenType.GlobalTimeScale: UnityEngine.Time.timeScale = FloatVal(); break;
            case TweenType.EulerAngles: (target as UnityEngine.Transform).eulerAngles = Vector3Val(); break;
            case TweenType.LocalEulerAngles: (target as UnityEngine.Transform).localEulerAngles = Vector3Val(); break;
            case TweenType.TweenTimeScale:
            case TweenType.TweenTimeScaleSequence: {
                var tweenTarget = target as ColdData;
                if (rt.cold.longParam != tweenTarget.id || !tweenTarget.data.isAlive) {
                    rt.EmergencyStop(false, ref d);
                    return false;
                }
                tweenTarget.data.timeScale = FloatVal();
                break;
            }

            case TweenType.ShakeLocalRotation: (target as UnityEngine.Transform).localRotation = startValue.quaternion * UnityEngine.Quaternion.Euler(Tween.getShakeVal(ref rt, ref d)); break;
            case TweenType.ShakeScale:         (target as UnityEngine.Transform).localScale    = startValue.vector3 +                                 Tween.getShakeVal(ref rt, ref d) ; break;
            case TweenType.ShakeLocalPosition: (target as UnityEngine.Transform).localPosition = startValue.vector3 +                                 Tween.getShakeVal(ref rt, ref d) ; break;

            // CODE GENERATOR BEGIN
            case TweenType.LightRange: (target as UnityEngine.Light).range = FloatVal(); break;
            case TweenType.LightShadowStrength: (target as UnityEngine.Light).shadowStrength = FloatVal(); break;
            case TweenType.LightIntensity: (target as UnityEngine.Light).intensity = FloatVal(); break;
            case TweenType.LightColor: (target as UnityEngine.Light).color = ColorVal(); break;
            case TweenType.CameraOrthographicSize: (target as UnityEngine.Camera).orthographicSize = FloatVal(); break;
            case TweenType.CameraBackgroundColor: (target as UnityEngine.Camera).backgroundColor = ColorVal(); break;
            case TweenType.CameraAspect: (target as UnityEngine.Camera).aspect = FloatVal(); break;
            case TweenType.CameraFarClipPlane: (target as UnityEngine.Camera).farClipPlane = FloatVal(); break;
            case TweenType.CameraFieldOfView: (target as UnityEngine.Camera).fieldOfView = FloatVal(); break;
            case TweenType.CameraNearClipPlane: (target as UnityEngine.Camera).nearClipPlane = FloatVal(); break;
            case TweenType.CameraPixelRect: (target as UnityEngine.Camera).pixelRect = RectVal(); break;
            case TweenType.CameraRect: (target as UnityEngine.Camera).rect = RectVal(); break;
            case TweenType.Position: (target as UnityEngine.Transform).position = Vector3Val(); break;
            case TweenType.PositionX: {
                var _target = target as UnityEngine.Transform;
                var val = FloatVal();
                _target.position = _target.position.WithComponent(0, val);
                break;
            }
            case TweenType.PositionY: {
                var _target = target as UnityEngine.Transform;
                var val = FloatVal();
                _target.position = _target.position.WithComponent(1, val);
                break;
            }
            case TweenType.PositionZ: {
                var _target = target as UnityEngine.Transform;
                var val = FloatVal();
                _target.position = _target.position.WithComponent(2, val);
                break;
            }
            case TweenType.LocalPosition: (target as UnityEngine.Transform).localPosition = Vector3Val(); break;
            case TweenType.LocalPositionX: {
                var _target = target as UnityEngine.Transform;
                var val = FloatVal();
                _target.localPosition = _target.localPosition.WithComponent(0, val);
                break;
            }
            case TweenType.LocalPositionY: {
                var _target = target as UnityEngine.Transform;
                var val = FloatVal();
                _target.localPosition = _target.localPosition.WithComponent(1, val);
                break;
            }
            case TweenType.LocalPositionZ: {
                var _target = target as UnityEngine.Transform;
                var val = FloatVal();
                _target.localPosition = _target.localPosition.WithComponent(2, val);
                break;
            }
            case TweenType.RotationQuaternion: (target as UnityEngine.Transform).rotation = QuaternionVal(); break;
            case TweenType.LocalRotationQuaternion: (target as UnityEngine.Transform).localRotation = QuaternionVal(); break;
            case TweenType.Scale: (target as UnityEngine.Transform).localScale = Vector3Val(); break;
            case TweenType.ScaleX: {
                var _target = target as UnityEngine.Transform;
                var val = FloatVal();
                _target.localScale = _target.localScale.WithComponent(0, val);
                break;
            }
            case TweenType.ScaleY: {
                var _target = target as UnityEngine.Transform;
                var val = FloatVal();
                _target.localScale = _target.localScale.WithComponent(1, val);
                break;
            }
            case TweenType.ScaleZ: {
                var _target = target as UnityEngine.Transform;
                var val = FloatVal();
                _target.localScale = _target.localScale.WithComponent(2, val);
                break;
            }
            case TweenType.ColorSpriteRenderer: (target as UnityEngine.SpriteRenderer).color = ColorVal(); break;
            case TweenType.AlphaSpriteRenderer: {
                var _target = target as UnityEngine.SpriteRenderer;
                var val = FloatVal();
                _target.color = _target.color.WithAlpha(val);
                break;
            }
            #if !UNITY_2019_1_OR_NEWER || UNITY_UGUI_INSTALLED
            case TweenType.UISliderValue: (target as UnityEngine.UI.Slider).value = FloatVal(); break;
            case TweenType.UINormalizedPosition: {
                var _target = target as UnityEngine.UI.ScrollRect;
                var val = Vector2Val();
                _target.SetNormalizedPosition(val);
                break;
            }
            case TweenType.UIHorizontalNormalizedPosition: (target as UnityEngine.UI.ScrollRect).horizontalNormalizedPosition = FloatVal(); break;
            case TweenType.UIVerticalNormalizedPosition: (target as UnityEngine.UI.ScrollRect).verticalNormalizedPosition = FloatVal(); break;
            case TweenType.UIPivotX: {
                var _target = target as UnityEngine.RectTransform;
                var val = FloatVal();
                _target.pivot = _target.pivot.WithComponent(0, val);
                break;
            }
            case TweenType.UIPivotY: {
                var _target = target as UnityEngine.RectTransform;
                var val = FloatVal();
                _target.pivot = _target.pivot.WithComponent(1, val);
                break;
            }
            case TweenType.UIPivot: (target as UnityEngine.RectTransform).pivot = Vector2Val(); break;
            case TweenType.UIAnchorMax: (target as UnityEngine.RectTransform).anchorMax = Vector2Val(); break;
            case TweenType.UIAnchorMin: (target as UnityEngine.RectTransform).anchorMin = Vector2Val(); break;
            case TweenType.UIAnchoredPosition3D: (target as UnityEngine.RectTransform).anchoredPosition3D = Vector3Val(); break;
            case TweenType.UIAnchoredPosition3DX: {
                var _target = target as UnityEngine.RectTransform;
                var val = FloatVal();
                _target.anchoredPosition3D = _target.anchoredPosition3D.WithComponent(0, val);
                break;
            }
            case TweenType.UIAnchoredPosition3DY: {
                var _target = target as UnityEngine.RectTransform;
                var val = FloatVal();
                _target.anchoredPosition3D = _target.anchoredPosition3D.WithComponent(1, val);
                break;
            }
            case TweenType.UIAnchoredPosition3DZ: {
                var _target = target as UnityEngine.RectTransform;
                var val = FloatVal();
                _target.anchoredPosition3D = _target.anchoredPosition3D.WithComponent(2, val);
                break;
            }
            case TweenType.UIEffectDistance: (target as UnityEngine.UI.Shadow).effectDistance = Vector2Val(); break;
            case TweenType.UIAlphaShadow: {
                var _target = target as UnityEngine.UI.Shadow;
                var val = FloatVal();
                _target.effectColor = _target.effectColor.WithAlpha(val);
                break;
            }
            case TweenType.UIColorShadow: (target as UnityEngine.UI.Shadow).effectColor = ColorVal(); break;
            case TweenType.UIPreferredSize: {
                var _target = target as UnityEngine.UI.LayoutElement;
                var val = Vector2Val();
                _target.SetPreferredSize(val);
                break;
            }
            case TweenType.UIPreferredWidth: (target as UnityEngine.UI.LayoutElement).preferredWidth = FloatVal(); break;
            case TweenType.UIPreferredHeight: (target as UnityEngine.UI.LayoutElement).preferredHeight = FloatVal(); break;
            case TweenType.UIFlexibleSize: {
                var _target = target as UnityEngine.UI.LayoutElement;
                var val = Vector2Val();
                _target.SetFlexibleSize(val);
                break;
            }
            case TweenType.UIFlexibleWidth: (target as UnityEngine.UI.LayoutElement).flexibleWidth = FloatVal(); break;
            case TweenType.UIFlexibleHeight: (target as UnityEngine.UI.LayoutElement).flexibleHeight = FloatVal(); break;
            case TweenType.UIMinSize: {
                var _target = target as UnityEngine.UI.LayoutElement;
                var val = Vector2Val();
                _target.SetMinSize(val);
                break;
            }
            case TweenType.UIMinWidth: (target as UnityEngine.UI.LayoutElement).minWidth = FloatVal(); break;
            case TweenType.UIMinHeight: (target as UnityEngine.UI.LayoutElement).minHeight = FloatVal(); break;
            case TweenType.UIColorGraphic: (target as UnityEngine.UI.Graphic).color = ColorVal(); break;
            case TweenType.UIAnchoredPosition: (target as UnityEngine.RectTransform).anchoredPosition = Vector2Val(); break;
            case TweenType.UIAnchoredPositionX: {
                var _target = target as UnityEngine.RectTransform;
                var val = FloatVal();
                _target.anchoredPosition = _target.anchoredPosition.WithComponent(0, val);
                break;
            }
            case TweenType.UIAnchoredPositionY: {
                var _target = target as UnityEngine.RectTransform;
                var val = FloatVal();
                _target.anchoredPosition = _target.anchoredPosition.WithComponent(1, val);
                break;
            }
            case TweenType.UISizeDelta: (target as UnityEngine.RectTransform).sizeDelta = Vector2Val(); break;
            case TweenType.UIAlphaCanvasGroup: (target as UnityEngine.CanvasGroup).alpha = FloatVal(); break;
            case TweenType.UIAlphaGraphic: {
                var _target = target as UnityEngine.UI.Graphic;
                var val = FloatVal();
                _target.color = _target.color.WithAlpha(val);
                break;
            }
            case TweenType.UIFillAmount: (target as UnityEngine.UI.Image).fillAmount = FloatVal(); break;
            case TweenType.UIOffsetMin: (target as UnityEngine.RectTransform).offsetMin = Vector2Val(); break;
            case TweenType.UIOffsetMinX: {
                var _target = target as UnityEngine.RectTransform;
                var val = FloatVal();
                _target.offsetMin = _target.offsetMin.WithComponent(0, val);
                break;
            }
            case TweenType.UIOffsetMinY: {
                var _target = target as UnityEngine.RectTransform;
                var val = FloatVal();
                _target.offsetMin = _target.offsetMin.WithComponent(1, val);
                break;
            }
            case TweenType.UIOffsetMax: (target as UnityEngine.RectTransform).offsetMax = Vector2Val(); break;
            case TweenType.UIOffsetMaxX: {
                var _target = target as UnityEngine.RectTransform;
                var val = FloatVal();
                _target.offsetMax = _target.offsetMax.WithComponent(0, val);
                break;
            }
            case TweenType.UIOffsetMaxY: {
                var _target = target as UnityEngine.RectTransform;
                var val = FloatVal();
                _target.offsetMax = _target.offsetMax.WithComponent(1, val);
                break;
            }
            #endif
            #if !UNITY_2019_1_OR_NEWER || PHYSICS_MODULE_INSTALLED
            case TweenType.RigidbodyMovePosition: {
                var _target = target as UnityEngine.Rigidbody;
                var val = Vector3Val();
                _target.MovePosition(val);
                break;
            }
            case TweenType.RigidbodyMoveRotationQuaternion: {
                var _target = target as UnityEngine.Rigidbody;
                var val = QuaternionVal();
                _target.MoveRotation(val);
                break;
            }
            #endif
            #if !UNITY_2019_1_OR_NEWER || PHYSICS2D_MODULE_INSTALLED
            case TweenType.RigidbodyMovePosition2D: {
                var _target = target as UnityEngine.Rigidbody2D;
                var val = Vector2Val();
                _target.MovePosition(val);
                break;
            }
            case TweenType.RigidbodyMoveRotation2D: {
                var _target = target as UnityEngine.Rigidbody2D;
                var val = FloatVal();
                _target.MoveRotation(val);
                break;
            }
            #endif
            #if !UNITY_2019_1_OR_NEWER || AUDIO_MODULE_INSTALLED
            case TweenType.AudioVolume: (target as UnityEngine.AudioSource).volume = FloatVal(); break;
            case TweenType.AudioPitch: (target as UnityEngine.AudioSource).pitch = FloatVal(); break;
            case TweenType.AudioPanStereo: (target as UnityEngine.AudioSource).panStereo = FloatVal(); break;
            #endif
            #if UI_ELEMENTS_MODULE_INSTALLED
            case TweenType.VisualElementLayout: {
                var _target = target as UnityEngine.UIElements.VisualElement;
                var val = RectVal();
                _target.SetStyleRect(val);
                break;
            }
            case TweenType.VisualElementPosition: (target as UnityEngine.UIElements.ITransform).position = Vector3Val(); break;
            case TweenType.VisualElementRotationQuaternion: (target as UnityEngine.UIElements.ITransform).rotation = QuaternionVal(); break;
            case TweenType.VisualElementScale: (target as UnityEngine.UIElements.ITransform).scale = Vector3Val(); break;
            case TweenType.VisualElementSize: {
                var _target = target as UnityEngine.UIElements.VisualElement;
                var val = Vector2Val();
                _target.style.width = val.x; _target.style.height = val.y;
                break;
            }
            case TweenType.VisualElementTopLeft: {
                var _target = target as UnityEngine.UIElements.VisualElement;
                var val = Vector2Val();
                _target.SetTopLeft(val);
                break;
            }
            case TweenType.VisualElementColor: {
                var _target = target as UnityEngine.UIElements.VisualElement;
                var val = ColorVal();
                _target.style.color = val;
                break;
            }
            case TweenType.VisualElementBackgroundColor: {
                var _target = target as UnityEngine.UIElements.VisualElement;
                var val = ColorVal();
                _target.style.backgroundColor = val;
                break;
            }
            case TweenType.VisualElementOpacity: {
                var _target = target as UnityEngine.UIElements.VisualElement;
                var val = FloatVal();
                _target.style.opacity = val;
                break;
            }
            #endif
            #if TEXT_MESH_PRO_INSTALLED
            case TweenType.TextFontSize: (target as TMPro.TMP_Text).fontSize = FloatVal(); break;
            #endif
            default: {
                if (IsMaterialAnimation(tweenType)) {
                    SetMaterialValue(tweenType, target, rt.cold.longParam, Vector4Val().ToContainer());
                    break;
                }
                rt.cold.onValueChange(ref rt, ref d);
                break;
            }
        }
        return true;
    }

    internal static (PropType, Type) TweenTypeToTweenData(TweenType tweenType) {
        switch (tweenType) {
            case TweenType.Disabled: return (PropType.Float, null);
            case TweenType.LightRange: return (PropType.Float, typeof(UnityEngine.Light));
            case TweenType.LightShadowStrength: return (PropType.Float, typeof(UnityEngine.Light));
            case TweenType.LightIntensity: return (PropType.Float, typeof(UnityEngine.Light));
            case TweenType.LightColor: return (PropType.Color, typeof(UnityEngine.Light));
            case TweenType.CameraOrthographicSize: return (PropType.Float, typeof(UnityEngine.Camera));
            case TweenType.CameraBackgroundColor: return (PropType.Color, typeof(UnityEngine.Camera));
            case TweenType.CameraAspect: return (PropType.Float, typeof(UnityEngine.Camera));
            case TweenType.CameraFarClipPlane: return (PropType.Float, typeof(UnityEngine.Camera));
            case TweenType.CameraFieldOfView: return (PropType.Float, typeof(UnityEngine.Camera));
            case TweenType.CameraNearClipPlane: return (PropType.Float, typeof(UnityEngine.Camera));
            case TweenType.CameraPixelRect: return (PropType.Rect, typeof(UnityEngine.Camera));
            case TweenType.CameraRect: return (PropType.Rect, typeof(UnityEngine.Camera));
            case TweenType.LocalRotation: return (PropType.Vector3, typeof(UnityEngine.Transform));
            case TweenType.ScaleUniform: return (PropType.Float, typeof(UnityEngine.Transform));
            case TweenType.Rotation: return (PropType.Vector3, typeof(UnityEngine.Transform));
            case TweenType.Position: return (PropType.Vector3, typeof(UnityEngine.Transform));
            case TweenType.PositionX: return (PropType.Float, typeof(UnityEngine.Transform));
            case TweenType.PositionY: return (PropType.Float, typeof(UnityEngine.Transform));
            case TweenType.PositionZ: return (PropType.Float, typeof(UnityEngine.Transform));
            case TweenType.LocalPosition: return (PropType.Vector3, typeof(UnityEngine.Transform));
            case TweenType.LocalPositionX: return (PropType.Float, typeof(UnityEngine.Transform));
            case TweenType.LocalPositionY: return (PropType.Float, typeof(UnityEngine.Transform));
            case TweenType.LocalPositionZ: return (PropType.Float, typeof(UnityEngine.Transform));
            case TweenType.RotationQuaternion: return (PropType.Quaternion, typeof(UnityEngine.Transform));
            case TweenType.LocalRotationQuaternion: return (PropType.Quaternion, typeof(UnityEngine.Transform));
            case TweenType.Scale: return (PropType.Vector3, typeof(UnityEngine.Transform));
            case TweenType.ScaleX: return (PropType.Float, typeof(UnityEngine.Transform));
            case TweenType.ScaleY: return (PropType.Float, typeof(UnityEngine.Transform));
            case TweenType.ScaleZ: return (PropType.Float, typeof(UnityEngine.Transform));
            case TweenType.ColorSpriteRenderer: return (PropType.Color, typeof(UnityEngine.SpriteRenderer));
            case TweenType.AlphaSpriteRenderer: return (PropType.Float, typeof(UnityEngine.SpriteRenderer));
            case TweenType.TweenTimeScale: return (PropType.Float, typeof(PrimeTween.Tween));
            case TweenType.TweenTimeScaleSequence: return (PropType.Float, typeof(PrimeTween.Sequence));
            #if !UNITY_2019_1_OR_NEWER || UNITY_UGUI_INSTALLED
            case TweenType.UISliderValue: return (PropType.Float, typeof(UnityEngine.UI.Slider));
            case TweenType.UINormalizedPosition: return (PropType.Vector2, typeof(UnityEngine.UI.ScrollRect));
            case TweenType.UIHorizontalNormalizedPosition: return (PropType.Float, typeof(UnityEngine.UI.ScrollRect));
            case TweenType.UIVerticalNormalizedPosition: return (PropType.Float, typeof(UnityEngine.UI.ScrollRect));
            case TweenType.UIPivotX: return (PropType.Float, typeof(UnityEngine.RectTransform));
            case TweenType.UIPivotY: return (PropType.Float, typeof(UnityEngine.RectTransform));
            case TweenType.UIPivot: return (PropType.Vector2, typeof(UnityEngine.RectTransform));
            case TweenType.UIAnchorMax: return (PropType.Vector2, typeof(UnityEngine.RectTransform));
            case TweenType.UIAnchorMin: return (PropType.Vector2, typeof(UnityEngine.RectTransform));
            case TweenType.UIAnchoredPosition3D: return (PropType.Vector3, typeof(UnityEngine.RectTransform));
            case TweenType.UIAnchoredPosition3DX: return (PropType.Float, typeof(UnityEngine.RectTransform));
            case TweenType.UIAnchoredPosition3DY: return (PropType.Float, typeof(UnityEngine.RectTransform));
            case TweenType.UIAnchoredPosition3DZ: return (PropType.Float, typeof(UnityEngine.RectTransform));
            case TweenType.UIEffectDistance: return (PropType.Vector2, typeof(UnityEngine.UI.Shadow));
            case TweenType.UIAlphaShadow: return (PropType.Float, typeof(UnityEngine.UI.Shadow));
            case TweenType.UIColorShadow: return (PropType.Color, typeof(UnityEngine.UI.Shadow));
            case TweenType.UIPreferredSize: return (PropType.Vector2, typeof(UnityEngine.UI.LayoutElement));
            case TweenType.UIPreferredWidth: return (PropType.Float, typeof(UnityEngine.UI.LayoutElement));
            case TweenType.UIPreferredHeight: return (PropType.Float, typeof(UnityEngine.UI.LayoutElement));
            case TweenType.UIFlexibleSize: return (PropType.Vector2, typeof(UnityEngine.UI.LayoutElement));
            case TweenType.UIFlexibleWidth: return (PropType.Float, typeof(UnityEngine.UI.LayoutElement));
            case TweenType.UIFlexibleHeight: return (PropType.Float, typeof(UnityEngine.UI.LayoutElement));
            case TweenType.UIMinSize: return (PropType.Vector2, typeof(UnityEngine.UI.LayoutElement));
            case TweenType.UIMinWidth: return (PropType.Float, typeof(UnityEngine.UI.LayoutElement));
            case TweenType.UIMinHeight: return (PropType.Float, typeof(UnityEngine.UI.LayoutElement));
            case TweenType.UIColorGraphic: return (PropType.Color, typeof(UnityEngine.UI.Graphic));
            case TweenType.UIAnchoredPosition: return (PropType.Vector2, typeof(UnityEngine.RectTransform));
            case TweenType.UIAnchoredPositionX: return (PropType.Float, typeof(UnityEngine.RectTransform));
            case TweenType.UIAnchoredPositionY: return (PropType.Float, typeof(UnityEngine.RectTransform));
            case TweenType.UISizeDelta: return (PropType.Vector2, typeof(UnityEngine.RectTransform));
            case TweenType.UIAlphaCanvasGroup: return (PropType.Float, typeof(UnityEngine.CanvasGroup));
            case TweenType.UIAlphaGraphic: return (PropType.Float, typeof(UnityEngine.UI.Graphic));
            case TweenType.UIFillAmount: return (PropType.Float, typeof(UnityEngine.UI.Image));
            case TweenType.UIOffsetMin: return (PropType.Vector2, typeof(UnityEngine.RectTransform));
            case TweenType.UIOffsetMinX: return (PropType.Float, typeof(UnityEngine.RectTransform));
            case TweenType.UIOffsetMinY: return (PropType.Float, typeof(UnityEngine.RectTransform));
            case TweenType.UIOffsetMax: return (PropType.Vector2, typeof(UnityEngine.RectTransform));
            case TweenType.UIOffsetMaxX: return (PropType.Float, typeof(UnityEngine.RectTransform));
            case TweenType.UIOffsetMaxY: return (PropType.Float, typeof(UnityEngine.RectTransform));
            #endif
            #if !UNITY_2019_1_OR_NEWER || PHYSICS_MODULE_INSTALLED
            case TweenType.RigidbodyMoveRotation: return (PropType.Vector3, typeof(UnityEngine.Rigidbody));
            case TweenType.RigidbodyMovePosition: return (PropType.Vector3, typeof(UnityEngine.Rigidbody));
            case TweenType.RigidbodyMoveRotationQuaternion: return (PropType.Quaternion, typeof(UnityEngine.Rigidbody));
            #endif
            #if !UNITY_2019_1_OR_NEWER || PHYSICS2D_MODULE_INSTALLED
            case TweenType.RigidbodyMovePosition2D: return (PropType.Vector2, typeof(UnityEngine.Rigidbody2D));
            case TweenType.RigidbodyMoveRotation2D: return (PropType.Float, typeof(UnityEngine.Rigidbody2D));
            #endif
            case TweenType.MaterialColor: return (PropType.Color, typeof(UnityEngine.Material));
            case TweenType.MaterialAlpha: return (PropType.Float, typeof(UnityEngine.Material));
            case TweenType.MaterialMainTextureOffset: return (PropType.Vector2, typeof(UnityEngine.Material));
            case TweenType.MaterialMainTextureScale: return (PropType.Vector2, typeof(UnityEngine.Material));
            #if !UNITY_2019_1_OR_NEWER || AUDIO_MODULE_INSTALLED
            case TweenType.AudioVolume: return (PropType.Float, typeof(UnityEngine.AudioSource));
            case TweenType.AudioPitch: return (PropType.Float, typeof(UnityEngine.AudioSource));
            case TweenType.AudioPanStereo: return (PropType.Float, typeof(UnityEngine.AudioSource));
            #endif
            #if UI_ELEMENTS_MODULE_INSTALLED
            case TweenType.VisualElementLayout: return (PropType.Rect, typeof(UnityEngine.UIElements.VisualElement));
            case TweenType.VisualElementPosition: return (PropType.Vector3, typeof(UnityEngine.UIElements.ITransform));
            case TweenType.VisualElementRotationQuaternion: return (PropType.Quaternion, typeof(UnityEngine.UIElements.ITransform));
            case TweenType.VisualElementScale: return (PropType.Vector3, typeof(UnityEngine.UIElements.ITransform));
            case TweenType.VisualElementSize: return (PropType.Vector2, typeof(UnityEngine.UIElements.VisualElement));
            case TweenType.VisualElementTopLeft: return (PropType.Vector2, typeof(UnityEngine.UIElements.VisualElement));
            case TweenType.VisualElementColor: return (PropType.Color, typeof(UnityEngine.UIElements.VisualElement));
            case TweenType.VisualElementBackgroundColor: return (PropType.Color, typeof(UnityEngine.UIElements.VisualElement));
            case TweenType.VisualElementOpacity: return (PropType.Float, typeof(UnityEngine.UIElements.VisualElement));
            #endif
            #if TEXT_MESH_PRO_INSTALLED
            case TweenType.TextMaxVisibleCharacters: return (PropType.Int, typeof(TMPro.TMP_Text));
            case TweenType.TextFontSize: return (PropType.Float, typeof(TMPro.TMP_Text));
            #endif
            case TweenType.Delay: return (PropType.Float, null);
            case TweenType.Callback: return (PropType.Float, null);
            case TweenType.ShakeLocalPosition: return (PropType.Vector3, typeof(UnityEngine.Transform));
            case TweenType.ShakeLocalRotation: return (PropType.Quaternion, typeof(UnityEngine.Transform));
            case TweenType.ShakeScale: return (PropType.Vector3, typeof(UnityEngine.Transform));
            case TweenType.ShakeCustom: return (PropType.Vector3, null);
            case TweenType.ShakeCamera: return (PropType.Float, typeof(UnityEngine.Camera));
            case TweenType.CustomFloat: return (PropType.Float, null);
            case TweenType.CustomColor: return (PropType.Color, null);
            case TweenType.CustomVector2: return (PropType.Vector2, null);
            case TweenType.CustomVector3: return (PropType.Vector3, null);
            case TweenType.CustomVector4: return (PropType.Vector4, null);
            case TweenType.CustomQuaternion: return (PropType.Quaternion, null);
            case TweenType.CustomRect: return (PropType.Rect, null);
            #if PRIME_TWEEN_EXPERIMENTAL
            case TweenType.CustomDouble: return (PropType.Double, null);
            #endif
            case TweenType.MaterialColorProperty: return (PropType.Color, typeof(UnityEngine.Material));
            case TweenType.MaterialProperty: return (PropType.Float, typeof(UnityEngine.Material));
            case TweenType.MaterialAlphaProperty: return (PropType.Float, typeof(UnityEngine.Material));
            case TweenType.MaterialTextureOffset: return (PropType.Vector2, typeof(UnityEngine.Material));
            case TweenType.MaterialTextureScale: return (PropType.Vector2, typeof(UnityEngine.Material));
            case TweenType.MaterialPropertyVector4: return (PropType.Vector4, typeof(UnityEngine.Material));
            case TweenType.EulerAngles: return (PropType.Vector3, typeof(UnityEngine.Transform));
            case TweenType.LocalEulerAngles: return (PropType.Vector3, typeof(UnityEngine.Transform));
            case TweenType.GlobalTimeScale: return (PropType.Float, null);
            case TweenType.MainSequence: return (PropType.Float, null);
            case TweenType.NestedSequence: return (PropType.Float, null);
            case TweenType.TweenAwaiter: return (PropType.Float, null);
            #if TEXT_MESH_PRO_INSTALLED
            case TweenType.TextMaxVisibleCharactersNormalized: return (PropType.Float, typeof(TMPro.TMP_Text));
            #endif
            default:
                throw new Exception($"Unsupported tween type: {tweenType}. Please install necessary packages (TextMeshPro, UGUI, etc.) or use a newer version of Unity.");
        }
    }
}
}
