using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


/// <summary>
/// Drives the title screen: waits for Enter key press to reveal the level-select menu, handles up/down
/// navigation between levels, and plays a short scripted intro (the player character jumping and
/// swinging in the background) before fading out and loading the chosen level.
/// </summary>


public class MainMenuController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] public Image logoImage;
    [SerializeField] public Text option1Text;
    [SerializeField] public Text option2Text;
    [SerializeField] public Text option3Text;
    [SerializeField] public Text option4Text;
    [SerializeField] public Text pressEnterText;


    [Header("Audio")]
    [SerializeField] public AudioSource audioSource;
    [SerializeField] public AudioClip confirmSound;
    [SerializeField] public AudioClip scrollSound;


    [Header("Background Player Demo")]
    [Tooltip("The decorative player character animating in the background of the title screen.")]
    [SerializeField] public MenuPlayer player;


    [Header("Scenes")]
    [SerializeField] public string TestScene;
    [SerializeField] public string MainScene;
    [SerializeField] public string BossScene;
    [SerializeField] public string MissionScene;


    bool started = false;
    bool movable = true;
    int phase = 0;
    int option = 0;
    float alpha = 1f;


    Color red = Color.red;
    Color aqua = Color.cyan;


    private Outline option1Outline;
    private Outline option2Outline;
    private Outline option3Outline;
    private Outline option4Outline;




    void Start()
    {
        option1Outline = option1Text.GetComponent<Outline>();
        option2Outline = option2Text.GetComponent<Outline>();
        option3Outline = option3Text.GetComponent<Outline>();
        option4Outline = option4Text.GetComponent<Outline>();

        SetOptionColors();
        UpdateLogoAlpha();
    }




    void Update()
    {
        HandleInput();
        HandleFade();
    }




    /// <summary>
    /// Handles user input to control menu navigation and initiate the game based on the current phase and state.
    /// </summary>
    void HandleInput()
    {
        if (!started)
        {
            if (Input.GetKeyDown(KeyCode.Return) && phase == 0)
            {
                audioSource.PlayOneShot(confirmSound);
                phase = 1;
            }
            else if (Input.GetKeyDown(KeyCode.Return) && phase == 1)
            {
                StartGame();
            }
        }

        if (phase == 1 && movable)
        {
            if (Input.GetKeyDown(KeyCode.DownArrow))
                MoveDown();

            if (Input.GetKeyDown(KeyCode.UpArrow))
                MoveUp();
        }

        if (phase == 1)
            SetOptionColors();
    }




    /// <summary>
    /// Moves the current selection down by one position, if possible.
    /// </summary>
    void MoveDown()
    {
        if (option < 3)
        {
            option++;
            audioSource.PlayOneShot(scrollSound);
            StartCoroutine(MoveCooldown());
        }
    }




    /// <summary>
    /// Moves the current selection up by one position, if possible.
    /// </summary>
    void MoveUp()
    {
        if (option > 0)
        {
            option--;
            audioSource.PlayOneShot(scrollSound);
            StartCoroutine(MoveCooldown());
        }
    }




    /// <summary>
    /// Temporarily disables movement for a short cooldown period.
    /// </summary>
    /// <returns>An enumerator that manages the cooldown delay. The enumerator yields once for the duration of the cooldown.</returns>
    IEnumerator MoveCooldown()
    {
        movable = false;
        yield return new WaitForSeconds(0.15f);
        movable = true;
    }




    /// <summary>
    /// Confirms the level selection and begins the intro sequence: plays a scripted jump/swing
    /// demo on the background player (see the Alarm coroutines below), then loads the chosen level
    /// once the intro finishes.
    /// </summary>
    void StartGame()
    {
        started = true;
        phase = 2;

        player.dirX = 1;

        audioSource.PlayOneShot(confirmSound);

        StartCoroutine(Alarm2());
        StartCoroutine(Alarm3());
        StartCoroutine(Alarm4());
        StartCoroutine(Alarm0());
    }




    /// <summary>
    /// Waits out the intro sequence, then loads whichever level is currently selected.
    /// </summary>
    IEnumerator Alarm0()
    {
        yield return new WaitForSeconds(3.75f);

        string nextScene = TestScene;
        if (option == 1) nextScene = MainScene;
        if (option == 2) nextScene = BossScene;
        if (option == 3) nextScene = MissionScene;

        SceneManager.LoadScene(nextScene);
    }




    /// <summary>
    /// Taps the background player's jump input for one frame partway through the intro, as a scripted animation beat.
    /// </summary>
    IEnumerator Alarm2()
    {
        yield return new WaitForSeconds(1f);
        player.jumpKey = true;
        yield return new WaitForSeconds(0.03f);
        player.jumpKey = false;
    }




    /// <summary>
    /// Taps the background player's swing input for one frame partway through the intro, as a scripted animation beat.
    /// </summary>
    IEnumerator Alarm3()
    {
        yield return new WaitForSeconds(1.5f);
        player.swingKey = true;
        yield return new WaitForSeconds(0.03f);
        player.swingKey = false;
    }




    /// <summary>
    /// Taps the background player's swing-release input for one frame partway through the intro, as a scripted animation beat.
    /// </summary>
    IEnumerator Alarm4()
    {
        yield return new WaitForSeconds(2.15f);
        player.swingKeyR = true;
        yield return new WaitForSeconds(0.03f);
        player.swingKeyR = false;
    }




    /// <summary>
    /// Fades the logo and menu options out once the intro sequence begins (phase 2).
    /// </summary>
    void HandleFade()
    {
        if (phase == 2)
        {
            alpha = Mathf.Max(0, alpha - Time.deltaTime * 3f);
            UpdateLogoAlpha();
            UpdateOptionsAlpha(option1Text);
            UpdateOptionsAlpha(option2Text);
            UpdateOptionsAlpha(option3Text);
            UpdateOptionsAlpha(option4Text);
        }
    }




    /// <summary>
    /// Updates the alpha (transparency) value of the logo image to match the current alpha setting.
    /// </summary>
    void UpdateLogoAlpha()
    {
        Color c = logoImage.color;
        c.a = alpha;
        logoImage.color = c;
    }




    /// <summary>
    /// Updates the alpha (transparency) value of the specified text option.
    /// </summary>
    /// <param name="Option">The text option whose color alpha value will be updated.</param>
    void UpdateOptionsAlpha(Text Option)
    {
        Color c = Option.color;
        c.a = alpha;
        Option.color = c;
    }




    /// <summary>
    /// Highlights whichever level option is currently selected, and shows/hides the menu text depending on whether the level-select menu is open yet.
    /// </summary>
    void SetOptionColors()
    {
        option1Outline.effectColor = (option == 0) ? aqua : red;
        option2Outline.effectColor = (option == 1) ? aqua : red;
        option3Outline.effectColor = (option == 2) ? aqua : red;
        option4Outline.effectColor = (option == 3) ? aqua : red;

        if (phase == 0) pressEnterText.text = "PRESS ENTER"; else pressEnterText.text = "";

        if (phase == 1)
        {
            option1Text.text = "TEST LEVEL";
            option2Text.text = "QUEENS LEVEL";
            option3Text.text = "BOSS LEVEL";
            option4Text.text = "MISSION LEVEL";
        }
        else
        {
            option1Text.text = "";
            option2Text.text = "";
            option3Text.text = "";
            option4Text.text = "";
        }
    }
}