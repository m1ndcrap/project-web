using UnityEngine;

namespace Framework.Core
{
    /// <summary>
    /// Wraps an Animator so gameplay code only sees the four things it actually uses:
    /// set the "mstate" parameter, play a clip, change speed, and read the current clip.
    /// Used by the player and every enemy type.
    /// </summary>
    public class AnimationDriver
    {
        private readonly Animator animator;

        /// <summary>
        /// Creates a new driver that controls the given Animator.
        /// </summary>
        /// <param name="animator">The Animator this driver will send state changes and play requests to.</param>
        public AnimationDriver(Animator animator)
        {
            this.animator = animator;
        }




        /// <summary>
        /// Gets or sets the Animator's playback speed. A value of 0 pauses the current animation in
        /// place (commonly used to freeze on a hit-stun or attack-wind-up frame); 1 is normal speed.
        /// </summary>
        public float Speed
        {
            get => animator.speed;
            set => animator.speed = value;
        }




        /// <summary>
        /// The state info for whatever clip is currently playing on the base layer (layer 0). Use
        /// .IsName("ClipName") to check which clip is active and .normalizedTime to check
        /// its playback progress, where 1.0 means the clip has finished one full loop.
        /// </summary>
        public AnimatorStateInfo CurrentState => animator.GetCurrentAnimatorStateInfo(0);




        /// <summary>
        /// Reads the current "mstate" value, for scripts that need to react to what the character is doing.
        /// </summary>
        public int GetMovementState()
        {
            return animator.GetInteger("mstate");
        }




        /// <summary>
        /// Sets the shared "mstate" integer parameter that drives this character's Animator
        /// Controller transitions.
        /// </summary>
        /// <param name="state">The integer value of the state to switch to (cast from a MovementState enum at the call site).</param>
        public void SetMovementState(int state)
        {
            animator.SetInteger("mstate", state);
        }




        /// <summary>
        /// Immediately plays a clip on the base layer, starting at a specific point in its timeline
        /// instead of always restarting from the beginning.
        /// </summary>
        /// <param name="clipName">The name of the animation state/clip to play.</param>
        /// <param name="normalizedStartTime">Where in the clip to start playback, from 0 (beginning) to 1 (end).</param>
        public void Play(string clipName, float normalizedStartTime)
        {
            animator.Play(clipName, 0, normalizedStartTime);
        }
    }

}