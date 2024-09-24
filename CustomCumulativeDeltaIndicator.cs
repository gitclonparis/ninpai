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
    public class CustomCumulativeDeltaIndicator : Indicator
    {
        private OrderFlowCumulativeDelta cumulativeDeltaSizeFilterZero;
        private OrderFlowCumulativeDelta cumulativeDeltaSizeFilterCustom;

        [NinjaScriptProperty]
        [Display(Name = "Delta Min", Order = 1, GroupName = "Parameters")]
        public double DeltaMin { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Delta Max", Order = 2, GroupName = "Parameters")]
        public double DeltaMax { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Size Filter", Order = 3, GroupName = "Parameters")]
        public int SizeFilter { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Nombre de barres précédentes", Order = 4, GroupName = "Parameters")]
        public int PreviousBarsToCheck { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                 = "Indicateur personnalisé basé sur le Cumulative Delta";
                Name                        = "CustomCumulativeDeltaIndicator";
                Calculate                   = Calculate.OnBarClose;
                IsOverlay                   = true;
                DisplayInDataBox            = true;
                DrawOnPricePanel            = true;
                DrawHorizontalGridLines     = true;
                DrawVerticalGridLines       = true;
                PaintPriceMarkers           = true;

                DeltaMin                    = 1000;     // Valeur par défaut
                DeltaMax                    = 10000;    // Valeur par défaut
                SizeFilter                  = 0;
                PreviousBarsToCheck         = 3;        // Par défaut, on vérifie les 3 dernières barres delta
            }
            else if (State == State.Configure)
            {
                // Ajouter une série de données de ticks nécessaire pour le Cumulative Delta
                AddDataSeries(Data.BarsPeriodType.Tick, 1);
            }
            else if (State == State.DataLoaded)
            {
                // Initialiser les instances de l'indicateur Cumulative Delta
                cumulativeDeltaSizeFilterZero = OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0);
                cumulativeDeltaSizeFilterCustom = OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, SizeFilter);
            }
        }

        protected override void OnBarUpdate()
        {
            // S'assurer que nous sommes sur la série principale
            if (BarsInProgress != 0)
                return;

            // Vérifier que nous avons suffisamment de données
            if (CurrentBar < PreviousBarsToCheck)
                return;

            // Mettre à jour les indicateurs Cumulative Delta
            cumulativeDeltaSizeFilterZero.Update(cumulativeDeltaSizeFilterZero.BarsArray[1].Count - 1, 1);
            cumulativeDeltaSizeFilterCustom.Update(cumulativeDeltaSizeFilterCustom.BarsArray[1].Count - 1, 1);

            // Conditions pour sizeFilter = 0
            double currentDeltaZero = cumulativeDeltaSizeFilterZero.DeltaClose[0];

            if (IsLimousine(currentDeltaZero))
            {
                if (BreaksHighs(cumulativeDeltaSizeFilterZero, PreviousBarsToCheck))
                {
                    // Afficher une flèche vers le haut
                    Draw.ArrowUp(this, "ArrowUpZero" + CurrentBar, true, 0, Low[0] - TickSize * 2, Brushes.Green);
                }
                else if (BreaksLows(cumulativeDeltaSizeFilterZero, PreviousBarsToCheck))
                {
                    // Afficher une flèche vers le bas
                    Draw.ArrowDown(this, "ArrowDownZero" + CurrentBar, true, 0, High[0] + TickSize * 2, Brushes.Red);
                }
            }

            // Conditions pour sizeFilter = SizeFilter défini par l'utilisateur
            double currentDeltaCustom = cumulativeDeltaSizeFilterCustom.DeltaClose[0];

            if (IsLimousine(currentDeltaCustom))
            {
                if (BreaksHighs(cumulativeDeltaSizeFilterCustom, PreviousBarsToCheck))
                {
                    // Afficher une flèche vers le haut
                    Draw.ArrowUp(this, "ArrowUpCustom" + CurrentBar, true, 0, Low[0] - TickSize * 20, Brushes.Blue);
                }
                else if (BreaksLows(cumulativeDeltaSizeFilterCustom, PreviousBarsToCheck))
                {
                    // Afficher une flèche vers le bas
                    Draw.ArrowDown(this, "ArrowDownCustom" + CurrentBar, true, 0, High[0] + TickSize * 20, Brushes.Orange);
                }
            }
        }

        // Fonction pour déterminer si la barre actuelle est une "limousine"
        private bool IsLimousine(double deltaValue)
        {
            return deltaValue >= DeltaMin && deltaValue <= DeltaMax;
        }

        // Fonction pour vérifier la cassure des plus hauts
        private bool BreaksHighs(OrderFlowCumulativeDelta cumulativeDelta, int barsToCheck)
        {
            double currentDelta = cumulativeDelta.DeltaClose[0];
            for (int i = 1; i <= barsToCheck; i++)
            {
                if (i >= cumulativeDelta.DeltaClose.Count)
                    return false;

                if (currentDelta <= cumulativeDelta.DeltaClose[i])
                    return false;
            }
            return true;
        }

        // Fonction pour vérifier la cassure des plus bas
        private bool BreaksLows(OrderFlowCumulativeDelta cumulativeDelta, int barsToCheck)
        {
            double currentDelta = cumulativeDelta.DeltaClose[0];
            for (int i = 1; i <= barsToCheck; i++)
            {
                if (i >= cumulativeDelta.DeltaClose.Count)
                    return false;

                if (currentDelta >= cumulativeDelta.DeltaClose[i])
                    return false;
            }
            return true;
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ninpai.CustomCumulativeDeltaIndicator[] cacheCustomCumulativeDeltaIndicator;
		public ninpai.CustomCumulativeDeltaIndicator CustomCumulativeDeltaIndicator(double deltaMin, double deltaMax, int sizeFilter, int previousBarsToCheck)
		{
			return CustomCumulativeDeltaIndicator(Input, deltaMin, deltaMax, sizeFilter, previousBarsToCheck);
		}

		public ninpai.CustomCumulativeDeltaIndicator CustomCumulativeDeltaIndicator(ISeries<double> input, double deltaMin, double deltaMax, int sizeFilter, int previousBarsToCheck)
		{
			if (cacheCustomCumulativeDeltaIndicator != null)
				for (int idx = 0; idx < cacheCustomCumulativeDeltaIndicator.Length; idx++)
					if (cacheCustomCumulativeDeltaIndicator[idx] != null && cacheCustomCumulativeDeltaIndicator[idx].DeltaMin == deltaMin && cacheCustomCumulativeDeltaIndicator[idx].DeltaMax == deltaMax && cacheCustomCumulativeDeltaIndicator[idx].SizeFilter == sizeFilter && cacheCustomCumulativeDeltaIndicator[idx].PreviousBarsToCheck == previousBarsToCheck && cacheCustomCumulativeDeltaIndicator[idx].EqualsInput(input))
						return cacheCustomCumulativeDeltaIndicator[idx];
			return CacheIndicator<ninpai.CustomCumulativeDeltaIndicator>(new ninpai.CustomCumulativeDeltaIndicator(){ DeltaMin = deltaMin, DeltaMax = deltaMax, SizeFilter = sizeFilter, PreviousBarsToCheck = previousBarsToCheck }, input, ref cacheCustomCumulativeDeltaIndicator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.CustomCumulativeDeltaIndicator CustomCumulativeDeltaIndicator(double deltaMin, double deltaMax, int sizeFilter, int previousBarsToCheck)
		{
			return indicator.CustomCumulativeDeltaIndicator(Input, deltaMin, deltaMax, sizeFilter, previousBarsToCheck);
		}

		public Indicators.ninpai.CustomCumulativeDeltaIndicator CustomCumulativeDeltaIndicator(ISeries<double> input , double deltaMin, double deltaMax, int sizeFilter, int previousBarsToCheck)
		{
			return indicator.CustomCumulativeDeltaIndicator(input, deltaMin, deltaMax, sizeFilter, previousBarsToCheck);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.CustomCumulativeDeltaIndicator CustomCumulativeDeltaIndicator(double deltaMin, double deltaMax, int sizeFilter, int previousBarsToCheck)
		{
			return indicator.CustomCumulativeDeltaIndicator(Input, deltaMin, deltaMax, sizeFilter, previousBarsToCheck);
		}

		public Indicators.ninpai.CustomCumulativeDeltaIndicator CustomCumulativeDeltaIndicator(ISeries<double> input , double deltaMin, double deltaMax, int sizeFilter, int previousBarsToCheck)
		{
			return indicator.CustomCumulativeDeltaIndicator(input, deltaMin, deltaMax, sizeFilter, previousBarsToCheck);
		}
	}
}

#endregion
