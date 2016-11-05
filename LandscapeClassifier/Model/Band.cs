using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;

namespace LandscapeClassifier.Model
{
    public class Band
    {
        public readonly string ProjectionName;

        public readonly Matrix<double> ScreenToWorld;

        public readonly Matrix<double> WorldToScreen;

        public readonly Vector<double> UpperLeft;
        public readonly Vector<double> BottomRight;

        public Band(string projectionName, Matrix<double> screenToWorld, Vector<double> upperLeft, Vector<double> bottomRight)
        {
            ProjectionName = projectionName;
            ScreenToWorld = screenToWorld;
            UpperLeft = upperLeft;
            BottomRight = bottomRight;

            WorldToScreen = ScreenToWorld.Clone().Inverse();
        }
    }
}
