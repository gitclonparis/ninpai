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
    public class CombinedIndicatorV4 : Indicator
    {
        // Variables de LimusineIndicatorV2
        private int minimumTicks;
        private bool showLimusineOpenCloseUP;
        private bool showLimusineOpenCloseDOWN;
        private bool showLimusineHighLowUP;
        private bool showLimusineHighLowDOWN;

        // Variables de BVAv11092024
        private double sumPriceVolume;
        private double sumVolume;
        private double sumSquaredPriceVolume;
        private DateTime lastResetTime;
        private int barsSinceReset;
        private int upperBreakoutCount;
        private int lowerBreakoutCount;

        private ADX ADX1;
        private ATR ATR1;
        private VOL VOL1;
        private VOLMA VOLMA1;

        // Ajout des variables manquantes
        private double openCloseDiff;
        private double highLowDiff;

        // Méthode OnStateChange
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Indicateur combiné";
                Name = "CombinedIndicatorV4";
                Calculate = Calculate.OnEachTick;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive = true;

                // Défauts LimusineIndicatorV2
                MinimumTicks = 20;
                ShowLimusineOpenCloseUP = true;
                ShowLimusineOpenCloseDOWN = true;
                ShowLimusineHighLowUP = true;
                ShowLimusineHighLowDOWN = true;

                // Défauts BVAv11092024
                ResetPeriod = 120;
                MinBarsForSignal = 10;

                MinEntryDistanceUP = 3;
                MaxEntryDistanceUP = 40;
                MaxUpperBreakouts = 3;
                OKisAfterBarsSinceResetUP = true;
                OKisAboveUpperThreshold = true;
                OKisWithinMaxEntryDistance = true;
                OKisUpperBreakoutCountExceeded = true;

                MinEntryDistanceDOWN = 3;
                MaxEntryDistanceDOWN = 40;
                MaxLowerBreakouts = 3;
                OKisAfterBarsSinceResetDown = true;
                OKisBelovLowerThreshold = true;
                OKisWithinMaxEntryDistanceDown = true;
                OKisLowerBreakoutCountExceeded = true;

                FminADX = 0;
                FmaxADX = 0;
                OKisADX = false;

                FminATR = 0;
                FmaxATR = 0;
                OKisATR = false;

                FperiodVol = 9;
                OKisVOL = false;

                // Ajouter les tracés de BVAv11092024
                AddPlot(Brushes.Orange, "VWAP");
                AddPlot(Brushes.Red, "StdDev1Upper");
                AddPlot(Brushes.Red, "StdDev1Lower");
                AddPlot(Brushes.Green, "StdDev2Upper");
                AddPlot(Brushes.Green, "StdDev2Lower");
                AddPlot(Brushes.Blue, "StdDev3Upper");
                AddPlot(Brushes.Blue, "StdDev3Lower");
            }
            else if (State == State.Configure)
            {
                AddDataSeries(Data.BarsPeriodType.Tick, 1);
                ResetValues(DateTime.MinValue);
            }
            else if (State == State.DataLoaded)
            {
                ADX1 = ADX(Close, 14);
                ATR1 = ATR(Close, 14);
                VOL1 = VOL(Close);
                VOLMA1 = VOLMA(Close, FperiodVol);
            }
        }

        // Méthode OnBarUpdate
        protected override void OnBarUpdate()
        {
            if (CurrentBars[0] < 20) return;

            DateTime currentBarTime = Time[0];

            // Logique de réinitialisation de BVAv11092024
            if (Bars.IsFirstBarOfSession)
            {
                ResetValues(currentBarTime);
            }
            else if (lastResetTime != DateTime.MinValue && (currentBarTime - lastResetTime).TotalMinutes >= ResetPeriod)
            {
                ResetValues(currentBarTime);
            }

            if (BarsInProgress == 1)
            {
                // Mettre à jour le delta cumulatif si nécessaire
                return;
            }

            // Calculs de LimusineIndicatorV2
            openCloseDiff = Math.Abs(Open[0] - Close[0]) / TickSize;
            highLowDiff = Math.Abs(High[0] - Low[0]) / TickSize;

            // Calculs de BVAv11092024
            double typicalPrice = (High[0] + Low[0] + Close[0]) / 3;
            double volume = Volume[0];

            sumPriceVolume += typicalPrice * volume;
            sumVolume += volume;
            sumSquaredPriceVolume += typicalPrice * typicalPrice * volume;

            double vwap = sumPriceVolume / sumVolume;
            double variance = (sumSquaredPriceVolume / sumVolume) - (vwap * vwap);
            double stdDev = Math.Sqrt(variance);

            Values[0][0] = vwap;
            Values[1][0] = vwap + stdDev;
            Values[2][0] = vwap - stdDev;
            Values[3][0] = vwap + 2 * stdDev;
            Values[4][0] = vwap - 2 * stdDev;
            Values[5][0] = vwap + 3 * stdDev;
            Values[6][0] = vwap - 3 * stdDev;

            barsSinceReset++;

            // Appel des méthodes ShouldBuy et ShouldSell
            ShouldBuy();
            ShouldSell();
        }

        // Méthode ShouldBuy modifiée
        private void ShouldBuy()
        {
            bool isLimusineOpenCloseUP = ShowLimusineOpenCloseUP && openCloseDiff >= MinimumTicks && Close[0] > Open[0];
            bool isLimusineHighLowUP = ShowLimusineHighLowUP && highLowDiff >= MinimumTicks && Close[0] > Open[0];
            bool isADX = OKisADX && ADX1[0] > FminADX && ADX1[0] < FmaxADX;
            bool isATR = OKisATR && ATR1[0] > FminATR && ATR1[0] < FmaxATR;
            bool isVOL = OKisVOL && VOL1[0] > VOLMA1[0];
            bool isAfterBarsSinceResetUP = OKisAfterBarsSinceResetUP && barsSinceReset > MinBarsForSignal;
            bool isAboveUpperThreshold = OKisAboveUpperThreshold && Close[0] > Values[1][0] + MinEntryDistanceUP * TickSize;
            bool isWithinMaxEntryDistance = OKisWithinMaxEntryDistance && Close[0] <= Values[1][0] + MaxEntryDistanceUP * TickSize;
            bool isUpperBreakoutCountExceeded = OKisUpperBreakoutCountExceeded && upperBreakoutCount < MaxUpperBreakouts;

            bool buyCondition = (Close[0] > Open[0]) &&
                   (isADX) &&
                   (isATR) &&
                   (isVOL) &&
                   (isAfterBarsSinceResetUP) &&
                   (isAboveUpperThreshold) &&
                   (isWithinMaxEntryDistance) &&
                   (isUpperBreakoutCountExceeded) &&
                   (isLimusineOpenCloseUP) &&
                   (isLimusineHighLowUP);

            if (buyCondition)
            {
                Draw.ArrowUp(this, "CombinedUpArrow" + CurrentBar, true, 0, Low[0] - 2 * TickSize, Brushes.Green);
                upperBreakoutCount++;
            }
        }

        // Méthode ShouldSell modifiée
        private void ShouldSell()
        {
            bool isLimusineOpenCloseDOWN = ShowLimusineOpenCloseDOWN && openCloseDiff >= MinimumTicks && Close[0] < Open[0];
            bool isLimusineHighLowDOWN = ShowLimusineHighLowDOWN && highLowDiff >= MinimumTicks && Close[0] < Open[0];
            bool isADX = OKisADX && ADX1[0] > FminADX && ADX1[0] < FmaxADX;
            bool isATR = OKisATR && ATR1[0] > FminATR && ATR1[0] < FmaxATR;
            bool isVOL = OKisVOL && VOL1[0] > VOLMA1[0];
            bool isAfterBarsSinceResetDown = OKisAfterBarsSinceResetDown && barsSinceReset > MinBarsForSignal;
            bool isBelovLowerThreshold = OKisBelovLowerThreshold && Close[0] < Values[2][0] - MinEntryDistanceDOWN * TickSize;
            bool isWithinMaxEntryDistanceDown = OKisWithinMaxEntryDistanceDown && Close[0] >= Values[2][0] - MaxEntryDistanceDOWN * TickSize;
            bool isLowerBreakoutCountExceeded = OKisLowerBreakoutCountExceeded && lowerBreakoutCount < MaxLowerBreakouts;

            bool sellCondition = (Close[0] < Open[0]) &&
                   (isADX) &&
                   (isATR) &&
                   (isVOL) &&
                   (isAfterBarsSinceResetDown) &&
                   (isBelovLowerThreshold) &&
                   (isWithinMaxEntryDistanceDown) &&
                   (isLowerBreakoutCountExceeded) &&
                   (isLimusineOpenCloseDOWN) &&
                   (isLimusineHighLowDOWN);

            if (sellCondition)
            {
                Draw.ArrowDown(this, "CombinedDownArrow" + CurrentBar, true, 0, High[0] + 2 * TickSize, Brushes.Red);
                lowerBreakoutCount++;
            }
        }

        // Méthode ResetValues de BVAv11092024
        private void ResetValues(DateTime resetTime)
        {
            sumPriceVolume = 0;
            sumVolume = 0;
            sumSquaredPriceVolume = 0;
            barsSinceReset = 0;
            upperBreakoutCount = 0;
            lowerBreakoutCount = 0;
            lastResetTime = resetTime;
        }

        #region Properties
        // Propriétés de LimusineIndicatorV2
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Minimum Ticks", Description = "Nombre minimum de ticks pour une limusine", Order = 1, GroupName = "Limusine Parameters")]
        public int MinimumTicks { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Afficher Limusine Open-Close UP", Description = "Afficher les limusines Open-Close UP", Order = 2, GroupName = "Limusine Parameters")]
        public bool ShowLimusineOpenCloseUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Afficher Limusine Open-Close DOWN", Description = "Afficher les limusines Open-Close DOWN", Order = 3, GroupName = "Limusine Parameters")]
        public bool ShowLimusineOpenCloseDOWN { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Afficher Limusine High-Low UP", Description = "Afficher les limusines High-Low UP", Order = 4, GroupName = "Limusine Parameters")]
        public bool ShowLimusineHighLowUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Afficher Limusine High-Low DOWN", Description = "Afficher les limusines High-Low DOWN", Order = 5, GroupName = "Limusine Parameters")]
        public bool ShowLimusineHighLowDOWN { get; set; }

        // Propriétés de BVAv11092024
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Reset Period (Minutes)", Order = 1, GroupName = "BVAv Parameters")]
        public int ResetPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Min Bars for Signal", Order = 2, GroupName = "BVAv Parameters")]
        public int MinBarsForSignal { get; set; }

        // Propriétés supplémentaires pour BVAv11092024
        // Propriétés d'achat
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Min Entry Distance UP", Order = 1, GroupName = "Buy")]
        public int MinEntryDistanceUP { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Max Entry Distance UP", Order = 2, GroupName = "Buy")]
        public int MaxEntryDistanceUP { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Max Upper Breakouts", Order = 3, GroupName = "Buy")]
        public int MaxUpperBreakouts { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Check Bars Since Reset UP", Order = 4, GroupName = "Buy")]
        public bool OKisAfterBarsSinceResetUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Check Above Upper Threshold", Order = 5, GroupName = "Buy")]
        public bool OKisAboveUpperThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Check Within Max Entry Distance", Order = 6, GroupName = "Buy")]
        public bool OKisWithinMaxEntryDistance { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Check Upper Breakout Count Exceeded", Order = 7, GroupName = "Buy")]
        public bool OKisUpperBreakoutCountExceeded { get; set; }

        // Propriétés de vente
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Min Entry Distance DOWN", Order = 1, GroupName = "Sell")]
        public int MinEntryDistanceDOWN { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Max Entry Distance DOWN", Order = 2, GroupName = "Sell")]
        public int MaxEntryDistanceDOWN { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Max Lower Breakouts", Order = 3, GroupName = "Sell")]
        public int MaxLowerBreakouts { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Check Bars Since Reset Down", Order = 4, GroupName = "Sell")]
        public bool OKisAfterBarsSinceResetDown { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Check Below Lower Threshold", Order = 5, GroupName = "Sell")]
        public bool OKisBelovLowerThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Check Within Max Entry Distance Down", Order = 6, GroupName = "Sell")]
        public bool OKisWithinMaxEntryDistanceDown { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Check Lower Breakout Count Exceeded", Order = 7, GroupName = "Sell")]
        public bool OKisLowerBreakoutCountExceeded { get; set; }

        // Propriétés ADX
        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "Fmin ADX", Order = 1, GroupName = "ADX")]
        public double FminADX { get; set; }

        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "Fmax ADX", Order = 2, GroupName = "ADX")]
        public double FmaxADX { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Check ADX", Order = 3, GroupName = "ADX")]
        public bool OKisADX { get; set; }

        // Propriétés ATR
        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "Fmin ATR", Order = 1, GroupName = "ATR")]
        public double FminATR { get; set; }

        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "Fmax ATR", Order = 2, GroupName = "ATR")]
        public double FmaxATR { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Check ATR", Order = 3, GroupName = "ATR")]
        public bool OKisATR { get; set; }

        // Propriétés Volume
        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Fperiod Vol", Order = 1, GroupName = "Volume")]
        public int FperiodVol { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Check Volume", Order = 2, GroupName = "Volume")]
        public bool OKisVOL { get; set; }

        // Propriétés pour les tracés
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> VWAP => Values[0];

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> StdDev1Upper => Values[1];

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> StdDev1Lower => Values[2];
        #endregion

        // Ajoutez d'autres propriétés si nécessaire
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ninpai.CombinedIndicatorV4[] cacheCombinedIndicatorV4;
		public ninpai.CombinedIndicatorV4 CombinedIndicatorV4(int minimumTicks, bool showLimusineOpenCloseUP, bool showLimusineOpenCloseDOWN, bool showLimusineHighLowUP, bool showLimusineHighLowDOWN, int resetPeriod, int minBarsForSignal, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, bool oKisAfterBarsSinceResetUP, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, bool oKisAfterBarsSinceResetDown, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded, double fminADX, double fmaxADX, bool oKisADX, double fminATR, double fmaxATR, bool oKisATR, int fperiodVol, bool oKisVOL)
		{
			return CombinedIndicatorV4(Input, minimumTicks, showLimusineOpenCloseUP, showLimusineOpenCloseDOWN, showLimusineHighLowUP, showLimusineHighLowDOWN, resetPeriod, minBarsForSignal, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, oKisAfterBarsSinceResetUP, oKisAboveUpperThreshold, oKisWithinMaxEntryDistance, oKisUpperBreakoutCountExceeded, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts, oKisAfterBarsSinceResetDown, oKisBelovLowerThreshold, oKisWithinMaxEntryDistanceDown, oKisLowerBreakoutCountExceeded, fminADX, fmaxADX, oKisADX, fminATR, fmaxATR, oKisATR, fperiodVol, oKisVOL);
		}

		public ninpai.CombinedIndicatorV4 CombinedIndicatorV4(ISeries<double> input, int minimumTicks, bool showLimusineOpenCloseUP, bool showLimusineOpenCloseDOWN, bool showLimusineHighLowUP, bool showLimusineHighLowDOWN, int resetPeriod, int minBarsForSignal, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, bool oKisAfterBarsSinceResetUP, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, bool oKisAfterBarsSinceResetDown, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded, double fminADX, double fmaxADX, bool oKisADX, double fminATR, double fmaxATR, bool oKisATR, int fperiodVol, bool oKisVOL)
		{
			if (cacheCombinedIndicatorV4 != null)
				for (int idx = 0; idx < cacheCombinedIndicatorV4.Length; idx++)
					if (cacheCombinedIndicatorV4[idx] != null && cacheCombinedIndicatorV4[idx].MinimumTicks == minimumTicks && cacheCombinedIndicatorV4[idx].ShowLimusineOpenCloseUP == showLimusineOpenCloseUP && cacheCombinedIndicatorV4[idx].ShowLimusineOpenCloseDOWN == showLimusineOpenCloseDOWN && cacheCombinedIndicatorV4[idx].ShowLimusineHighLowUP == showLimusineHighLowUP && cacheCombinedIndicatorV4[idx].ShowLimusineHighLowDOWN == showLimusineHighLowDOWN && cacheCombinedIndicatorV4[idx].ResetPeriod == resetPeriod && cacheCombinedIndicatorV4[idx].MinBarsForSignal == minBarsForSignal && cacheCombinedIndicatorV4[idx].MinEntryDistanceUP == minEntryDistanceUP && cacheCombinedIndicatorV4[idx].MaxEntryDistanceUP == maxEntryDistanceUP && cacheCombinedIndicatorV4[idx].MaxUpperBreakouts == maxUpperBreakouts && cacheCombinedIndicatorV4[idx].OKisAfterBarsSinceResetUP == oKisAfterBarsSinceResetUP && cacheCombinedIndicatorV4[idx].OKisAboveUpperThreshold == oKisAboveUpperThreshold && cacheCombinedIndicatorV4[idx].OKisWithinMaxEntryDistance == oKisWithinMaxEntryDistance && cacheCombinedIndicatorV4[idx].OKisUpperBreakoutCountExceeded == oKisUpperBreakoutCountExceeded && cacheCombinedIndicatorV4[idx].MinEntryDistanceDOWN == minEntryDistanceDOWN && cacheCombinedIndicatorV4[idx].MaxEntryDistanceDOWN == maxEntryDistanceDOWN && cacheCombinedIndicatorV4[idx].MaxLowerBreakouts == maxLowerBreakouts && cacheCombinedIndicatorV4[idx].OKisAfterBarsSinceResetDown == oKisAfterBarsSinceResetDown && cacheCombinedIndicatorV4[idx].OKisBelovLowerThreshold == oKisBelovLowerThreshold && cacheCombinedIndicatorV4[idx].OKisWithinMaxEntryDistanceDown == oKisWithinMaxEntryDistanceDown && cacheCombinedIndicatorV4[idx].OKisLowerBreakoutCountExceeded == oKisLowerBreakoutCountExceeded && cacheCombinedIndicatorV4[idx].FminADX == fminADX && cacheCombinedIndicatorV4[idx].FmaxADX == fmaxADX && cacheCombinedIndicatorV4[idx].OKisADX == oKisADX && cacheCombinedIndicatorV4[idx].FminATR == fminATR && cacheCombinedIndicatorV4[idx].FmaxATR == fmaxATR && cacheCombinedIndicatorV4[idx].OKisATR == oKisATR && cacheCombinedIndicatorV4[idx].FperiodVol == fperiodVol && cacheCombinedIndicatorV4[idx].OKisVOL == oKisVOL && cacheCombinedIndicatorV4[idx].EqualsInput(input))
						return cacheCombinedIndicatorV4[idx];
			return CacheIndicator<ninpai.CombinedIndicatorV4>(new ninpai.CombinedIndicatorV4(){ MinimumTicks = minimumTicks, ShowLimusineOpenCloseUP = showLimusineOpenCloseUP, ShowLimusineOpenCloseDOWN = showLimusineOpenCloseDOWN, ShowLimusineHighLowUP = showLimusineHighLowUP, ShowLimusineHighLowDOWN = showLimusineHighLowDOWN, ResetPeriod = resetPeriod, MinBarsForSignal = minBarsForSignal, MinEntryDistanceUP = minEntryDistanceUP, MaxEntryDistanceUP = maxEntryDistanceUP, MaxUpperBreakouts = maxUpperBreakouts, OKisAfterBarsSinceResetUP = oKisAfterBarsSinceResetUP, OKisAboveUpperThreshold = oKisAboveUpperThreshold, OKisWithinMaxEntryDistance = oKisWithinMaxEntryDistance, OKisUpperBreakoutCountExceeded = oKisUpperBreakoutCountExceeded, MinEntryDistanceDOWN = minEntryDistanceDOWN, MaxEntryDistanceDOWN = maxEntryDistanceDOWN, MaxLowerBreakouts = maxLowerBreakouts, OKisAfterBarsSinceResetDown = oKisAfterBarsSinceResetDown, OKisBelovLowerThreshold = oKisBelovLowerThreshold, OKisWithinMaxEntryDistanceDown = oKisWithinMaxEntryDistanceDown, OKisLowerBreakoutCountExceeded = oKisLowerBreakoutCountExceeded, FminADX = fminADX, FmaxADX = fmaxADX, OKisADX = oKisADX, FminATR = fminATR, FmaxATR = fmaxATR, OKisATR = oKisATR, FperiodVol = fperiodVol, OKisVOL = oKisVOL }, input, ref cacheCombinedIndicatorV4);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.CombinedIndicatorV4 CombinedIndicatorV4(int minimumTicks, bool showLimusineOpenCloseUP, bool showLimusineOpenCloseDOWN, bool showLimusineHighLowUP, bool showLimusineHighLowDOWN, int resetPeriod, int minBarsForSignal, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, bool oKisAfterBarsSinceResetUP, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, bool oKisAfterBarsSinceResetDown, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded, double fminADX, double fmaxADX, bool oKisADX, double fminATR, double fmaxATR, bool oKisATR, int fperiodVol, bool oKisVOL)
		{
			return indicator.CombinedIndicatorV4(Input, minimumTicks, showLimusineOpenCloseUP, showLimusineOpenCloseDOWN, showLimusineHighLowUP, showLimusineHighLowDOWN, resetPeriod, minBarsForSignal, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, oKisAfterBarsSinceResetUP, oKisAboveUpperThreshold, oKisWithinMaxEntryDistance, oKisUpperBreakoutCountExceeded, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts, oKisAfterBarsSinceResetDown, oKisBelovLowerThreshold, oKisWithinMaxEntryDistanceDown, oKisLowerBreakoutCountExceeded, fminADX, fmaxADX, oKisADX, fminATR, fmaxATR, oKisATR, fperiodVol, oKisVOL);
		}

		public Indicators.ninpai.CombinedIndicatorV4 CombinedIndicatorV4(ISeries<double> input , int minimumTicks, bool showLimusineOpenCloseUP, bool showLimusineOpenCloseDOWN, bool showLimusineHighLowUP, bool showLimusineHighLowDOWN, int resetPeriod, int minBarsForSignal, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, bool oKisAfterBarsSinceResetUP, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, bool oKisAfterBarsSinceResetDown, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded, double fminADX, double fmaxADX, bool oKisADX, double fminATR, double fmaxATR, bool oKisATR, int fperiodVol, bool oKisVOL)
		{
			return indicator.CombinedIndicatorV4(input, minimumTicks, showLimusineOpenCloseUP, showLimusineOpenCloseDOWN, showLimusineHighLowUP, showLimusineHighLowDOWN, resetPeriod, minBarsForSignal, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, oKisAfterBarsSinceResetUP, oKisAboveUpperThreshold, oKisWithinMaxEntryDistance, oKisUpperBreakoutCountExceeded, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts, oKisAfterBarsSinceResetDown, oKisBelovLowerThreshold, oKisWithinMaxEntryDistanceDown, oKisLowerBreakoutCountExceeded, fminADX, fmaxADX, oKisADX, fminATR, fmaxATR, oKisATR, fperiodVol, oKisVOL);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.CombinedIndicatorV4 CombinedIndicatorV4(int minimumTicks, bool showLimusineOpenCloseUP, bool showLimusineOpenCloseDOWN, bool showLimusineHighLowUP, bool showLimusineHighLowDOWN, int resetPeriod, int minBarsForSignal, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, bool oKisAfterBarsSinceResetUP, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, bool oKisAfterBarsSinceResetDown, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded, double fminADX, double fmaxADX, bool oKisADX, double fminATR, double fmaxATR, bool oKisATR, int fperiodVol, bool oKisVOL)
		{
			return indicator.CombinedIndicatorV4(Input, minimumTicks, showLimusineOpenCloseUP, showLimusineOpenCloseDOWN, showLimusineHighLowUP, showLimusineHighLowDOWN, resetPeriod, minBarsForSignal, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, oKisAfterBarsSinceResetUP, oKisAboveUpperThreshold, oKisWithinMaxEntryDistance, oKisUpperBreakoutCountExceeded, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts, oKisAfterBarsSinceResetDown, oKisBelovLowerThreshold, oKisWithinMaxEntryDistanceDown, oKisLowerBreakoutCountExceeded, fminADX, fmaxADX, oKisADX, fminATR, fmaxATR, oKisATR, fperiodVol, oKisVOL);
		}

		public Indicators.ninpai.CombinedIndicatorV4 CombinedIndicatorV4(ISeries<double> input , int minimumTicks, bool showLimusineOpenCloseUP, bool showLimusineOpenCloseDOWN, bool showLimusineHighLowUP, bool showLimusineHighLowDOWN, int resetPeriod, int minBarsForSignal, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, bool oKisAfterBarsSinceResetUP, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, bool oKisAfterBarsSinceResetDown, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded, double fminADX, double fmaxADX, bool oKisADX, double fminATR, double fmaxATR, bool oKisATR, int fperiodVol, bool oKisVOL)
		{
			return indicator.CombinedIndicatorV4(input, minimumTicks, showLimusineOpenCloseUP, showLimusineOpenCloseDOWN, showLimusineHighLowUP, showLimusineHighLowDOWN, resetPeriod, minBarsForSignal, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, oKisAfterBarsSinceResetUP, oKisAboveUpperThreshold, oKisWithinMaxEntryDistance, oKisUpperBreakoutCountExceeded, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts, oKisAfterBarsSinceResetDown, oKisBelovLowerThreshold, oKisWithinMaxEntryDistanceDown, oKisLowerBreakoutCountExceeded, fminADX, fmaxADX, oKisADX, fminATR, fmaxATR, oKisATR, fperiodVol, oKisVOL);
		}
	}
}

#endregion
