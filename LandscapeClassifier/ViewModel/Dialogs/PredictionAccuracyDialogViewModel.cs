using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using LandscapeClassifier.Model;

namespace LandscapeClassifier.ViewModel.Dialogs
{
    public class PredictionAccuracyDialogViewModel : ViewModelBase
    {
        private DataTable _predictionDataTable;
        private double _kappa;

        public DataTable PredictionDataTable
        {
            get { return _predictionDataTable; }
            set { _predictionDataTable = value; RaisePropertyChanged(); }
        }

        public double Kappa
        {
            get { return _kappa; }
            set { _kappa = value; RaisePropertyChanged(); }
        }

        public PredictionAccuracyDialogViewModel()
        {
            PredictionDataTable = new DataTable();
            PredictionDataTable.Columns.Add("Type");
            var landcoverArray = Enum.GetValues(typeof(LandcoverType)).Cast<LandcoverType>().Where(l => l != LandcoverType.None);
            var landcoverTypes = landcoverArray as IList<LandcoverType> ?? landcoverArray.ToList();

            foreach (LandcoverType type in landcoverTypes)
            {
                 PredictionDataTable.Columns.Add(type.ToString());
            }
            PredictionDataTable.Columns.Add("Sum");

            foreach (LandcoverType type in landcoverTypes)
            {
                var row = PredictionDataTable.NewRow();
                row[PredictionDataTable.Columns[0]] = type.ToString();
                PredictionDataTable.Rows.Add(row);
            }
            var sumRow = PredictionDataTable.NewRow();
            sumRow[PredictionDataTable.Columns[0]] = "Sum";
            PredictionDataTable.Rows.Add(sumRow);
        }

        public void SetPredictionData(int[,] data)
        {
            for (int row = 0; row < data.GetLength(1); ++row)
            {
                var dataRow = PredictionDataTable.Rows[row];
                for (int column = 0; column < data.GetLength(0); ++column)
                {
                    dataRow[column + 1] = data[row, column];
                }
            }
        }

    }
}
