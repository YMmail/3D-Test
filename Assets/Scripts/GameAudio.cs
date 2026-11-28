using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================
// GameAudio — เล่นเสียงทั้งเกม
// 💡 ไม่ต้องลากใส่ฉาก สร้างตัวเองตอนเริ่มเกมและอยู่ข้ามฉาก
// วิธีทำงาน: คอยดูสถานะของมาริสา/ปลา/ลัง พอเปลี่ยนก็เล่นเสียง (ไม่ต้องไปแก้สคริปต์เดิม)
// สคริปต์อื่นอยากเล่นเสียงเอง: GameAudio.Play(l => l.gateOpen)
// ============================================================
public class GameAudio : MonoBehaviour
{
    // --- 1. SETTINGS ---
    const float WalkStep = 0.75f, RunStep = 1.15f; // เดิน/วิ่งกี่เมตรต่อ 1 ก้าว
    const float FishHearRange = 14f;               // ปลาไกลกว่านี้ไม่ได้ยิน

    public static GameAudio Instance { get; private set; }
    public GameAudioLibrary Library { get; private set; }
    public string LastPlayed { get; private set; } = ""; // ไว้ดูตอนทดสอบ

    AudioSource sfx, ui, ambience, push;
    AudioSource musicA, musicB, musicNow, stinger; // ดนตรี: สลับ 2 ตัวไว้เฟดข้ามกัน

    // --- 2. ของที่คอยดู ---
    PlayerController25D player;
    Flashlight flashlight;
    Pushable[] crates = new Pushable[0];
    readonly Dictionary<FishAI, FishAI.State> fishStates = new Dictionary<FishAI, FishAI.State>();
    Vector3 lastPos;
    float stepDistance, airTime;
    bool wasGrounded, wasOn, hadFlashlight, wasDead, ready;

