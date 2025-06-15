using ChaosSoft.Core;
using ChaosSoft.Core.Data;
using ChaosSoft.Core.IO;
using ChaosSoft.NumericalMethods;
using ChaosSoft.NumericalMethods.Lyapunov;
using ChaosSoft.NumericalMethods.PhaseSpace;
using ChaosSoft.NumericalMethods.Transform;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using TsaToolbox.Helpers;
using TsaToolbox.Models;
using TsaToolbox.Models.Setups;

namespace TsaToolbox
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly LyapunovExponents _lyapunov;
        private Charts charts;

        public MainWindow()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            InitializeComponent();

            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            statusVersionText.Text = $" v{versionInfo.ProductVersion}";
            _lyapunov = new LyapunovExponents();

            Instance = this;
        }

        public static MainWindow Instance { get; set; }

        public Settings Settings { get; set; }

        public Setup Setup { get; set; }

        internal Charts Charts => charts ?? (charts = new Charts(Settings));

        public DataSource Source { get; set; }

        public void CleanUp()
        {
            Source.Data = null;
            Charts.ClearPlot(ch_SignalChart);
            Charts.ClearPlot(ch_PseudoPoincareChart);
            Charts.ClearPlot(ch_acfChart);
            Charts.ClearPlot(an_FnnChart);
            Charts.ClearPlot(an_miChart);
            Charts.ClearPlot(FftView.ch_FftChart);

            WaveletView.ch_wavChart.Reset();
            _lyapunov.CleanUp(this);

            DeleteWaveletTempFile();
        }

        private void le_rosRad_Checked(object sender, RoutedEventArgs e) =>
            le_rosGbox.Visibility = Visibility.Visible;

        private void le_rosRad_Unchecked(object sender, RoutedEventArgs e) =>
            le_rosGbox.Visibility = Visibility.Hidden;

        private void le_kantzRad_Checked(object sender, RoutedEventArgs e)
        {
            le_kantzGbox.Visibility = Visibility.Visible;
            le_k_epsLbl.Visibility = Visibility.Visible;
            le_k_epsCombo.Visibility = Visibility.Visible;
        }

        private void le_kantzRad_Unchecked(object sender, RoutedEventArgs e)
        {
            le_kantzGbox.Visibility = Visibility.Hidden;
            le_k_epsLbl.Visibility = Visibility.Hidden;
            le_k_epsCombo.Visibility = Visibility.Hidden;
        }

        private void le_wolfRad_Checked(object sender, RoutedEventArgs e) =>
            le_wolfGbox.Visibility = Visibility.Visible;

        private void le_wolfRad_Unchecked(object sender, RoutedEventArgs e) =>
            le_wolfGbox.Visibility = Visibility.Hidden;

        private void le_ssRad_Checked(object sender, RoutedEventArgs e) =>
            le_ssGbox.Visibility = Visibility.Visible;

        private void le_ssRad_Unchecked(object sender, RoutedEventArgs e) =>
            le_ssGbox.Visibility = Visibility.Hidden;

        private void ch_buildBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ch_signalCbox.IsChecked.Value)
            {
                Charts.PlotScatter(ch_SignalChart, Source.Data.TimeSeries, "t", "f(t)");
            }

            if (ch_poincareCbox.IsChecked.Value)
            {
                var pPoincare = DelayedCoordinates.GetData(Source.Data.TimeSeries.YValues, 1);
                Charts.PlotScatterPoints(ch_PseudoPoincareChart, pPoincare, "Xn", "Xn+1");
            }

            if (ch_acfCbox.IsChecked.Value)
            {
                BuildAcfChart();
            }

            if (ch_fnnCbox.IsChecked.Value)
            {
                BuildFnnChart();
            }

            if (ch_miCbox.IsChecked.Value)
            {
                BuildMiChart();
            }
        }

        private void BuildAcfChart()
        {
            var autoCor = Statistics.Acf(Source.Data.TimeSeries.YValues);

            Charts.PlotSignal(ch_acfChart, autoCor, "t", "acf");

            int i;

            for (i = 1; i < autoCor.Length; i++)
            {
                if (Math.Sign(autoCor[i]) != Math.Sign(autoCor[i - 1]))
                {
                    break;
                }
            }

            Charts.AddVerticalLine(ch_acfChart, i);
        }

        private void BuildFnnChart()
        {
            var fnn = new FalseNearestNeighbors(
                fnn_minDim.ReadInt(), 
                fnn_maxDim.ReadInt(), 
                fnn_tau.ReadInt(), 
                fnn_rt.ReadDouble(), 
                fnn_theiler.ReadInt());

            fnn.Calculate(Source.Data.TimeSeries.YValues);

            Charts.PlotScatter(an_FnnChart,
                fnn.FalseNeighbors.Keys.Select(x => (double)x).ToArray(),
                fnn.FalseNeighbors.Values.Select(y => (double)y).ToArray(),
                "d",
                "fnn");

            int key = fnn.FalseNeighbors.Keys.First(k => fnn.FalseNeighbors[k] == 0);
            Charts.AddVerticalLine(an_FnnChart, key);
        }

        private void BuildMiChart()
        {
            var mi = new MutualInformation(mi_partitions.ReadInt(), mi_maxDelay.ReadInt());
            mi.Calculate(Source.Data.TimeSeries.YValues);

            Charts.PlotScatter(an_miChart, mi.EntropySlope, "d", "mi");

            int index = 1;
            bool firstMinimumReached = false;

            while (index < mi.EntropySlope.Length && !firstMinimumReached)
            {
                firstMinimumReached = mi.EntropySlope.YValues[index] > mi.EntropySlope.YValues[index - 1];
                index++;
            }

            double tau = mi.EntropySlope.XValues[index - 2];

            Charts.AddVerticalLine(an_miChart, tau);
        }

        private void ch_buildMlBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Setup.Fft.Enabled)
            {
                var fftTs = GetFft();
                string yLabel = "power (dB)";

                if (Setup.Fft.LogScale)
                {
                    Charts.PlotScatterLogScale(FftView.ch_FftChart, fftTs.XValues, fftTs.YValues, "ω", yLabel);
                }
                else
                {
                    Charts.PlotScatter(FftView.ch_FftChart, fftTs.XValues, fftTs.YValues, "ω", yLabel);
                }
            }

            if (Setup.Wavelet.Enabled)
            {
                DeleteWaveletTempFile();

                ScottPlot.WpfPlot wvlChart = WaveletView.ch_wavChart;

                var brush = GetWavelet(
                    wvlChart.Width * 2,
                    wvlChart.Height * 2, 
                    Properties.Resources.WaveletFile);


                WaveletView.ch_wavChart.Reset();

                wvlChart.Plot.Style(dataBackgroundImage: brush);
                wvlChart.Plot.Grid(enable: false);

                wvlChart.Plot.SetAxisLimits(
                    Source.Data.TimeSeries.Min.X,
                    Source.Data.TimeSeries.Max.X,
                    Setup.Wavelet.OmegaFrom,
                    Setup.Wavelet.OmegaTo);

                Charts.RenderPlot(wvlChart, "t", "ω");
            }
        }

        private void le_calculateBtn_Click(object sender, RoutedEventArgs e)
        {
            _lyapunov.CleanUp(this);
            SetLyapunovMethod(Source.Data.TimeSeries.YValues.Length);

            new Thread(() => _lyapunov.ExecuteLyapunovMethod(this, Source.Data.TimeSeries.YValues))
                    .Start();
        }

        private void SetLyapunovMethod(int lenght)
        {
            var dim = le_eDimTbox.ReadInt();
            var tau = le_tauTbox.ReadInt();
            var scaleMin = le_epsMinTbox.ReadDouble();

            if (le_wolfRad.IsChecked.Value)
            {
                var dt = le_dtTbox.ReadDouble();
                var scaleMax = le_w_epsMaxTbox.ReadDouble();
                var evolSteps = le_w_evolStepsTbox.ReadInt();

                _lyapunov.Method = new LleWolf(dim, tau, dt, scaleMin, scaleMax, evolSteps);
            }
            else if (le_rosRad.IsChecked.Value)
            {
                var iter = le_r_iterTbox.ReadInt();
                var window = le_r_windowTbox.ReadInt();

                _lyapunov.Method = new LleRosenstein(dim, tau, iter, window, scaleMin);
            }
            else if (le_kantzRad.IsChecked.Value)
            {
                var iter = le_k_iterTbox.ReadInt();
                var window = le_k_windowTbox.ReadInt();
                var scaleMax = le_k_epsMaxTbox.ReadDouble();
                var scales = le_k_scalesTbox.ReadInt();

                _lyapunov.Method = new LleKantz(dim, tau, iter, window, scaleMin, scaleMax, scales);
            }
            else if (le_ssRad.IsChecked.Value)
            {
                var inverse = le_ss_inverseCbox.IsChecked.Value;
                var scaleFactor = le_ss_scaleFactorTbox.ReadDouble();
                var minNeigh = le_ss_minNeighbTbox.ReadInt();

                _lyapunov.Method = new LeSpecSanoSawada(dim, tau, lenght, scaleMin, scaleFactor, minNeigh, inverse);
            }
        }

        private void le_k_adjustBtn_Click(object sender, RoutedEventArgs e) =>
            _lyapunov.AdjustSlope(this);

        private void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Source.Data == null)
            {
                MessageBox.Show(Properties.Resources.MsgEmptyFile);
                return;
            }

            string outDir =
                Settings.SeparateOutputDir ?
                Path.Combine(Settings.OutputDir, Source.Data.FileName) :
                Settings.OutputDir;

            string fName = Path.Combine(outDir, Source.Data.FileName);

            if (File.Exists(outDir))
            {
                throw new IOException(
                    "Unable to create folder as file with this name already exists. Folder name: " + outDir);
            }

            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            if (ch_signalCbox.IsChecked.Value)
            {
                FileUtils.CreateDataFile(fName + "_signal.dat", NumFormat.Format(Source.Data.TimeSeries.YValues, "\n"));
                Charts.SavePlot(ch_SignalChart, fName + "_signal.png");
            }

            if (ch_poincareCbox.IsChecked.Value)
            {
                Charts.SavePlot(ch_PseudoPoincareChart, fName + "_poincare.png");
            }

            if (ch_acfCbox.IsChecked.Value)
            {
                Charts.SavePlot(ch_acfChart, fName + "_acf.png");
            }

            if (ch_fnnCbox.IsChecked.Value)
            {
                Charts.SavePlot(an_FnnChart, fName + "_fnn.png");
            }

            if (ch_miCbox.IsChecked.Value)
            {
                Charts.SavePlot(an_miChart, fName + "_mi.png");
            }

            if (Setup.Fft.Enabled)
            {
                Charts.SavePlot(FftView.ch_FftChart, fName + "_fft.png");
            }

            if (Setup.Wavelet.Enabled)
            {
                string waveletName = Setup.Wavelet.Family.ToString().ToLowerInvariant();
                Charts.SavePlot(WaveletView.ch_wavChart, $"{fName}_{waveletName}_wavelet.png");
            }

            if (_lyapunov.Method != null)
            {
                GenerateLeFile(fName);
                Charts.SavePlot(le_slopeChart, fName + "_lyapunovSlope.png");
            }
        }

        private void GenerateLeFile(string baseFileName)
        {
            var sb = new StringBuilder();
            sb.AppendLine(_lyapunov.Method.ToString())
                .AppendLine()
                .AppendLine("Result:")
                .AppendLine(le_resultTbox.Text)
                .AppendLine()
                .AppendLine("Execution log:")
                .AppendLine()
                .AppendLine(/*_lyapunov.Method.Log.ToString()*/); // TODO: return log

            FileUtils.CreateDataFile(baseFileName + "_lyapunov.txt", sb.ToString());
        }

        private DataSeries GetFft() =>
            FreqAnalysis.GetFourier(Source.Data.TimeSeries.YValues, Setup.Fft);

        private Bitmap GetWavelet(double width, double height, string fileName) =>
            FreqAnalysis.GetWavelet(Source.Data.TimeSeries.YValues,
                    Source.Data.TimeSeries.XValues,
                    fileName,
                    Setup.Wavelet,
                    width,
                    height);

        private void le_k_epsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
            _lyapunov.AdjustSlope(this);

        private void DeleteWaveletTempFile() =>
            File.Delete(Properties.Resources.WaveletFile);
    }
}
