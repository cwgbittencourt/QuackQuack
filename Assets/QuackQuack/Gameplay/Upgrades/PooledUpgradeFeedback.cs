using System;
using QuackQuack.Presentation.VFX;
using QuackQuack.Systems.Pooling;
using UnityEngine;

namespace QuackQuack.Gameplay.Upgrades
{
    [RequireComponent(typeof(UpgradeFeedbackController))]
    public sealed class PooledUpgradeFeedback : MonoBehaviour, IPoolLifecycle
    {
        private PoolService pools;
        private Action release;
        public UpgradeFeedbackController View { get; private set; }
        public void Initialize(PoolService owner)
        { pools = owner; release = Release; View = GetComponent<UpgradeFeedbackController>(); View.Initialize(); }
        private void Release() => pools.Release(this);
        public void OnAcquire() { View.Finished = release; }
        public void OnRelease() { }
        public void ResetState() { View.ResetEffect(); }
    }
}
