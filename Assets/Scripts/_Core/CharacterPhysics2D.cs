using UnityEngine;

namespace Framework.Core
{
    /// <summary>
    /// Small, stateless physics queries shared by every character controller in the project (the
    /// player and each enemy type). Kept here so a tuning change only has to happen in one place.
    /// </summary>
    public static class CharacterPhysics2D
    {
        /// <summary>
        /// Checks whether a character is currently standing on the ground, by box-casting the
        /// character's own collider bounds a short distance downward.
        /// </summary>
        /// <param name="characterCollider">The character's own collider. Its bounds are used as the box-cast shape.</param>
        /// <param name="groundMask">Which layers count as ground.</param>
        /// <param name="checkDistance">How far below the collider to check.</param>
        /// <returns>True if the box-cast hits something on the ground layer within range.</returns>
        public static bool IsGrounded(Collider2D characterCollider, LayerMask groundMask, float checkDistance = 0.1f)
        {
            return Physics2D.BoxCast(characterCollider.bounds.center, characterCollider.bounds.size, 0f, Vector2.down, checkDistance, groundMask);
        }




        /// <summary>
        /// Nudges a character a small step sideways, but only if nothing solid is immediately in the
        /// way, used to gently separate two characters standing on top of each other without pushing
        /// one through a wall.
        /// </summary>
        /// <param name="rb">The Rigidbody2D to nudge.</param>
        /// <param name="direction">Which way to push: a negative value nudges left, positive nudges right.</param>
        /// <param name="obstacleMask">Which layers block the nudge.</param>
        /// <param name="checkDistance">How far ahead to check for a blocking obstacle before nudging.</param>
        /// <param name="nudgeAmount">How far to move the character when the nudge isn't blocked.</param>
        public static void NudgeAwayFromOverlap(Rigidbody2D rb, float direction, LayerMask obstacleMask, float checkDistance = 0.15f, float nudgeAmount = 0.02f)
        {
            Vector2 push = new Vector2(direction, 0f);

            if (Physics2D.Raycast(rb.position, push, checkDistance, obstacleMask).collider == null)
            {
                rb.position += push * nudgeAmount;
            }
        }
    }
}