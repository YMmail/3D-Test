using System.Collections;
using UnityEngine;

// ============================================================
// OpeningFall — ฉากเปิดเกม: จอดำ → ค่อยๆ สว่าง → มาริสาร่วงลงมาจากฝาท่อ → ตกถึงพื้น → เริ่มเล่น
// ใส่ที่ GameObject ในฉาก Game แล้วลาก Marisa ใส่ช่อง Player
// 💡 ตายแล้วเกิดใหม่ไม่เล่นซ้ำ (เล่นแค่ตอนเปิดฉาก)
// ============================================================
public class OpeningFall : MonoBehaviour
{
    // --- 1. SETTINGS (ปรับได้) ---
    [Tooltip("🚨 ลากตัว Marisa มาใส่")] public PlayerController25D player;
    [Tooltip("เริ่มร่วงจากความสูงเท่าไหร่ (เมตร) ปากปล่องอยู่ที่ประมาณ 10")] public float startHeight = 9f;
    [Tooltip("จอดำค้างกี่วินาทีก่อนเริ่มร่วง")] public float blackHold = 0.8f;
    [Tooltip("จอค่อยๆ สว่างกี่วินาที")] public float fadeTime = 1.5f;
    [Tooltip("ตกถึงพื้นแล้วนิ่งอยู่กี่วินาที ก่อนบังคับได้")] public float getUpTime = 1.0f;
    [Tooltip("ตอนตกถึงพื้น ตัวยุบลงเท่าไหร่ (0 = ไม่ยุบ)")] [Range(0f, 0.5f)] public float squash = 0.25f;

    // กำลังเล่นฉากเปิด (สคริปต์อื่นเช็คได้)
    public static bool Playing { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay() { Playing = false; }

    // --- 2. เริ่มฉาก ---
    void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController25D>();
        if (player == null) return;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        Playing = true;
        var body = player.GetComponent<Rigidbody>();
        Transform model = player.transform.Find("Model");

        // 2.1 จอดำ + ยกตัวไปไว้ปากปล่อง (ค้างไว้ก่อน)
        var canvas = UIKit.CreateCanvas("OpeningCanvas", 180);
        var black = UIKit.Panel(canvas.transform, Color.black);
        black.raycastTarget = false;
        player.canMove = false;
        Vector3 p = player.transform.position; p.y = startHeight;
        player.transform.position = p;
        body.position = p; body.linearVelocity = Vector3.zero; body.isKinematic = true;
        var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow25D>() : null;
        yield return new WaitForSeconds(blackHold);

        // 2.2 ปล่อยให้ร่วง + จอค่อยๆ สว่าง
        body.isKinematic = false;
        float t0 = Time.time;
        bool landed = false;
        float fadeT = 0f;
        while (!landed || fadeT < fadeTime)
        {
            fadeT += Time.deltaTime;
            black.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(fadeT / fadeTime));
            if (!landed && Time.time - t0 > 0.3f && player.IsGrounded) landed = true;
            if (Time.time - t0 > 6f) landed = true; // กันค้าง
            yield return null;
        }
        Destroy(canvas.gameObject);

        // 2.3 ตกถึงพื้น: ตัวยุบแล้วค่อยๆ ลุก
        if (model != null && squash > 0f)
        {
            Vector3 baseScale = model.localScale;
            for (float t = 0f; t < getUpTime; t += Time.deltaTime)
            {
                float k = t / getUpTime;
                float s = squash * (1f - k) * (1f - k);
                model.localScale = new Vector3(baseScale.x * (1f + s * 0.5f), baseScale.y * (1f - s), baseScale.z * (1f + s * 0.5f));
                yield return null;
            }
            model.localScale = baseScale;
        }
        else yield return new WaitForSeconds(getUpTime);

        player.canMove = true;
        Playing = false;
    }

    void OnDestroy() { Playing = false; }
}
