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

    // 追従対象を切り替える（陣営選択でプレイヤーが決まった時にFactionSelectUIが呼ぶ）。
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null) yaw = target.eulerAngles.y + 180f; // 新しいキャラの背後に回り込む
    }

    // 装備画面のクローズアップ状態（開始時の視点を保存し、閉じたら戻す）。
    private bool closeUp;
    private float savedDistance, savedPitch, savedYaw, savedHeight;
    private const float CloseUpMinDistance = 0.8f; // クローズアップ中だけ通常のminDistanceより寄れる

    // 装備画面用：自キャラ正面の寄りカメラへ切り替える（右ドラッグ回転・ホイールズームは効いたまま）。
    //   closeHeight＝注視点の高さ（通常heightより低くして胸あたりを画面中央に）。
    public void BeginCloseUp(float closeDistance, float closePitch, float closeHeight)
    {
        if (closeUp) return;
        closeUp = true;
        savedDistance = distance;
        savedPitch = pitch;
        savedYaw = yaw;
        savedHeight = height;
        distance = Mathf.Clamp(closeDistance, CloseUpMinDistance, maxDistance);
        pitch = Mathf.Clamp(closePitch, minPitch, maxPitch);
        height = closeHeight;
        if (target != null) yaw = target.eulerAngles.y + 180f; // キャラの正面に回り込む
    }

    // クローズアップを終了し、元の視点に戻す。
    public void EndCloseUp()
    {
        if (!closeUp) return;
        closeUp = false;
        distance = savedDistance;
        pitch = savedPitch;
        yaw = savedYaw;
        height = savedHeight;
    }

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

        // ミニマップ（M画面）中・商人UI中はカメラ操作（回転・ズーム）を受け付けない。追従だけ続ける。
        if (!UIScreens.MinimapOpen && !UIScreens.MerchantOpen)
        {
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
            distance = Mathf.Clamp(distance, closeUp ? CloseUpMinDistance : minDistance, maxDistance);
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focus = target.position + Vector3.up * height;
        transform.position = focus - rotation * Vector3.forward * distance;
        transform.rotation = rotation;
    }
}