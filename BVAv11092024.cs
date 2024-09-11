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
using System.IO; 
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
    public class BVAv11092024 : Indicator
    {
        // Private variables
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

        // Override OnStateChange
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"BVAv11092024";
                Name = "BVAv11092024";
                Calculate = Calculate.OnEachTick;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                IsSuspendedWhileInactive = true;
                ResetPeriod = 120;
                MinBarsForSignal = 10;
				
                MinEntryDistanceUP = 3;
                MaxEntryDistanceUP = 40;
                MaxUpperBreakouts = 3;
				
                MinEntryDistanceDOWN = 3;
                MaxEntryDistanceDOWN = 40;
                MaxLowerBreakouts = 3;
				
                FminADX = 0;
                FmaxADX = 0;
                FminATR = 0;
                FmaxATR = 0;
                FperiodVol = 9;

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
                VOLMA1 = VOLMA(Close, Convert.ToInt32(FperiodVol));
            }
        }

        // Override OnBarUpdate
        protected override void OnBarUpdate()
        {
            if (CurrentBars[0] < 20) return;
            DateTime currentBarTime = Time[0];

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
                OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0).Update(
                    OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0).BarsArray[1].Count - 1, 1);
				
                OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0).Update(
                    OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0).BarsArray[1].Count - 1, 1);
                return;
            }

            // Calculate VWAP and Standard Deviations
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

            // Buy/Sell conditions (simplified for clarity)
            if (ShouldBuy())
            {
                Draw.ArrowUp(this, "UpArrow" + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Green);
                upperBreakoutCount++;
            }
            else if (ShouldSell())
            {
                Draw.ArrowDown(this, "DownArrow" + CurrentBar, true, 0, High[0] + TickSize, Brushes.Red);
                lowerBreakoutCount++;
            }
        }

        // Buy Condition
        private bool ShouldBuy()
        {
            return (Close[0] > Open[0]) &&
                   (!OKisADX || (ADX1[0] > FminADX && ADX1[0] < FmaxADX)) &&
                   (!OKisATR || (ATR1[0] > FminATR && ATR1[0] < FmaxATR)) &&
                   (!OKisVOL || (VOL1[0] > VOLMA1[0])) &&
                   (!OKisAfterBarsSinceResetUP || barsSinceReset > MinBarsForSignal) &&
                   (!OKisAboveUpperThreshold || Close[0] > (Values[1][0] + MinEntryDistanceUP * TickSize)) &&
                   (!OKisWithinMaxEntryDistance || Close[0] <= (Values[1][0] + MaxEntryDistanceUP * TickSize)) &&
                   (!OKisUpperBreakoutCountExceeded || upperBreakoutCount < MaxUpperBreakouts);
        }

        // Sell Condition
        private bool ShouldSell()
        {
            return (Close[0] < Open[0]) &&
                   (!OKisADX || (ADX1[0] > FminADX && ADX1[0] < FmaxADX)) &&
                   (!OKisATR || (ATR1[0] > FminATR && ATR1[0] < FmaxATR)) &&
                   (!OKisVOL || (VOL1[0] > VOLMA1[0])) &&
                   (!OKisAfterBarsSinceResetDown || barsSinceReset > MinBarsForSignal) &&
                   (!OKisBelovLowerThreshold || Close[0] < (Values[2][0] - MinEntryDistanceDOWN * TickSize)) &&
                   (!OKisWithinMaxEntryDistanceDown || Close[0] >= (Values[2][0] - MaxEntryDistanceDOWN * TickSize)) &&
                   (!OKisLowerBreakoutCountExceeded || lowerBreakoutCount < MaxLowerBreakouts);
        }

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
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Reset Period (Minutes)", Order = 1, GroupName = "Parameters")]
        public int ResetPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Min Bars for Signal", Order = 2, GroupName = "Parameters")]
        public int MinBarsForSignal { get; set; }
		
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
		[Range(0, double.MaxValue)]
		[Display(Name = "Fmin ADX", Order = 1, GroupName = "ADX")]
		public double FminADX { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Fmax ADX", Order = 2, GroupName = "ADX")]
		public double FmaxADX { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Fmin ATR", Order = 1, GroupName = "ATR")]
		public double FminATR { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Fmax ATR", Order = 2, GroupName = "ATR")]
		public double FmaxATR { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Fperiod Vol", Order = 1, GroupName = "Volume")]
		public int FperiodVol { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 1)]
		[Display(Name = "OKisADX", Description = "Check ADX", Order = 1, GroupName = "ADX")]
		public bool OKisADX { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 1)]
		[Display(Name = "OKisATR", Description = "Check ATR", Order = 1, GroupName = "ATR")]
		public bool OKisATR { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 1)]
		[Display(Name = "OKisVOL", Description = "Check Volume", Order = 1, GroupName = "Volume")]
		public bool OKisVOL { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 1)]
		[Display(Name = "OKisAfterBarsSinceResetUP", Description = "Check Bars Since Reset UP", Order = 1, GroupName = "Buy")]
		public bool OKisAfterBarsSinceResetUP { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 1)]
		[Display(Name = "OKisAboveUpperThreshold", Description = "Check Above Upper Threshold", Order = 1, GroupName = "Buy")]
		public bool OKisAboveUpperThreshold { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 1)]
		[Display(Name = "OKisWithinMaxEntryDistance", Description = "Check Within Max Entry Distance", Order = 1, GroupName = "Buy")]
		public bool OKisWithinMaxEntryDistance { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 1)]
		[Display(Name = "OKisUpperBreakoutCountExceeded", Description = "Check Upper Breakout Count Exceeded", Order = 1, GroupName = "Buy")]
		public bool OKisUpperBreakoutCountExceeded { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 1)]
		[Display(Name = "OKisAfterBarsSinceResetDown", Description = "Check Bars Since Reset Down", Order = 1, GroupName = "Sell")]
		public bool OKisAfterBarsSinceResetDown { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 1)]
		[Display(Name = "OKisBelovLowerThreshold", Description = "Check Below Lower Threshold", Order = 1, GroupName = "Sell")]
		public bool OKisBelovLowerThreshold { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 1)]
		[Display(Name = "OKisWithinMaxEntryDistanceDown", Description = "Check Within Max Entry Distance Down", Order = 1, GroupName = "Sell")]
		public bool OKisWithinMaxEntryDistanceDown { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 1)]
		[Display(Name = "OKisLowerBreakoutCountExceeded", Description = "Check Lower Breakout Count Exceeded", Order = 1, GroupName = "Sell")]
		public bool OKisLowerBreakoutCountExceeded { get; set; }

        // Additional Properties for Buy/Sell conditions...

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
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ninpai.BVAv11092024[] cacheBVAv11092024;
		public ninpai.BVAv11092024 BVAv11092024(int resetPeriod, int minBarsForSignal, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, double fminADX, double fmaxADX, double fminATR, double fmaxATR, int fperiodVol, bool oKisADX, bool oKisATR, bool oKisVOL, bool oKisAfterBarsSinceResetUP, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, bool oKisAfterBarsSinceResetDown, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded)
		{
			return BVAv11092024(Input, resetPeriod, minBarsForSignal, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts, fminADX, fmaxADX, fminATR, fmaxATR, fperiodVol, oKisADX, oKisATR, oKisVOL, oKisAfterBarsSinceResetUP, oKisAboveUpperThreshold, oKisWithinMaxEntryDistance, oKisUpperBreakoutCountExceeded, oKisAfterBarsSinceResetDown, oKisBelovLowerThreshold, oKisWithinMaxEntryDistanceDown, oKisLowerBreakoutCountExceeded);
		}

		public ninpai.BVAv11092024 BVAv11092024(ISeries<double> input, int resetPeriod, int minBarsForSignal, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, double fminADX, double fmaxADX, double fminATR, double fmaxATR, int fperiodVol, bool oKisADX, bool oKisATR, bool oKisVOL, bool oKisAfterBarsSinceResetUP, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, bool oKisAfterBarsSinceResetDown, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded)
		{
			if (cacheBVAv11092024 != null)
				for (int idx = 0; idx < cacheBVAv11092024.Length; idx++)
					if (cacheBVAv11092024[idx] != null && cacheBVAv11092024[idx].ResetPeriod == resetPeriod && cacheBVAv11092024[idx].MinBarsForSignal == minBarsForSignal && cacheBVAv11092024[idx].MinEntryDistanceUP == minEntryDistanceUP && cacheBVAv11092024[idx].MaxEntryDistanceUP == maxEntryDistanceUP && cacheBVAv11092024[idx].MaxUpperBreakouts == maxUpperBreakouts && cacheBVAv11092024[idx].MinEntryDistanceDOWN == minEntryDistanceDOWN && cacheBVAv11092024[idx].MaxEntryDistanceDOWN == maxEntryDistanceDOWN && cacheBVAv11092024[idx].MaxLowerBreakouts == maxLowerBreakouts && cacheBVAv11092024[idx].FminADX == fminADX && cacheBVAv11092024[idx].FmaxADX == fmaxADX && cacheBVAv11092024[idx].FminATR == fminATR && cacheBVAv11092024[idx].FmaxATR == fmaxATR && cacheBVAv11092024[idx].FperiodVol == fperiodVol && cacheBVAv11092024[idx].OKisADX == oKisADX && cacheBVAv11092024[idx].OKisATR == oKisATR && cacheBVAv11092024[idx].OKisVOL == oKisVOL && cacheBVAv11092024[idx].OKisAfterBarsSinceResetUP == oKisAfterBarsSinceResetUP && cacheBVAv11092024[idx].OKisAboveUpperThreshold == oKisAboveUpperThreshold && cacheBVAv11092024[idx].OKisWithinMaxEntryDistance == oKisWithinMaxEntryDistance && cacheBVAv11092024[idx].OKisUpperBreakoutCountExceeded == oKisUpperBreakoutCountExceeded && cacheBVAv11092024[idx].OKisAfterBarsSinceResetDown == oKisAfterBarsSinceResetDown && cacheBVAv11092024[idx].OKisBelovLowerThreshold == oKisBelovLowerThreshold && cacheBVAv11092024[idx].OKisWithinMaxEntryDistanceDown == oKisWithinMaxEntryDistanceDown && cacheBVAv11092024[idx].OKisLowerBreakoutCountExceeded == oKisLowerBreakoutCountExceeded && cacheBVAv11092024[idx].EqualsInput(input))
						return cacheBVAv11092024[idx];
			return CacheIndicator<ninpai.BVAv11092024>(new ninpai.BVAv11092024(){ ResetPeriod = resetPeriod, MinBarsForSignal = minBarsForSignal, MinEntryDistanceUP = minEntryDistanceUP, MaxEntryDistanceUP = maxEntryDistanceUP, MaxUpperBreakouts = maxUpperBreakouts, MinEntryDistanceDOWN = minEntryDistanceDOWN, MaxEntryDistanceDOWN = maxEntryDistanceDOWN, MaxLowerBreakouts = maxLowerBreakouts, FminADX = fminADX, FmaxADX = fmaxADX, FminATR = fminATR, FmaxATR = fmaxATR, FperiodVol = fperiodVol, OKisADX = oKisADX, OKisATR = oKisATR, OKisVOL = oKisVOL, OKisAfterBarsSinceResetUP = oKisAfterBarsSinceResetUP, OKisAboveUpperThreshold = oKisAboveUpperThreshold, OKisWithinMaxEntryDistance = oKisWithinMaxEntryDistance, OKisUpperBreakoutCountExceeded = oKisUpperBreakoutCountExceeded, OKisAfterBarsSinceResetDown = oKisAfterBarsSinceResetDown, OKisBelovLowerThreshold = oKisBelovLowerThreshold, OKisWithinMaxEntryDistanceDown = oKisWithinMaxEntryDistanceDown, OKisLowerBreakoutCountExceeded = oKisLowerBreakoutCountExceeded }, input, ref cacheBVAv11092024);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.BVAv11092024 BVAv11092024(int resetPeriod, int minBarsForSignal, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, double fminADX, double fmaxADX, double fminATR, double fmaxATR, int fperiodVol, bool oKisADX, bool oKisATR, bool oKisVOL, bool oKisAfterBarsSinceResetUP, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, bool oKisAfterBarsSinceResetDown, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded)
		{
			return indicator.BVAv11092024(Input, resetPeriod, minBarsForSignal, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts, fminADX, fmaxADX, fminATR, fmaxATR, fperiodVol, oKisADX, oKisATR, oKisVOL, oKisAfterBarsSinceResetUP, oKisAboveUpperThreshold, oKisWithinMaxEntryDistance, oKisUpperBreakoutCountExceeded, oKisAfterBarsSinceResetDown, oKisBelovLowerThreshold, oKisWithinMaxEntryDistanceDown, oKisLowerBreakoutCountExceeded);
		}

		public Indicators.ninpai.BVAv11092024 BVAv11092024(ISeries<double> input , int resetPeriod, int minBarsForSignal, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, double fminADX, double fmaxADX, double fminATR, double fmaxATR, int fperiodVol, bool oKisADX, bool oKisATR, bool oKisVOL, bool oKisAfterBarsSinceResetUP, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, bool oKisAfterBarsSinceResetDown, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded)
		{
			return indicator.BVAv11092024(input, resetPeriod, minBarsForSignal, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts, fminADX, fmaxADX, fminATR, fmaxATR, fperiodVol, oKisADX, oKisATR, oKisVOL, oKisAfterBarsSinceResetUP, oKisAboveUpperThreshold, oKisWithinMaxEntryDistance, oKisUpperBreakoutCountExceeded, oKisAfterBarsSinceResetDown, oKisBelovLowerThreshold, oKisWithinMaxEntryDistanceDown, oKisLowerBreakoutCountExceeded);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.BVAv11092024 BVAv11092024(int resetPeriod, int minBarsForSignal, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, double fminADX, double fmaxADX, double fminATR, double fmaxATR, int fperiodVol, bool oKisADX, bool oKisATR, bool oKisVOL, bool oKisAfterBarsSinceResetUP, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, bool oKisAfterBarsSinceResetDown, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded)
		{
			return indicator.BVAv11092024(Input, resetPeriod, minBarsForSignal, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts, fminADX, fmaxADX, fminATR, fmaxATR, fperiodVol, oKisADX, oKisATR, oKisVOL, oKisAfterBarsSinceResetUP, oKisAboveUpperThreshold, oKisWithinMaxEntryDistance, oKisUpperBreakoutCountExceeded, oKisAfterBarsSinceResetDown, oKisBelovLowerThreshold, oKisWithinMaxEntryDistanceDown, oKisLowerBreakoutCountExceeded);
		}

		public Indicators.ninpai.BVAv11092024 BVAv11092024(ISeries<double> input , int resetPeriod, int minBarsForSignal, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, double fminADX, double fmaxADX, double fminATR, double fmaxATR, int fperiodVol, bool oKisADX, bool oKisATR, bool oKisVOL, bool oKisAfterBarsSinceResetUP, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, bool oKisAfterBarsSinceResetDown, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded)
		{
			return indicator.BVAv11092024(input, resetPeriod, minBarsForSignal, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts, fminADX, fmaxADX, fminATR, fmaxATR, fperiodVol, oKisADX, oKisATR, oKisVOL, oKisAfterBarsSinceResetUP, oKisAboveUpperThreshold, oKisWithinMaxEntryDistance, oKisUpperBreakoutCountExceeded, oKisAfterBarsSinceResetDown, oKisBelovLowerThreshold, oKisWithinMaxEntryDistanceDown, oKisLowerBreakoutCountExceeded);
		}
	}
}

#endregion
