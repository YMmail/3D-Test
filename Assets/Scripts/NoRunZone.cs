using UnityEngine;

// ============================================================
// NoRunZone — พื้นที่ห้ามวิ่ง (เช่น ทางเดินแคบลื่นๆ ริมน้ำ)
// เข้าไปแล้วกด Shift ก็เดินเท่าเดิม ออกมาแล้ววิ่งได้ตามปกติ
// 💡 ใช้กันผู้เล่นวิ่งกระโดดข้ามช่องที่ตั้งใจให้ต้องดันกล่องไปอุด
// ใส่ที่ GameObject ที่มี Box Collider ติ๊ก Is Trigger
// ============================================================
[RequireComponent(typeof(Collider))]
public class NoRunZone : MonoBehaviour
{
    PlayerController25D player;
    float savedRunSpeed;

    void OnTriggerEnter(Collider other)
    {
        var pc = other.GetComponentInParent<PlayerController25D>();
        if (pc == null || player != null) return;
        player = pc;
        savedRunSpeed = pc.runSpeed;
        pc.runSpeed = pc.walkSpeed; // วิ่ง = เดิน
    }

    void OnTriggerExit(Collider other)
    {
        var pc = other.GetComponentInParent<PlayerController25D>();
        if (pc == null || pc != player) return;
        pc.runSpeed = savedRunSpeed;
        player = null;
    }
}
