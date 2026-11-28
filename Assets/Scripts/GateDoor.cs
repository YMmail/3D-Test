using System.Collections;
using UnityEngine;

// ============================================================
// GateDoor — ตะแกรง/ประตูที่เลื่อนเปิด (CodePanel เรียก Open ตอนใส่รหัสถูก)
// ใส่ที่ object ไหนก็ได้ แล้วลากส่วนที่ขยับ (มี Collider กั้นทาง) ใส่ช่อง movingPart
// ============================================================
public class GateDoor : MonoBehaviour
{
    [Tooltip("🚨 ส่วนที่เลื่อน (ซี่เหล็ก) เว้นว่าง = ตัวนี้เอง")] public Transform movingPart;
    [Tooltip("เลื่อนไปทางไหนกี่เมตร")] public Vector3 openOffset = new Vector3(0f, 2.1f, 0f);
    [Tooltip("ใช้เวลาเปิดกี่วินาที")] public float openTime = 2f;

    public bool IsOpen { get; private set; }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        GameAudio.Play(l => l.gateOpen);
        StartCoroutine(OpenRoutine());
    }

    IEnumerator OpenRoutine()
    {
        var part = movingPart != null ? movingPart : transform;
        Vector3 from = part.localPosition, to = from + openOffset;
        for (float t = 0f; t < openTime; t += Time.deltaTime)
        {
            part.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / openTime));
            yield return null;
        }
        part.localPosition = to;
    }
}
