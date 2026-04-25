using UnityEngine;

namespace SurvivalGame.Core.Utilities
{
    public static class TransformExtensions
    {
        public static void DestroyChildren(this Transform transform)
        {
            foreach (Transform child in transform)
            {
                Object.Destroy(child.gameObject);
            }
        }

        public static void ResetTransform(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        public static void LookAtY(this Transform transform, Vector3 target)
        {
            Vector3 direction = target - transform.position;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        public static Vector3 DirectionTo(this Transform transform, Transform target)
        {
            return (target.position - transform.position).normalized;
        }

        public static float DistanceTo(this Transform transform, Transform target)
        {
            return Vector3.Distance(transform.position, target.position);
        }
    }
}
