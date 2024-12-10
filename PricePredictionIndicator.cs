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
    public class PricePredictionIndicator : Indicator
    {
        private double[] openPrices;
        private double[] highPrices;
        private double[] lowPrices;
        private double[] volumes;
        private double[] dowDummies;
        private LinearRegression lr;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Prédiction de prix basée sur ML";
                Name = "Price Prediction";
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
                dowDummies = new double[5];
                
                lr = new LinearRegression();
            }
            else if (State == State.Configure)
            {
                AddPlot(new Stroke(Brushes.Yellow, 2), PlotStyle.Dot, "Prediction");
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 5) return;

            // Mettre à jour les arrays avec les 5 dernières barres
            for (int i = 1; i <= 5; i++)
            {
                openPrices[i-1] = Open[i];
                highPrices[i-1] = High[i];
                lowPrices[i-1] = Low[i];
                volumes[i-1] = Volume[i];
                dowDummies[i-1] = Time[i].DayOfWeek == DayOfWeek.Monday ? 1 : 0;
            }

            // Calculer les moyennes mais en gardant l'échelle des prix réels
            double avgOpen = openPrices.Average();
            double avgHigh = highPrices.Average();
            double avgLow = lowPrices.Average();
            double lastClose = Close[1];  // Prix de clôture précédent
            
            // Normaliser le volume pour qu'il n'affecte pas trop la prédiction
            double avgVolume = volumes.Average() / 10000.0;
            
            // Utiliser le prix de clôture précédent comme base
            double basePrice = lastClose;
            
            // Calculer les variations relatives par rapport au dernier prix
            double[] features = new double[]
            {
                (avgOpen - basePrice) / basePrice,
                (avgHigh - basePrice) / basePrice,
                (avgLow - basePrice) / basePrice,
                avgVolume / basePrice,
                Time[0].DayOfYear / 365.0,  // Normaliser la date
                dowDummies[0] * 0.001,      // Réduire l'impact des variables dummy
                dowDummies[1] * 0.001,
                dowDummies[2] * 0.001,
                dowDummies[3] * 0.001,
                dowDummies[4] * 0.001
            };

            // Faire la prédiction en utilisant le prix de base
            double prediction = basePrice * (1 + lr.Predict(features));
            
            // Limiter la variation maximale à ±1% du prix actuel
            double maxChange = basePrice * 0.01;
            prediction = Math.Max(basePrice - maxChange, 
                        Math.Min(basePrice + maxChange, prediction));
            
            Value[0] = prediction;
        }

        #region Linear Regression Implementation
        private class LinearRegression
        {
            private double[] weights;
            private double bias;

            public LinearRegression()
            {
                // Poids ajustés pour des variations relatives
                weights = new double[] 
                { 
                    0.4,    // Open
                    0.3,    // High
                    0.2,    // Low
                    0.001,  // Volume
                    0.0005, // Jour de l'année
                    0.0001, // DOW1
                    0.0001, // DOW2
                    0.0001, // DOW3
                    0.0001, // DOW4
                    0.0001  // DOW5
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
		private ninpai.PricePredictionIndicator[] cachePricePredictionIndicator;
		public ninpai.PricePredictionIndicator PricePredictionIndicator()
		{
			return PricePredictionIndicator(Input);
		}

		public ninpai.PricePredictionIndicator PricePredictionIndicator(ISeries<double> input)
		{
			if (cachePricePredictionIndicator != null)
				for (int idx = 0; idx < cachePricePredictionIndicator.Length; idx++)
					if (cachePricePredictionIndicator[idx] != null &&  cachePricePredictionIndicator[idx].EqualsInput(input))
						return cachePricePredictionIndicator[idx];
			return CacheIndicator<ninpai.PricePredictionIndicator>(new ninpai.PricePredictionIndicator(), input, ref cachePricePredictionIndicator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.PricePredictionIndicator PricePredictionIndicator()
		{
			return indicator.PricePredictionIndicator(Input);
		}

		public Indicators.ninpai.PricePredictionIndicator PricePredictionIndicator(ISeries<double> input )
		{
			return indicator.PricePredictionIndicator(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.PricePredictionIndicator PricePredictionIndicator()
		{
			return indicator.PricePredictionIndicator(Input);
		}

		public Indicators.ninpai.PricePredictionIndicator PricePredictionIndicator(ISeries<double> input )
		{
			return indicator.PricePredictionIndicator(input);
		}
	}
}

#endregion
