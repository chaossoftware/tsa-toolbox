using ChaosSoft.Core;
using ChaosSoft.Core.Data;
using ChaosSoft.NumericalMethods.Extensions;
using ChaosSoft.NumericalMethods.Lyapunov;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using TsaToolbox.Properties;

namespace TsaToolbox
{
    internal class LyapunovExponents
    {
        private const string DefaultStartPoint = "1";

        public LyapunovExponents()
        {
        }

        public ITimeSeriesLyapunov Method { get; set; }

        public void ExecuteLyapunovMethod(MainWindow wnd, double[] series)
        {
            try
            {
                wnd.Dispatcher.Invoke(() =>
                {
                    wnd.le_resultTbox.Background = Brushes.Gold;
                    wnd.le_resultTbox.Text = Resources.Calculating;
                });

                Method.Calculate(series);
                wnd.Dispatcher.Invoke(() => SetLyapunovResult(wnd));
            }
            catch (Exception ex)
            {
                wnd.Dispatcher.Invoke(() =>
                {
                    wnd.le_logTbox.Text = ex.ToString();
                    wnd.le_resultTbox.Background = Brushes.Coral;
                    wnd.le_resultTbox.Text = Resources.Error;
                });
            }
        }

        public void AdjustSlope(MainWindow wnd)
        {
            try
            {
                if (Method is LleKantz k)
                {
                    if (string.IsNullOrEmpty(wnd.le_k_epsCombo.Text))
                    {
                        return;
                    }

                    k.SetSlope(wnd.le_k_epsCombo.Text);
                }

                var res = FillLyapunovChart(wnd);

                if (Method is LleKantz || Method is LleRosenstein)
                {
                    wnd.le_resultTbox.Text = res;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Resources.LePlotError + Environment.NewLine + ex.Message);
            }
        }

        public void CleanUp(MainWindow wnd)
        {
            Method = null;

            wnd.Charts.ClearPlot(wnd.le_slopeChart);

            wnd.le_logTbox.Text = string.Empty;
            wnd.le_resultTbox.Text = string.Empty;
            wnd.le_resultTbox.Background = Brushes.LightGray;
        }

        private void SetLyapunovResult(MainWindow wnd)
        {
            wnd.le_resultTbox.Background = Brushes.LightGreen;
            string result = GetLyapunovResultString(wnd.le_dtTbox.ReadDouble());
            wnd.le_resultTbox.Text = result;

            // TODO: to remove?
            //if (Method is LeSpecSanoSawada ss)
            //{
            //    double dt = wnd.le_dtTbox.ReadDouble();
            //    double[] leSpec = ss.Result.Select(e => e / dt).ToArray();
            //    wnd.le_resultTbox.Text = NumFormat.Format(leSpec, " ");
            //}
            //else
            //{
            //    wnd.le_resultTbox.Text = (Method as IStringifyResult).StringifyResult;
            //}
           
            wnd.le_logTbox.Text = Method.ToString() + "\n\nResult:\n" + result + "\n\nLog:\n"/* + Method.Log.ToString()*/;

            if (Method is LleKantz || Method is LleRosenstein)
            {
                wnd.le_slopeAdjGbox.Visibility = Visibility.Visible;
            }
            else
            {
                wnd.le_slopeAdjGbox.Visibility = Visibility.Hidden;
            }

            if (Method is LleKantz k)
            {
                k.SetSlope(k.SlopesList.Keys.First());
                wnd.le_k_epsCombo.ItemsSource = k.SlopesList.Keys;
                wnd.le_k_epsCombo.SelectedIndex = 0;
            }

            if (Method.Slope.Length > 1)
            {
                try
                {
                    if (Method is LleKantz || Method is LleRosenstein)
                    {
                        var leSectorEnd = DataSeriesUtils.SlopeChangePointIndex(Method.Slope, 3, Method.Slope.Amplitude.Y / 30);

                        if (leSectorEnd <= 0)
                        {
                            leSectorEnd = Method.Slope.Length;
                        }

                        wnd.le_slopeEndTbox.Text = leSectorEnd.ToString();
                        wnd.le_slopeStartTbox.Text = DefaultStartPoint;
                    }

                    result = FillLyapunovChart(wnd);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(Resources.LePlotError + Environment.NewLine + ex.Message);
                    result = Resources.Nda;
                }
            }
            else
            {
                result = Resources.Nda;
            }

            if (Method is LleKantz || Method is LleRosenstein)
            {
                wnd.le_resultTbox.Text = result;
            }
        }

        private string GetLyapunovResultString(double dt)
        {
            string leNumFormat = "0.#####";

            switch (Method)
            {
                case LeSpecSanoSawada ss:
                    double[] leSpec = ss.Result.Select(e => e / dt).ToArray();
                    return NumFormat.Format(leSpec, leNumFormat, " ");
                case LleWolf wolf:
                    return NumFormat.Format(wolf.Result, leNumFormat);
                case LleRosenstein ros:
                    return ros.Success ? "slope calculated" : "calculation error";
                case LleKantz kantz:
                    return kantz.Success ? "slope calculated" : "calculation error";
                default:
                    return "NA";
            }
        }

        private string FillLyapunovChart(MainWindow wnd)
        {
            var result = string.Empty;

            wnd.Charts.PlotScatter(wnd.le_slopeChart, Method.Slope, "t", "");

            if (Method is LleWolf wolf)
            {
                wnd.le_slopeChart.Plot.YLabel("LE");
                wnd.le_slopeChart.Plot.Title("Lyapunov Exponent in time");
                wnd.le_slopeChart.Plot.AddAnnotation(GetLyapunovResultString(1), 0, 0);
            }
            else
            {
                var startPoint = wnd.le_slopeStartTbox.ReadInt() - 1;
                var endPoint = wnd.le_slopeEndTbox.ReadInt() - 1;

                var tsSector = new DataSeries();

                tsSector.AddDataPoint(Method.Slope.DataPoints[startPoint].X, Method.Slope.DataPoints[startPoint].Y);
                tsSector.AddDataPoint(Method.Slope.DataPoints[endPoint].X, Method.Slope.DataPoints[endPoint].Y);

                wnd.le_slopeChart.Plot.YLabel("Slope");
                wnd.le_slopeChart.Plot.Title("Lyapunov Slope");
                wnd.le_slopeChart.Plot.AddScatter(tsSector.XValues, tsSector.YValues, System.Drawing.Color.Red, 2);

                var slope = Math.Atan2(Method.Slope.DataPoints[endPoint].Y - Method.Slope.DataPoints[startPoint].Y, Method.Slope.DataPoints[endPoint].X - Method.Slope.DataPoints[startPoint].X);
                result = NumFormat.Format(slope / wnd.le_dtTbox.ReadDouble());
                wnd.le_slopeChart.Plot.AddAnnotation(result, 0, 0);
            }

            wnd.le_slopeChart.Render();

            return result;
        }
    }
}
