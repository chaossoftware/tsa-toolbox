using ChaosSoft.Core.Data;
using ChaosSoft.Core.DataUtils;
using ChaosSoft.Core.IO;
using Microsoft.Win32;
using System;
using TsaToolbox.Models;
using TsaToolbox.ViewModels;

namespace TsaToolbox.Commands
{
    public class LoadDataCommand : CommandBase
    {
        private readonly DataSource _source;
        private readonly SourceAndSettingsViewModel _viewModel;

        public LoadDataCommand(DataSource source, SourceAndSettingsViewModel viewModel)
        {
            _source = source;
            _viewModel = viewModel;
        }

        public override void Execute(object parameter)
        {
            bool parameterized = (bool)parameter;

            var openFileDialog = new OpenFileDialog()
            {
                Filter = "All files|*.*|TimeSeries data|*.dat *.txt *.csv"
            };

            if (openFileDialog.ShowDialog().Value)
            {
                try
                {
                    OpenFile(openFileDialog.FileName, parameterized);
                }
                catch (ArgumentException ex)
                {
                    System.Windows.MessageBox.Show("Unable to read file: " + ex.Message);
                }
            }
        }

        public void OpenFile(string fileName, bool parameterized)
        {
            MainWindow.Instance.CleanUp();

            if (_source.ReadFromBytes)
            {
                _source.Data = SourceData.FromBytesFile(fileName);
            }
            else
            {
                IDataReader dataReader = parameterized ?
                    new PlainTextFileReader(_source.LinesToSkip, _source.LinesToRead) :
                    new PlainTextFileReader();

                _source.Data = new SourceData(dataReader, fileName);
            }

            int[] columnsCount = Vector.CreateUniform(_source.Data.ColumnsCount, 1, 1);

            _viewModel.DataColumnsCount = columnsCount;
            _viewModel.MultilineData = columnsCount.Length > 1;

            if (_source.SignalColumn > _source.Data.ColumnsCount || _source.SignalColumn == 0)
            {
                _viewModel.TimeInFirstColumn = false;
                _viewModel.SignalColumn = 1;
            }

            if (_source.EndPoint > _source.Data.LinesCount || _source.EndPoint == 0)
            {
                _viewModel.StartPoint = 1;
                _viewModel.EndPoint = _source.Data.LinesCount;
            }

            _viewModel.FireDataLoadedEvent();
            _viewModel.StaleTimeSeries();
            _viewModel.SetTimeseriesCommand.Execute(null);
        }
    }
}
