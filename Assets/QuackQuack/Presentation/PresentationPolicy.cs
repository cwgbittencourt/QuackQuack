using QuackQuack.Core.Combat;

namespace QuackQuack.Presentation
{
    public static class PresentationPolicy
    {
        public static bool CanReplace(EventPriority current, EventPriority incoming) => incoming >= current;
    }
}
