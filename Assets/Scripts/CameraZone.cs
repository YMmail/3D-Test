using UnityEngine;

// ============================================================
// CameraZone — เปลี่ยนความสูงสูงสุดของกล้องตามโซน
// เพดานแต่ละโซนสูงไม่เท่ากัน กล้องต้องอยู่ใต้เพดานเสมอ ไม่งั้นเพดานบังตัวละคร
// ใส่ที่ GameObject ที่มี Box Collider (ติ๊ก Is Trigger) คลุมพื้นที่ของโซน
// 🚨 เช็คจาก "จุดกลางตัวมาริสา" อยู่ในกล่องไหน → กล่องแต่ละโซนห้ามทับกัน
// 💡 สูตรคร่าวๆ: Max Y = (ความสูงเพดาน - 0.1) - 1.8   (กล้องระยะ 7 ม. ก้ม 15°)
// ============================================================
[RequireComponent(typeof(Collider))]
public class CameraZone : MonoBehaviour
{
    // --- 1. SETTINGS ---
    [Tooltip("ความสูงสูงสุดของจุดที่กล้องมอง ในโซนนี้")] public float maxY = 1.1f;

    static Transform player;
    Collider box;

    void Awake() { box = GetComponent<Collider>(); }

    // --- 2. ทุกเฟรม: จุดกลางตัวมาริสาอยู่ในโซนนี้ = ใช้ค่านี้ ---
    void Update()
    {
        if (player == null)
        {
            var pc = FindFirstObjectByType<PlayerController25D>();
            if (pc == null) return;
            player = pc.transform;
        }
        if (!box.bounds.Contains(player.position + Vector3.up * 0.6f)) return;
        var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow25D>() : null;
        if (cam == null) return;
        cam.useBounds = true;
        cam.maxY = maxY;
    }
}
