using UnityEngine;


/// <summary>
/// Simulates the player's rope as a simple pendulum and uses gravity-driven angular acceleration
/// plus player input that can push the swing or reel the rope in and out. This class only does
/// the math. It doesn't move the Rigidbody2D, draw anything, or know what state the player is in.
/// PlayerStep reads <see cref="Position"/> each physics step and decides what to do with it.
/// </summary>


public class PlayerRopePhysics
{
    /// <summary>Gravity-like constant driving the pendulum's swing acceleration. Negative values are expected (matches the original hand-tuned feel).</summary>
    private readonly float accelerationRate;


    /// <summary>The world-space point the rope is currently anchored to.</summary>
    public Vector2 GrapplePoint { get; private set; }


    /// <summary>The current simulated position of the player end of the rope (the pendulum bob).</summary>
    public Vector2 Position { get; private set; }


    /// <summary>The rope's current swing angle, in degrees, measured from the grapple point.</summary>
    public float Angle { get; private set; }


    /// <summary>The rope's current angular velocity, in degrees per step. Positive/negative indicates swing direction.</summary>
    public float AngularVelocity { get; private set; }


    /// <summary>The current distance from the grapple point to the bob.</summary>
    public float Length { get; private set; }




    /// <summary>
    /// Creates a new rope physics simulation.
    /// </summary>
    /// <param name="accelerationRate">Gravity-like constant driving the pendulum's swing acceleration. Negative values are expected (matches the original hand-tuned feel).</param>
    public PlayerRopePhysics(float accelerationRate)
    {
        this.accelerationRate = accelerationRate;
    }




    /// <summary>
    /// Attaches the rope to a new anchor point, starting a fresh swing from the player's current position.
    /// </summary>
    /// <param name="grapplePoint">The world-space point to anchor the rope to.</param>
    /// <param name="startPosition">The player's current position, used as the initial bob position.</param>
    public void Attach(Vector2 grapplePoint, Vector2 startPosition)
    {
        GrapplePoint = grapplePoint;
        Position = startPosition;
        AngularVelocity = 0f;
        Angle = Mathf.Atan2(Position.y - GrapplePoint.y, Position.x - GrapplePoint.x) * Mathf.Rad2Deg;
        Length = Vector2.Distance(GrapplePoint, Position);
    }




    /// <summary>
    /// Moves the anchor point without resetting the rest of the swing state. Used to track an
    /// anchor that moves while the player is already attached to it (for example, a glider).
    /// </summary>
    /// <param name="grapplePoint">The anchor's new world-space position.</param>
    public void SetGrapplePoint(Vector2 grapplePoint)
    {
        GrapplePoint = grapplePoint;
    }




    /// <summary>
    /// Advances the pendulum simulation by one frame: applies gravity-driven angular acceleration,
    /// lets horizontal input add a push to that acceleration, and lets vertical input reel the rope
    /// in or out. Updates <see cref="Angle"/>, <see cref="AngularVelocity"/>, <see cref="Length"/>,
    /// and <see cref="Position"/>.
    /// </summary>
    /// <param name="horizontalInput">Horizontal input axis, from -1 to 1, adds a push to the swing's angular acceleration.</param>
    /// <param name="verticalInput">Vertical input axis (already sign-flipped by the caller), reels the rope shorter or longer.</param>
    public void Step(float horizontalInput, float verticalInput)
    {
        float angleAcceleration = accelerationRate * Mathf.Cos(Angle * Mathf.Deg2Rad);
        angleAcceleration += horizontalInput * 0.04f;

        Length += verticalInput * 0.01f;
        Length = Mathf.Max(Length, 0f);

        AngularVelocity += angleAcceleration;
        Angle += AngularVelocity;
        AngularVelocity *= 0.99f;

        Position = new Vector2(
            GrapplePoint.x + Mathf.Cos(Angle * Mathf.Deg2Rad) * Length,
            GrapplePoint.y + Mathf.Sin(Angle * Mathf.Deg2Rad) * Length
        );
    }




    /// <summary>
    /// Immediately zeroes out angular velocity, e.g. right before forcibly ending the swing.
    /// </summary>
    public void StopSpinning()
    {
        AngularVelocity = 0f;
    }
}