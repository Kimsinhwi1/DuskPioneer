using UnityEngine;

/// <summary>
/// 2D 탑다운 카메라 따라가기. 회전 없이 위치만 추적.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0, 0, -10);

    private void LateUpdate()
    {
        if (target == null) return;
        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.identity;
    }
}
