using UnityEngine;

/// <summary>
/// Reads the keyboard once per frame and exposes it as named properties.
/// This is the only player script that touches Unity's Input class directly, so swapping
/// to a gamepad or adding rebindable keys later only means editing this one file.
/// </summary>
public class PlayerInputReader : MonoBehaviour
{
    /// <summary>
    /// Raw horizontal input axis, from -1 (left) to 1 (right). Matches Unity's "Horizontal" input axis.
    /// </summary>
    public float Horizontal { get; private set; }




    /// <summary>
    /// Raw vertical input axis, from -1 (down) to 1 (up). Matches Unity's "Vertical" input axis.
    /// </summary>
    public float Vertical { get; private set; }




    /// <summary>
    /// True while any of the four movement keys (WASD or the arrow keys) are held down.
    /// </summary>
    public bool IsMovementKeyHeld { get; private set; }




    /// <summary>
    /// True while any key at all is held down, movement keys included. Combine with
    /// <see cref="IsMovementKeyHeld"/> (IsAnyKeyHeld and !IsMovementKeyHeld) to detect
    /// "a non-movement key is currently being pressed."
    /// </summary>
    public bool IsAnyKeyHeld { get; private set; }




    /// <summary>
    /// True on the single frame the Jump button (Space) is pressed down. Used to jump, attach to a
    /// swing point, or attach to a quick-zip point, depending on the player's current state.
    /// </summary>
    public bool JumpPressed { get; private set; }




    /// <summary>
    /// True on the single frame the Jump button (Space) is released. Used to detect the player letting go of Space while mid-swing.
    /// </summary>
    public bool JumpReleased { get; private set; }




    /// <summary>
    /// True on the single frame the Quick-Zip button (I) is pressed down.
    /// </summary>
    public bool QuickZipPressed { get; private set; }




    /// <summary>
    /// True while the Web-Shoot / Zip-Aim button (U) is held down.
    /// </summary>
    public bool ShootHeld { get; private set; }




    /// <summary>
    /// True on the single frame the Web-Shoot / Zip-Aim button (U) is released.
    /// </summary>
    public bool ShootReleased { get; private set; }




    /// <summary>
    /// True while the Attack button (O) is held down.
    /// </summary>
    public bool AttackHeld { get; private set; }




    /// <summary>
    /// True on the single frame the Attack button (O) is pressed down. Used for context where a single tap should fire once, such as a crawl kick.
    /// </summary>
    public bool AttackPressed { get; private set; }




    /// <summary>
    /// True while the Uppercut button (L) is held down.
    /// </summary>
    public bool UppercutHeld { get; private set; }




    /// <summary>
    /// True while the Counter button (P) is held down.
    /// </summary>
    public bool CounterHeld { get; private set; }




    /// <summary>
    /// Polls every tracked input for the current frame. Call this once, at the very top of any
    /// consumer's Update() method, before reading any of the properties above. This is a
    /// deliberate design choice: relying on Unity's own Update() call order between two
    /// different components is fragile, so this reader only refreshes when explicitly told to.
    /// </summary>
    public void RefreshInput()
    {
        if (Time.timeScale <= 0f)
        {
            Horizontal = 0f;
            Vertical = 0f;
            IsMovementKeyHeld = false;
            IsAnyKeyHeld = false;
            JumpPressed = false;
            JumpReleased = false;
            QuickZipPressed = false;
            ShootHeld = false;
            ShootReleased = false;
            AttackHeld = false;
            AttackPressed = false;
            UppercutHeld = false;
            CounterHeld = false;
            return;
        }

        Horizontal = Input.GetAxisRaw("Horizontal");
        Vertical = Input.GetAxisRaw("Vertical");

        IsMovementKeyHeld = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D)
                          || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);
        IsAnyKeyHeld = Input.anyKey;

        JumpPressed = Input.GetKeyDown(KeyCode.Space);
        JumpReleased = Input.GetKeyUp(KeyCode.Space);

        QuickZipPressed = Input.GetKeyDown(KeyCode.I);

        ShootHeld = Input.GetKey(KeyCode.U);
        ShootReleased = Input.GetKeyUp(KeyCode.U);

        AttackHeld = Input.GetKey(KeyCode.O);
        AttackPressed = Input.GetKeyDown(KeyCode.O);

        UppercutHeld = Input.GetKey(KeyCode.L);
        CounterHeld = Input.GetKey(KeyCode.P);
    }
}