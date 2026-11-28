using UnityEngine;
using UnityEngine.UI;

// ============================================================
// ThaiToneFix — ยกวรรณยุกต์ขึ้นเมื่ออยู่เหนือสระบน (เช่น "ริ่", "พื่")
// เพราะ Text ของ Unity วางวรรณยุกต์ทับสระ มองไม่เห็นไม้เอก
// 💡 UIKit.Label ใส่ให้อัตโนมัติแล้ว
// ============================================================
[RequireComponent(typeof(Text))]
public class ThaiToneFix : BaseMeshEffect
{
    [Tooltip("ยกขึ้นกี่เท่าของขนาดตัวอักษร")] public float raise = 0.2f;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;
        var text = GetComponent<Text>();
        string s = text.text;
        int quads = vh.currentVertCount / 4;
        if (string.IsNullOrEmpty(s) || quads == 0) return;

        // Unity บางรุ่นไม่สร้างรูปให้ช่องว่าง ต้องนับให้ตรงกัน
        int visible = 0;
        foreach (char ch in s) if (!char.IsWhiteSpace(ch)) visible++;
        bool skipSpaces = quads == visible && visible != s.Length;

        float dy = text.fontSize * raise;
        int q = 0;
        for (int i = 0; i < s.Length && q < quads; i++)
        {
            if (skipSpaces && char.IsWhiteSpace(s[i])) continue;
            if (i > 0 && IsTone(s[i]) && IsUpperVowel(s[i - 1]))
            {
                for (int k = 0; k < 4; k++)
                {
                    var v = UIVertex.simpleVert;
                    vh.PopulateUIVertex(ref v, q * 4 + k);
                    v.position += Vector3.up * dy;
                    vh.SetUIVertex(v, q * 4 + k);
                }
            }
            q++;
        }
    }

    static bool IsTone(char c) { return c >= '่' && c <= '์'; }
    static bool IsUpperVowel(char c) { return c == 'ั' || (c >= 'ิ' && c <= 'ื') || c == 'ํ'; }
}
