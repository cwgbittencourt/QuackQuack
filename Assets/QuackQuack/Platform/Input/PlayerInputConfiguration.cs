using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QuackQuack.Platform.Input
{
    public sealed class PlayerInputConfiguration : ScriptableObject
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private GameObject mobileControlsPrefab;
        public InputActionAsset Actions => actions;
        public GameObject MobileControlsPrefab => mobileControlsPrefab;
        public static PlayerInputConfiguration Load()
        {
            var configuration = Resources.Load<PlayerInputConfiguration>("PlayerInput");
            if (configuration == null || configuration.actions == null || configuration.mobileControlsPrefab == null)
                throw new InvalidOperationException("Authored PlayerInput configuration is missing.");
            return configuration;
        }
    }
}