    // --- 3. สร้างตัวเอง ---
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Instance != null) return;
        var lib = Resources.Load<GameAudioLibrary>("GameAudioLibrary");
        if (lib == null) return; // ไม่มีคลังเสียง = เกมเงียบ แต่ไม่พัง
        var go = new GameObject("GameAudio");
        DontDestroyOnLoad(go);
        var a = go.AddComponent<GameAudio>();
        a.Setup(lib);
    }

    void Setup(GameAudioLibrary lib)
    {
        Instance = this; Library = lib;
        sfx = NewSource(false); ui = NewSource(false); ambience = NewSource(true); push = NewSource(true);
        ui.ignoreListenerPause = true; // เสียงปุ่มดังได้ตอนหยุดเกม
        musicA = NewSource(true); musicB = NewSource(true); stinger = NewSource(false); musicNow = musicA;
        StartLoop(ambience, lib.ambience, 1f);
        StartLoop(push, lib.cratePush, 0f);
        SceneManager.sceneLoaded += OnSceneLoaded;
        FindThings();
    }

    AudioSource NewSource(bool loop)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false; s.loop = loop; s.spatialBlend = 0f;
        return s;
    }

    void StartLoop(AudioSource s, GameAudioLibrary.Sound sound, float mul)
    {
        if (sound.clips == null || sound.clips.Length == 0 || sound.clips[0] == null) return;
        s.clip = sound.clips[0]; s.volume = sound.volume * mul; s.Play();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m) { FindThings(); }

    void FindThings()
    {
        player = FindFirstObjectByType<PlayerController25D>();
        flashlight = player != null ? player.GetComponent<Flashlight>() : null;
        crates = FindObjectsByType<Pushable>(FindObjectsInactive.Exclude);
        fishStates.Clear();
        push.volume = 0f;
        ready = false; // เฟรมแรกแค่จำค่า ไม่เล่นเสียง
    }

    // --- 4. เล่นเสียง (สคริปต์อื่นเรียกได้) ---
    public static void Play(System.Func<GameAudioLibrary, GameAudioLibrary.Sound> pick, float volume = 1f)
    {
        if (Instance != null) Instance.PlayOn(Instance.sfx, pick(Instance.Library), volume);
    }

    public static void PlayUI(System.Func<GameAudioLibrary, GameAudioLibrary.Sound> pick)
    {
        if (Instance != null) Instance.PlayOn(Instance.ui, pick(Instance.Library), 1f);
    }

    void PlayOn(AudioSource source, GameAudioLibrary.Sound s, float volume)
    {
        if (s.clips == null || s.clips.Length == 0) return;
        var clip = s.clips[Random.Range(0, s.clips.Length)];
        if (clip == null) return;
        source.PlayOneShot(clip, s.volume * volume);
        LastPlayed = clip.name;
    }

    static bool Has(GameAudioLibrary.Sound s) { return s.clips != null && s.clips.Length > 0 && s.clips[0] != null; }

    // ยืนบนลัง = เสียงไม้
    static bool OnWood(Vector3 feet)
    {
        RaycastHit h;
        return Physics.Raycast(feet + Vector3.up * 0.2f, Vector3.down, out h, 0.5f, ~0, QueryTriggerInteraction.Ignore) && h.collider.GetComponentInParent<Pushable>() != null;
    }

    // --- 5. ทุกเฟรม: ดูว่าอะไรเปลี่ยน ---
    void Update()
    {
        UpdateMusic();
        if (player == null || Time.timeScale == 0f) return;
        bool dead = PlayerRespawn.Instance != null && PlayerRespawn.Instance.IsDead;
        bool grounded = player.IsGrounded;
        bool on = flashlight != null && flashlight.IsOn;
        bool has = flashlight != null && flashlight.hasFlashlight;
        Vector3 pos = player.transform.position;

        if (!ready)
        {
            ready = true;
            wasDead = dead; wasGrounded = grounded; wasOn = on; hadFlashlight = has; lastPos = pos; stepDistance = 0f; airTime = 0f;
            return;
        }

        // 5.1 ตาย
        if (dead && !wasDead && !PlayerRespawn.Instance.IsSilentReturn) { Play(l => l.death); PlayOn(stinger, Library.musicDeath, 1f); }

        // 5.2 ก้าวเท้า / กระโดด / ลงพื้น
        Vector3 move = pos - lastPos; float up = move.y; move.y = 0f;
        if (!dead)
        {
            if (grounded && move.magnitude < 1f) // ขยับเกิน 1 ม. ในเฟรมเดียว = วาร์ป ไม่นับ
            {
                stepDistance += move.magnitude;
                if (stepDistance >= (player.MoveDirection.magnitude > 3.5f ? RunStep : WalkStep)) { stepDistance = 0f; if (OnWood(pos)) Play(l => Has(l.footstepWood) ? l.footstepWood : l.footstep); else Play(l => l.footstep); }
            }
            if (!grounded) airTime += Time.deltaTime;
            if (wasGrounded && !grounded && up > 0.001f) Play(l => l.jump);
            if (!wasGrounded && grounded && airTime > 0.25f) Play(l => l.land);
        }
        if (grounded) airTime = 0f;

        // 5.3 ไฟฉาย
        if (has && !hadFlashlight) Play(l => l.pickup);
        else if (on != wasOn) Play(l => on || !Has(l.flashlightOff) ? l.flashlight : l.flashlightOff);

        // 5.4 ปลา
        foreach (var fish in FishAI.All)
        {
            FishAI.State old;
            if (fishStates.TryGetValue(fish, out old) && old != fish.CurrentState)
            {
                float near = 1f - Mathf.Clamp01(Vector3.Distance(fish.transform.position, pos) / FishHearRange);
                if (fish.CurrentState == FishAI.State.Warn) Play(l => l.fishWarn, near);
                if (fish.CurrentState == FishAI.State.Lunge) Play(l => l.fishLunge, near);
            }
            fishStates[fish] = fish.CurrentState;
        }

        // 5.5 ดันลัง (เสียงวน ค่อยๆ ดัง/เบา)
        bool pushing = false;
        foreach (var c in crates) if (c != null && c.IsBeingPushed) pushing = true;
        push.volume = Mathf.MoveTowards(push.volume, pushing && !dead ? Library.cratePush.volume : 0f, Time.deltaTime * 4f);

        wasDead = dead; wasGrounded = grounded; wasOn = on; hadFlashlight = has; lastPos = pos;
    }

    // --- 6. ดนตรี: เลือกเพลงตามฉาก/โซน แล้วค่อยๆ เฟดเปลี่ยน ---
    void UpdateMusic()
    {
        string scene = SceneManager.GetActiveScene().name;
        bool inGame = player != null;
        GameAudioLibrary.Sound want = null;
        bool loop = true;
        if (scene == PauseMenu.MenuScene) want = Library.musicMenu;
        else if (scene == "Ending") { want = Library.musicEnding; loop = false; }
        else if (inGame)
        {
            bool dead = PlayerRespawn.Instance != null && PlayerRespawn.Instance.IsDead && !PlayerRespawn.Instance.IsSilentReturn;
            float x = player.transform.position.x;
            if (!dead) want = x >= Library.zone2MinX && x < Library.zone2MaxX ? Library.musicZone2 : Library.musicZone1_3;
        }
        AudioClip clip = want != null && Has(want) ? want.clips[0] : null;
        float target = want != null ? want.volume : 0f;

        // เปลี่ยนเพลง: ตัวใหม่เริ่มเบาๆ ตัวเก่าค่อยๆ หาย
        if (clip != musicNow.clip)
        {
            var other = musicNow == musicA ? musicB : musicA;
            other.Stop(); other.clip = clip; other.loop = loop; other.volume = 0f;
            if (clip != null) other.Play();
            musicNow = other;
        }
        float rate = Time.unscaledDeltaTime / Mathf.Max(0.05f, Library.musicFade);
        musicNow.volume = Mathf.MoveTowards(musicNow.volume, target, rate);
        var old = musicNow == musicA ? musicB : musicA;
        old.volume = Mathf.MoveTowards(old.volume, 0f, rate);
        if (old.isPlaying && old.volume <= 0f) old.Stop();

        // เสียงน้ำวน: ดังเฉพาะในเกม
        if (Has(Library.ambience)) ambience.volume = Mathf.MoveTowards(ambience.volume, inGame ? Library.ambience.volume : 0f, rate);
    }
}
