using QuackQuack.Core.Progression;
using UnityEngine;

namespace QuackQuack.Gameplay.Pickups
{
    public interface IPickupAttractionView
    {
        bool IsHealing { get; }
        long ViewLeaseId { get; }
        RewardState ViewState { get; }
        Transform PickupTransform { get; }
        CircleCollider2D PickupCollider { get; }
        AttractionState Attraction { get; set; }
        bool TryMarkAttracted(Transform target);
        void CancelAttraction();
        void DiscardCapturedHealing();
        void CollectContact(Collider2D player);
    }
}
