using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [SerializeField] private string pauseSceneName = "Pause";
    [SerializeField] private string titleSceneName = "Title Screen";
    [SerializeField] private AudioClip snd_pause;
    [SerializeField] private AudioClip snd_confirm;
    [SerializeField] private AudioClip snd_scroll;
    private bool paused = false;
    private int option = 0;   // 0 = Continue, 1 = Restart, 2 = Quit
    private bool unpause = false;
    private bool movable = true;
    private string pausedFromScene = "";
    private Texture2D screenshotTexture;
    private Vector2 playerScreenPos = Vector2.zero;
    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == titleSceneName) Destroy(gameObject);
        if (scene.name == pauseSceneName && screenshotTexture != null) StartCoroutine(SendScreenshotToUI());
    }

    private void Update()
    {
        bool enterPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);

        if (enterPressed && !paused)
        {
            TriggerPause();
            return;
        }

        if (enterPressed && paused && unpause)
        {
            ConfirmOption();
            return;
        }

        if (paused && SceneManager.GetSceneByName(pauseSceneName).isLoaded) HandleMenuNavigation();
    }

    private void TriggerPause()
    {
        paused = true;
        pausedFromScene = SceneManager.GetActiveScene().name;

        // Capture player screen position BEFORE timeScale changes anything
        CapturePlayerScreenPos();

        StartCoroutine(CaptureAndLoadPauseScene());
    }

    private IEnumerator CaptureAndLoadPauseScene()
    {
        yield return new WaitForEndOfFrame();

        screenshotTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenshotTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenshotTexture.Apply();

        AudioListener.pause = true;
        PlaySoundUnpaused(snd_pause);

        Time.timeScale = 0f;

        // The gameplay scene stays in memory if player chooses to continue
        SceneManager.LoadScene(pauseSceneName, LoadSceneMode.Additive);
    }

    private IEnumerator SendScreenshotToUI()
    {
        yield return null;
        PauseMenuUI ui = FindPauseMenuUI();
        if (ui != null) ui.SetScreenshot(screenshotTexture, playerScreenPos);
        option = 0;
        movable = true;
        unpause = false;
        yield return new WaitForSecondsRealtime(0.167f);
        unpause = true;
        ui = FindPauseMenuUI();
        if (ui != null) ui.SetOption(option);
    }

    private void HandleMenuNavigation()
    {
        if (!movable) return;

        bool changed = false;

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (option < 2) { option++; changed = true; }
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (option > 0) { option--; changed = true; }
        }

        if (changed)
        {
            PlaySound(snd_scroll);
            StartMoveCooldown();

            PauseMenuUI ui = FindPauseMenuUI();
            if (ui != null) ui.SetOption(option);
        }
    }

    private void StartMoveCooldown()
    {
        movable = false;
        StartCoroutine(MoveCooldown());
    }

    private IEnumerator MoveCooldown()
    {
        yield return new WaitForSecondsRealtime(0.167f);
        movable = true;
    }

    private void ConfirmOption()
    {
        switch (option)
        {
            case 0: StartCoroutine(Continue()); break;  // Continue
            case 1: StartCoroutine(Restart()); break;  // Restart
            case 2: Quit(); break;  // Quit
        }
    }

    private IEnumerator Continue()
    {
        PlaySoundUnpaused(snd_confirm);
        yield return new WaitForSecondsRealtime(0.05f);

        CleanupPauseState();
        yield return SceneManager.UnloadSceneAsync(pauseSceneName);
    }

    private IEnumerator Restart()
    {
        PlaySoundUnpaused(snd_confirm);
        yield return new WaitForSecondsRealtime(0.05f);

        string targetScene = pausedFromScene;
        CleanupPauseState();

        yield return SceneManager.UnloadSceneAsync(pauseSceneName);
        SceneManager.LoadScene(targetScene);
    }

    private void Quit()
    {
        CleanupPauseState();
        SceneManager.LoadScene(titleSceneName);
    }

    private void CleanupPauseState()
    {
        paused = false;
        unpause = false;
        option = 0;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (screenshotTexture != null)
        {
            Destroy(screenshotTexture);
            screenshotTexture = null;
        }
    }

    private void CapturePlayerScreenPos()
    {
        GameObject player = GameObject.FindWithTag("Player");
        Camera cam = Camera.main;

        if (player != null && cam != null)
        {
            Vector3 sp = cam.WorldToScreenPoint(player.transform.position);
            playerScreenPos = new Vector2(sp.x, sp.y);
        }
        else
        {
            playerScreenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }
    }

    private PauseMenuUI FindPauseMenuUI()
    {
        // Search only in the pause scene so we don't accidentally find one elsewhere
        Scene ps = SceneManager.GetSceneByName(pauseSceneName);
        if (!ps.isLoaded) return null;

        foreach (GameObject root in ps.GetRootGameObjects())
        {
            PauseMenuUI ui = root.GetComponentInChildren<PauseMenuUI>(true);
            if (ui != null) return ui;
        }

        return null;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null) audioSource.PlayOneShot(clip);
    }

    private void PlaySoundUnpaused(AudioClip clip)
    {
        if (clip == null) return;
        GameObject go = new GameObject("TempAudio");
        DontDestroyOnLoad(go);
        AudioSource src = go.AddComponent<AudioSource>();
        src.ignoreListenerPause = true;
        src.PlayOneShot(clip);
        Destroy(go, clip.length + 0.1f);
    }
}