using UnityEngine;

public class CameraController
{
    private Camera camera;

    public Camera Camera => camera;

    public CameraController(Camera camera)
    {
        this.camera = camera;
    }

    public void SetPos(Vector3 pos)
    {
        camera.transform.position = Vector3.zero;
    }
    public void SetEuler(Vector3 euler)
    {
        camera.transform.rotation = Quaternion.Euler(euler);
    }
}
