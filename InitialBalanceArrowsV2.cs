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
    public class InitialBalanceArrowsV2 : Indicator
    {
		 #region Variables
        private double ibHigh = double.MinValue;
        private double ibLow = double.MaxValue;
        private bool ibPeriod = true;
        private Dictionary<DateTime, double> ibHighs = new Dictionary<DateTime, double>();
        private Dictionary<DateTime, double> ibLows = new Dictionary<DateTime, double>();
        #endregion

        #region Propriétés
        [NinjaScriptProperty]
        [Display(Name = "Activer IB", Order = 1, GroupName = "Paramètres")]
        public bool ActivateIB { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Heure début IB", Order = 2, GroupName = "Paramètres")]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        public DateTime IBStartTime { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Heure fin IB", Order = 3, GroupName = "Paramètres")]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        public DateTime IBEndTime { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Ticks de décalage", Order = 4, GroupName = "Paramètres")]
        public int OffsetTicks { get; set; }
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                                    = @"Affiche des flèches et des lignes basées sur l'Initial Balance.";
                Name                                           = "InitialBalanceArrowsV2";
                Calculate                                      = Calculate.OnBarClose;
                IsOverlay                                      = true;
                DisplayInDataBox                               = false;
                DrawOnPricePanel                               = true;
                PaintPriceMarkers                              = false;
                // Par défaut, l'IB commence à 15h30 et se termine à 16h30 (heure française)
				ActivateIB                                     = true;
                IBStartTime                                    = DateTime.Parse("15:30", System.Globalization.CultureInfo.InvariantCulture);
                IBEndTime                                      = DateTime.Parse("16:30", System.Globalization.CultureInfo.InvariantCulture);
                OffsetTicks                                    = 0;
            }
            // else if (State == State.Configure)
            // {
                // ibStartTime = new DateTime();
                // ibEndTime = new DateTime();
            // }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < BarsRequiredToPlot || !ActivateIB)
                return;

            DateTime barTime = Time[0];

            // Obtenir la date de la session actuelle
            DateTime sessionDate = Times[0][0].Date;

            // Déterminer l'heure de début et de fin de l'IB pour la session actuelle
            DateTime ibStart = sessionDate.AddHours(IBStartTime.Hour).AddMinutes(IBStartTime.Minute);
            DateTime ibEnd = sessionDate.AddHours(IBEndTime.Hour).AddMinutes(IBEndTime.Minute);

            // Gérer le cas où l'heure de fin est inférieure à l'heure de début (IB traversant minuit)
            if (ibEnd <= ibStart)
                ibEnd = ibEnd.AddDays(1);

            // Réinitialiser les valeurs IB au début de chaque session
            if (Bars.IsFirstBarOfSession)
            {
                ibHigh = double.MinValue;
                ibLow = double.MaxValue;
                ibPeriod = true;
            }

            // Pendant la période IB
            if (Time[0] >= ibStart && Time[0] <= ibEnd)
            {
                ibHigh = Math.Max(ibHigh, High[0]);
                ibLow = Math.Min(ibLow, Low[0]);

                // Afficher les flèches haut et bas
                Draw.ArrowUp(this, "UpArrow" + CurrentBar, false, 0, Low[0] - TickSize, Brushes.Green);
                Draw.ArrowDown(this, "DownArrow" + CurrentBar, false, 0, High[0] + TickSize, Brushes.Red);
            }
            // Après la période IB
            else if (Time[0] > ibEnd && ibPeriod)
            {
                // Stocker les IBH et IBL pour la session
                if (!ibHighs.ContainsKey(sessionDate))
                {
                    ibHighs[sessionDate] = ibHigh;
                    ibLows[sessionDate] = ibLow;
                }
                ibPeriod = false;
            }

            // Tracer les lignes IBH et IBL pour les sessions précédentes
            foreach (var date in ibHighs.Keys)
            {
                double sessionIBHigh = ibHighs[date];
                double sessionIBLow = ibLows[date];

                // Trouver les indices des barres pour la session
                int sessionStartBar = -1;
                int sessionEndBar = -1;

                // Recherche du début de la session
                for (int i = CurrentBar; i >= 0; i--)
                {
                    if (Times[0][i].Date == date && Times[0][i] >= ibStart)
                    {
                        sessionStartBar = i;
                    }
                    if (Times[0][i].Date < date)
                        break;
                }

                // Recherche de la fin de la session
                for (int i = sessionStartBar; i >= 0; i--)
                {
                    if (Times[0][i].Date == date && Times[0][i] > ibEnd)
                    {
                        sessionEndBar = i;
                        break;
                    }
                }

                if (sessionEndBar == -1)
                    sessionEndBar = sessionStartBar;

                // Tracer la ligne IBH
                Draw.Line(this, "IBHLine" + date.ToString("yyyyMMdd"), false, sessionStartBar - CurrentBar, sessionIBHigh, sessionEndBar - CurrentBar, sessionIBHigh, Brushes.Blue, DashStyleHelper.Solid, 2);

                // Tracer la ligne IBL
                Draw.Line(this, "IBLLine" + date.ToString("yyyyMMdd"), false, sessionStartBar - CurrentBar, sessionIBLow, sessionEndBar - CurrentBar, sessionIBLow, Brushes.Blue, DashStyleHelper.Solid, 2);
            }

            // Logique pour les flèches après la période IB
            if (!ibPeriod && ibHigh != double.MinValue && ibLow != double.MaxValue)
            {
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
		private ninpai.InitialBalanceArrowsV2[] cacheInitialBalanceArrowsV2;
		public ninpai.InitialBalanceArrowsV2 InitialBalanceArrowsV2(bool activateIB, DateTime iBStartTime, DateTime iBEndTime, int offsetTicks)
		{
			return InitialBalanceArrowsV2(Input, activateIB, iBStartTime, iBEndTime, offsetTicks);
		}

		public ninpai.InitialBalanceArrowsV2 InitialBalanceArrowsV2(ISeries<double> input, bool activateIB, DateTime iBStartTime, DateTime iBEndTime, int offsetTicks)
		{
			if (cacheInitialBalanceArrowsV2 != null)
				for (int idx = 0; idx < cacheInitialBalanceArrowsV2.Length; idx++)
					if (cacheInitialBalanceArrowsV2[idx] != null && cacheInitialBalanceArrowsV2[idx].ActivateIB == activateIB && cacheInitialBalanceArrowsV2[idx].IBStartTime == iBStartTime && cacheInitialBalanceArrowsV2[idx].IBEndTime == iBEndTime && cacheInitialBalanceArrowsV2[idx].OffsetTicks == offsetTicks && cacheInitialBalanceArrowsV2[idx].EqualsInput(input))
						return cacheInitialBalanceArrowsV2[idx];
			return CacheIndicator<ninpai.InitialBalanceArrowsV2>(new ninpai.InitialBalanceArrowsV2(){ ActivateIB = activateIB, IBStartTime = iBStartTime, IBEndTime = iBEndTime, OffsetTicks = offsetTicks }, input, ref cacheInitialBalanceArrowsV2);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.InitialBalanceArrowsV2 InitialBalanceArrowsV2(bool activateIB, DateTime iBStartTime, DateTime iBEndTime, int offsetTicks)
		{
			return indicator.InitialBalanceArrowsV2(Input, activateIB, iBStartTime, iBEndTime, offsetTicks);
		}

		public Indicators.ninpai.InitialBalanceArrowsV2 InitialBalanceArrowsV2(ISeries<double> input , bool activateIB, DateTime iBStartTime, DateTime iBEndTime, int offsetTicks)
		{
			return indicator.InitialBalanceArrowsV2(input, activateIB, iBStartTime, iBEndTime, offsetTicks);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.InitialBalanceArrowsV2 InitialBalanceArrowsV2(bool activateIB, DateTime iBStartTime, DateTime iBEndTime, int offsetTicks)
		{
			return indicator.InitialBalanceArrowsV2(Input, activateIB, iBStartTime, iBEndTime, offsetTicks);
		}

		public Indicators.ninpai.InitialBalanceArrowsV2 InitialBalanceArrowsV2(ISeries<double> input , bool activateIB, DateTime iBStartTime, DateTime iBEndTime, int offsetTicks)
		{
			return indicator.InitialBalanceArrowsV2(input, activateIB, iBStartTime, iBEndTime, offsetTicks);
		}
	}
}

#endregion
