using System;

namespace LandscapeClassifier.ViewModel.MainWindow.Classification.Algorithms.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class OptionAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class BiggerThan : Attribute
    {
        public double Value { get; set; }

        public BiggerThan(double value)
        {
            Value = value;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SmallerThan : Attribute
    {
        public double Value { get; set; }

        public SmallerThan(double value)
        {
            Value = value;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class VisibleWhen : Attribute
    {
        public string Property { get; set; }
        public object Value { get; set; }

        public VisibleWhen(string property, object value)
        {
            Property = property;
            Value = value;
        }
    }
}
