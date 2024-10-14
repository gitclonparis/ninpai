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
//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators.ninpai
{
    public class InitialBalanceArrows : Indicator
    {
        #region Variables
        private double ibHigh = double.MinValue;
        private double ibLow = double.MaxValue;
        private bool ibPeriod = true;
        private DateTime ibStartTime;
        private DateTime ibEndTime;
        private bool sessionStarted = false;
        private string ibHighTag = "IBHLine";
        private string ibLowTag = "IBLLine";
        #endregion

        #region Propriétés
        [NinjaScriptProperty]
        [Display(Name = "Activer IB", Order = 1, GroupName = "Paramètres")]
        public bool ActivateIB { get; set; }

        [NinjaScriptProperty]
        [Range(1, 240)]
        [Display(Name = "Durée IB (minutes)", Order = 2, GroupName = "Paramètres")]
        public int IBDuration { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Ticks de décalage", Order = 3, GroupName = "Paramètres")]
        public int OffsetTicks { get; set; }
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                                    = @"Affiche des flèches et des lignes basées sur l'Initial Balance.";
                Name                                           = "InitialBalanceArrows";
                Calculate                                      = Calculate.OnBarClose;
                IsOverlay                                      = true;
                DisplayInDataBox                               = false;
                DrawOnPricePanel                               = true;
                PaintPriceMarkers                              = false;
                ActivateIB                                     = true;
                IBDuration                                     = 60;
                OffsetTicks                                    = 0;
            }
            else if (State == State.Configure)
            {
                ibStartTime = new DateTime();
                ibEndTime = new DateTime();
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 1 || !ActivateIB)
                return;

            DateTime barTime = Time[0];

            // Détection du début de session
            if (Bars.IsFirstBarOfSession)
            {
                sessionStarted = true;
                ibHigh = double.MinValue;
                ibLow = double.MaxValue;
                ibStartTime = new DateTime(barTime.Year, barTime.Month, barTime.Day, barTime.Hour, barTime.Minute, 0);
                ibEndTime = ibStartTime.AddMinutes(IBDuration);

                // Suppression des lignes IB précédentes
                RemoveDrawObject(ibHighTag);
                RemoveDrawObject(ibLowTag);
            }

            // Pendant la période IB
            if (barTime >= ibStartTime && barTime < ibEndTime)
            {
                ibPeriod = true;
                ibHigh = Math.Max(ibHigh, High[0]);
                ibLow = Math.Min(ibLow, Low[0]);

                // Afficher les flèches haut et bas
                Draw.ArrowUp(this, "UpArrow" + CurrentBar, false, 0, Low[0] - TickSize, Brushes.Green);
                Draw.ArrowDown(this, "DownArrow" + CurrentBar, false, 0, High[0] + TickSize, Brushes.Red);
            }
            // Après la période IB
            else if (barTime >= ibEndTime)
            {
                if (ibPeriod)
                {
                    // Tracer les lignes IBH et IBL une fois que la période IB est terminée
                    Draw.Line(this, ibHighTag, false, 0, ibHigh, -CurrentBar, ibHigh, Brushes.Blue, DashStyleHelper.Solid, 2);
                    Draw.Line(this, ibLowTag, false, 0, ibLow, -CurrentBar, ibLow, Brushes.Blue, DashStyleHelper.Solid, 2);
                    ibPeriod = false;
                }

                double upperBreak = ibHigh + OffsetTicks * TickSize;
                double lowerBreak = ibLow - OffsetTicks * TickSize;

                // Prix dans le range IB
                if (Close[0] <= upperBreak && Close[0] >= lowerBreak)
                {
                    // Afficher les flèches haut et bas
                    Draw.ArrowUp(this, "UpArrow" + CurrentBar, false, 0, Low[0] - TickSize, Brushes.Green);
                    Draw.ArrowDown(this, "DownArrow" + CurrentBar, false, 0, High[0] + TickSize, Brushes.Red);
                }
                // Prix casse au-dessus de IBH + décalage
                else if (Close[0] > upperBreak)
                {
                    // Afficher uniquement les flèches haut
                    Draw.ArrowUp(this, "UpArrow" + CurrentBar, false, 0, Low[0] - TickSize, Brushes.Green);
                }
                // Prix casse en dessous de IBL - décalage
                else if (Close[0] < lowerBreak)
                {
                    // Afficher uniquement les flèches bas
                    Draw.ArrowDown(this, "DownArrow" + CurrentBar, false, 0, High[0] + TickSize, Brushes.Red);
                }
            }
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ninpai.InitialBalanceArrows[] cacheInitialBalanceArrows;
		public ninpai.InitialBalanceArrows InitialBalanceArrows(bool activateIB, int iBDuration, int offsetTicks)
		{
			return InitialBalanceArrows(Input, activateIB, iBDuration, offsetTicks);
		}

		public ninpai.InitialBalanceArrows InitialBalanceArrows(ISeries<double> input, bool activateIB, int iBDuration, int offsetTicks)
		{
			if (cacheInitialBalanceArrows != null)
				for (int idx = 0; idx < cacheInitialBalanceArrows.Length; idx++)
					if (cacheInitialBalanceArrows[idx] != null && cacheInitialBalanceArrows[idx].ActivateIB == activateIB && cacheInitialBalanceArrows[idx].IBDuration == iBDuration && cacheInitialBalanceArrows[idx].OffsetTicks == offsetTicks && cacheInitialBalanceArrows[idx].EqualsInput(input))
						return cacheInitialBalanceArrows[idx];
			return CacheIndicator<ninpai.InitialBalanceArrows>(new ninpai.InitialBalanceArrows(){ ActivateIB = activateIB, IBDuration = iBDuration, OffsetTicks = offsetTicks }, input, ref cacheInitialBalanceArrows);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.InitialBalanceArrows InitialBalanceArrows(bool activateIB, int iBDuration, int offsetTicks)
		{
			return indicator.InitialBalanceArrows(Input, activateIB, iBDuration, offsetTicks);
		}

		public Indicators.ninpai.InitialBalanceArrows InitialBalanceArrows(ISeries<double> input , bool activateIB, int iBDuration, int offsetTicks)
		{
			return indicator.InitialBalanceArrows(input, activateIB, iBDuration, offsetTicks);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.InitialBalanceArrows InitialBalanceArrows(bool activateIB, int iBDuration, int offsetTicks)
		{
			return indicator.InitialBalanceArrows(Input, activateIB, iBDuration, offsetTicks);
		}

		public Indicators.ninpai.InitialBalanceArrows InitialBalanceArrows(ISeries<double> input , bool activateIB, int iBDuration, int offsetTicks)
		{
			return indicator.InitialBalanceArrows(input, activateIB, iBDuration, offsetTicks);
		}
	}
}

#endregion
