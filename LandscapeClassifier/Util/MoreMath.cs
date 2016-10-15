using System;

namespace LandscapeClassifier.Util
{
    public static class MoreMath
    {
        private const double Epsilon = 0.0001;

        public static bool AlmostEquals(double a, double b, double epsilon = Epsilon)
        {
            return Math.Abs(a - b) < epsilon;
        }

        public static bool AlmostZero(double v, double epsilon = Epsilon)
        {
            return AlmostEquals(v, 0, epsilon);
        }

        public static double ToDegrees(double rad)
        {
            return rad * (180.0 / Math.PI);
        }

        public static double Clamp(double value, double min, double max)
        {
            return Math.Max(Math.Min(value, max), min);
        }
    }
}
