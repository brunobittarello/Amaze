using UnityEngine;

public class CameraController : MonoBehaviour
{
    Camera _camera;

    public void Awake()
    {
        _camera = this.gameObject.GetComponent<Camera>();
    }

    public void Focus(Vector2 size)
    {
        this.transform.localPosition = size * 0.5f;
        _camera.orthographicSize = size.x;
    }
}
