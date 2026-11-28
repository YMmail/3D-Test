using UnityEngine;
using UnityEngine.UI;

// ============================================================
// SignText — ข้อความบนป้ายสอนเล่น (ใส่ที่ตัวป้าย แล้วพิมพ์ข้อความในช่อง Text)
// ตัวหนังสือลอยอยู่หน้าป้าย หันเข้ากล้อง / รองรับภาษาไทย (วรรณยุกต์ไม่ทับ)
// ============================================================
public class SignText : MonoBehaviour
{
    // --- 1. SETTINGS (ปรับได้) ---
    [Tooltip("ข้อความบนป้าย (Enter = ขึ้นบรรทัดใหม่)")] [TextArea(2, 4)] public string text = "ข้อความ";
    [Tooltip("ขนาดตัวหนังสือ")] public int fontSize = 150;
    public Color color = new Color(0.95f, 0.92f, 0.8f);
    [Tooltip("ห่างจากหน้าป้ายมาทางกล้องกี่เมตร")] public float offset = 0.03f;

    // --- 2. สร้างตัวหนังสือตอนเริ่มเกม ---
    void Start()
    {
        var col = GetComponent<Renderer>();
        Bounds b = col != null ? col.bounds : new Bounds(transform.position, new Vector3(1.2f, 0.8f, 0.05f));

        var go = new GameObject("SignCanvas", typeof(Canvas));
        go.transform.SetParent(transform, true);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = (RectTransform)go.transform;
        const float unit = 0.001f; // 1000 หน่วย UI = 1 เมตร
        rt.sizeDelta = new Vector2(b.size.x / unit, b.size.y / unit);
        rt.position = new Vector3(b.center.x, b.center.y, b.min.z - offset); // หน้าป้ายฝั่งกล้อง
        rt.rotation = Quaternion.identity;
        rt.localScale = Vector3.one * unit;
        if (go.transform.lossyScale.x != unit) rt.localScale = new Vector3(unit / transform.lossyScale.x, unit / transform.lossyScale.y, unit / transform.lossyScale.z);

        var label = UIKit.Label(go.transform, text, fontSize, Vector2.zero, rt.sizeDelta * 0.92f);
        label.color = color;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.lineSpacing = 1.05f;
        label.fontStyle = FontStyle.Bold;
    }
}
