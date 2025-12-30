using CW.Common;
using Lean.Touch;
using UnityEngine;

public enum CameraState
{
    Touch = 1,
    Target = 2,
}
public class CameraController
{
    private Camera camera;
    private CameraState state;
    private Vector3 pos;
    private float zoom;
    private Vector2 euler;
    //Touch
    private LeanFingerFilter filter;
    private Vector2 posDelta;
    private float zoomDelta;
    private Vector2 eulerDelta;
    //Target
    private float targetTime;
    private Vector3 targetPos;
    private float targetZoom;
    private Vector2 targetEuler;

    public Camera Camera => camera;

    public CameraController(Camera camera)
    {
        this.camera = camera;
        state = CameraState.Touch;
        filter = new LeanFingerFilter(true);
        zoom = camera.fieldOfView;
        Driver.Instance.StartUpdate(Update);
    }

    private void StopDelta()
    {
        posDelta = Vector2.zero;
        zoomDelta = 0;
        eulerDelta = Vector2.zero;
    }
    public void SetTouch(bool enable)
    {
        if (enable)
        {
            state |= CameraState.Touch;
        }
        else
        {
            StopDelta();
            state &= ~CameraState.Touch;
        }
    }
    public void SetTRS(Vector3? pos = null, float? zoom = null, Vector2? euler = null)
    {
        StopDelta();
        if (pos.HasValue) this.pos = pos.Value;
        if (zoom.HasValue) this.zoom = zoom.Value;
        if (euler.HasValue) this.euler = euler.Value;
    }
    public void SetTarget(float t, Vector3? pos = null, float? zoom = null, Vector2? euler = null)
    {
        StopDelta();
        state |= CameraState.Target;
        targetTime = t;
        targetPos = pos.HasValue ? pos.Value : this.pos;
        targetZoom = zoom.HasValue ? zoom.Value : this.zoom;
        targetEuler = euler.HasValue ? euler.Value : this.euler;
    }

    private void Update(float t)
    {
        if (state.HasFlag(CameraState.Target))
        {
            targetTime -= t;
            if (targetTime > 0)
            {
                pos = Vector3.Lerp(pos, targetPos, t / targetTime);
                zoom = Mathf.Lerp(zoom, targetZoom, t / targetTime);
                euler = Vector2.Lerp(euler, targetEuler, t / targetTime);
            }
            else
            {
                pos = targetPos;
                zoom = targetZoom;
                euler = targetEuler;
                state &= ~CameraState.Target;
            }
        }
        if (state.HasFlag(CameraState.Touch))
        {
            var fingers = filter.UpdateAndGetFingers();
            if (fingers.Count == 0)
            {
                //移动限制xy(-10,10)
                var pos = camera.transform.position;
                if (pos.x < -10) posDelta.x = (pos.x + 10) * 0.5f;
                else if (pos.x > 10) posDelta.x = (pos.x - 10) * 0.5f;
                if (pos.z < -10) posDelta.y = (pos.z + 10) * 0.5f;
                else if (pos.z > 10) posDelta.y = (pos.z - 10) * 0.5f;
                //缩放限制(30,60)
                if (zoom < 30) zoomDelta = (30 - zoom) * 0.5f;
                else if (zoom > 60) zoomDelta = (60 - zoom) * 0.5f;
            }
            else if (fingers.Count == 1)
            {
                state &= ~CameraState.Target;
                //移动
                var delta = LeanGesture.GetScreenDelta(fingers);
                //超过限制添加阻尼
                var pos = camera.transform.position;
                float damping = 1;
                if (pos.x < -10 || pos.x > 10) damping = 1 / ((Mathf.Abs(pos.x) - 10) + 1);
                if (pos.z < -10 || pos.z > 10) damping = Mathf.Min(damping, 1 / ((Mathf.Abs(pos.z) - 10) + 1));
                posDelta += delta / zoom * 0.1f * damping;
            }
            else
            {
                state &= ~CameraState.Target;
                var angle = Vector2.Angle(fingers[0].ScreenDelta, fingers[1].ScreenDelta);
                if (angle > 90)
                {
                    //缩放
                    var center = LeanGesture.GetScreenCenter(fingers);
                    var lastCenter = LeanGesture.GetLastScreenCenter(fingers);
                    var dis = LeanGesture.GetScreenDistance(fingers, center);
                    var lastDis = LeanGesture.GetLastScreenDistance(fingers, lastCenter);
                    //超过限制添加阻尼
                    var delta = Mathf.Clamp((lastDis - dis) * 0.1f, -10, 10);
                    if (zoom < 30)
                    {
                        float damping = 1 / ((30 - zoom) * 10 + 1);
                        zoomDelta = Mathf.Clamp(zoomDelta + delta * damping, -10, 10);
                    }
                    else if (zoom > 60)
                    {
                        float damping = 1 / ((zoom - 60) * 10 + 1);
                        zoomDelta = Mathf.Clamp(zoomDelta + delta * damping, -10, 10);
                    }
                    else
                    {
                        zoomDelta += delta;
                    }
                }
                else
                {
                    //旋转
                    var delta = LeanGesture.GetScreenDelta(fingers);
                    eulerDelta -= delta / zoom;
                }
            }
            //移动
            var f = CwHelper.DampenFactor(10, t);
            pos -= new Vector3(posDelta.x * f, 0, posDelta.y * f);
            posDelta = posDelta * (1 - f);
            //缩放
            zoom += zoomDelta * f;
            zoomDelta = zoomDelta * (1 - f);
            //旋转
            euler.x -= eulerDelta.y * f;
            euler.y += eulerDelta.x * f;
            eulerDelta = eulerDelta * (1 - f);
        }
        camera.transform.position = pos;
        camera.fieldOfView = zoom;
        camera.transform.rotation = Quaternion.Euler(euler);
    }
}