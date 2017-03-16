using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Statistics.Analysis;
using GalaSoft.MvvmLight;
using LandscapeClassifier.Model;
using LandscapeClassifier.ViewModel.MainWindow;

namespace LandscapeClassifier.ViewModel.Dialogs
{
    public class PredictionAccuracyDialogViewModel : ViewModelBase
    {
        private DataTable _predictionDataTable;
        private string _kappa;
        private string _accuracy;

        public string Kappa
        {
            get { return _kappa; }
            set { _kappa = value; RaisePropertyChanged(); }
        }

        public string Accuracy
        {
            get { return _accuracy; }
            set { _accuracy = value; RaisePropertyChanged(); }
        }

        public DataTable PredictionDataTable
        {
            get { return _predictionDataTable; }
            set { _predictionDataTable = value; RaisePropertyChanged(); }
        }

        public PredictionAccuracyDialogViewModel()
        {
            PredictionDataTable = new DataTable();

            InitializeTable();
        }

        public void Initialize(List<GeneralConfusionMatrix> confusionMatrices)
        {
            PredictionDataTable.Clear();

            var landcoverTypes = MainWindowViewModel.Default.LandcoverTypes.Values.ToList();
            var numClasses = landcoverTypes.Count;

            foreach (LandcoverTypeViewModel type in landcoverTypes)
            {
                var row = PredictionDataTable.NewRow();
                row[PredictionDataTable.Columns[0]] = type.ToString();
                PredictionDataTable.Rows.Add(row);
            }
            var sumRow = PredictionDataTable.NewRow();
            sumRow[PredictionDataTable.Columns[0]] = "Sum";
            PredictionDataTable.Rows.Add(sumRow);

            int[,] data = new int[numClasses, numClasses];
            for (int matIndex = 0; matIndex < confusionMatrices.Count; ++matIndex)
            {
                for (int row = 0; row < data.GetLength(1); ++row)
                {
                    for (int column = 0; column < data.GetLength(0); ++column)
                    {
                        data[column, row] += confusionMatrices[matIndex].Matrix[column, row];
                    }
                }
            }

            for (int row = 0; row < data.GetLength(1); ++row)
            {
                var dataRow = PredictionDataTable.Rows[row];
                for (int column = 0; column < data.GetLength(0); ++column)
                {
                    dataRow[column + 1] = data[column, row];
                }
            }

            // Row totals
            int[] rowTotals = new int[numClasses];
            for (int matIndex = 0; matIndex < confusionMatrices.Count; ++matIndex)
            {
                for (int totalColumn = 0; totalColumn < confusionMatrices[matIndex].RowTotals.Length; ++totalColumn)
                {
                    rowTotals[totalColumn] += confusionMatrices[matIndex].RowTotals[totalColumn];
                }
            }
            for (int totalColumn = 0; totalColumn < rowTotals.Length; ++totalColumn)
            {
                var dataRow = PredictionDataTable.Rows[numClasses];
                dataRow[totalColumn + 1] = rowTotals[totalColumn];
            }

            // Column totals
            int[] columnTotals = new int[numClasses];
            for (int matIndex = 0; matIndex < confusionMatrices.Count; ++matIndex)
            {
                for (int totalRow = 0; totalRow < confusionMatrices[matIndex].ColumnTotals.Length; ++totalRow)
                {
                    columnTotals[totalRow] += confusionMatrices[matIndex].ColumnTotals[totalRow];
                }
            }
            for (int totalRow = 0; totalRow < columnTotals.Length; ++totalRow)
            {
                var dataRow = PredictionDataTable.Rows[totalRow];
                dataRow[numClasses + 1] = columnTotals[totalRow];
            }

            // Compute mean and stddev
            double kappaMean = 0, kappaStddev = 0, kappaVar = 0;
            double accuracyMean = 0, accuracyStddev = 0, accuracyVar = 0;
            for (int matIndex = 0; matIndex < confusionMatrices.Count; ++matIndex)
            {
                kappaMean += confusionMatrices[matIndex].Kappa / confusionMatrices.Count;
                accuracyMean += confusionMatrices[matIndex].OverallAgreement / confusionMatrices.Count;
            }
            for (int matIndex = 0; matIndex < confusionMatrices.Count; ++matIndex)
            {
                kappaVar += Math.Pow(confusionMatrices[matIndex].Kappa - kappaMean, 2);
                accuracyVar += Math.Pow(confusionMatrices[matIndex].OverallAgreement - accuracyMean, 2);
            }

            kappaVar = kappaVar / (confusionMatrices.Count - 1);
            accuracyVar = accuracyVar / (confusionMatrices.Count - 1);

            kappaStddev = Math.Sqrt(kappaVar);
            accuracyStddev = Math.Sqrt(accuracyVar);

            Accuracy = $"{Math.Round(accuracyMean * 100, 2)} (± {Math.Round(accuracyStddev * 100, 4)})%";
            Kappa = $"{Math.Round(kappaMean * 100, 2)} (± {Math.Round(kappaStddev * 100, 4)})";

            
            printLatexTable(data, rowTotals, columnTotals);
            Console.WriteLine();
            Console.WriteLine("\\resulttable{\\DT}");
            Console.WriteLine();
            Console.WriteLine($"With an overall accuracy of ${Math.Round(accuracyMean * 100, 2)} (\\pm {Math.Round(accuracyStddev * 100, 4)})\\%$ and $\\kappa * 100 = {Math.Round(kappaMean * 100, 2)} (\\pm {Math.Round(kappaStddev * 100, 4)})$.");
        }

        private void printLatexTable(int[,] matrix, int[] rowTotals, int[] columnTotals)
        {
            Console.WriteLine("\\newarray\\DT");
            Console.WriteLine("\\readarray{DT}{%");
            int total = 0;
            for (int row = 0; row < matrix.GetLength(1); ++row)
            {
                for (int column = 0; column < matrix.GetLength(0); ++column)
                {
                    Console.Write(matrix[column, row] + "&");
                    total += matrix[column, row];
                }
                Console.Write(columnTotals[row] + "&%" + Environment.NewLine);
            }
            for (int column = 0; column < columnTotals.GetLength(0); ++column)
            {
                Console.Write(rowTotals[column] + "&");
            }
            Console.Write(total + "&%" + Environment.NewLine);
            Console.WriteLine("}");
            Console.WriteLine("\\dataheight=10");
        }

        private void InitializeTable()
        {
            PredictionDataTable.Columns.Add("Type");
            
            var landcoverTypes = MainWindowViewModel.Default.LandcoverTypes.Values.ToList();

            foreach (LandcoverTypeViewModel type in landcoverTypes)
            {
                PredictionDataTable.Columns.Add(type.ToString());
            }
            PredictionDataTable.Columns.Add("Sum");

            foreach (LandcoverTypeViewModel type in landcoverTypes)
            {
                var row = PredictionDataTable.NewRow();
                row[PredictionDataTable.Columns[0]] = type.ToString();
                PredictionDataTable.Rows.Add(row);
            }
            var sumRow = PredictionDataTable.NewRow();
            sumRow[PredictionDataTable.Columns[0]] = "Sum";
            PredictionDataTable.Rows.Add(sumRow);
        }
    }
}
