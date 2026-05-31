using UnityEngine;
using UnityEngine.InputSystem;

public class TPSCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 8f;
    [SerializeField] private float height = 2f;
    [SerializeField] private float rotationSpeed = 0.2f; // 新方式はdelta値が大きいので感度は小さめ
    [SerializeField] private float minPitch = -20f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistance = 30f;

    private float yaw;
    private float pitch;

    private void Start()
    {
        yaw = transform.eulerAngles.y;
        pitch = 20f;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        var mouse = Mouse.current;
        if (mouse == null) return;

        // 右ボタン押下中のみ回転（右クリックドラッグ）
        if (mouse.rightButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            yaw += delta.x * rotationSpeed;
            pitch -= delta.y * rotationSpeed;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        // ホイールでズーム
        float scroll = mouse.scroll.ReadValue().y;
        distance -= scroll * 0.01f; // 新方式のscrollは値が大きいので係数を小さく
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focus = target.position + Vector3.up * height;
        transform.position = focus - rotation * Vector3.forward * distance;
        transform.rotation = rotation;
    }
}