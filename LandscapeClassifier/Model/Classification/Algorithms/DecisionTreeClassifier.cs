using System;
using System.Linq;
using System.Threading.Tasks;
using Accord.MachineLearning;
using Accord.MachineLearning.Boosting;
using Accord.MachineLearning.Boosting.Learners;
using Accord.MachineLearning.DecisionTrees;
using Accord.MachineLearning.DecisionTrees.Learning;
using Accord.Statistics.Analysis;
using LandscapeClassifier.Model.Classification.Boosting;
using LandscapeClassifier.Util;

namespace LandscapeClassifier.Model.Classification.Algorithms
{
    public class DecisionTreeClassifier : AbstractLandCoverClassifier
    {
        private IEither<DecisionTree, Boost<Weak<DecisionTree>>> _tree;

        public bool Boosting { get; set; }
        public int Iterations { get; set; } = 5;

        public override Task TrainAsync(ClassificationModel classificationModel)
        {
            int numFeatures = classificationModel.ClassifiedFeatureVectors.Count;
            DecisionVariable[]  decisionVariables = classificationModel.Bands.Select(b => DecisionVariable.Continuous(b.ToString())).ToArray();

            double[][] input = new double[numFeatures][];
            int[] responses = new int[numFeatures];
            
            for (int featureIndex = 0;
                featureIndex < classificationModel.ClassifiedFeatureVectors.Count;
                ++featureIndex)
            {
                var featureVector = classificationModel.ClassifiedFeatureVectors[featureIndex];
                input[featureIndex] = Array.ConvertAll(featureVector.FeatureVector.BandIntensities, s => (double)s / ushort.MaxValue);
                responses[featureIndex] = (int) featureVector.Type;
            }

            if (Boosting)
            {
                return Task.Factory.StartNew(() =>
                {
                    
                    var classifier = new Boost<Weak<DecisionTree>>();

                    var teacher = new AdaBoostM1<Weak<DecisionTree>>(classifier)
                    {
                        Creation = (weights) =>
                        {
                            var tree = new DecisionTree(decisionVariables, Enum.GetValues(typeof(LandcoverType)).Length);
                            var c45Learning = new C45Learning(tree);
                            c45Learning.Learn(input, responses, weights);
                            return new Weak<DecisionTree>(tree, (s, x) => s.Decide(x));
                        },

                        Iterations = Iterations,
                        Tolerance = 1e-2
                    };

                    teacher.Run(input, responses);
                    _tree = Either.Right<DecisionTree, Boost<Weak<DecisionTree>>>(classifier);
                   

                });
            }
            else
            {
                return Task.Factory.StartNew(() =>
                {
                    var tree = new DecisionTree(decisionVariables, Enum.GetValues(typeof(LandcoverType)).Length);
                    C45Learning id3Learning = new C45Learning(tree);
                    id3Learning.Learn(input, responses);

                    _tree = Either.Left<DecisionTree, Boost<Weak<DecisionTree>>>(tree);
                });
            }
        }

        
        public override LandcoverType Predict(FeatureVector feature)
        {
            var features = Array.ConvertAll(feature.BandIntensities, s => (double) s/ushort.MaxValue);
            return (LandcoverType)_tree.Case(l => l.Decide(features), r => r.Compute(features));
        }

        public override double Probabilty(FeatureVector feature)
        {
            return 0.0;
        }

        public override double Probabilty(FeatureVector feature, int classIndex)
        {
            return 0.0;
        }

        public override int[] Predict(double[][] features)
        {

            return _tree.Case(l => l.Decide(features), r =>
            {
                int[] actual = new int[features.Length];
                for (int i = 0; i < actual.Length; i++)
                    actual[i] = r.Compute(features[i]);
                return actual;
            });
        }

        public override double[] Probability(double[][] features)
        {
            return new double[0];
        }

        public override double[][] Probabilities(double[][] features)
        {
            return new double[0][];
        }

        public override Task<GridSearchParameterCollection> GridSearchAsync(ClassificationModel classificationModel)
        {
            throw new NotImplementedException();
        }

        public override Task<GeneralConfusionMatrix> ComputeConfusionMatrixAsync(ClassificationModel classificationModel)
        {
            throw new NotImplementedException();
        }
    }
}
