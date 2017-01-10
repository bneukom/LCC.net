using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.MachineLearning.Boosting;
using Accord.Math;

namespace LandscapeClassifier.Model.Classification.Boosting
{
    /// <summary>
    ///   AdaBoost learning algorithm.
    /// </summary>
    /// 
    /// <typeparam name="TModel">The type of the model to be trained.</typeparam>
    /// 
    public class AdaBoostM1<TModel> where TModel : IWeakClassifier
    {

        Boost<TModel> classifier;
        RelativeConvergence convergence;

        double threshold = 0.5;

        /// <summary>
        ///   Initializes a new instance of the <see cref="AdaBoostM1{TModel}"/> class.
        /// </summary>
        /// 
        /// <param name="model">The model to be learned.</param>
        /// 
        public AdaBoostM1(Boost<TModel> model)
        {
            this.classifier = model;
            this.convergence = new RelativeConvergence();
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="AdaBoostM1{TModel}"/> class.
        /// </summary>
        /// 
        /// <param name="model">The model to be learned.</param>
        /// <param name="creationFunction">The model fitting function.</param>
        /// 
        public AdaBoostM1(Boost<TModel> model, ModelConstructor<TModel> creationFunction)
        {
            this.classifier = model;
            this.Creation = creationFunction;
            this.convergence = new RelativeConvergence();
        }

        /// <summary>
        ///   Gets or sets the maximum number of iterations
        ///   performed by the learning algorithm.
        /// </summary>
        /// 
        public int Iterations
        {
            get { return convergence.Iterations; }
            set { convergence.Iterations = value; }
        }

        /// <summary>
        ///   Gets or sets the relative tolerance used to 
        ///   detect convergence of the learning algorithm.
        /// </summary>
        /// 
        public double Tolerance
        {
            get { return convergence.Tolerance; }
            set { convergence.Tolerance = value; }
        }

        /// <summary>
        ///   Gets or sets the error limit before learning stops. Default is 0.5.
        /// </summary>
        /// 
        public double Threshold
        {
            get { return threshold; }
            set { threshold = value; }
        }

        /// <summary>
        ///   Gets or sets the fitting function which creates
        ///   and trains a model given a weighted data set.
        /// </summary>
        /// 
        public ModelConstructor<TModel> Creation { get; set; }

        /// <summary>
        ///   Runs the learning algorithm.
        /// </summary>
        /// 
        /// <param name="inputs">The input samples.</param>
        /// <param name="outputs">The corresponding output labels.</param>
        /// 
        /// <returns>The classifier error.</returns>
        /// 
        public double Run(double[][] inputs, int[] outputs)
        {
            double[] weights = new double[inputs.Length];
            for (int i = 0; i < weights.Length; i++)
                weights[i] = 1.0 / weights.Length;
            return run(inputs, outputs, weights);
        }

        /// <summary>
        ///   Runs the learning algorithm.
        /// </summary>
        /// 
        /// <param name="inputs">The input samples.</param>
        /// <param name="outputs">The corresponding output labels.</param>
        /// <param name="sampleWeights">The weights for each of the samples.</param>
        /// 
        /// <returns>The classifier error.</returns>
        /// 
        public double Run(double[][] inputs, int[] outputs, double[] sampleWeights)
        {
            return run(inputs, outputs, (double[])sampleWeights.Clone());
        }

        private double run(double[][] inputs, int[] outputs, double[] sampleWeights)
        {
            double error = 0;
            double weightSum = 0;

            int[] actualOutputs = new int[outputs.Length];

            do
            {
                // Create and train a classifier
                TModel model = Creation(sampleWeights);

                if (model == null)
                    break;

                // Determine its current accuracy
                for (int i = 0; i < actualOutputs.Length; i++)
                    actualOutputs[i] = model.Compute(inputs[i]);

                error = 0;
                for (int i = 0; i < actualOutputs.Length; i++)
                    if (actualOutputs[i] != outputs[i]) error += sampleWeights[i];

                if (error >= threshold)
                    break;


                // AdaBoost
                // double w = 0.5 * System.Math.Log((1.0 - error) / error);
                double w = error / (1 - error);

                double sum = 0;
                for (int i = 0; i < sampleWeights.Length; i++)
                {
                    if (actualOutputs[i] != outputs[i])
                    {
                        sum += sampleWeights[i] *= w;
                    }
                    else
                    {
                        sum += sampleWeights[i];
                    }
                }
                 

                // Update sample weights
                for (int i = 0; i < sampleWeights.Length; i++)
                    sampleWeights[i] /= sum;

                classifier.Add(w, model);

                weightSum += w;

                convergence.NewValue = error;

            } while (!convergence.HasConverged);


            // Normalize weights for confidence calculation
            for (int i = 0; i < classifier.Models.Count; i++)
                classifier.Models[i].Weight /= weightSum;

            return ComputeError(inputs, outputs);
        }

        /// <summary>
        ///   Computes the error ratio, the number of
        ///   misclassifications divided by the total
        ///   number of samples in a dataset.
        /// </summary>
        /// 
        public double ComputeError(double[][] inputs, int[] outputs)
        {
            int miss = 0;
            for (int i = 0; i < inputs.Length; i++)
            {
                int expected = outputs[i];
                int actual = classifier.Compute(inputs[i]);
                if (expected != actual) miss++;
            }

            return miss / (double)inputs.Length;
        }
    }
}
