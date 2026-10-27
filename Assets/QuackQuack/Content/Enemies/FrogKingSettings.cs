using UnityEngine;

namespace QuackQuack.Content.Enemies
{
    [CreateAssetMenu(menuName = "QuackQuack/Frog King Settings")]
    public sealed class FrogKingSettings : ScriptableObject
    {
        public float Health = 1000;
        [Range(0, 1)] public float SummonPhaseHealth = 0.7f;
        [Range(0, 1)] public float DestructionPhaseHealth = 0.35f;
        public float TelegraphSeconds = 1;
        public float RecoverySeconds = 1.5f;
        public float JumpRadius = 2;
        public float JumpDamage = 18;
        public float WaveSpeed = 4;
        public float WaveWidth = 0.4f;
        public float WaveLifetime = 3;
        public float WaveDamage = 12;
        public int TadpoleCount = 4;
        public float TadpoleSpeed = 2;
        public float TadpoleRadius = 0.3f;
        public float TadpoleLifetime = 10;
        public float TadpoleDamage = 6;
        public int HazardCapacity = 24;
        public GameObject HazardPrefab;
        public AudioClip WarningAudio;
        public Material MarkerMaterial;
        public bool IsValid(out string reason)
        {
            reason = "Invalid Frog King settings.";
            float[] positive = { Health, TelegraphSeconds, RecoverySeconds, JumpRadius, JumpDamage,
                WaveSpeed, WaveWidth, WaveLifetime, WaveDamage, TadpoleSpeed, TadpoleRadius, TadpoleLifetime, TadpoleDamage };
            foreach (float value in positive) if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0) return false;
            if (!(DestructionPhaseHealth > 0 && DestructionPhaseHealth < SummonPhaseHealth && SummonPhaseHealth < 1)
                || TadpoleCount < 1 || TadpoleCount > HazardCapacity || HazardCapacity > 128 || HazardCapacity < 1
                || HazardPrefab == null || HazardPrefab.GetComponent<QuackQuack.Gameplay.Bosses.FrogKingHazard>() == null || WarningAudio == null || WarningAudio.samples <= 0 || MarkerMaterial == null) return false;
            reason = null; return true;
        }
    }
}
