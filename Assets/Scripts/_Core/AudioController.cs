using UnityEngine;

namespace Framework.Core
{
    /// <summary>
    /// Plays one-shot sounds through a single AudioSource. Keeps the clip-array and
    /// Random.Range boilerplate in one place instead of repeating it at every call site.
    /// It has no idea what a "jump" or "hurt" sound is, it only knows how to play clips.
    /// </summary>
    public class AudioController
    {
        private readonly AudioSource audioSource;




        /// <summary>
        /// Creates a new controller that plays sounds through the given AudioSource.
        /// </summary>
        /// <param name="audioSource">The AudioSource this controller will call PlayOneShot on.</param>
        public AudioController(AudioSource audioSource)
        {
            this.audioSource = audioSource;
        }




        /// <summary>
        /// Plays one randomly chosen clip from <paramref name="clips"/> with equal probability. Always
        /// plays something (as long as at least one non-null clip is given).
        /// </summary>
        /// <param name="clips">The set of clips to choose from, e.g. a few variations of a footstep or impact sound.</param>
        public void PlayRandom(params AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return;
            audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
        }




        /// <summary>
        /// Picks one clip at random, but can also play nothing at all. There is a
        /// 1-in-(N+1) chance of silence, where N is the number of clips passed in.
        /// Used for sounds that would get repetitive if they fired on every single hit.
        /// </summary>
        /// <param name="clips">Clips to choose from. A "silent" outcome is added on top of these.</param>
        public void PlayRandomWithSilenceChance(params AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return;
            int index = Random.Range(0, clips.Length + 1);
            if (index < clips.Length) audioSource.PlayOneShot(clips[index]);
        }




        /// <summary>
        /// Plays a single specific clip, with no randomization.
        /// </summary>
        /// <param name="clip">The clip to play. Safe to call with null, does nothing in that case.</param>
        public void Play(AudioClip clip)
        {
            if (clip != null) audioSource.PlayOneShot(clip);
        }
    }
}