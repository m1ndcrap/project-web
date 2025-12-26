using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] public Image logoImage;
    [SerializeField] public Text option1Text;
    [SerializeField] public Text option2Text;
    [SerializeField] public Text option3Text;
    [SerializeField] public Text option4Text;
    [SerializeField] public Text pressEnterText;
    [SerializeField] public AudioSource audioSource;
    [SerializeField] public AudioClip confirmSound;
    [SerializeField] public AudioClip scrollSound;
    [SerializeField] public MenuPlayer player;
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

    void Start()
    {
        SetOptionColors();
        UpdateLogoAlpha();
    }

    void Update()
    {
        HandleInput();
        HandleFade();
    }

    void HandleInput()
    {
        if (!started)
        {
            if (Input.GetKeyDown(KeyCode.Return) && phase == 0)
            {
                PlayConfirm();
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

    void MoveDown()
    {
        if (option < 3)
        {
            option++;
            PlayScroll();
            StartCoroutine(MoveCooldown());
        }
    }

    void MoveUp()
    {
        if (option > 0)
        {
            option--;
            PlayScroll();
            StartCoroutine(MoveCooldown());
        }
    }

    IEnumerator MoveCooldown()
    {
        movable = false;
        yield return new WaitForSeconds(0.15f); // Alarm[1] = 10
        movable = true;
    }

    void StartGame()
    {
        started = true;
        phase = 2;

        player.dirX = 1;

        PlayConfirm();

        StartCoroutine(Alarm2());
        StartCoroutine(Alarm3());
        StartCoroutine(Alarm4());
        StartCoroutine(Alarm0());
    }

    IEnumerator Alarm0()
    {
        yield return new WaitForSeconds(3.75f); // Alarm[0] = 225

        string nextScene = TestScene;
        if (option == 1) nextScene = MainScene;
        if (option == 2) nextScene = BossScene;
        if (option == 3) nextScene = MissionScene;

        SceneManager.LoadScene(nextScene);
    }

    IEnumerator Alarm2()
    {
        yield return new WaitForSeconds(1f); // Alarm[2] = 60
        player.jumpKey = true;
        yield return new WaitForSeconds(0.03f);
        player.jumpKey = false;
    }

    IEnumerator Alarm3()
    {
        yield return new WaitForSeconds(1.5f); // Alarm[3] = 90
        player.swingKey = true;
        yield return new WaitForSeconds(0.03f);
        player.swingKey = false;
    }

    IEnumerator Alarm4()
    {
        yield return new WaitForSeconds(2.15f); // Alarm[4] = 130
        player.swingKeyR = true;
        yield return new WaitForSeconds(0.03f);
        player.swingKeyR = false;
    }

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

    void UpdateLogoAlpha()
    {
        Color c = logoImage.color;
        c.a = alpha;
        logoImage.color = c;
    }
    
    void UpdateOptionsAlpha(Text Option)
    {
        Color c = Option.color;
        c.a = alpha;
        Option.color = c;
    }

    void SetOptionColors()
    {
        option1Text.GetComponent<Outline>().effectColor = (option == 0) ? aqua : red;
        option2Text.GetComponent<Outline>().effectColor = (option == 1) ? aqua : red;
        option3Text.GetComponent<Outline>().effectColor = (option == 2) ? aqua : red;
        option4Text.GetComponent<Outline>().effectColor = (option == 3) ? aqua : red;

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

    void PlayConfirm()
    {
        audioSource.PlayOneShot(confirmSound);
    }

    void PlayScroll()
    {
        audioSource.PlayOneShot(scrollSound);
    }
}