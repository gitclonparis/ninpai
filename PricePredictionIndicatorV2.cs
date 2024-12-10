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
    public class PricePredictionIndicatorV2 : Indicator
    {
        private double[] openPrices;
        private double[] highPrices;
        private double[] lowPrices;
        private double[] volumes;
        private LinearRegression lr;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Prédiction basée sur données récentes";
                Name = "Price Prediction V2";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                BarsRequiredToPlot = 6;

                // Initialiser les arrays
                openPrices = new double[5];
                highPrices = new double[5];
                lowPrices = new double[5];
                volumes = new double[5];
                
                lr = new LinearRegression();
            }
            else if (State == State.Configure)
            {
                AddPlot(new Stroke(Brushes.Yellow, 2), PlotStyle.Dot, "Prediction");
            }
        }

        protected override void OnBarUpdate()
        {
			if (CurrentBars[0] < 20)
                return;
            if (CurrentBar < 5) return;

            // Mettre à jour les arrays avec les 5 dernières barres
            for (int i = 1; i <= 5; i++)
            {
                openPrices[i-1] = Open[i];
                highPrices[i-1] = High[i];
                lowPrices[i-1] = Low[i];
                volumes[i-1] = Volume[i];
            }

            // Calculer les moyennes
            double avgOpen = openPrices.Average();
            double avgHigh = highPrices.Average();
            double avgLow = lowPrices.Average();
            double lastClose = Close[1];
            
            // Normaliser le volume
            double avgVolume = volumes.Average() / 10000.0;
            
            // Utiliser uniquement les données de prix et volume
            double[] features = new double[]
            {
                (avgOpen - lastClose) / lastClose,
                (avgHigh - lastClose) / lastClose,
                (avgLow - lastClose) / lastClose,
                avgVolume / lastClose
            };

            // Faire la prédiction
            double prediction = lastClose * (1 + lr.Predict(features));
            
            // Limiter la variation maximale à ±1%
            double maxChange = lastClose * 0.01;
            prediction = Math.Max(lastClose - maxChange, 
                        Math.Min(lastClose + maxChange, prediction));
            
            Value[0] = prediction;
        }

        #region Linear Regression Implementation
        private class LinearRegression
        {
            private double[] weights;
            private double bias;

            public LinearRegression()
            {
                // Poids simplifiés pour les 4 features uniquement
                weights = new double[] 
                { 
                    0.4,    // Open
                    0.3,    // High
                    0.2,    // Low
                    0.1   // Volume
                };
                bias = 0;
            }

            public double Predict(double[] features)
            {
                double prediction = bias;
                for (int i = 0; i < features.Length; i++)
                {
                    prediction += features[i] * weights[i];
                }
                return prediction;
            }
        }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ninpai.PricePredictionIndicatorV2[] cachePricePredictionIndicatorV2;
		public ninpai.PricePredictionIndicatorV2 PricePredictionIndicatorV2()
		{
			return PricePredictionIndicatorV2(Input);
		}

		public ninpai.PricePredictionIndicatorV2 PricePredictionIndicatorV2(ISeries<double> input)
		{
			if (cachePricePredictionIndicatorV2 != null)
				for (int idx = 0; idx < cachePricePredictionIndicatorV2.Length; idx++)
					if (cachePricePredictionIndicatorV2[idx] != null &&  cachePricePredictionIndicatorV2[idx].EqualsInput(input))
						return cachePricePredictionIndicatorV2[idx];
			return CacheIndicator<ninpai.PricePredictionIndicatorV2>(new ninpai.PricePredictionIndicatorV2(), input, ref cachePricePredictionIndicatorV2);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.PricePredictionIndicatorV2 PricePredictionIndicatorV2()
		{
			return indicator.PricePredictionIndicatorV2(Input);
		}

		public Indicators.ninpai.PricePredictionIndicatorV2 PricePredictionIndicatorV2(ISeries<double> input )
		{
			return indicator.PricePredictionIndicatorV2(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.PricePredictionIndicatorV2 PricePredictionIndicatorV2()
		{
			return indicator.PricePredictionIndicatorV2(Input);
		}

		public Indicators.ninpai.PricePredictionIndicatorV2 PricePredictionIndicatorV2(ISeries<double> input )
		{
			return indicator.PricePredictionIndicatorV2(input);
		}
	}
}

#endregion
