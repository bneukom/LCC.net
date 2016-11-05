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

        public readonly Matrix<double> Transform;

        public readonly Matrix<double> InverseTransform;

        public readonly Vector<double> UpperLeft;
        public readonly Vector<double> BottomRight;

        public Band(string projectionName, Matrix<double> transform, Vector<double> upperLeft, Vector<double> bottomRight)
        {
            ProjectionName = projectionName;
            Transform = transform;
            UpperLeft = upperLeft;
            BottomRight = bottomRight;

            InverseTransform = Transform.Clone().Inverse();
        }
    }
}
