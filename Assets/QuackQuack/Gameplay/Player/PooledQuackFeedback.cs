using System;
using QuackQuack.Presentation.VFX;
using QuackQuack.Systems.Pooling;
using UnityEngine;

namespace QuackQuack.Gameplay.Player
{
    [RequireComponent(typeof(QuackFeedbackController))]
    public sealed class PooledQuackFeedback : MonoBehaviour, IPoolLifecycle
    {
        private PoolService pools;
        private Action release;
        public QuackFeedbackController View { get; private set; }
        public void Initialize(PoolService owner)
        {
            pools = owner; release = Release;
            View = GetComponent<QuackFeedbackController>();
            View.Initialize(GetComponent<LineRenderer>().sharedMaterial);
            View.ExternallyDriven = true;
        }
        private void Release() => pools.Release(this);
        public void OnAcquire() { View.Finished = release; }
        public void OnRelease() { }
        public void ResetState() { View.ResetEffect(); }
    }
}
