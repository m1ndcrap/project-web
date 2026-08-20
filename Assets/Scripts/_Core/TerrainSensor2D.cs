using UnityEngine;

namespace Framework.Core
{
    /// <summary>
    /// Pure environmental sensing for a ground-based character: what's directly ahead, whether the
    /// ground continues, and whether a hazard is nearby. Every method here only answers "what is
    /// true about the world right now", none of them change any state or make a decision about what
    /// to do with the answer. That decision-making stays with whichever character is asking.
    /// </summary>
    public class TerrainSensor2D
    {
        private static readonly string[] HazardTags = { "Wires", "Lightning", "OneHitHazard", "Hydrant" };




        private readonly Rigidbody2D rb;
        private readonly Collider2D coll;
        private readonly LayerMask groundMask;




        /// <summary>
        /// Creates a new sensor for the given character.
        /// </summary>
        /// <param name="rb">The character's Rigidbody2D, used as the origin point for most checks.</param>
        /// <param name="coll">The character's own collider, used for overlap checks against its current footprint.</param>
        /// <param name="groundMask">Which layers count as solid ground/walls for these checks.</param>
        public TerrainSensor2D(Rigidbody2D rb, Collider2D coll, LayerMask groundMask)
        {
            this.rb = rb;
            this.coll = coll;
            this.groundMask = groundMask;
        }




        /// <summary>
        /// True if the given collider belongs to one of the recognized hazard types (wires, lightning, a one-hit hazard, or a hydrant).
        /// </summary>
        public bool IsHazardTag(Collider2D c)
        {
            foreach (var tag in HazardTags)
            {
                if (c.CompareTag(tag)) return true;
            }

            return false;
        }




        /// <summary>
        /// True if a hazard overlaps a box extending out from the character in the given direction.
        /// </summary>
        /// <param name="dir">Direction to check: negative is left, positive is right.</param>
        /// <param name="checkDistance">How far out the check box extends.</param>
        /// <param name="heightTolerance">The check box's height.</param>
        public bool IsHazardAhead(float dir, float checkDistance = 2.5f, float heightTolerance = 1f)
        {
            Vector2 origin = rb.position;
            Vector2 boxSize = new Vector2(checkDistance, heightTolerance);
            Vector2 center = origin + new Vector2(dir * checkDistance * 0.5f, 0f);

            Collider2D[] hits = Physics2D.OverlapBoxAll(center, boxSize, 0f);

            foreach (var hit in hits)
            {
                if (IsHazardTag(hit))
                    return true;
            }

            return false;
        }




        /// <summary>
        /// Finds a hazard collider the character is currently standing inside of, or null if there isn't one.
        /// </summary>
        public Collider2D GetOverlappingHazard()
        {
            Collider2D[] hits = Physics2D.OverlapBoxAll(coll.bounds.center, coll.bounds.size, 0f);

            foreach (var hit in hits)
            {
                if (IsHazardTag(hit))
                    return hit;
            }

            return null;
        }




        /// <summary>
        /// True if the ground continues at least <paramref name="aheadDist"/> in front of the character in the given direction (i.e. there's no ledge to walk off).
        /// </summary>
        /// <param name="dir">Direction to check: negative is left, positive is right.</param>
        /// <param name="aheadDist">How far ahead of the character to probe.</param>
        /// <param name="castDist">How far downward the probe raycasts looking for ground.</param>
        public bool IsGroundAhead(float dir, float aheadDist, float castDist = 1f)
        {
            Vector2 probeOrigin = new Vector2(rb.position.x + dir * aheadDist, coll.bounds.min.y + 0.05f);
            return Physics2D.Raycast(probeOrigin, Vector2.down, castDist, groundMask).collider != null;
        }




        /// <summary>
        /// True if solid ground-layer geometry (a wall) blocks the given direction within range.
        /// </summary>
        /// <param name="dir">Direction to check: negative is left, positive is right.</param>
        /// <param name="checkDistance">How far to check.</param>
        public bool IsWallAhead(float dir, float checkDistance = 0.4f)
        {
            return Physics2D.Raycast(rb.position, new Vector2(dir, 0f), checkDistance, groundMask).collider != null;
        }




        /// <summary>
        /// True if a solid-looking but non-ground-layer object blocks the given direction. A car
        /// that hasn't broken yet, or a door that hasn't opened yet. These only count as blocking
        /// while intact/closed; a broken car or an open door doesn't obstruct.
        /// </summary>
        /// <param name="dir">Direction to check: negative is left, positive is right.</param>
        /// <param name="checkDistance">How far out the check box extends.</param>
        /// <param name="heightTolerance">The check box's height.</param>
        public bool IsBlockingObjectAhead(float dir, float checkDistance = 1f, float heightTolerance = 1f)
        {
            Vector2 origin = rb.position;
            Vector2 boxSize = new Vector2(checkDistance, heightTolerance);
            Vector2 center = origin + new Vector2(dir * checkDistance * 0.5f, 0f);

            Collider2D[] hits = Physics2D.OverlapBoxAll(center, boxSize, 0f);

            foreach (var hit in hits)
            {
                if (hit.CompareTag("Car"))
                {
                    Animator carAnim = hit.GetComponent<Animator>();

                    if (carAnim != null && carAnim.GetCurrentAnimatorStateInfo(0).IsName("CarNormal"))
                        return true;
                }
                else if (hit.CompareTag("Door"))
                {
                    BreakableDoor door = hit.GetComponent<BreakableDoor>();

                    if (door != null && door.phase == 0)
                        return true;
                }
                else if (hit.CompareTag("RedKeyDoor") || hit.CompareTag("BlueKeyDoor") || hit.CompareTag("YellowKeyDoor"))
                {
                    KeyDoors keyDoor = hit.GetComponent<KeyDoors>();

                    if (keyDoor != null && keyDoor.phase == 0)
                        return true;
                }
            }

            return false;
        }




        /// <summary>
        /// True if a direction is clear enough to safely move or evade into: no hazard, no blocking object, and solid ground continues for at least 3 units.
        /// </summary>
        /// <param name="dir">Direction to check: negative is left, positive is right.</param>
        public bool IsDirectionSafeToEvade(float dir)
        {
            if (IsHazardAhead(dir)) return false;
            if (IsBlockingObjectAhead(dir)) return false;
            if (!IsGroundAhead(dir, 3f)) return false;
            return true;
        }
    }
}