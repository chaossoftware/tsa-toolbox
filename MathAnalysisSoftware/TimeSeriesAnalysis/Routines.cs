﻿using MathLib;
using MathLib.DrawEngine;
using MathLib.DrawEngine.Charts;
using MathLib.IO;
using MathLib.MathMethods.Lyapunov;
using MathLib.Transform;
using MathWorks.MATLAB.NET.Arrays;
using System.Drawing;
using System.IO;

namespace TimeSeriesAnalysis
{
    class Routines {

        public SourceData sourceData;

        public LyapunovMethod lyapunov;


        public SignalPlot GetSignalPlot(Size size, int thickness, bool withTime, int startPoint, int endPoint) {
            return new SignalPlot(sourceData.TimeSeries, size, thickness);
        }


        public MapPlot GetPoincarePlot(Size size, int thickness) {
            return new MapPlot(Ext.GeneratePseudoPoincareMapData(sourceData.TimeSeries.ValY), size, thickness);
        }


        public PlotObject GetLyapunovPlot(Size size, int thickness, int startPoint, int endPoint, bool isWolf, out string result) {
            PlotObject lyap;
            int range = endPoint - startPoint + 1;
            result = "";

            DataSeries plotSeries = new DataSeries();
            for (int i = startPoint; i < range; i++)
                plotSeries.AddDataPoint(lyapunov.slope.ListDataPoints[i].X, lyapunov.slope.ListDataPoints[i].Y);

            lyap = new SignalPlot(plotSeries, size, 1);

            if (isWolf) {
                lyap.LabelY = "LE";
            }
            else {
                lyap.LabelY = "Slope";

                result = string.Format("{0:F5}",
                    (lyapunov.slope.ListDataPoints[endPoint].Y - lyapunov.slope.ListDataPoints[startPoint].Y) / (lyapunov.slope.ListDataPoints[endPoint].X - lyapunov.slope.ListDataPoints[startPoint].X)
                );
            }
            return lyap;
        }


        public SignalPlot GetFourierPlot(Size size, int thickness, double statrFreq, double endFreq, double dt, int logScale) {

            DataSeries fourierSeries = Fourier.GetFourier(sourceData.TimeSeries.ValY, statrFreq, endFreq, dt, logScale);
            return new SignalPlot(fourierSeries, size, thickness);
        }


        public void BuildWavelet(string wName, double tStart, double tEnd, double startFreq, double endFreq, double dt, string colMap)
        {
            MatlabEngine.MatlabEngine signalAnalysis = new MatlabEngine.MatlabEngine();
            MWCharArray mw_Folder = new MWCharArray("");
            MWCharArray mw_fileName = new MWCharArray("wavelet.tmp");
            MWCharArray mw_wname = new MWCharArray(wName);
            MWCharArray mw_colMap = new MWCharArray(colMap);
            MWNumericArray mw_signalArray = new MWNumericArray(sourceData.TimeSeries.ValY);

            signalAnalysis.Get2DWavelet(mw_signalArray, mw_Folder, mw_fileName, mw_wname, tStart, tEnd, startFreq, endFreq, dt, mw_colMap);
        }


        public void deleteTempFiles() {
            File.Delete("wavelet.tmp");
        }
    }
}
