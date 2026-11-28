using UnityEngine;

// ============================================================
// DeathZone — พื้นที่ตาย (น้ำเสีย, หลุม) เข้าไป = ตายทันที
// ใส่ที่ GameObject ที่มี Box Collider ติ๊ก Is Trigger
// ============================================================
[RequireComponent(typeof(Collider))]
public class DeathZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController25D>() != null) PlayerRespawn.Kill();
    }
}
