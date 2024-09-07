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

namespace NinjaTrader.NinjaScript.Indicators.Ninpai
{
    public class BVA : Indicator
    {
        private double sumPriceVolume;
        private double sumVolume;
        private double sumSquaredPriceVolume;
        private DateTime lastResetTime;
        private int barsSinceReset;
        private int upperBreakoutCount;
		private int lowerBreakoutCount;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"BVA";
                Name = "BVA";
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
                MinBarsForSignal = 9;
				
                MinEntryDistanceUP = 3;
                MaxEntryDistanceUP = 40;
                MaxUpperBreakouts = 3;
				
				MinEntryDistanceDOWN = 3;
                MaxEntryDistanceDOWN = 40;
                MaxLowerBreakouts = 3;
				

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
                ResetValues(DateTime.MinValue);
            }
        }

        protected override void OnBarUpdate()
        {
            DateTime currentBarTime = Time[0];

            if (Bars.IsFirstBarOfSession)
            {
                ResetValues(currentBarTime);
            }
            else if (lastResetTime != DateTime.MinValue && (currentBarTime - lastResetTime).TotalMinutes >= ResetPeriod)
            {
                ResetValues(currentBarTime);
            }

            double typicalPrice = (High[0] + Low[0] + Close[0]) / 3;
            double volume = Volume[0];

            sumPriceVolume += typicalPrice * volume;
            sumVolume += volume;
            sumSquaredPriceVolume += typicalPrice * typicalPrice * volume;

            double vwap = sumPriceVolume / sumVolume;
            double variance = (sumSquaredPriceVolume / sumVolume) - (vwap * vwap);
            double stdDev = Math.Sqrt(variance);

            double stdDev1Upper = vwap + stdDev;
            double stdDev1Lower = vwap - stdDev;

            Values[0][0] = vwap;
            Values[1][0] = stdDev1Upper;
            Values[2][0] = stdDev1Lower;
            Values[3][0] = vwap + 2 * stdDev;
            Values[4][0] = vwap - 2 * stdDev;
            Values[5][0] = vwap + 3 * stdDev;
            Values[6][0] = vwap - 3 * stdDev;

            barsSinceReset++;

            double upperThreshold = stdDev1Upper + MinEntryDistanceUP * TickSize;
			bool isAboveUpperThreshold = Close[0] > upperThreshold;
            bool isWithinMaxEntryDistance = Close[0] <= stdDev1Upper + MaxEntryDistanceUP * TickSize;
            bool isUpperBreakoutCountExceeded = upperBreakoutCount < MaxUpperBreakouts;

            double lowerThreshold = stdDev1Lower - MinEntryDistanceDOWN * TickSize;
			bool isBelovLowerThreshold = Close[0] < lowerThreshold;
            bool isWithinMaxEntryDistanceDown = Close[0] >= stdDev1Lower - MaxEntryDistanceDOWN * TickSize;
            bool isLowerBreakoutCountExceeded = lowerBreakoutCount < MaxLowerBreakouts;
			
			bool isAfterBarsSinceReset = barsSinceReset > MinBarsForSignal;
			
			// ####################
			// Buy Condition
			if (
				(Close[0] > Open[0])
				&& isAboveUpperThreshold
				&& (isWithinMaxEntryDistance)
				&& (isUpperBreakoutCountExceeded)
				&& isAfterBarsSinceReset
				)
            {
                 Draw.ArrowUp(this, "UpArrow" + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Green);
                 upperBreakoutCount++;
            }
			
			// ######################
			// Sell Condition
			if (
				(Close[0] < Open[0])
				&& isBelovLowerThreshold
				&& (isWithinMaxEntryDistanceDown)
				&& (isLowerBreakoutCountExceeded)
				&& isAfterBarsSinceReset
				)
            {
				Draw.ArrowDown(this, "DownArrow" + CurrentBar, true, 0, High[0] + TickSize, Brushes.Red);
                lowerBreakoutCount++;
            }
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
		
		
		// ### Buy ###
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
		
		
		
		// ### Sell ###
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
		


        [Browsable(false)]
        [XmlIgnore]
        public Series<double> VWAP
        {
            get { return Values[0]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> StdDev1Upper
        {
            get { return Values[1]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> StdDev1Lower
        {
            get { return Values[2]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> StdDev2Upper
        {
            get { return Values[3]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> StdDev2Lower
        {
            get { return Values[4]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> StdDev3Upper
        {
            get { return Values[5]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> StdDev3Lower
        {
            get { return Values[6]; }
        }

        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private Ninpa.BVA[] cacheBVA;
		public Ninpa.BVA BVA(int resetPeriod, int minBarsForSignal, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts)
		{
			return BVA(Input, resetPeriod, minBarsForSignal, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts);
		}

		public Ninpa.BVA BVA(ISeries<double> input, int resetPeriod, int minBarsForSignal, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts)
		{
			if (cacheBVA != null)
				for (int idx = 0; idx < cacheBVA.Length; idx++)
					if (cacheBVA[idx] != null && cacheBVA[idx].ResetPeriod == resetPeriod && cacheBVA[idx].MinBarsForSignal == minBarsForSignal && cacheBVA[idx].MinEntryDistanceUP == minEntryDistanceUP && cacheBVA[idx].MaxEntryDistanceUP == maxEntryDistanceUP && cacheBVA[idx].MaxUpperBreakouts == maxUpperBreakouts && cacheBVA[idx].MinEntryDistanceDOWN == minEntryDistanceDOWN && cacheBVA[idx].MaxEntryDistanceDOWN == maxEntryDistanceDOWN && cacheBVA[idx].MaxLowerBreakouts == maxLowerBreakouts && cacheBVA[idx].EqualsInput(input))
						return cacheBVA[idx];
			return CacheIndicator<Ninpa.BVA>(new Ninpa.BVA(){ ResetPeriod = resetPeriod, MinBarsForSignal = minBarsForSignal, MinEntryDistanceUP = minEntryDistanceUP, MaxEntryDistanceUP = maxEntryDistanceUP, MaxUpperBreakouts = maxUpperBreakouts, MinEntryDistanceDOWN = minEntryDistanceDOWN, MaxEntryDistanceDOWN = maxEntryDistanceDOWN, MaxLowerBreakouts = maxLowerBreakouts }, input, ref cacheBVA);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.Ninpa.BVA BVA(int resetPeriod, int minBarsForSignal, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts)
		{
			return indicator.BVA(Input, resetPeriod, minBarsForSignal, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts);
		}

		public Indicators.Ninpa.BVA BVA(ISeries<double> input , int resetPeriod, int minBarsForSignal, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts)
		{
			return indicator.BVA(input, resetPeriod, minBarsForSignal, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.Ninpa.BVA BVA(int resetPeriod, int minBarsForSignal, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts)
		{
			return indicator.BVA(Input, resetPeriod, minBarsForSignal, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts);
		}

		public Indicators.Ninpa.BVA BVA(ISeries<double> input , int resetPeriod, int minBarsForSignal, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts)
		{
			return indicator.BVA(input, resetPeriod, minBarsForSignal, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts);
		}
	}
}

#endregion
