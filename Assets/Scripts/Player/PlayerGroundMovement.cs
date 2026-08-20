using UnityEngine;


/// <summary>
/// Handles the player's ground-movement math: turning raw horizontal input into a barrier-aware
/// velocity, and tracking grounded/airborne transitions to detect the exact moment the player
/// lands. This class doesn't touch the Rigidbody2D or AudioSource directly. It hands back plain
/// values (a velocity, a landing result) and lets PlayerStep decide what to do with them.
/// </summary>


public class PlayerGroundMovement
{
    /// <summary>
    /// Describes what kind of landing (if any) just happened, so the caller knows which landing sound to play.
    /// </summary>
    public enum LandingResult
    {
        /// <summary>The player didn't just land this frame.</summary>
        None,

        /// <summary>The player landed gently (low downward speed).</summary>
        Soft,

        /// <summary>The player landed hard (high downward speed), fast enough to warrant a heavier landing sound.</summary>
        Hard
    }




    /// <summary>The speed threshold (in units/second, negative = falling) above which a landing counts as "hard" instead of "soft".</summary>
    private const float HardLandingVelocityThreshold = -10f;




    /// <summary>Whether the player was grounded as of the last call to <see cref="UpdateLandingState"/>. Exposed so other states (like crawling) can force it when their own grounded concept doesn't apply.</summary>
    public bool WasGrounded { get; set; }




    /// <summary>
    /// Computes the player's horizontal run velocity, blocking movement in whichever direction the player is currently pressed against a barrier.
    /// </summary>
    /// <param name="dirX">Horizontal input direction, from -1 (left) to 1 (right).</param>
    /// <param name="horizontalSpeed">The player's horizontal movement speed.</param>
    /// <param name="currentVerticalVelocity">The Rigidbody2D's current vertical velocity, preserved unchanged in the result.</param>
    /// <param name="barrierContactDir">-1 if a barrier is blocking leftward movement, 1 if blocking rightward movement, 0 if neither.</param>
    /// <returns>The velocity to assign to the player's Rigidbody2D this frame.</returns>
    public Vector2 ComputeHorizontalVelocity(float dirX, float horizontalSpeed, float currentVerticalVelocity, int barrierContactDir)
    {
        float moveX = dirX * horizontalSpeed;

        if (barrierContactDir == 1 && moveX > 0) moveX = 0f;
        if (barrierContactDir == -1 && moveX < 0) moveX = 0f;

        return new Vector2(moveX, currentVerticalVelocity);
    }




    /// <summary>
    /// Call once per frame to detect the exact moment the player transitions from airborne to grounded while in normal movement, and updates <see cref="WasGrounded"/> as a side effect. Read the return value to decide which landing sound, if any, to play, this method doesn't play sounds itself.
    /// </summary>
    /// <param name="isGrounded">Whether the player is grounded this frame.</param>
    /// <param name="isInNormalMovementState">True only while the player is in its normal ground-movement state.</param>
    /// <param name="verticalVelocity">The Rigidbody2D's current vertical velocity, used to tell a hard landing from a soft one.</param>
    public LandingResult UpdateLandingState(bool isGrounded, bool isInNormalMovementState, float verticalVelocity)
    {
        LandingResult result = LandingResult.None;

        if (!WasGrounded && isGrounded && isInNormalMovementState)
        {
            result = verticalVelocity < HardLandingVelocityThreshold ? LandingResult.Hard : LandingResult.Soft;
        }

        WasGrounded = isGrounded;
        return result;
    }
}