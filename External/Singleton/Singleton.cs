﻿using UnityEngine;

namespace Custom.Singleton
{
    ///
    /// Provides static access to MonoBehaviour of type <typeparam name="T">
    /// Creates persistent game object if none is present
    ///
    /// Use <see cref="destroyed"> to protect against unwanted instantiations
    /// Like when accessing <see cref="instance"> inside OnDisable method
    ///
    /// Please note that auto created instances will be marked DontDestroyOnLoad
    /// Thus consequently be placed into the very same named scene
    ///
    /// Intended to be used like
    /// public class ClassName : AutoInstanceMonoBehaviour<ClassName> { ... }
    ///
    public abstract class AutoInstanceMonoBehaviour<T> : MonoBehaviour
        where T : AutoInstanceMonoBehaviour<T>
    {
        //
        // API
        //

        public static bool destroyed { get; private set; }

        private static T _instance;

        public static T instance
        {
            get
            {
                if (_instance)
                {
                    return _instance;
                }

                _instance = GetAutoMonoBehaviour(dontDestroyOnLoad: true);
                destroyed = false;
                return _instance;
            }
        }

        //
        // Callbacks from Unity
        //

        protected void Awake()
        {
            if (!_instance || destroyed)
            {
                _instance = (T)this;
                destroyed = false;
            }
        }

        protected void OnDestroy()
        {
            if (ReferenceEquals(this, _instance))
            {
                destroyed = true;
            }
        }

        //
        //
        //

        public static T GetAutoMonoBehaviour(bool dontDestroyOnLoad = false)
        {
            var result = FindObjectOfType<T>();
            if (!result)
            {
                result = new GameObject(
                    $"Auto instance: {typeof(T).Name}"
                ).AddComponent<T>();
            }

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(result.gameObject);
            }

            return result;
        }
    }
}