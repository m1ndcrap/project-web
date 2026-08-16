using UnityEngine;

public class SenseScript : MonoBehaviour
{
    [SerializeField] private PlayerStep player;
    float dir = 1f;
    int mstate = 0;

    // Start is called before the first frame update
    void Start()
    {
        if (player == null)
        {
            player = FindObjectOfType<PlayerStep>();
        }

        CalculateSensePosition();
    }

    // Update is called once per frame
    void Update()
    {
        if (!player.HasCounterTarget && !player.trigger || player.pState == PlayerStep.PlayerState.hurt)
        {
            Destroy(gameObject);
            return;
        }

        CalculateSensePosition();
    }

    private void CalculateSensePosition()
    {
        if (player.sprite.flipX) dir = -1f; else dir = 1f;
        mstate = player.anim.GetInteger("mstate");
        float normalizedTime = player.anim.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f;

        switch (mstate)
        {
            // positioning when idle
            case 0:
                transform.position = ToWorld(player.transform, new Vector2(0.046f * dir, 0.568f));
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            // positioning when running
            case 1:
                transform.position = ToWorld(player.transform, new Vector2(0.25f * dir, 0.496f));
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            // positioning when jumping
            case 2:
                if (normalizedTime >= 0f && normalizedTime < 0.077f) transform.position = ToWorld(player.transform, new Vector2(-0.028f * dir, 0.696f));
                else if (normalizedTime >= 0.077f && normalizedTime < 0.231f) transform.position = ToWorld(player.transform, new Vector2(-0.064f * dir, 0.696f));
                else if (normalizedTime >= 0.231f && normalizedTime < 0.308f) transform.position = ToWorld(player.transform, new Vector2(-0.02f * dir, 0.696f));
                else if (normalizedTime >= 0.308f && normalizedTime < 0.385f) transform.position = ToWorld(player.transform, new Vector2(0.054f * dir, 0.696f));
                else if (normalizedTime >= 0.385f && normalizedTime < 0.462f) transform.position = ToWorld(player.transform, new Vector2(0.158f * dir, 0.686f));
                else if (normalizedTime >= 0.462f && normalizedTime < 0.538f) transform.position = ToWorld(player.transform, new Vector2(0.232f * dir, 0.65f));
                else if (normalizedTime >= 0.538f && normalizedTime < 0.615f) transform.position = ToWorld(player.transform, new Vector2(0.274f * dir, 0.656f));
                else if (normalizedTime >= 0.615f && normalizedTime < 0.692f) transform.position = ToWorld(player.transform, new Vector2(0.332f * dir, 0.614f));
                else if (normalizedTime >= 0.692f && normalizedTime < 0.769f) transform.position = ToWorld(player.transform, new Vector2(0.368f * dir, 0.614f));
                else if (normalizedTime >= 0.769f && normalizedTime < 0.846f) transform.position = ToWorld(player.transform, new Vector2(0.394f * dir, 0.594f));
                else if (normalizedTime >= 0.846f && normalizedTime < 0.923f) transform.position = ToWorld(player.transform, new Vector2(0.4f * dir, 0.578f));
                else transform.position = ToWorld(player.transform, new Vector2(0.4f * dir, 0.556f));
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            // positioning when falling
            case 3:
                transform.position = ToWorld(player.transform, new Vector2(0.366f * dir, 0.42f));
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            // positioning when swinging
            case 4:
                if (normalizedTime >= 0f && normalizedTime < 0.25f) transform.position = ToWorld(player.transform, new Vector2(-0.292f * dir, -0.126f));
                else if (normalizedTime >= 0.25f && normalizedTime < 0.50f) transform.position = ToWorld(player.transform, new Vector2(-0.292f * dir, -0.034f));
                else if (normalizedTime >= 0.50f && normalizedTime < 0.75f) transform.position = ToWorld(player.transform, new Vector2(-0.286f * dir, -0.104f));
                else if (normalizedTime >= 0.75f && normalizedTime < 0.906f) transform.position = ToWorld(player.transform, new Vector2(-0.206f * dir, -0.162f));
                else transform.position = ToWorld(player.transform, new Vector2(-0.16f * dir, -0.174f));
                transform.rotation = player.transform.rotation;
                break;

            // position when ending swing
            case 5:
                transform.position = ToWorld(player.transform, new Vector2(0.218f * dir, 0.47f));
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            // position when crawling
            case 6:
                transform.position = ToWorld(player.transform, new Vector2(0.52f * dir, -0.244f));
                transform.rotation = player.transform.rotation;
                break;

            // position when zipping
            case 7:
                transform.position = ToWorld(player.transform, new Vector2(-0.63f * dir, -0.266f));
                transform.rotation = Quaternion.Euler(0f, 0f, player.transform.eulerAngles.z + (38.84f * dir));
                break;

            // position when aiming on ground
            case 8:
                if (normalizedTime >= 0f && normalizedTime < 0.333f) transform.position = ToWorld(player.transform, new Vector2(0.084f * dir, 0.57f));
                else if (normalizedTime >= 0.333f && normalizedTime < 0.667f) transform.position = ToWorld(player.transform, new Vector2(-0.196f * dir, 0.574f));
                else transform.position = ToWorld(player.transform, new Vector2(-0.458f * dir, 0.462f));
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            // position when aiming in air
            case 9:
                if (normalizedTime >= 0f && normalizedTime < 0.333f) transform.position = ToWorld(player.transform, new Vector2(0.084f * dir, 0.57f));
                else if (normalizedTime >= 0.333f && normalizedTime < 0.667f) transform.position = ToWorld(player.transform, new Vector2(-0.196f * dir, 0.574f));
                else transform.position = ToWorld(player.transform, new Vector2(-0.458f * dir, 0.462f));
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            // position when aiming during crawl
            case 10:
                if (normalizedTime >= 0f && normalizedTime < 0.333f) transform.position = ToWorld(player.transform, new Vector2(-0.104f * dir, 0.014f));
                else if (normalizedTime >= 0.333f && normalizedTime < 0.667f) transform.position = ToWorld(player.transform, new Vector2(-0.22f * dir, 0.014f));
                else transform.position = ToWorld(player.transform, new Vector2(-0.336f * dir, -0.062f));
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            // position when 1st punch anim
            case 11:
                if (normalizedTime >= 0f && normalizedTime < 0.0655f) transform.position = ToWorld(player.transform, new Vector2(-0.022f * dir, 0.576f));
                else if (normalizedTime >= 0.0655f && normalizedTime < 0.131f) transform.position = ToWorld(player.transform, new Vector2(-0.065f * dir, 0.568f));
                else if (normalizedTime >= 0.131f && normalizedTime < 0.1965f) transform.position = ToWorld(player.transform, new Vector2(-0.108f * dir, 0.56f));
                else if (normalizedTime >= 0.1965f && normalizedTime < 0.262f) transform.position = ToWorld(player.transform, new Vector2(-0.152f * dir, 0.552f));
                else if (normalizedTime >= 0.262f && normalizedTime < 0.295f) transform.position = ToWorld(player.transform, new Vector2(-0.196f * dir, 0.544f));
                else if (normalizedTime >= 0.295f && normalizedTime < 0.328f) transform.position = ToWorld(player.transform, new Vector2(-0.113f * dir, 0.545f));
                else if (normalizedTime >= 0.328f && normalizedTime < 0.3605f) transform.position = ToWorld(player.transform, new Vector2(-0.03f * dir, 0.546f));
                else if (normalizedTime >= 0.3605f && normalizedTime < 0.393f) transform.position = ToWorld(player.transform, new Vector2(0.053f * dir, 0.547f));
                else if (normalizedTime >= 0.393f && normalizedTime < 0.4175f) transform.position = ToWorld(player.transform, new Vector2(0.136f * dir, 0.548f));
                else if (normalizedTime >= 0.4175f && normalizedTime < 0.442f) transform.position = ToWorld(player.transform, new Vector2(0.163f * dir, 0.555f));
                else if (normalizedTime >= 0.442f && normalizedTime < 0.467f) transform.position = ToWorld(player.transform, new Vector2(0.19f * dir, 0.562f));
                else if (normalizedTime >= 0.467f && normalizedTime < 0.492f) transform.position = ToWorld(player.transform, new Vector2(0.217f * dir, 0.569f));
                else if (normalizedTime >= 0.492f && normalizedTime < 0.586f) transform.position = ToWorld(player.transform, new Vector2(0.244f * dir, 0.576f));
                else if (normalizedTime >= 0.586f && normalizedTime < 0.680f) transform.position = ToWorld(player.transform, new Vector2(0.178f * dir, 0.576f));
                else if (normalizedTime >= 0.680f && normalizedTime < 0.7745f) transform.position = ToWorld(player.transform, new Vector2(0.112f * dir, 0.576f));
                else if (normalizedTime >= 0.7745f && normalizedTime < 0.869f) transform.position = ToWorld(player.transform, new Vector2(0.045f * dir, 0.576f));
                else transform.position = ToWorld(player.transform, new Vector2(-0.022f * dir, 0.576f));
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            // position when 2nd punch anim
            case 12:
                if (normalizedTime >= 0f && normalizedTime < 0.303f) transform.position = ToWorld(player.transform, new Vector2(-0.028f * dir, 0.568f));
                else if (normalizedTime >= 0.303f && normalizedTime < 0.697f) transform.position = ToWorld(player.transform, new Vector2(0.282f * dir, 0.566f));
                else transform.position = ToWorld(player.transform, new Vector2(0.14f * dir, 0.588f));
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            // position when 3rd punch anim
            case 13:
                if (normalizedTime >= 0f && normalizedTime < 0.09425f) transform.position = ToWorld(player.transform, new Vector2(-0.062f * dir, 0.544f));
                else if (normalizedTime >= 0.09425f && normalizedTime < 0.1885f) transform.position = ToWorld(player.transform, new Vector2(0.0675f * dir, 0.5125f));
                else if (normalizedTime >= 0.1885f && normalizedTime < 0.28275f) transform.position = ToWorld(player.transform, new Vector2(0.197f * dir, 0.481f));
                else if (normalizedTime >= 0.28275f && normalizedTime < 0.377f) transform.position = ToWorld(player.transform, new Vector2(0.3265f * dir, 0.4495f));
                else if (normalizedTime >= 0.377f && normalizedTime < 0.4345f) transform.position = ToWorld(player.transform, new Vector2(0.456f * dir, 0.418f));
                else if (normalizedTime >= 0.4345f && normalizedTime < 0.492f) transform.position = ToWorld(player.transform, new Vector2(0.3675f * dir, 0.445f));
                else if (normalizedTime >= 0.492f && normalizedTime < 0.5495f) transform.position = ToWorld(player.transform, new Vector2(0.279f * dir, 0.472f));
                else if (normalizedTime >= 0.5495f && normalizedTime < 0.607f) transform.position = ToWorld(player.transform, new Vector2(0.1905f * dir, 0.499f));
                else if (normalizedTime >= 0.607f && normalizedTime < 0.652f) transform.position = ToWorld(player.transform, new Vector2(0.102f * dir, 0.526f));
                else if (normalizedTime >= 0.652f && normalizedTime < 0.697f) transform.position = ToWorld(player.transform, new Vector2(0.061f * dir, 0.5305f));
                else if (normalizedTime >= 0.697f && normalizedTime < 0.742f) transform.position = ToWorld(player.transform, new Vector2(0.02f * dir, 0.535f));
                else if (normalizedTime >= 0.742f && normalizedTime < 0.787f) transform.position = ToWorld(player.transform, new Vector2(-0.021f * dir, 0.5395f));
                else transform.position = ToWorld(player.transform, new Vector2(-0.062f * dir, 0.544f));
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            // position when 4th punch anim
            case 14:
                if (normalizedTime >= 0f && normalizedTime < 0.0595f) transform.position = ToWorld(player.transform, new Vector2(0.14f * dir, 0.498f));
                else if (normalizedTime >= 0.0595f && normalizedTime < 0.119f) transform.position = ToWorld(player.transform, new Vector2(0.22925f * dir, 0.48825f));
                else if (normalizedTime >= 0.119f && normalizedTime < 0.1785f) transform.position = ToWorld(player.transform, new Vector2(0.3185f * dir, 0.4785f));
                else if (normalizedTime >= 0.1785f && normalizedTime < 0.238f) transform.position = ToWorld(player.transform, new Vector2(0.40775f * dir, 0.46875f));
                else if (normalizedTime >= 0.238f && normalizedTime < 0.2975f) transform.position = ToWorld(player.transform, new Vector2(0.497f * dir, 0.459f));
                else if (normalizedTime >= 0.2975f && normalizedTime < 0.357f) transform.position = ToWorld(player.transform, new Vector2(0.5855f * dir, 0.4485f));
                else if (normalizedTime >= 0.357f && normalizedTime < 0.4165f) transform.position = ToWorld(player.transform, new Vector2(0.674f * dir, 0.438f));
                else if (normalizedTime >= 0.4165f && normalizedTime < 0.44625f) transform.position = ToWorld(player.transform, new Vector2(0.603f * dir, 0.44675f));
                else if (normalizedTime >= 0.44625f && normalizedTime < 0.476f) transform.position = ToWorld(player.transform, new Vector2(0.532f * dir, 0.461f));
                else if (normalizedTime >= 0.476f && normalizedTime < 0.49975f) transform.position = ToWorld(player.transform, new Vector2(0.461f * dir, 0.4725f));
                else if (normalizedTime >= 0.49975f && normalizedTime < 0.5235f) transform.position = ToWorld(player.transform, new Vector2(0.39f * dir, 0.484f));
                else if (normalizedTime >= 0.5235f && normalizedTime < 0.54725f) transform.position = ToWorld(player.transform, new Vector2(0.3545f * dir, 0.4905f));
                else if (normalizedTime >= 0.54725f && normalizedTime < 0.571f) transform.position = ToWorld(player.transform, new Vector2(0.32f * dir, 0.497f));
                else if (normalizedTime >= 0.571f && normalizedTime < 0.6425f) transform.position = ToWorld(player.transform, new Vector2(0.3025f * dir, 0.49975f));
                else if (normalizedTime >= 0.6425f && normalizedTime < 0.714f) transform.position = ToWorld(player.transform, new Vector2(0.285f * dir, 0.5025f));
                else if (normalizedTime >= 0.714f && normalizedTime < 0.7855f) transform.position = ToWorld(player.transform, new Vector2(0.268f * dir, 0.50525f));
                else if (normalizedTime >= 0.7855f && normalizedTime < 0.857f) transform.position = ToWorld(player.transform, new Vector2(0.259f * dir, 0.506625f));
                else transform.position = ToWorld(player.transform, new Vector2(0.25f * dir, 0.508f));
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            // position when doing air kick
            case 15:
                if (normalizedTime >= 0f && normalizedTime < 0.11675f) transform.position = ToWorld(player.transform, new Vector2(0.364f * dir, 0.438f));
                else if (normalizedTime >= 0.11675f && normalizedTime < 0.2335f) transform.position = ToWorld(player.transform, new Vector2(0.189f * dir, 0.4845f));
                else if (normalizedTime >= 0.2335f && normalizedTime < 0.35025f) transform.position = ToWorld(player.transform, new Vector2(0.014f * dir, 0.531f));
                else if (normalizedTime >= 0.35025f && normalizedTime < 0.467f) transform.position = ToWorld(player.transform, new Vector2(-0.164f * dir, 0.624f));
                else if (normalizedTime >= 0.467f && normalizedTime < 0.51175f) transform.position = ToWorld(player.transform, new Vector2(-0.034f * dir, 0.6265f));
                else if (normalizedTime >= 0.51175f && normalizedTime < 0.5565f) transform.position = ToWorld(player.transform, new Vector2(0.056f * dir, 0.629f));
                else if (normalizedTime >= 0.5565f && normalizedTime < 0.6f) transform.position = ToWorld(player.transform, new Vector2(0.12f * dir, 0.6295f));
                else if (normalizedTime >= 0.6f && normalizedTime < 0.7f) transform.position = ToWorld(player.transform, new Vector2(0.152f * dir, 0.63f));
                else if (normalizedTime >= 0.7f && normalizedTime < 0.8f) transform.position = ToWorld(player.transform, new Vector2(0.168f * dir, 0.63f));
                else if (normalizedTime >= 0.8f && normalizedTime < 0.9f) transform.position = ToWorld(player.transform, new Vector2(0.176f * dir, 0.63f));
                else transform.position = ToWorld(player.transform, new Vector2(0.184f * dir, 0.63f));
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            // position when doing air punch
            case 16:
                transform.position = ToWorld(player.transform, new Vector2(0.298f * dir, 0.668f));
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            // position when 1st kick anim
            case 17:
                transform.position = ToWorld(player.transform, new Vector2(-0.016f * dir, 0.624f));
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            // position when 2nd kick anim
            case 18:
                if (normalizedTime >= 0f && normalizedTime < 0.093f) transform.position = ToWorld(player.transform, new Vector2(-0.152f * dir, 0.558f));
                else if (normalizedTime >= 0.093f && normalizedTime < 0.186f) transform.position = ToWorld(player.transform, new Vector2(-0.14f * dir, 0.638f));
                else if (normalizedTime >= 0.186f && normalizedTime < 0.259f) transform.position = ToWorld(player.transform, new Vector2(-0.134f * dir, 0.678f));
                else if (normalizedTime >= 0.259f && normalizedTime < 0.333f) transform.position = ToWorld(player.transform, new Vector2(-0.13f * dir, 0.698f));
                else if (normalizedTime >= 0.333f && normalizedTime < 0.407f) transform.position = ToWorld(player.transform, new Vector2(-0.128f * dir, 0.718f));
                else if (normalizedTime >= 0.407f && normalizedTime < 0.456f) transform.position = ToWorld(player.transform, new Vector2(-0.256f * dir, 0.75f));
                else if (normalizedTime >= 0.456f && normalizedTime < 0.506f) transform.position = ToWorld(player.transform, new Vector2(-0.386f * dir, 0.782f));
                else if (normalizedTime >= 0.506f && normalizedTime < 0.556f) transform.position = ToWorld(player.transform, new Vector2(-0.514f * dir, 0.812f));
                else if (normalizedTime >= 0.556f && normalizedTime < 0.692f) transform.position = ToWorld(player.transform, new Vector2(-0.53f * dir, 0.724f));
                else if (normalizedTime >= 0.692f && normalizedTime < 0.827f) transform.position = ToWorld(player.transform, new Vector2(-0.534f * dir, 0.682f));
                else if (normalizedTime >= 0.827f && normalizedTime < 0.963f) transform.position = ToWorld(player.transform, new Vector2(-0.538f * dir, 0.64f));
                else if (normalizedTime >= 0.963f && normalizedTime < 0.982f) transform.position = ToWorld(player.transform, new Vector2(-0.15f * dir, 0.488f));
                else if (normalizedTime >= 0.982f && normalizedTime < 1f) transform.position = ToWorld(player.transform, new Vector2(0.044f * dir, 0.412f));
                else transform.position = ToWorld(player.transform, new Vector2(0.24f * dir, 0.338f));
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            // position when uppercut
            case 19:
                transform.position = ToWorld(player.transform, new Vector2(0.108f * dir, 0.594f));
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            // position when launched
            case 20:
                if (normalizedTime >= 0f && normalizedTime < 0.083f) transform.position = ToWorld(player.transform, new Vector2(-0.17f * dir, 0.564f));
                else if (normalizedTime >= 0.083f && normalizedTime < 0.125f) transform.position = ToWorld(player.transform, new Vector2(-0.357f * dir, 0.721f));
                else if (normalizedTime >= 0.125f && normalizedTime < 0.167f) transform.position = ToWorld(player.transform, new Vector2(-0.452f * dir, 0.7995f));
                else if (normalizedTime >= 0.167f && normalizedTime < 0.233f) transform.position = ToWorld(player.transform, new Vector2(-0.544f * dir, 0.878f));
                else if (normalizedTime >= 0.233f && normalizedTime < 0.3f) transform.position = ToWorld(player.transform, new Vector2(-0.6f * dir, 0.5954f));
                else if (normalizedTime >= 0.3f && normalizedTime < 0.367f) transform.position = ToWorld(player.transform, new Vector2(-0.628f * dir, 0.3126f));
                else if (normalizedTime >= 0.367f && normalizedTime < 0.467f) transform.position = ToWorld(player.transform, new Vector2(-0.656f * dir, 0.03f));
                else if (normalizedTime >= 0.467f && normalizedTime < 0.567f) transform.position = ToWorld(player.transform, new Vector2(-0.446f * dir, 0.012f));
                else if (normalizedTime >= 0.567f && normalizedTime < 0.667f) transform.position = ToWorld(player.transform, new Vector2(-0.34f * dir, 0.003f));
                else if (normalizedTime >= 0.667f && normalizedTime < 0.767f) transform.position = ToWorld(player.transform, new Vector2(-0.234f * dir, -0.006f));
                else if (normalizedTime >= 0.767f && normalizedTime < 0.867f) transform.position = ToWorld(player.transform, new Vector2(-0.239f * dir, -0.208f));
                else if (normalizedTime >= 0.867f && normalizedTime < 0.967f) transform.position = ToWorld(player.transform, new Vector2(-0.242f * dir, -0.309f));
                else if (normalizedTime >= 0.967f && normalizedTime < 0.983f) transform.position = ToWorld(player.transform, new Vector2(-0.243f * dir, -0.3596f));
                else if (normalizedTime >= 0.983f && normalizedTime < 1f) transform.position = ToWorld(player.transform, new Vector2(-0.244f * dir, -0.3848f));
                else transform.position = ToWorld(player.transform, new Vector2(-0.244f * dir, -0.41f));
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            // position when 1st hurt anim
            case 21:
                if (normalizedTime >= 0f && normalizedTime < 0.111f) transform.position = ToWorld(player.transform, new Vector2(-0.092f * dir, 0.56f));
                else if (normalizedTime >= 0.111f && normalizedTime < 0.222f) transform.position = ToWorld(player.transform, new Vector2(-0.22f * dir, 0.504f));
                else if (normalizedTime >= 0.222f && normalizedTime < 0.333f) transform.position = ToWorld(player.transform, new Vector2(-0.348f * dir, 0.448f));
                else if (normalizedTime >= 0.333f && normalizedTime < 0.349f) transform.position = ToWorld(player.transform, new Vector2(-0.476f * dir, 0.392f));
                else if (normalizedTime >= 0.349f && normalizedTime < 0.365f) transform.position = ToWorld(player.transform, new Vector2(-0.316f * dir, 0.428f));
                else if (normalizedTime >= 0.365f && normalizedTime < 0.381f) transform.position = ToWorld(player.transform, new Vector2(-0.156f * dir, 0.464f));
                else if (normalizedTime >= 0.381f && normalizedTime < 0.514f) transform.position = ToWorld(player.transform, new Vector2(-0.102f * dir, 0.476f));
                else if (normalizedTime >= 0.514f && normalizedTime < 0.648f) transform.position = ToWorld(player.transform, new Vector2(-0.05f * dir, 0.488f));
                else if (normalizedTime >= 0.648f && normalizedTime < 0.781f) transform.position = ToWorld(player.transform, new Vector2(-0.024f * dir, 0.494f));
                else if (normalizedTime >= 0.781f && normalizedTime < 0.905f) transform.position = ToWorld(player.transform, new Vector2(-0.01f * dir, 0.498f));
                else transform.position = ToWorld(player.transform, new Vector2(0.004f * dir, 0.5f));
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            // position when 2nd hurt anim
            case 22:
                if (normalizedTime >= 0f && normalizedTime < 0.193f) transform.position = ToWorld(player.transform, new Vector2(0.058f * dir, 0.598f));
                else if (normalizedTime >= 0.193f && normalizedTime < 0.386f) transform.position = ToWorld(player.transform, new Vector2(-0.1074f * dir, 0.5586f));
                else if (normalizedTime >= 0.386f && normalizedTime < 0.579f) transform.position = ToWorld(player.transform, new Vector2(-0.2726f * dir, 0.5194f));
                else if (normalizedTime >= 0.579f && normalizedTime < 0.702f) transform.position = ToWorld(player.transform, new Vector2(-0.438f * dir, 0.48f));
                else if (normalizedTime >= 0.702f && normalizedTime < 0.825f) transform.position = ToWorld(player.transform, new Vector2(-0.4054f * dir, 0.4834f));
                else if (normalizedTime >= 0.825f && normalizedTime < 0.947f) transform.position = ToWorld(player.transform, new Vector2(-0.3726f * dir, 0.4866f));
                else if (normalizedTime >= 0.947f && normalizedTime < 0.974f) transform.position = ToWorld(player.transform, new Vector2(-0.3562f * dir, 0.4884f));
                else transform.position = ToWorld(player.transform, new Vector2(-0.34f * dir, 0.49f));
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            // position when 1st block anim
            case 23:
                transform.position = ToWorld(player.transform, new Vector2(-0.064f * dir, 0.604f));
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            // position when 2nd block anim
            case 24:
                transform.position = ToWorld(player.transform, new Vector2(-0.064f * dir, 0.604f));
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            // position when 3rd block anim
            case 25:
                if (normalizedTime >= 0f && normalizedTime < 0.035f) transform.position = ToWorld(player.transform, new Vector2(0.038f * dir, 0.528f));
                else if (normalizedTime >= 0.035f && normalizedTime < 0.07f) transform.position = ToWorld(player.transform, new Vector2(0.0414f * dir, 0.5268f));
                else if (normalizedTime >= 0.07f && normalizedTime < 0.104f) transform.position = ToWorld(player.transform, new Vector2(0.0446f * dir, 0.5254f));
                else if (normalizedTime >= 0.104f && normalizedTime < 0.13f) transform.position = ToWorld(player.transform, new Vector2(0.0472f * dir, 0.523f));
                else if (normalizedTime >= 0.13f && normalizedTime < 0.156f) transform.position = ToWorld(player.transform, new Vector2(0.0514f * dir, 0.5206f));
                else if (normalizedTime >= 0.156f && normalizedTime < 0.197f) transform.position = ToWorld(player.transform, new Vector2(0.054f * dir, 0.516f));
                else if (normalizedTime >= 0.197f && normalizedTime < 0.268f) transform.position = ToWorld(player.transform, new Vector2(-0.0464f * dir, 0.497f));
                else if (normalizedTime >= 0.268f && normalizedTime < 0.321f) transform.position = ToWorld(player.transform, new Vector2(-0.1494f * dir, 0.478f));
                else if (normalizedTime >= 0.321f && normalizedTime < 0.382f) transform.position = ToWorld(player.transform, new Vector2(-0.251f * dir, 0.459f));
                else if (normalizedTime >= 0.382f && normalizedTime < 0.403f) transform.position = ToWorld(player.transform, new Vector2(-0.3526f * dir, 0.44f));
                else if (normalizedTime >= 0.403f && normalizedTime < 0.459f) transform.position = ToWorld(player.transform, new Vector2(-0.488f * dir, 0.4206f));
                else if (normalizedTime >= 0.459f && normalizedTime < 0.572f) transform.position = ToWorld(player.transform, new Vector2(-0.556f * dir, 0.402f));
                else if (normalizedTime >= 0.572f && normalizedTime < 0.657f) transform.position = ToWorld(player.transform, new Vector2(-0.4574f * dir, 0.4172f));
                else if (normalizedTime >= 0.657f && normalizedTime < 0.741f) transform.position = ToWorld(player.transform, new Vector2(-0.3574f * dir, 0.4454f));
                else if (normalizedTime >= 0.741f && normalizedTime < 0.825f) transform.position = ToWorld(player.transform, new Vector2(-0.257f * dir, 0.473f));
                else if (normalizedTime >= 0.825f && normalizedTime < 0.909f) transform.position = ToWorld(player.transform, new Vector2(-0.1586f * dir, 0.4886f));
                else if (normalizedTime >= 0.909f && normalizedTime < 0.931f) transform.position = ToWorld(player.transform, new Vector2(-0.057f * dir, 0.502f));
                else if (normalizedTime >= 0.931f && normalizedTime < 0.948f) transform.position = ToWorld(player.transform, new Vector2(0.044f * dir, 0.532f));
                else if (normalizedTime >= 0.948f && normalizedTime < 0.967f) transform.position = ToWorld(player.transform, new Vector2(0.0426f * dir, 0.531f));
                else if (normalizedTime >= 0.967f && normalizedTime < 0.987f) transform.position = ToWorld(player.transform, new Vector2(0.041f * dir, 0.53f));
                else if (normalizedTime >= 0.987f && normalizedTime < 1f) transform.position = ToWorld(player.transform, new Vector2(0.0424f * dir, 0.531f));
                else transform.position = ToWorld(player.transform, new Vector2(0.044f * dir, 0.532f));
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            // position when 4th block anim
            case 26:
                if (normalizedTime >= 0f && normalizedTime < 0.035f) transform.position = ToWorld(player.transform, new Vector2(0.038f * dir, 0.528f));
                else if (normalizedTime >= 0.035f && normalizedTime < 0.07f) transform.position = ToWorld(player.transform, new Vector2(0.0414f * dir, 0.5268f));
                else if (normalizedTime >= 0.07f && normalizedTime < 0.104f) transform.position = ToWorld(player.transform, new Vector2(0.0446f * dir, 0.5254f));
                else if (normalizedTime >= 0.104f && normalizedTime < 0.13f) transform.position = ToWorld(player.transform, new Vector2(0.0472f * dir, 0.523f));
                else if (normalizedTime >= 0.13f && normalizedTime < 0.156f) transform.position = ToWorld(player.transform, new Vector2(0.0514f * dir, 0.5206f));
                else if (normalizedTime >= 0.156f && normalizedTime < 0.197f) transform.position = ToWorld(player.transform, new Vector2(0.054f * dir, 0.516f));
                else if (normalizedTime >= 0.197f && normalizedTime < 0.268f) transform.position = ToWorld(player.transform, new Vector2(-0.0464f * dir, 0.497f));
                else if (normalizedTime >= 0.268f && normalizedTime < 0.321f) transform.position = ToWorld(player.transform, new Vector2(-0.1494f * dir, 0.478f));
                else if (normalizedTime >= 0.321f && normalizedTime < 0.382f) transform.position = ToWorld(player.transform, new Vector2(-0.251f * dir, 0.459f));
                else if (normalizedTime >= 0.382f && normalizedTime < 0.403f) transform.position = ToWorld(player.transform, new Vector2(-0.3526f * dir, 0.44f));
                else if (normalizedTime >= 0.403f && normalizedTime < 0.459f) transform.position = ToWorld(player.transform, new Vector2(-0.488f * dir, 0.4206f));
                else if (normalizedTime >= 0.459f && normalizedTime < 0.572f) transform.position = ToWorld(player.transform, new Vector2(-0.556f * dir, 0.402f));
                else if (normalizedTime >= 0.572f && normalizedTime < 0.657f) transform.position = ToWorld(player.transform, new Vector2(-0.4574f * dir, 0.4172f));
                else if (normalizedTime >= 0.657f && normalizedTime < 0.741f) transform.position = ToWorld(player.transform, new Vector2(-0.3574f * dir, 0.4454f));
                else if (normalizedTime >= 0.741f && normalizedTime < 0.825f) transform.position = ToWorld(player.transform, new Vector2(-0.257f * dir, 0.473f));
                else if (normalizedTime >= 0.825f && normalizedTime < 0.909f) transform.position = ToWorld(player.transform, new Vector2(-0.1586f * dir, 0.4886f));
                else if (normalizedTime >= 0.909f && normalizedTime < 0.931f) transform.position = ToWorld(player.transform, new Vector2(-0.057f * dir, 0.502f));
                else if (normalizedTime >= 0.931f && normalizedTime < 0.948f) transform.position = ToWorld(player.transform, new Vector2(0.044f * dir, 0.532f));
                else if (normalizedTime >= 0.948f && normalizedTime < 0.967f) transform.position = ToWorld(player.transform, new Vector2(0.0426f * dir, 0.531f));
                else if (normalizedTime >= 0.967f && normalizedTime < 0.987f) transform.position = ToWorld(player.transform, new Vector2(0.041f * dir, 0.53f));
                else if (normalizedTime >= 0.987f && normalizedTime < 1f) transform.position = ToWorld(player.transform, new Vector2(0.0424f * dir, 0.531f));
                else transform.position = ToWorld(player.transform, new Vector2(0.044f * dir, 0.532f));
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            // position when swing kick anim
            case 28:
                if (normalizedTime >= 0f && normalizedTime < 0.25f) transform.position = ToWorld(player.transform, new Vector2(-0.292f * dir, -0.126f));
                else if (normalizedTime >= 0.25f && normalizedTime < 0.50f) transform.position = ToWorld(player.transform, new Vector2(-0.292f * dir, -0.034f));
                else if (normalizedTime >= 0.50f && normalizedTime < 0.75f) transform.position = ToWorld(player.transform, new Vector2(-0.286f * dir, -0.104f));
                else if (normalizedTime >= 0.75f && normalizedTime < 0.906f) transform.position = ToWorld(player.transform, new Vector2(-0.206f * dir, -0.162f));
                else transform.position = ToWorld(player.transform, new Vector2(-0.16f * dir, -0.174f));
                transform.rotation = player.transform.rotation;
                break;

            // position when crawl kick anim
            case 29:
                if (normalizedTime >= 0f && normalizedTime < 0.15f) transform.position = ToWorld(player.transform, new Vector2(0.562f * dir, -0.258f));
                else if (normalizedTime >= 0.15f && normalizedTime < 0.40f) transform.position = ToWorld(player.transform, new Vector2(0.016f * dir, -0.07f));
                else if (normalizedTime >= 0.40f && normalizedTime < 0.85f) transform.position = ToWorld(player.transform, new Vector2(-0.284f * dir, -0.102f));
                else transform.position = ToWorld(player.transform, new Vector2(0.562f * dir, -0.258f));
                transform.rotation = player.transform.rotation;
                break;
        }
    }

    private Vector3 ToWorld(Transform playerTransform, Vector2 offset)
    {
        Vector3 worldPos = playerTransform.TransformPoint(offset);
        return new Vector3(worldPos.x, worldPos.y, transform.position.z);
    }
}