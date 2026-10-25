using UnityEngine;

namespace QuackQuack.Content.Shared
{
    public abstract class ContentDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        public string Id => id;
        public string DisplayName => displayName;
        public virtual bool IsValid(out string reason)
        {
            reason = string.IsNullOrWhiteSpace(id) ? "Content ID is required." : null;
            return reason == null;
        }
    }
}
