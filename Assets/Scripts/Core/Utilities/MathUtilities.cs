using UnityEngine;

namespace SurvivalGame.Core.Utilities
{
    public static class MathUtilities
    {
        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }

        public static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f)
                angle += 360f;
            if (angle > 360f)
                angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }

        public static float SmoothDampAngle(float current, float target, ref float currentVelocity, float smoothTime)
        {
            return Mathf.SmoothDampAngle(current, target, ref currentVelocity, smoothTime);
        }

        public static bool Approximately(float a, float b, float tolerance = 0.01f)
        {
            return Mathf.Abs(a - b) < tolerance;
        }

        public static int RoundToInt(float value)
        {
            return Mathf.RoundToInt(value);
        }

        public static float LerpAngle(float a, float b, float t)
        {
            return Mathf.LerpAngle(a, b, t);
        }
    }
}
