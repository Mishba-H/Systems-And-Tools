using UnityEngine;

namespace Sciphone
{
    public static class Extensions
    {
        public static Vector3 With(this Vector3 value, float? x = null, float? y = null, float? z = null, float? scale = null)
        {
            value.x = x ?? value.x;
            value.y = y ?? value.y;
            value.z = z ?? value.z;

            if (scale != null)
                value = (float)scale * value.normalized;

            return value;
        }
        public static Vector2 With(this Vector2 value, float? x = null, float? y = null)
        {
            value.x = x ?? value.x;
            value.y = y ?? value.y;

            return value;
        }
        public static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            return angle;
        }
    }
}
