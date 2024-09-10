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
                MinBarsForSignal = 10;
				
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
				AddDataSeries(Data.BarsPeriodType.Tick, 1);
                ResetValues(DateTime.MinValue);
            }
        }

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
                OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0).Update(OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0).BarsArray[1].Count - 1, 1);
				OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0).Update(OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0).BarsArray[1].Count - 1, 1);
                return;
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
			// Delta Condition
			double deltaBarClose0 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0).DeltaClose[0];
			double deltaBarClose1 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0).DeltaClose[1];
			double deltaBarClose2 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0).DeltaClose[2];
			double deltaBarClose3 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0).DeltaClose[3];
			double deltaBarOpen0 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0).DeltaOpen[0];
			double deltaBarOpen1 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0).DeltaOpen[1];
			double deltaBarOpen2 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0).DeltaOpen[2];
			double deltaBarOpen3 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0).DeltaOpen[3];
			
			double deltaBarLow0 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0).DeltaLow[0];
			double deltaBarLow1 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0).DeltaLow[1];
			double deltaBarLow2 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0).DeltaLow[2];
			double deltaBarLow3 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0).DeltaLow[3];
			double deltaBarHigh0 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0).DeltaHigh[0];
			double deltaBarHigh1 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0).DeltaHigh[1];
			double deltaBarHigh2 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0).DeltaHigh[2];
			double deltaBarHigh3 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0).DeltaHigh[3];
			
			// ###############
			double deltaSessionClose0 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0).DeltaClose[0];
			double deltaSessionClose1 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0).DeltaClose[1];
			double deltaSessionClose2 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0).DeltaClose[2];
			double deltaSessionClose3 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0).DeltaClose[3];
			double deltaSessionOpen0 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0).DeltaOpen[0];
			double deltaSessionOpen1 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0).DeltaOpen[1];
			double deltaSessionOpen2 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0).DeltaOpen[2];
			double deltaSessionOpen3 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0).DeltaOpen[3];
			
			double deltaSessionLow0 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0).DeltaLow[0];
			double deltaSessionLow1 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0).DeltaLow[1];
			double deltaSessionLow2 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0).DeltaLow[2];
			double deltaSessionLow3 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0).DeltaLow[3];
			double deltaSessionHigh0 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0).DeltaHigh[0];
			double deltaSessionHigh1 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0).DeltaHigh[1];
			double deltaSessionHigh2 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0).DeltaHigh[2];
			double deltaSessionHigh3 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0).DeltaHigh[3];
			
			bool isDeltaSessionClose4barUP = deltaSessionClose0 > deltaSessionClose3;
			bool isDeltaSessionClose4barDOWN = deltaSessionClose0 < deltaSessionClose3;
			
			
			
			// ####################
			// Data Condition
			
			// ####################
			// Buy Condition
			if (
				(Close[0] > Open[0])
				&& isAfterBarsSinceReset
				&& (!OKisAboveUpperThreshold || isAboveUpperThreshold)
				&& (!OKisWithinMaxEntryDistance || isWithinMaxEntryDistance)
				&& (!OKisUpperBreakoutCountExceeded || isUpperBreakoutCountExceeded)
				&& (!OKisDeltaSessionClose4barUP || isDeltaSessionClose4barUP)
				)
            {
                 Draw.ArrowUp(this, "UpArrow" + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Green);
                 upperBreakoutCount++;
            }
			
			// ######################
			// Sell Condition
			if (
				(Close[0] < Open[0])
				&& isAfterBarsSinceReset
				&& (!OKisBelovLowerThreshold || isBelovLowerThreshold)
				&& (!OKisWithinMaxEntryDistanceDown || isWithinMaxEntryDistanceDown)
				&& (!OKisLowerBreakoutCountExceeded || isLowerBreakoutCountExceeded)
				&& (!OKisDeltaSessionClose4barDOWN || isDeltaSessionClose4barDOWN)
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
		
		// #################################
		
		/// ########## 3.Buy_Setup ########################
		[Range(0, 1), NinjaScriptProperty]
		[Display(Name="OKisAboveUpperThreshold", Description="OKisAboveUpperThreshold", Order=1, GroupName="3.Buy_Setup")]
		public bool OKisAboveUpperThreshold { get; set; }
		
		[Range(0, 1), NinjaScriptProperty]
		[Display(Name="OKisWithinMaxEntryDistance", Description="OKisWithinMaxEntryDistance", Order=2, GroupName="3.Buy_Setup")]
		public bool OKisWithinMaxEntryDistance { get; set; }
		
		[Range(0, 1), NinjaScriptProperty]
		[Display(Name="OKisUpperBreakoutCountExceeded", Description="OKisUpperBreakoutCountExceeded", Order=3, GroupName="3.Buy_Setup")]
		public bool OKisUpperBreakoutCountExceeded { get; set; }
		
		[Range(0, 1), NinjaScriptProperty]
		[Display(Name="OKisDeltaSessionClose4barUP", Description="OKisDeltaSessionClose4barUP", Order=4, GroupName="3.Buy_Setup")]
		public bool OKisDeltaSessionClose4barUP { get; set; }
		
		
		
		// ##########  4.Sel_Setup  ########################
		[Range(0, 1), NinjaScriptProperty]
		[Display(Name="OKisBelovLowerThreshold", Description="OKisBelovLowerThreshold", Order=1, GroupName="4.Sel_Setup")]
		public bool OKisBelovLowerThreshold { get; set; }
		
		[Range(0, 1), NinjaScriptProperty]
		[Display(Name="OKisWithinMaxEntryDistanceDown", Description="OKisWithinMaxEntryDistanceDown", Order=2, GroupName="4.Sel_Setup")]
		public bool OKisWithinMaxEntryDistanceDown { get; set; }
		
		[Range(0, 1), NinjaScriptProperty]
		[Display(Name="OKisLowerBreakoutCountExceeded", Description="OKisLowerBreakoutCountExceeded", Order=3, GroupName="4.Sel_Setup")]
		public bool OKisLowerBreakoutCountExceeded { get; set; }
		
		[Range(0, 1), NinjaScriptProperty]
		[Display(Name="OKisDeltaSessionClose4barDOWN", Description="OKisDeltaSessionClose4barDOWN", Order=4, GroupName="4.Sel_Setup")]
		public bool OKisDeltaSessionClose4barDOWN { get; set; }
		
		
		// ##################################
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
		private ninpai.BVA[] cacheBVA;
		public ninpai.BVA BVA(int resetPeriod, int minBarsForSignal, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, bool oKisDeltaSessionClose4barUP, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded, bool oKisDeltaSessionClose4barDOWN)
		{
			return BVA(Input, resetPeriod, minBarsForSignal, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts, oKisAboveUpperThreshold, oKisWithinMaxEntryDistance, oKisUpperBreakoutCountExceeded, oKisDeltaSessionClose4barUP, oKisBelovLowerThreshold, oKisWithinMaxEntryDistanceDown, oKisLowerBreakoutCountExceeded, oKisDeltaSessionClose4barDOWN);
		}

		public ninpai.BVA BVA(ISeries<double> input, int resetPeriod, int minBarsForSignal, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, bool oKisDeltaSessionClose4barUP, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded, bool oKisDeltaSessionClose4barDOWN)
		{
			if (cacheBVA != null)
				for (int idx = 0; idx < cacheBVA.Length; idx++)
					if (cacheBVA[idx] != null && cacheBVA[idx].ResetPeriod == resetPeriod && cacheBVA[idx].MinBarsForSignal == minBarsForSignal && cacheBVA[idx].MinEntryDistanceUP == minEntryDistanceUP && cacheBVA[idx].MaxEntryDistanceUP == maxEntryDistanceUP && cacheBVA[idx].MaxUpperBreakouts == maxUpperBreakouts && cacheBVA[idx].MinEntryDistanceDOWN == minEntryDistanceDOWN && cacheBVA[idx].MaxEntryDistanceDOWN == maxEntryDistanceDOWN && cacheBVA[idx].MaxLowerBreakouts == maxLowerBreakouts && cacheBVA[idx].OKisAboveUpperThreshold == oKisAboveUpperThreshold && cacheBVA[idx].OKisWithinMaxEntryDistance == oKisWithinMaxEntryDistance && cacheBVA[idx].OKisUpperBreakoutCountExceeded == oKisUpperBreakoutCountExceeded && cacheBVA[idx].OKisDeltaSessionClose4barUP == oKisDeltaSessionClose4barUP && cacheBVA[idx].OKisBelovLowerThreshold == oKisBelovLowerThreshold && cacheBVA[idx].OKisWithinMaxEntryDistanceDown == oKisWithinMaxEntryDistanceDown && cacheBVA[idx].OKisLowerBreakoutCountExceeded == oKisLowerBreakoutCountExceeded && cacheBVA[idx].OKisDeltaSessionClose4barDOWN == oKisDeltaSessionClose4barDOWN && cacheBVA[idx].EqualsInput(input))
						return cacheBVA[idx];
			return CacheIndicator<ninpai.BVA>(new ninpai.BVA(){ ResetPeriod = resetPeriod, MinBarsForSignal = minBarsForSignal, MinEntryDistanceUP = minEntryDistanceUP, MaxEntryDistanceUP = maxEntryDistanceUP, MaxUpperBreakouts = maxUpperBreakouts, MinEntryDistanceDOWN = minEntryDistanceDOWN, MaxEntryDistanceDOWN = maxEntryDistanceDOWN, MaxLowerBreakouts = maxLowerBreakouts, OKisAboveUpperThreshold = oKisAboveUpperThreshold, OKisWithinMaxEntryDistance = oKisWithinMaxEntryDistance, OKisUpperBreakoutCountExceeded = oKisUpperBreakoutCountExceeded, OKisDeltaSessionClose4barUP = oKisDeltaSessionClose4barUP, OKisBelovLowerThreshold = oKisBelovLowerThreshold, OKisWithinMaxEntryDistanceDown = oKisWithinMaxEntryDistanceDown, OKisLowerBreakoutCountExceeded = oKisLowerBreakoutCountExceeded, OKisDeltaSessionClose4barDOWN = oKisDeltaSessionClose4barDOWN }, input, ref cacheBVA);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.BVA BVA(int resetPeriod, int minBarsForSignal, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, bool oKisDeltaSessionClose4barUP, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded, bool oKisDeltaSessionClose4barDOWN)
		{
			return indicator.BVA(Input, resetPeriod, minBarsForSignal, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts, oKisAboveUpperThreshold, oKisWithinMaxEntryDistance, oKisUpperBreakoutCountExceeded, oKisDeltaSessionClose4barUP, oKisBelovLowerThreshold, oKisWithinMaxEntryDistanceDown, oKisLowerBreakoutCountExceeded, oKisDeltaSessionClose4barDOWN);
		}

		public Indicators.ninpai.BVA BVA(ISeries<double> input , int resetPeriod, int minBarsForSignal, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, bool oKisDeltaSessionClose4barUP, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded, bool oKisDeltaSessionClose4barDOWN)
		{
			return indicator.BVA(input, resetPeriod, minBarsForSignal, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts, oKisAboveUpperThreshold, oKisWithinMaxEntryDistance, oKisUpperBreakoutCountExceeded, oKisDeltaSessionClose4barUP, oKisBelovLowerThreshold, oKisWithinMaxEntryDistanceDown, oKisLowerBreakoutCountExceeded, oKisDeltaSessionClose4barDOWN);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.BVA BVA(int resetPeriod, int minBarsForSignal, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, bool oKisDeltaSessionClose4barUP, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded, bool oKisDeltaSessionClose4barDOWN)
		{
			return indicator.BVA(Input, resetPeriod, minBarsForSignal, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts, oKisAboveUpperThreshold, oKisWithinMaxEntryDistance, oKisUpperBreakoutCountExceeded, oKisDeltaSessionClose4barUP, oKisBelovLowerThreshold, oKisWithinMaxEntryDistanceDown, oKisLowerBreakoutCountExceeded, oKisDeltaSessionClose4barDOWN);
		}

		public Indicators.ninpai.BVA BVA(ISeries<double> input , int resetPeriod, int minBarsForSignal, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, bool oKisDeltaSessionClose4barUP, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded, bool oKisDeltaSessionClose4barDOWN)
		{
			return indicator.BVA(input, resetPeriod, minBarsForSignal, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts, oKisAboveUpperThreshold, oKisWithinMaxEntryDistance, oKisUpperBreakoutCountExceeded, oKisDeltaSessionClose4barUP, oKisBelovLowerThreshold, oKisWithinMaxEntryDistanceDown, oKisLowerBreakoutCountExceeded, oKisDeltaSessionClose4barDOWN);
		}
	}
}

#endregion
