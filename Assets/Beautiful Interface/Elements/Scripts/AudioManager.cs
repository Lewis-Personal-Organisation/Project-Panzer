﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace Interface.Elements.Scripts
{
    /// <summary>
    /// Manages the UI audio in the game
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        private AudioSource audioComponent;
        public List<Sound> sounds;

        private static AudioManager _i;

        private void Awake()
        {
            audioComponent = GetComponent<AudioSource>();
            if (_i)
            {
                Destroy(gameObject);
            }
            else
            {
                _i = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        /// <summary>
        /// Play sound by referencing an AudioClip
        /// </summary>
        /// <param name="clip"></param>
        public static void Play(AudioClip clip)
        {
            if (!clip) return;
            
            if (!_i)
            {
                Debug.LogError("No AudioManager instance running");
                return;
            }

            _i.audioComponent.PlayOneShot(clip);
        }

        /// <summary>
        /// Play sound by searching for SoundEffect enum in sounds
        /// </summary>
        /// <param name="effect"></param>
        public static void Play(SoundEffects effect)
        {
            foreach (var sound in _i.sounds)
            {
                if (sound.Effect == effect)
                {
                    Play(sound.Clip);
                    return;
                }
            }
        }
    }

    [Serializable]
    public struct Sound
    {
        public SoundEffects Effect;
        public AudioClip Clip;
    }

    public enum SoundEffects
    {
        Success,
        Error,
        Hover,
        Click,
        Logout
    }
}