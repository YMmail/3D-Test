using UnityEngine;

// ============================================================
// Checkpoint — เดินผ่านแล้ว ตายครั้งหน้าจะเกิดที่นี่
// ใส่ที่ GameObject ที่มี Box Collider ติ๊ก Is Trigger
// จุดเกิด = ตำแหน่ง spawnPoint (ถ้าเว้นว่าง = ตำแหน่งของ checkpoint นี้)
// ============================================================
[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("จุดที่จะให้เกิด (เท้าแตะพื้น)")] public Transform spawnPoint;
    [Tooltip("ตอนเกิดให้หันไปทางไหน (องศาแกน Y, 90 = หันขวา)")] public float facingY = 90f;

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController25D>() == null) return;
        if (PlayerRespawn.Instance == null) return;
        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        PlayerRespawn.Instance.SetCheckpoint(pos, Quaternion.Euler(0f, facingY, 0f));
    }
}
