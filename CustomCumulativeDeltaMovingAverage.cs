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
    public class CustomCumulativeDeltaMovingAverage : Indicator
    {
        private OrderFlowCumulativeDelta cumulativeDelta;
        private Series<double> deltaSinceResetSeries;
        private DateTime lastResetTime;
        private double resetDeltaValue;
        private int barsSinceLastReset;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Période Moyenne Mobile", Order = 1, GroupName = "Paramètres")]
        public int MovingAveragePeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Intervalle de Réinitialisation (Minutes)", Order = 2, GroupName = "Paramètres")]
        public int ResetIntervalMinutes { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                 = "Indicateur qui affiche une moyenne mobile du delta cumulatif, se réinitialisant à intervalles spécifiés.";
                Name                        = "CustomCumulativeDeltaMovingAverage";
                Calculate                   = Calculate.OnBarClose;
                IsOverlay                   = false;
                DisplayInDataBox            = true;
                DrawOnPricePanel            = false;
                DrawHorizontalGridLines     = true;
                DrawVerticalGridLines       = true;
                PaintPriceMarkers           = true;

                MovingAveragePeriod         = 9;
                ResetIntervalMinutes        = 120;

                AddPlot(Brushes.Blue, "DeltaMovingAverage");
            }
            else if (State == State.Configure)
            {
                // Ajouter une série de ticks nécessaire pour le delta cumulatif
                AddDataSeries(Data.BarsPeriodType.Tick, 1);
            }
            else if (State == State.DataLoaded)
            {
                // Initialiser l'indicateur cumulative delta
                cumulativeDelta = OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0);
                deltaSinceResetSeries = new Series<double>(this, MaximumBarsLookBack.Infinite);
                lastResetTime = Times[0][0];
                resetDeltaValue = 0;
                barsSinceLastReset = 0;
            }
        }

        protected override void OnBarUpdate()
        {
			if (CurrentBars[0] < 20)
                return;
            if (BarsInProgress != 0)
                return;

            // Mettre à jour le delta cumulatif
            cumulativeDelta.Update(cumulativeDelta.BarsArray[1].Count - 1, 1);

            // Obtenir la valeur actuelle du delta cumulatif
            double currentDelta = cumulativeDelta.DeltaClose[0];

            // Vérifier si nous devons réinitialiser
            TimeSpan timeSinceLastReset = Times[0][0] - lastResetTime;
            if (timeSinceLastReset.TotalMinutes >= ResetIntervalMinutes)
            {
                // Réinitialiser
                lastResetTime = Times[0][0];
                resetDeltaValue = currentDelta;
                barsSinceLastReset = 0;
            }
            else
            {
                barsSinceLastReset++;
            }

            // Calculer le delta depuis la réinitialisation
            double deltaSinceReset = currentDelta - resetDeltaValue;

            // Stocker dans la série
            deltaSinceResetSeries[0] = deltaSinceReset;

            // Calculer la moyenne mobile sur deltaSinceResetSeries
            double sum = 0;
            int count = 0;
            for (int i = 0; i < MovingAveragePeriod && i <= barsSinceLastReset; i++)
            {
                if (i >= deltaSinceResetSeries.Count)
                    break;

                double val = deltaSinceResetSeries[i];
                sum += val;
                count++;
            }

            double movingAverage = count > 0 ? sum / count : double.NaN;

            // Tracer la moyenne mobile
            Values[0][0] = movingAverage;
        }

        #region Propriétés
        [Browsable(false)]
        [XmlIgnore()]
        public Series<double> DeltaMovingAverage
        {
            get { return Values[0]; }
        }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ninpai.CustomCumulativeDeltaMovingAverage[] cacheCustomCumulativeDeltaMovingAverage;
		public ninpai.CustomCumulativeDeltaMovingAverage CustomCumulativeDeltaMovingAverage(int movingAveragePeriod, int resetIntervalMinutes)
		{
			return CustomCumulativeDeltaMovingAverage(Input, movingAveragePeriod, resetIntervalMinutes);
		}

		public ninpai.CustomCumulativeDeltaMovingAverage CustomCumulativeDeltaMovingAverage(ISeries<double> input, int movingAveragePeriod, int resetIntervalMinutes)
		{
			if (cacheCustomCumulativeDeltaMovingAverage != null)
				for (int idx = 0; idx < cacheCustomCumulativeDeltaMovingAverage.Length; idx++)
					if (cacheCustomCumulativeDeltaMovingAverage[idx] != null && cacheCustomCumulativeDeltaMovingAverage[idx].MovingAveragePeriod == movingAveragePeriod && cacheCustomCumulativeDeltaMovingAverage[idx].ResetIntervalMinutes == resetIntervalMinutes && cacheCustomCumulativeDeltaMovingAverage[idx].EqualsInput(input))
						return cacheCustomCumulativeDeltaMovingAverage[idx];
			return CacheIndicator<ninpai.CustomCumulativeDeltaMovingAverage>(new ninpai.CustomCumulativeDeltaMovingAverage(){ MovingAveragePeriod = movingAveragePeriod, ResetIntervalMinutes = resetIntervalMinutes }, input, ref cacheCustomCumulativeDeltaMovingAverage);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.CustomCumulativeDeltaMovingAverage CustomCumulativeDeltaMovingAverage(int movingAveragePeriod, int resetIntervalMinutes)
		{
			return indicator.CustomCumulativeDeltaMovingAverage(Input, movingAveragePeriod, resetIntervalMinutes);
		}

		public Indicators.ninpai.CustomCumulativeDeltaMovingAverage CustomCumulativeDeltaMovingAverage(ISeries<double> input , int movingAveragePeriod, int resetIntervalMinutes)
		{
			return indicator.CustomCumulativeDeltaMovingAverage(input, movingAveragePeriod, resetIntervalMinutes);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.CustomCumulativeDeltaMovingAverage CustomCumulativeDeltaMovingAverage(int movingAveragePeriod, int resetIntervalMinutes)
		{
			return indicator.CustomCumulativeDeltaMovingAverage(Input, movingAveragePeriod, resetIntervalMinutes);
		}

		public Indicators.ninpai.CustomCumulativeDeltaMovingAverage CustomCumulativeDeltaMovingAverage(ISeries<double> input , int movingAveragePeriod, int resetIntervalMinutes)
		{
			return indicator.CustomCumulativeDeltaMovingAverage(input, movingAveragePeriod, resetIntervalMinutes);
		}
	}
}

#endregion
