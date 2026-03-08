using UnityEngine;

/// <summary>
/// 화면에 디버그 정보를 표시합니다.
/// </summary>
public class RotationDebugOverlay : MonoBehaviour
{
    private GUIStyle _style;

    private void Start()
    {
        _style = new GUIStyle
        {
            fontSize = 22,
            normal = { textColor = Color.yellow },
            padding = new RectOffset(10, 10, 10, 10)
        };
    }

    private void OnGUI()
    {
        float y = 10;
        float lineH = 28;

        var player = GameObject.FindWithTag("Player");
        if (player == null) return;

        var rb = player.GetComponent<Rigidbody2D>();
        var anim = player.GetComponent<Animator>();

        // 위치/회전
        GUI.Label(new Rect(10, y, 800, lineH), $"POS: {player.transform.position}", _style);
        y += lineH;
        GUI.Label(new Rect(10, y, 800, lineH), $"P.rot: {player.transform.rotation.eulerAngles}  C.rot: {Camera.main.transform.rotation.eulerAngles}", _style);
        y += lineH;

        // Rigidbody
        if (rb != null)
        {
            GUI.Label(new Rect(10, y, 800, lineH), $"velocity: {rb.linearVelocity}  angVel: {rb.angularVelocity:F2}", _style);
            y += lineH;
        }

        // Animator
        if (anim != null && anim.runtimeAnimatorController != null && anim.parameterCount > 0)
        {
            int dir = anim.GetInteger("Direction");
            bool moving = anim.GetBool("IsMoving");
            string[] dirNames = { "Down", "Left", "Right", "Up" };
            string dirName = (dir >= 0 && dir < 4) ? dirNames[dir] : $"?{dir}";

            var info = anim.GetCurrentAnimatorStateInfo(0);
            GUI.Label(new Rect(10, y, 800, lineH), $"Dir: {dir}({dirName}) Moving: {moving} Hash: {info.shortNameHash}", _style);
            y += lineH;
        }

        // SpriteRenderer 상태
        var sr = player.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            GUI.Label(new Rect(10, y, 800, lineH), $"Sprite: {(sr.sprite != null ? sr.sprite.name : "null")} flipX:{sr.flipX} flipY:{sr.flipY}", _style);
            y += lineH;
        }
    }
}
