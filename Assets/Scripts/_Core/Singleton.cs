using UnityEngine;

namespace Framework.Core
{
    /// <summary>
    /// Generic base class that turns any <see cref="MonoBehaviour"/> into a single, globally
    /// accessible instance. Inherit from this instead of hand-writing your own
    /// "private static X instance" pattern on every manager you create.
    /// </summary>
    /// <typeparam name="T">The concrete MonoBehaviour type that should be a singleton.</typeparam>
    /// <example>
    /// <code>
    /// public class AudioManager : Singleton&lt;AudioManager&gt;
    /// {
    ///     public void PlaySound(AudioClip clip) { ... }
    /// }
    ///
    /// // Anywhere else in the project:
    /// AudioManager.Instance.PlaySound(myClip);
    /// </code>
    /// </example>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static bool _isQuitting;


        [Header("Singleton Settings")]
        [Tooltip("If enabled, this object survives scene loads and only one instance will ever exist " + "for the lifetime of the application. Disable this for singletons that are meant to " + "be scene-specific (for example, a level-only manager).")]
        [SerializeField] private bool persistAcrossScenes = true;




        /// <summary>
        /// Whether this singleton survives scene loads. Defaults to the inspector checkbox above.
        /// Override it in a subclass to force an answer in code, which is what you want when the
        /// object holds references to other objects in its own scene: those get destroyed on load,
        /// and a surviving singleton would be left pointing at nothing.
        /// </summary>
        protected virtual bool ShouldPersistAcrossScenes => persistAcrossScenes;




        /// <summary>
        /// The single active instance of <typeparamref name="T"/>. Locates the instance automatically
        /// if it hasn't been cached yet. Returns <c>null</c> while the application is shutting down so
        /// that other objects' <c>OnDestroy</c> methods don't accidentally recreate it.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_isQuitting)
                {
                    return null;
                }

                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                }

                return _instance;
            }
        }




        /// <summary>
        /// Registers this object as the instance, or destroys itself if one already exists.
        /// Do not override this in a subclass. Override OnAwake() instead so this setup always runs first.
        /// </summary>
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                // A persistent singleton survives scene loads, so every scene that also has its own
                // copy will hand us a duplicate on load. That is the normal setup, so destroy it
                // quietly. Only a duplicate inside a single scene is an actual mistake worth warning about.
                if (!ShouldPersistAcrossScenes)
                {
                    Debug.LogWarning($"[Singleton] A second instance of '{typeof(T).Name}' was found on " + $"'{gameObject.name}' and will be destroyed. Only one should exist in the scene.", this);
                }

                Destroy(gameObject);
                return;
            }

            _instance = this as T;

            if (ShouldPersistAcrossScenes)
            {
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }

            OnAwake();
        }




        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }




        /// <summary>
        /// Called once, immediately after this instance has been registered as the singleton.
        /// Override this in derived classes for your own initialization logic instead of using Awake().
        /// </summary>
        protected virtual void OnAwake()
        {
        }
    }
}