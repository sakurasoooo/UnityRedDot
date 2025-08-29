using System;
using UnityEngine;

namespace RedDotSystem
{
    /// <summary>
    /// A MonoBehaviour component to attach to UI GameObjects.
    /// It listens to a specific red dot path and toggles a target GameObject.
    /// </summary>
    public class RedDotView : MonoBehaviour
    {
        [Tooltip("The path this view listens to (e.g., 'Mail/System').")]
        public string redDotPath;

        [Tooltip("The GameObject to show/hide as the red dot indicator.")]
        public GameObject redDotObject;
        
        [Tooltip("红点视图配置")]
        public RedDotSetting config;
        
        private void OnValidate()
        {
            if (config == null)
            {
                 HLogger.LogWarning($"红点视图配置未分配: {gameObject.name}", this);
            }
        }

        void OnEnable()
        {
            if (string.IsNullOrEmpty(redDotPath) || redDotObject == null) return;
            RedDotManager.Instance.Register(redDotPath, OnRedDotStateChange);
        }

        void OnDisable()
        {
            if (string.IsNullOrEmpty(redDotPath) || redDotObject == null) return;
            // Use a try-catch in case the manager is already destroyed on application quit
            try
            {
                RedDotManager.Instance.Unregister(redDotPath, OnRedDotStateChange);
            }
            catch (Exception)
            {
            }
        }

        private void OnRedDotStateChange(bool isActive)
        {
            if (redDotObject != null && redDotObject.activeSelf != isActive)
            {
                redDotObject.SetActive(isActive);
            }
        }
    }
}