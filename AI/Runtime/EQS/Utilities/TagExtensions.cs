using UnityEngine;

namespace Chris.AI.EQS
{
    internal static class ComponentExtensions
    {
        public static bool CompareTags(this Component target, string[] allowedTags)
        {
            if (target == null || allowedTags == null) return false;

            bool match = false;
            foreach (string tag in allowedTags)
            {
                if (target.CompareTag(tag)) match = true;
            }
            return match;
        }
    }
}