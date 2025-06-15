using ChaosSoft.Core.Data;
using ChaosSoft.Core.DataUtils;
using System.Drawing;
using TsaToolbox.Helpers;
using TsaToolbox.Models;

namespace TsaToolbox
{
    internal class Charts
    {
        private readonly Color _mainColor = Color.Blue;
        private readonly Color _markerColor = Color.Red;
        private readonly Settings _settings;

        internal Charts(Settings settings)
        {
            _settings = settings;
        }

        internal void SavePlot(ScottPlot.WpfPlot plot, string fileName)
        {
            plot.Plot.SaveFig(fileName, _settings.SaveChartWidth, _settings.SaveChartHeight, false, _settings.SaveChartScaling);
        }

        internal void PlotScatter(ScottPlot.WpfPlot plot, DataSeries series, string xLabel, string yLabel)
        {
            ClearPlot(plot);
            plot.Plot.AddScatter(series.XValues, series.YValues, _mainColor, 0.5f, 0f);
            RenderPlot(plot, xLabel, yLabel);
        }

        internal void PlotScatter(ScottPlot.WpfPlot plot, double[] xs, double[] ys, string xLabel, string yLabel)
        {
            ClearPlot(plot);
            plot.Plot.AddScatter(xs, ys, _mainColor, 0.5f, 0f);
            RenderPlot(plot, xLabel, yLabel);
        }

        internal void PlotScatterLogScale(ScottPlot.WpfPlot plot, double[] xs, double[] ys, string xLabel, string yLabel)
        {
            ClearPlot(plot);
            double[] logYs = CustomLogScale.Calculate(ys);
            plot.Plot.AddScatter(xs, logYs, _mainColor, 0.5f, 0f);
            RenderPlot(plot, xLabel, "Log " + yLabel);
        }

        internal void PlotSignal(ScottPlot.WpfPlot plot, double[] series, string xLabel, string yLabel)
        {
            double[] xs = Vector.CreateUniform(series.Length, 0d, 1d);
            PlotScatter(plot, xs, series, xLabel, yLabel);
        }

        internal void PlotScatterPoints(ScottPlot.WpfPlot plot, DataSeries series, string xLabel, string yLabel)
        {
            ClearPlot(plot);
            plot.Plot.AddScatterPoints(series.XValues, series.YValues, _mainColor, 1);
            RenderPlot(plot, xLabel, yLabel);
        }

        internal void AddVerticalLine(ScottPlot.WpfPlot plot, double x)
        {
            plot.Plot.AddVerticalLine(x, _markerColor, 1, ScottPlot.LineStyle.DashDot);
            var annotation = plot.Plot.AddAnnotation(x.ToString(), 0, 0);
            annotation.Shadow = false;
            plot.Render();
        }

        internal void ClearPlot(ScottPlot.WpfPlot plot)
        {
            plot.Plot.Clear();
            plot.Render();
        }

        internal void RenderPlot(ScottPlot.WpfPlot plot, string xLabel, string yLabel)
        {
            plot.Plot.XAxis.LabelStyle(fontSize: _settings.AxisLabelSize);
            plot.Plot.YAxis.LabelStyle(fontSize: _settings.AxisLabelSize); 
            plot.Plot.XAxis.TickLabelStyle(fontSize: _settings.AxisTickLabelSize);
            plot.Plot.YAxis.TickLabelStyle(fontSize: _settings.AxisTickLabelSize);
            plot.Plot.XAxis.Label(xLabel);
            plot.Plot.YAxis.Label(yLabel);
            plot.Plot.Grid(enable: _settings.ShowGridLines);
            plot.Render();
        }
    }
}
