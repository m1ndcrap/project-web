using UnityEngine;


/// <summary>
/// Computes the player's velocity while crawling along a surface. Crawling reuses the same
/// horizontal/vertical input depending on which surface the player is on, and needs to know which
/// way is forward along that surface relative to the player's current rotation, this class does
/// that translation. It does not handle corner-turning, surface-snapping, or blocking against
/// enemies/obstacles, those stay in PlayerStep since they depend on raycasts and other
/// player-specific state.
/// </summary>


public class PlayerCrawlMovement
{
    /// <summary>
    /// The result of a single crawl velocity computation.
    /// </summary>
    public readonly struct Result
    {
        /// <summary>The velocity to assign to the player's Rigidbody2D this frame, before any barrier or obstacle clamping.</summary>
        public readonly Vector2 Velocity;

        /// <summary>The signed crawl speed along the surface, positive/negative indicates direction, magnitude indicates speed.</summary>
        public readonly float CrawlDirection;

        public Result(Vector2 velocity, float crawlDirection)
        {
            Velocity = velocity;
            CrawlDirection = crawlDirection;
        }
    }




    /// <summary>
    /// Computes crawl velocity for the current frame.
    /// </summary>
    /// <param name="surfaceDirection">Which surface the player is on: 1 = floor, 2 = left wall, 3 = ceiling, 4 = right wall.</param>
    /// <param name="horizontalInput">Raw horizontal input axis.</param>
    /// <param name="verticalInput">Raw vertical input axis.</param>
    /// <param name="playerRight">The player's current "right" direction (transform.right), which rotates with the surface they're on.</param>
    /// <returns>The computed velocity and signed crawl direction for this frame.</returns>
    public Result ComputeVelocity(int surfaceDirection, float horizontalInput, float verticalInput, Vector2 playerRight)
    {
        // On the floor or ceiling, horizontal input moves the player; on a wall, vertical input does
        float rawInput = (surfaceDirection == 1 || surfaceDirection == 3) ? horizontalInput : verticalInput;
        Vector2 worldAxis = (surfaceDirection == 1 || surfaceDirection == 3) ? Vector2.right : Vector2.up;

        float rightAlignment = Vector2.Dot(playerRight, worldAxis);
        float crawlSign = rightAlignment >= 0f ? 1f : -1f;

        float crawlDirection = rawInput * crawlSign * 2.75f;
        Vector2 velocity = playerRight * crawlDirection;

        return new Result(velocity, crawlDirection);
    }
}