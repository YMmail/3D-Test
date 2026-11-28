using UnityEngine;
using UnityEngine.UI;

// ============================================================
// InteractPrompt — ข้อความกลางจอล่าง เช่น "กด E เพื่อเก็บไฟฉาย"
// เรียกใช้: InteractPrompt.Show(this, "ข้อความ") / InteractPrompt.Hide(this)
// ============================================================
public class InteractPrompt : MonoBehaviour
{
    static InteractPrompt instance;
    Object owner; // ใครเป็นคนสั่งโชว์ (คนอื่นสั่งซ่อนแทนไม่ได้)
    Text label;
    GameObject box;

    public static void Show(Object who, string text)
    {
        if (instance == null)
        {
            var canvas = UIKit.CreateCanvas("InteractPrompt", 50);
            instance = canvas.gameObject.AddComponent<InteractPrompt>();
            var bg = UIKit.Panel(canvas.transform, new Color(0, 0, 0, 0.65f));
            bg.raycastTarget = false;
            bg.rectTransform.anchorMin = bg.rectTransform.anchorMax = new Vector2(0.5f, 0.17f);
            bg.rectTransform.sizeDelta = new Vector2(620, 84); bg.rectTransform.anchoredPosition = Vector2.zero;
            instance.box = bg.gameObject;
            instance.label = UIKit.Label(bg.transform, text, 36, Vector2.zero, new Vector2(600, 80));
        }
        instance.owner = who;
        instance.label.text = text;
        instance.box.SetActive(true);
    }

    public static void Hide(Object who)
    {
        if (instance == null || instance.owner != who) return;
        instance.owner = null;
        if (instance.box != null) instance.box.SetActive(false);
    }
}
