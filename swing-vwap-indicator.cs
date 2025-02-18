#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.ninpai
{
    public class SwingVwapWithStdBands : Indicator
    {
        private Swing swing;
        private List<SwingPoint> swingPoints;
        private class SwingPoint
        {
            public bool IsHigh;
            public int BarIndex;
            public double Price;
            public double Vwap;
            public double[] UpperStd;
            public double[] LowerStd;
            
            public SwingPoint()
            {
                UpperStd = new double[3];
                LowerStd = new double[3];
            }
        }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "VWAP et bandes STD basés sur les points Swing";
                Name = "SwingVwapWithStdBands";
                SwingStrength = 5;
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
            }
            else if (State == State.Configure)
            {
                swing = Swing(SwingStrength);
                swingPoints = new List<SwingPoint>();
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < SwingStrength)
                return;

            // Vérifier nouveau Swing High
            int swingHighBar = swing.SwingHighBar(0, 1, SwingStrength * 2);
            if (swingHighBar >= 0 && swingHighBar < CurrentBar)
            {
                ProcessSwingPoint(true, swingHighBar);
            }

            // Vérifier nouveau Swing Low
            int swingLowBar = swing.SwingLowBar(0, 1, SwingStrength * 2);
            if (swingLowBar >= 0 && swingLowBar < CurrentBar)
            {
                ProcessSwingPoint(false, swingLowBar);
            }

            DrawVwapLines();
        }

        private void ProcessSwingPoint(bool isHigh, int swingBar)
        {
            // Vérifier si ce point existe déjà
            if (swingPoints.Exists(p => p.BarIndex == CurrentBar - swingBar))
                return;

            SwingPoint newPoint = new SwingPoint
            {
                IsHigh = isHigh,
                BarIndex = CurrentBar - swingBar,
                Price = isHigh ? High[swingBar] : Low[swingBar]
            };

            // Trouver le point Swing précédent
            if (swingPoints.Count > 0)
            {
                SwingPoint prevPoint = swingPoints[swingPoints.Count - 1];
                CalculateVwapAndStdBands(prevPoint, newPoint);
            }

            swingPoints.Add(newPoint);
        }

        private void CalculateVwapAndStdBands(SwingPoint startPoint, SwingPoint endPoint)
        {
            double sumPV = 0;
            double sumV = 0;
            List<double> prices = new List<double>();

            for (int i = startPoint.BarIndex; i <= endPoint.BarIndex; i++)
            {
                double typicalPrice = (High[CurrentBar - i] + Low[CurrentBar - i] + Close[CurrentBar - i]) / 3;
                double volume = Volume[CurrentBar - i];
                
                sumPV += typicalPrice * volume;
                sumV += volume;
                prices.Add(typicalPrice);
            }

            double vwap = sumPV / sumV;
            endPoint.Vwap = vwap;

            // Calculer les bandes STD
            double sumSquaredDiff = 0;
            foreach (double price in prices)
            {
                sumSquaredDiff += Math.Pow(price - vwap, 2);
            }
            double stdDev = Math.Sqrt(sumSquaredDiff / prices.Count);

            for (int i = 0; i < 3; i++)
            {
                endPoint.UpperStd[i] = vwap + (stdDev * (i + 1));
                endPoint.LowerStd[i] = vwap - (stdDev * (i + 1));
            }
        }

        private void DrawVwapLines()
        {
            if (swingPoints.Count < 2)
                return;

            for (int i = 1; i < swingPoints.Count; i++)
            {
                SwingPoint current = swingPoints[i];
                SwingPoint previous = swingPoints[i - 1];

                // Tracer VWAP
                Draw.Line(this, "VWAP_" + i, false, previous.BarIndex, previous.Vwap, 
                    current.BarIndex, current.Vwap, Brushes.Blue, DashStyleHelper.Solid, 2);

                // Tracer les bandes STD
                for (int band = 0; band < 3; band++)
                {
                    // Bandes supérieures
                    Draw.Line(this, "UpperStd" + (band + 1) + "_" + i, false,
                        previous.BarIndex, previous.UpperStd[band],
                        current.BarIndex, current.UpperStd[band],
                        Brushes.Red, DashStyleHelper.Dash, 1);

                    // Bandes inférieures
                    Draw.Line(this, "LowerStd" + (band + 1) + "_" + i, false,
                        previous.BarIndex, previous.LowerStd[band],
                        current.BarIndex, current.LowerStd[band],
                        Brushes.Green, DashStyleHelper.Dash, 1);
                }
            }
        }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "Swing Strength", Description = "Nombre de barres pour la force du swing", Order = 1, GroupName = "Parameters")]
        public int SwingStrength { get; set; }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ninpai.SwingVwapWithStdBands[] cacheSwingVwapWithStdBands;
		public ninpai.SwingVwapWithStdBands SwingVwapWithStdBands(int swingStrength)
		{
			return SwingVwapWithStdBands(Input, swingStrength);
		}

		public ninpai.SwingVwapWithStdBands SwingVwapWithStdBands(ISeries<double> input, int swingStrength)
		{
			if (cacheSwingVwapWithStdBands != null)
				for (int idx = 0; idx < cacheSwingVwapWithStdBands.Length; idx++)
					if (cacheSwingVwapWithStdBands[idx] != null && cacheSwingVwapWithStdBands[idx].SwingStrength == swingStrength && cacheSwingVwapWithStdBands[idx].EqualsInput(input))
						return cacheSwingVwapWithStdBands[idx];
			return CacheIndicator<ninpai.SwingVwapWithStdBands>(new ninpai.SwingVwapWithStdBands(){ SwingStrength = swingStrength }, input, ref cacheSwingVwapWithStdBands);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.SwingVwapWithStdBands SwingVwapWithStdBands(int swingStrength)
		{
			return indicator.SwingVwapWithStdBands(Input, swingStrength);
		}

		public Indicators.ninpai.SwingVwapWithStdBands SwingVwapWithStdBands(ISeries<double> input , int swingStrength)
		{
			return indicator.SwingVwapWithStdBands(input, swingStrength);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.SwingVwapWithStdBands SwingVwapWithStdBands(int swingStrength)
		{
			return indicator.SwingVwapWithStdBands(Input, swingStrength);
		}

		public Indicators.ninpai.SwingVwapWithStdBands SwingVwapWithStdBands(ISeries<double> input , int swingStrength)
		{
			return indicator.SwingVwapWithStdBands(input, swingStrength);
		}
	}
}

#endregion
