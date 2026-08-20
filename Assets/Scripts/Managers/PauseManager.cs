using System.Collections;
using Framework.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;


/// <summary>
/// Drives the pause menu: capturing a screenshot of gameplay to show behind the menu, loading the
/// pause scene additively (so gameplay stays in memory if the player resumes), handling menu
/// navigation input, and resolving the chosen option (continue/restart/quit). Persists across scene
/// loads so the same instance handles pausing no matter which level is currently active.
/// </summary>


public class PauseManager : Singleton<PauseManager>
{
    [Header("Scenes")]
    [Tooltip("The name of the pause menu scene, loaded additively on top of gameplay when the player pauses.")]
    [SerializeField] private string pauseSceneName = "Pause";
    [Tooltip("The name of the title screen scene. If this scene loads while a PauseManager still exists, the PauseManager destroys itself (a fresh one belongs to the title screen instead).")]
    [SerializeField] private string titleSceneName = "Title Screen";




    [Header("Audio")]
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




    /// <summary>
    /// Adds the AudioSource this manager plays its menu sounds through. Done here rather than in
    /// the Inspector, since this object is created once and expected to just work without extra setup.
    /// Set to ignore AudioListener.pause, since every sound this plays needs to be audible while the
    /// pause menu itself has gameplay audio paused.
    /// </summary>
    protected override void OnAwake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.ignoreListenerPause = true;
    }




    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;




    /// <summary>
    /// Called whenever a new scene is loaded. Handles destroying the PauseManager if the title screen
    /// loads, and removing duplicate AudioListeners/EventSystems if the pause scene loads.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == titleSceneName) Destroy(gameObject);

        if (scene.name == pauseSceneName)
        {
            RemoveDuplicatesFromScene(scene);
            if (screenshotTexture != null) StartCoroutine(SendScreenshotToUI());
        }
    }




    /// <summary>
    /// Unity only tolerates one active AudioListener and one active EventSystem at a time, warning
    /// in the console otherwise. Loading the pause scene additively on top of gameplay can bring in
    /// a second one of each if the pause scene's own Camera/Canvas were set up independently, rather
    /// than requiring every scene to be hand-configured to avoid the conflict, this disables any
    /// duplicate found specifically in the newly-loaded scene, leaving whichever one gameplay
    /// was already using untouched.
    /// </summary>
    /// <param name="loadedScene">The scene that was just loaded, whose duplicate listener/event system (if any) should be disabled.</param>
    private void RemoveDuplicatesFromScene(Scene loadedScene)
    {
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();
        bool listenerAlreadyExists = false;

        foreach (var listener in listeners)
        {
            if (listener.gameObject.scene != loadedScene) listenerAlreadyExists = true;
        }

        if (listenerAlreadyExists)
        {
            foreach (var listener in listeners)
            {
                if (listener.gameObject.scene == loadedScene) listener.enabled = false;
            }
        }

        EventSystem[] eventSystems = FindObjectsOfType<EventSystem>();
        bool eventSystemAlreadyExists = false;

        foreach (var eventSystem in eventSystems)
        {
            if (eventSystem.gameObject.scene != loadedScene) eventSystemAlreadyExists = true;
        }

        if (eventSystemAlreadyExists)
        {
            foreach (var eventSystem in eventSystems)
            {
                if (eventSystem.gameObject.scene == loadedScene) eventSystem.gameObject.SetActive(false);
            }
        }
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




    /// <summary>
    /// Begins the pause sequence: records which scene we're pausing from, captures the player's
    /// on-screen position (before Time.timeScale changes anything), and kicks off the screenshot
    /// capture & pause scene load.
    /// </summary>
    private void TriggerPause()
    {
        paused = true;
        pausedFromScene = SceneManager.GetActiveScene().name;

        // Capture player screen position BEFORE timeScale changes anything
        CapturePlayerScreenPos();

        StartCoroutine(CaptureAndLoadPauseScene());
    }




    /// <summary>Waits for the frame to finish rendering, captures a screenshot of it, freezes
    /// gameplay (audio and Time.timeScale), then loads the pause scene on top of it.
    /// </summary>
    private IEnumerator CaptureAndLoadPauseScene()
    {
        yield return new WaitForEndOfFrame();

        screenshotTexture = CaptureGameplayScreenshot();

        AudioListener.pause = true;
        PlaySoundUnpaused(snd_pause);

        Time.timeScale = 0f;

        // The gameplay scene stays in memory if player chooses to continue
        SceneManager.LoadScene(pauseSceneName, LoadSceneMode.Additive);
    }




    /// <summary>
    /// Renders the main camera's current view (minus the UI layer) to a texture, for use as
    /// the pause menu's background image.
    /// </summary>
    private Texture2D CaptureGameplayScreenshot()
    {
        Camera cam = Camera.main;
        int width = Screen.width;
        int height = Screen.height;

        RenderTexture rt = RenderTexture.GetTemporary(width, height, 24);

        // Save camera state so we can restore it after
        RenderTexture prevTarget = cam.targetTexture;
        int prevCullingMask = cam.cullingMask;

        cam.targetTexture = rt;
        cam.cullingMask &= ~LayerMask.GetMask("UI"); // exclude UI layer from this render
        cam.Render();

        RenderTexture prevActive = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        // Restore everything
        cam.targetTexture = prevTarget;
        cam.cullingMask = prevCullingMask;
        RenderTexture.active = prevActive;
        RenderTexture.ReleaseTemporary(rt);

        return tex;
    }




    /// <summary>
    /// Hands the captured screenshot off to the pause scene's UI once it's loaded, then briefly
    /// disables input before allowing menu navigation. A short guard against the same Enter press
    /// that opened the pause menu also being read as a menu confirm.
    /// </summary>
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




    /// <summary>
    /// Moves the menu selection up/down on arrow key input, with a short cooldown betweenmoves
    /// so a held key doesn't scroll through every option in one frame.
    /// </summary>
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
            if (ui != null) { ui.SetOption(option); }
        }
    }
    



    /// <summary>
    /// Starts the cooldown coroutine that prevents the menu selection from moving too quickly.
    /// </summary>
    private void StartMoveCooldown()
    {
        movable = false;
        StartCoroutine(MoveCooldown());
    }
    



    /// <summary>
    /// Coroutine that handles the cooldown period during which the menu selection cannot be moved.
    /// </summary>
    private IEnumerator MoveCooldown()
    {
        yield return new WaitForSecondsRealtime(0.167f);
        movable = true;
    }




    /// <summary>
    /// Resolves whichever menu option is currently selected: continue, restart, or quit to the title screen.
    /// </summary>
    private void ConfirmOption()
    {
        switch (option)
        {
            case 0: StartCoroutine(Continue()); break;  // Continue
            case 1: StartCoroutine(Restart()); break;  // Restart
            case 2: Quit(); break;  // Quit
        }
    }
    



    /// <summary>
    /// Coroutine that handles the continue action.
    /// </summary>
    private IEnumerator Continue()
    {
        PlaySoundUnpaused(snd_confirm);
        yield return new WaitForSecondsRealtime(0.05f);

        CleanupPauseState();
        yield return SceneManager.UnloadSceneAsync(pauseSceneName);
    }
    



    /// <summary>
    /// Coroutine that handles the restart action.
    /// </summary>
    private IEnumerator Restart()
    {
        PlaySoundUnpaused(snd_confirm);
        yield return new WaitForSecondsRealtime(0.05f);

        string targetScene = pausedFromScene;
        CleanupPauseState();

        yield return SceneManager.UnloadSceneAsync(pauseSceneName);
        SceneManager.LoadScene(targetScene);
    }
    



    /// <summary>
    /// Handles the quit action.
    /// </summary>
    private void Quit()
    {
        CleanupPauseState();
        SceneManager.LoadScene(titleSceneName);
    }




    /// <summary>
    /// Resets all pause state back to normal: unpauses time and audio, and releases the captured screenshot texture.
    /// </summary>
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




    /// <summary>
    /// Finds the player by tag and records their current screen-space position, used to align the
    /// captured screenshot's comic panel with where the player actually was. Falls back to the
    /// screen center if no player or camera is found.
    /// </summary>
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




    /// <summary>
    /// Finds the PauseMenuUI component within the pause scene specifically, so this never
    /// accidentally picks up an unrelated UI component from gameplay.
    /// </summary>
    private PauseMenuUI FindPauseMenuUI()
    {
        // Search only in the pause scene so we don't accidentally find one elsewhere
        Scene ps = SceneManager.GetSceneByName(pauseSceneName);
        if (!ps.isLoaded) { return null; }

        foreach (GameObject root in ps.GetRootGameObjects())
        {
            PauseMenuUI ui = root.GetComponentInChildren<PauseMenuUI>(true);
            if (ui != null) { return ui; }
        }

        return null;
    }




    /// <summary>
    /// Plays a clip through the attached AudioSource.
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip != null) { audioSource.PlayOneShot(clip); }
    }




    /// <summary>
    /// Plays a clip through a standalone, self-destructing AudioSource that ignores
    /// AudioListener.pause. Used for the pause/confirm sounds themselves, which need
    /// to be audible even while gameplay audio is paused.
    /// </summary>
    private void PlaySoundUnpaused(AudioClip clip)
    {
        if (clip == null) { return; }
        GameObject go = new GameObject("TempAudio");
        DontDestroyOnLoad(go);
        AudioSource src = go.AddComponent<AudioSource>();
        src.ignoreListenerPause = true;
        src.PlayOneShot(clip);
        Destroy(go, clip.length + 0.1f);
    }
}