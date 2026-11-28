using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ============================================================
// PauseMenu — กด Esc หยุดเกม
// 💡 ไม่ต้องลากใส่ฉาก: ฉากไหนมีมาริสา เมนูนี้สร้างตัวเองให้อัตโนมัติ
// ============================================================
public class PauseMenu : MonoBehaviour
{
    public const string MenuScene = "MainMenu";
    public static PauseMenu Instance { get; private set; }
    public bool IsPaused { get; private set; }

    Canvas canvas;
    Button first;
    bool resuming;
    readonly List<Behaviour> frozen = new List<Behaviour>(); // สคริปต์ที่ปิดไว้ตอนหยุดเกม

    // --- สร้างตัวเองทุกครั้งที่โหลดฉาก ---
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // กันซ้ำ (โปรเจกต์ปิด Domain Reload)
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance != null || FindFirstObjectByType<PlayerController25D>() == null) return;
        new GameObject("PauseMenu").AddComponent<PauseMenu>();
    }

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f; AudioListener.pause = false;
        UIKit.EnsureEventSystem();
        canvas = UIKit.CreateCanvas("PauseCanvas", 100);
        canvas.transform.SetParent(transform, false);
        var root = canvas.transform;
        UIKit.Panel(root, new Color(0f, 0f, 0f, 0.8f));
        UIKit.Label(root, "หยุดเกม", 90, new Vector2(0, 330), new Vector2(900, 140));
        first = UIKit.Button(root, "เล่นต่อ", new Vector2(0, 150), Resume);
        UIKit.Button(root, "กลับจุดเช็กพอยต์", new Vector2(0, 55), ToCheckpoint);
        UIKit.Button(root, "เริ่มฉากใหม่", new Vector2(0, -40), Restart);
        UIKit.Button(root, "เมนูหลัก", new Vector2(0, -135), ToMainMenu);
        UIKit.VolumeSlider(root, new Vector2(0, -250));
        canvas.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (IsPaused) Resume(); else Pause();
    }

    public void Pause()
    {
        if (IsPaused || resuming) return;
        if (PlayerRespawn.Instance != null && PlayerRespawn.Instance.IsDead) return; // กำลังตายอยู่ ไม่ให้หยุด
        if (CodePanel.BlocksPause || EndingTrigger.Playing) return; // เปิดแผงรหัสอยู่ / กำลังจบเกม
        IsPaused = true;
        Time.timeScale = 0f; AudioListener.pause = true;
        // ปิดปุ่มของเกม ไม่ให้กด Space/F/E ระหว่างหยุดแล้วมีผล
        Freeze<PlayerController25D>(); Freeze<Flashlight>(); Freeze<FlashlightPickup>(); Freeze<CodePanel>();
        canvas.gameObject.SetActive(true);
        first.Select();
    }

    public void Resume()
    {
        if (!IsPaused || resuming) return;
        StartCoroutine(ResumeRoutine());
    }

    IEnumerator ResumeRoutine()
    {
        resuming = true;
        canvas.gameObject.SetActive(false);
        yield return null; // รอ 1 เฟรม ปุ่ม Space ที่กดเลือกเมนูจะได้ไม่กลายเป็นกระโดด
        Unfreeze();
        Time.timeScale = 1f; AudioListener.pause = false;
        IsPaused = false; resuming = false;
    }

    public void ToCheckpoint() { if (!IsPaused || resuming) return; StartCoroutine(CheckpointRoutine()); }
    IEnumerator CheckpointRoutine() { yield return ResumeRoutine(); PlayerRespawn.ReturnToCheckpoint(); }

    public void Restart() { Load(SceneManager.GetActiveScene().name); }
    public void ToMainMenu() { Load(MenuScene); }

    void Load(string scene)
    {
        Time.timeScale = 1f; AudioListener.pause = false;
        SceneManager.LoadScene(scene);
    }

    void Freeze<T>() where T : Behaviour
    {
        foreach (var b in FindObjectsByType<T>(FindObjectsInactive.Exclude))
            if (b.enabled) { b.enabled = false; frozen.Add(b); }
    }

    void Unfreeze()
    {
        foreach (var b in frozen) if (b != null) b.enabled = true;
        frozen.Clear();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (IsPaused) { Time.timeScale = 1f; AudioListener.pause = false; }
    }
}
