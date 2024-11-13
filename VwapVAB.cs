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
    public class VwapVAB : Indicator
    {
        private double sumPriceVolume;
        private double sumVolume;
        private double sumSquaredPriceVolume;
        private DateTime lastResetTime;
        private int barsSinceReset;
        private int upperBreakoutCount;
        private int lowerBreakoutCount;
        
        private VOL VOL1;
        private VOLMA VOLMA1;
		
        // Variables for tracking STD3 highs and lows
        private double highestSTD3Upper;
        private double lowestSTD3Lower;
        private bool isFirstBarSinceReset;
		
        private double previousSessionHighStd1Upper;
        private double previousSessionLowStd1Lower;
        private DateTime currentSessionStartTime;
        private double highestStd1Upper;
        private double lowestStd1Lower;
		
		private OrderFlowCumulativeDelta cumulativeDelta;
	
        [NinjaScriptProperty]
        [Display(Name = "Enable Previous Session Range Breakout", Description = "Enable checking for breakouts of the previous session's StdDev1 range", Order = 1, GroupName = "0.01_Parameters")]
        public bool EnablePreviousSessionRangeBreakout { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Indicateur VwapVAB BVA-Limusine combiné";
                Name = "VwapVAB";
                Calculate = Calculate.OnEachTick;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive = true;

                // Paramètres BVA
                ResetPeriod = 120;
                MinBarsForSignal = 10;
                MaxBarsForSignal = 100;
                MinEntryDistanceUP = 3;
                MaxEntryDistanceUP = 40;
                MaxUpperBreakouts = 3;
                MinEntryDistanceDOWN = 3;
                MaxEntryDistanceDOWN = 40;
                MaxLowerBreakouts = 3;
                FperiodVol = 9;

                // Paramètres Limusine
                MinimumTicks = 10;
                MaximumTicks = 30;
                ShowLimusineOpenCloseUP = true;
                ShowLimusineOpenCloseDOWN = true;
                ShowLimusineHighLowUP = true;
                ShowLimusineHighLowDOWN = true;
                
                EnableDistanceFromVWAPCondition = false;
                MinDistanceFromVWAP = 10;
                MaxDistanceFromVWAP = 50;
                
                EnableSTD3HighLowTracking = false;
                EnablePreviousSessionRangeBreakout = false;
				
				// Paramètres pour DeltaSession
                UseDeltaSessionCloseOpenUP = false;
                UseDeltaSessionCloseOpenDOWN = false;
                UseDeltaSessionHighLowUP = false;
                UseDeltaSessionHighLowDOWN = false;
                DeltaSessionMinFilter = 1000;
                DeltaSessionMaxFilter = 5000;

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
                VOL1 = VOL(Close);
                VOLMA1 = VOLMA(Close, Convert.ToInt32(FperiodVol));
				cumulativeDelta = OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBars[0] < 20)
                return;

            DateTime currentBarTime = Time[0];
            bool shouldReset = false;

            if (Bars.IsFirstBarOfSession)
            {
                shouldReset = true;
            }
            else if (currentSessionStartTime != DateTime.MinValue && (currentBarTime - currentSessionStartTime).TotalMinutes >= ResetPeriod)
            {
                shouldReset = true;
            }
            
            if (shouldReset)
            {
                if (EnablePreviousSessionRangeBreakout)
                {
                    previousSessionHighStd1Upper = highestStd1Upper;
                    previousSessionLowStd1Lower = lowestStd1Lower;
                }
    
                ResetValues(currentBarTime);
                currentSessionStartTime = currentBarTime;
            }
            
            // Calcul VWAP et écarts-types
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
            
            if (EnablePreviousSessionRangeBreakout)
            {
                highestStd1Upper = Math.Max(highestStd1Upper, Values[1][0]);
                lowestStd1Lower = Math.Min(lowestStd1Lower, Values[2][0]);
            }

            if (EnableSTD3HighLowTracking)
            {
                if (isFirstBarSinceReset)
                {
                    highestSTD3Upper = Values[5][0];
                    lowestSTD3Lower = Values[6][0];
                    isFirstBarSinceReset = false;
                }
                else
                {
                    highestSTD3Upper = Math.Max(highestSTD3Upper, Values[5][0]);
                    lowestSTD3Lower = Math.Min(lowestSTD3Lower, Values[6][0]);
                }
            }
            
            barsSinceReset++;

            if (ShouldDrawUpArrow())
            {
                Draw.ArrowUp(this, "UpArrow" + CurrentBar, true, 0, Low[0] - 2 * TickSize, Brushes.Green);
                upperBreakoutCount++;
        
                double distanceRed = Values[0][0] - Values[2][0];
                double priceForRedDot = Close[0] - distanceRed;
                Draw.Dot(this, "RedDotUp" + CurrentBar, true, 0, priceForRedDot, Brushes.Red);
        
                double distanceBlue = Values[1][0] - Values[0][0];
                double priceForBlueDot = Close[0] + distanceBlue;
                Draw.Dot(this, "BlueDotUp" + CurrentBar, true, 0, priceForBlueDot, Brushes.Blue);
        
                Draw.Dot(this, "WhiteDotUp" + CurrentBar, true, 0, Close[0], Brushes.White);
            }
            else if (ShouldDrawDownArrow())
            {
                Draw.ArrowDown(this, "DownArrow" + CurrentBar, true, 0, High[0] + 2 * TickSize, Brushes.Red);
                lowerBreakoutCount++;
        
                double distanceRed = Values[1][0] - Values[0][0];
                double priceForRedDot = Close[0] + distanceRed;
                Draw.Dot(this, "RedDotDown" + CurrentBar, true, 0, priceForRedDot, Brushes.Red);
        
                double distanceBlue = Values[0][0] - Values[2][0];
                double priceForBlueDot = Close[0] - distanceBlue;
                Draw.Dot(this, "BlueDotDown" + CurrentBar, true, 0, priceForBlueDot, Brushes.Blue);
        
                Draw.Dot(this, "WhiteDotDown" + CurrentBar, true, 0, Close[0], Brushes.White);
            }
        }

        private bool ShouldDrawUpArrow()
        {
            double vwap = Values[0][0];
            double distanceInTicks = (Close[0] - vwap) / TickSize;
			double deltaSessionSize = 0;
            bool deltaSessionCondition = true;
            
            if (cumulativeDelta != null)
            {
                if (UseDeltaSessionCloseOpenUP)
                {
                    // Pour UP: Close - Open
                    deltaSessionSize = Math.Abs(cumulativeDelta.DeltaClose[0] - cumulativeDelta.DeltaOpen[0]);
                    deltaSessionCondition = deltaSessionSize >= DeltaSessionMinFilter && deltaSessionSize <= DeltaSessionMaxFilter;
                }
                else if (UseDeltaSessionHighLowUP)
                {
                    // Pour UP: High - Low
                    deltaSessionSize = Math.Abs(cumulativeDelta.DeltaHigh[0] - cumulativeDelta.DeltaLow[0]);
                    deltaSessionCondition = deltaSessionSize >= DeltaSessionMinFilter && deltaSessionSize <= DeltaSessionMaxFilter;
                }
            }
            
            bool bvaCondition = (Close[0] > Open[0]) &&
                   (!OKisVOL || (VOL1[0] > VOLMA1[0])) &&
                   (!OKisAfterBarsSinceResetUP || (barsSinceReset > MinBarsForSignal && barsSinceReset < MaxBarsForSignal)) &&
                   (!OKisAboveUpperThreshold || Close[0] > (Values[1][0] + MinEntryDistanceUP * TickSize)) &&
                   (!OKisWithinMaxEntryDistance || Close[0] <= (Values[1][0] + MaxEntryDistanceUP * TickSize)) &&
                   (!OKisUpperBreakoutCountExceeded || upperBreakoutCount < MaxUpperBreakouts) &&
                   (!EnableDistanceFromVWAPCondition || (distanceInTicks >= MinDistanceFromVWAP && distanceInTicks <= MaxDistanceFromVWAP));

            double openCloseDiff = Math.Abs(Open[0] - Close[0]) / TickSize;
            double highLowDiff = Math.Abs(High[0] - Low[0]) / TickSize;
            bool limusineCondition = (ShowLimusineOpenCloseUP && openCloseDiff >= MinimumTicks && openCloseDiff <= MaximumTicks && Close[0] > Open[0]) ||
                                    (ShowLimusineHighLowUP && highLowDiff >= MinimumTicks && highLowDiff <= MaximumTicks && Close[0] > Open[0]);

            bool std3Condition = !EnableSTD3HighLowTracking || Values[5][0] >= highestSTD3Upper;
            bool rangeBreakoutCondition = !EnablePreviousSessionRangeBreakout || 
                (previousSessionHighStd1Upper != double.MinValue && Close[0] > previousSessionHighStd1Upper);
            
            return bvaCondition && limusineCondition && std3Condition && rangeBreakoutCondition;
        }

        private bool ShouldDrawDownArrow()
        {
            double vwap = Values[0][0];
            double distanceInTicks = (vwap - Close[0]) / TickSize;
			double deltaSessionSize = 0;
            bool deltaSessionCondition = true;
            
            if (cumulativeDelta != null)
            {
                if (UseDeltaSessionCloseOpenDOWN)
                {
                    // Pour DOWN: Open - Close
                    deltaSessionSize = Math.Abs(cumulativeDelta.DeltaOpen[0] - cumulativeDelta.DeltaClose[0]);
                    deltaSessionCondition = deltaSessionSize >= DeltaSessionMinFilter && deltaSessionSize <= DeltaSessionMaxFilter;
                }
                else if (UseDeltaSessionHighLowDOWN)
                {
                    // Pour DOWN: High - Low
                    deltaSessionSize = Math.Abs(cumulativeDelta.DeltaHigh[0] - cumulativeDelta.DeltaLow[0]);
                    deltaSessionCondition = deltaSessionSize >= DeltaSessionMinFilter && deltaSessionSize <= DeltaSessionMaxFilter;
                }
            }
            
            bool bvaCondition = (Close[0] < Open[0]) &&
                   (!OKisVOL || (VOL1[0] > VOLMA1[0])) &&
                   (!OKisAfterBarsSinceResetDown || (barsSinceReset > MinBarsForSignal && barsSinceReset < MaxBarsForSignal)) &&
                   (!OKisBelovLowerThreshold || Close[0] < (Values[2][0] - MinEntryDistanceDOWN * TickSize)) &&
                   (!OKisWithinMaxEntryDistanceDown || Close[0] >= (Values[2][0] - MaxEntryDistanceDOWN * TickSize)) &&
                   (!OKisLowerBreakoutCountExceeded || lowerBreakoutCount < MaxLowerBreakouts) &&
                   (!EnableDistanceFromVWAPCondition || (distanceInTicks >= MinDistanceFromVWAP && distanceInTicks <= MaxDistanceFromVWAP));

            double openCloseDiff = Math.Abs(Open[0] - Close[0]) / TickSize;
            double highLowDiff = Math.Abs(High[0] - Low[0]) / TickSize;
            bool limusineCondition = (ShowLimusineOpenCloseDOWN && openCloseDiff >= MinimumTicks && openCloseDiff <= MaximumTicks && Close[0] < Open[0]) ||
                                    (ShowLimusineHighLowDOWN && highLowDiff >= MinimumTicks && highLowDiff <= MaximumTicks && Close[0] < Open[0]);

            bool std3Condition = !EnableSTD3HighLowTracking || Values[6][0] <= lowestSTD3Lower;
            bool rangeBreakoutCondition = !EnablePreviousSessionRangeBreakout || 
                (previousSessionLowStd1Lower != double.MaxValue && Close[0] < previousSessionLowStd1Lower);
            
            return bvaCondition && limusineCondition && std3Condition && rangeBreakoutCondition;
        }
        
        private void ResetSessionValues()
        {
            highestStd1Upper = double.MinValue;
            lowestStd1Lower = double.MaxValue;
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
            
            highestStd1Upper = double.MinValue;
            lowestStd1Lower = double.MaxValue;
    
            isFirstBarSinceReset = true;
            highestSTD3Upper = double.MinValue;
            lowestSTD3Lower = double.MaxValue;
			lowestSTD3Lower = double.MaxValue;
        }
		
		#region Properties
        // Propriétés BVA
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Reset Period (Minutes)", Order = 1, GroupName = "0.1_BVA Parameters")]
        public int ResetPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Min Bars for Signal", Order = 2, GroupName = "0.1_BVA Parameters")]
        public int MinBarsForSignal { get; set; }
        
        [Range(1, int.MaxValue)]
        [Display(Name = "Max Bars for Signal", Description = "Nombre maximum de barres depuis la réinitialisation pour un signal", Order = 3, GroupName = "0.1_BVA Parameters")]
        public int MaxBarsForSignal { get; set; }
        
        // Propriétés Limusine
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Minimum Ticks", Description = "Nombre minimum de ticks pour une limusine", Order = 1, GroupName = "0.2_Limusine Parameters")]
        public int MinimumTicks { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Maximum Ticks", Description = "Nombre maximum de ticks pour une limusine", Order = 2, GroupName = "0.2_Limusine Parameters")]
        public int MaximumTicks { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Afficher Limusine Open-Close UP", Description = "Afficher les limusines Open-Close UP", Order = 3, GroupName = "0.2_Limusine Parameters")]
        public bool ShowLimusineOpenCloseUP { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Afficher Limusine Open-Close DOWN", Description = "Afficher les limusines Open-Close DOWN", Order = 4, GroupName = "0.2_Limusine Parameters")]
        public bool ShowLimusineOpenCloseDOWN { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Afficher Limusine High-Low UP", Description = "Afficher les limusines High-Low UP", Order = 5, GroupName = "0.2_Limusine Parameters")]
        public bool ShowLimusineHighLowUP { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Afficher Limusine High-Low DOWN", Description = "Afficher les limusines High-Low DOWN", Order = 6, GroupName = "0.2_Limusine Parameters")]
        public bool ShowLimusineHighLowDOWN { get; set; }

        // ############ Buy #############
        // Buy
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Min Entry Distance UP", Order = 1, GroupName = "0.3_Buy")]
        public int MinEntryDistanceUP { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Max Entry Distance UP", Order = 2, GroupName = "0.3_Buy")]
        public int MaxEntryDistanceUP { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Max Upper Breakouts", Order = 3, GroupName = "0.3_Buy")]
        public int MaxUpperBreakouts { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, 1)]
        [Display(Name = "OKisAfterBarsSinceResetUP", Description = "Check Bars Since Reset UP", Order = 1, GroupName = "0.3_Buy")]
        public bool OKisAfterBarsSinceResetUP { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, 1)]
        [Display(Name = "OKisAboveUpperThreshold", Description = "Check Above Upper Threshold", Order = 1, GroupName = "0.3_Buy")]
        public bool OKisAboveUpperThreshold { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, 1)]
        [Display(Name = "OKisWithinMaxEntryDistance", Description = "Check Within Max Entry Distance", Order = 1, GroupName = "0.3_Buy")]
        public bool OKisWithinMaxEntryDistance { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, 1)]
        [Display(Name = "OKisUpperBreakoutCountExceeded", Description = "Check Upper Breakout Count Exceeded", Order = 1, GroupName = "0.3_Buy")]
        public bool OKisUpperBreakoutCountExceeded { get; set; }
        
        // ############ Sell #############
        // Sell
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Min Entry Distance DOWN", Order = 1, GroupName = "0.4_Sell")]
        public int MinEntryDistanceDOWN { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Max Entry Distance DOWN", Order = 2, GroupName = "0.4_Sell")]
        public int MaxEntryDistanceDOWN { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Max Lower Breakouts", Order = 3, GroupName = "0.4_Sell")]
        public int MaxLowerBreakouts { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, 1)]
        [Display(Name = "OKisAfterBarsSinceResetDown", Description = "Check Bars Since Reset Down", Order = 1, GroupName = "0.4_Sell")]
        public bool OKisAfterBarsSinceResetDown { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, 1)]
        [Display(Name = "OKisBelovLowerThreshold", Description = "Check Below Lower Threshold", Order = 1, GroupName = "0.4_Sell")]
        public bool OKisBelovLowerThreshold { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, 1)]
        [Display(Name = "OKisWithinMaxEntryDistanceDown", Description = "Check Within Max Entry Distance Down", Order = 1, GroupName = "0.4_Sell")]
        public bool OKisWithinMaxEntryDistanceDown { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, 1)]
        [Display(Name = "OKisLowerBreakoutCountExceeded", Description = "Check Lower Breakout Count Exceeded", Order = 1, GroupName = "0.4_Sell")]
        public bool OKisLowerBreakoutCountExceeded { get; set; }
		
		
		
		
		// Nouvelles propriétés pour DeltaSession
        [NinjaScriptProperty]
        [Display(Name = "Use Delta Session Close-Open UP", Description = "Utiliser Close-Open Delta Session pour UP", Order = 1, GroupName = "0.5_Delta Session")]
        public bool UseDeltaSessionCloseOpenUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Delta Session Close-Open DOWN", Description = "Utiliser Close-Open Delta Session pour DOWN", Order = 2, GroupName = "0.5_Delta Session")]
        public bool UseDeltaSessionCloseOpenDOWN { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Delta Session High-Low UP", Description = "Utiliser High-Low Delta Session pour UP", Order = 3, GroupName = "0.5_Delta Session")]
        public bool UseDeltaSessionHighLowUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Delta Session High-Low DOWN", Description = "Utiliser High-Low Delta Session pour DOWN", Order = 4, GroupName = "0.5_Delta Session")]
        public bool UseDeltaSessionHighLowDOWN { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Delta Session Min Filter", Description = "Filtre minimum pour Delta Session", Order = 5, GroupName = "0.5_Delta Session")]
        public int DeltaSessionMinFilter { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Delta Session Max Filter", Description = "Filtre maximum pour Delta Session", Order = 6, GroupName = "0.5_Delta Session")]
        public int DeltaSessionMaxFilter { get; set; }
        
        // Distance VWAP
        [NinjaScriptProperty]
        [Display(Name = "Enable Distance From VWAP Condition", Order = 1, GroupName = "0.7_Distance_VWAP")]
        public bool EnableDistanceFromVWAPCondition { get; set; }
        
        [Range(1, int.MaxValue)]
        [NinjaScriptProperty]
        [Display(Name = "Minimum Distance From VWAP (Ticks)", Order = 2, GroupName = "0.7_Distance_VWAP")]
        public int MinDistanceFromVWAP { get; set; }
        
        [Range(1, int.MaxValue)]
        [NinjaScriptProperty]
        [Display(Name = "Maximum Distance From VWAP (Ticks)", Order = 3, GroupName = "0.7_Distance_VWAP")]
        public int MaxDistanceFromVWAP { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name="Enable STD3 High/Low Tracking", Description="Track highest STD3 Upper and lowest STD3 Lower since last reset", Order=1000, GroupName="1.0_STD3 Tracking")]
        public bool EnableSTD3HighLowTracking { get; set; }
        
        // ############ Volume #############
        // Volume
        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Fperiod Vol", Order = 1, GroupName = "Volume")]
        public int FperiodVol { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, 1)]
        [Display(Name = "OKisVOL", Description = "Check Volume", Order = 1, GroupName = "Volume")]
        public bool OKisVOL { get; set; }

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
		private ninpai.VwapVAB[] cacheVwapVAB;
		public ninpai.VwapVAB VwapVAB(bool enablePreviousSessionRangeBreakout, int resetPeriod, int minBarsForSignal, int minimumTicks, int maximumTicks, bool showLimusineOpenCloseUP, bool showLimusineOpenCloseDOWN, bool showLimusineHighLowUP, bool showLimusineHighLowDOWN, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, bool oKisAfterBarsSinceResetUP, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, bool oKisAfterBarsSinceResetDown, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded, bool useDeltaSessionCloseOpenUP, bool useDeltaSessionCloseOpenDOWN, bool useDeltaSessionHighLowUP, bool useDeltaSessionHighLowDOWN, int deltaSessionMinFilter, int deltaSessionMaxFilter, bool enableDistanceFromVWAPCondition, int minDistanceFromVWAP, int maxDistanceFromVWAP, bool enableSTD3HighLowTracking, int fperiodVol, bool oKisVOL)
		{
			return VwapVAB(Input, enablePreviousSessionRangeBreakout, resetPeriod, minBarsForSignal, minimumTicks, maximumTicks, showLimusineOpenCloseUP, showLimusineOpenCloseDOWN, showLimusineHighLowUP, showLimusineHighLowDOWN, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, oKisAfterBarsSinceResetUP, oKisAboveUpperThreshold, oKisWithinMaxEntryDistance, oKisUpperBreakoutCountExceeded, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts, oKisAfterBarsSinceResetDown, oKisBelovLowerThreshold, oKisWithinMaxEntryDistanceDown, oKisLowerBreakoutCountExceeded, useDeltaSessionCloseOpenUP, useDeltaSessionCloseOpenDOWN, useDeltaSessionHighLowUP, useDeltaSessionHighLowDOWN, deltaSessionMinFilter, deltaSessionMaxFilter, enableDistanceFromVWAPCondition, minDistanceFromVWAP, maxDistanceFromVWAP, enableSTD3HighLowTracking, fperiodVol, oKisVOL);
		}

		public ninpai.VwapVAB VwapVAB(ISeries<double> input, bool enablePreviousSessionRangeBreakout, int resetPeriod, int minBarsForSignal, int minimumTicks, int maximumTicks, bool showLimusineOpenCloseUP, bool showLimusineOpenCloseDOWN, bool showLimusineHighLowUP, bool showLimusineHighLowDOWN, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, bool oKisAfterBarsSinceResetUP, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, bool oKisAfterBarsSinceResetDown, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded, bool useDeltaSessionCloseOpenUP, bool useDeltaSessionCloseOpenDOWN, bool useDeltaSessionHighLowUP, bool useDeltaSessionHighLowDOWN, int deltaSessionMinFilter, int deltaSessionMaxFilter, bool enableDistanceFromVWAPCondition, int minDistanceFromVWAP, int maxDistanceFromVWAP, bool enableSTD3HighLowTracking, int fperiodVol, bool oKisVOL)
		{
			if (cacheVwapVAB != null)
				for (int idx = 0; idx < cacheVwapVAB.Length; idx++)
					if (cacheVwapVAB[idx] != null && cacheVwapVAB[idx].EnablePreviousSessionRangeBreakout == enablePreviousSessionRangeBreakout && cacheVwapVAB[idx].ResetPeriod == resetPeriod && cacheVwapVAB[idx].MinBarsForSignal == minBarsForSignal && cacheVwapVAB[idx].MinimumTicks == minimumTicks && cacheVwapVAB[idx].MaximumTicks == maximumTicks && cacheVwapVAB[idx].ShowLimusineOpenCloseUP == showLimusineOpenCloseUP && cacheVwapVAB[idx].ShowLimusineOpenCloseDOWN == showLimusineOpenCloseDOWN && cacheVwapVAB[idx].ShowLimusineHighLowUP == showLimusineHighLowUP && cacheVwapVAB[idx].ShowLimusineHighLowDOWN == showLimusineHighLowDOWN && cacheVwapVAB[idx].MinEntryDistanceUP == minEntryDistanceUP && cacheVwapVAB[idx].MaxEntryDistanceUP == maxEntryDistanceUP && cacheVwapVAB[idx].MaxUpperBreakouts == maxUpperBreakouts && cacheVwapVAB[idx].OKisAfterBarsSinceResetUP == oKisAfterBarsSinceResetUP && cacheVwapVAB[idx].OKisAboveUpperThreshold == oKisAboveUpperThreshold && cacheVwapVAB[idx].OKisWithinMaxEntryDistance == oKisWithinMaxEntryDistance && cacheVwapVAB[idx].OKisUpperBreakoutCountExceeded == oKisUpperBreakoutCountExceeded && cacheVwapVAB[idx].MinEntryDistanceDOWN == minEntryDistanceDOWN && cacheVwapVAB[idx].MaxEntryDistanceDOWN == maxEntryDistanceDOWN && cacheVwapVAB[idx].MaxLowerBreakouts == maxLowerBreakouts && cacheVwapVAB[idx].OKisAfterBarsSinceResetDown == oKisAfterBarsSinceResetDown && cacheVwapVAB[idx].OKisBelovLowerThreshold == oKisBelovLowerThreshold && cacheVwapVAB[idx].OKisWithinMaxEntryDistanceDown == oKisWithinMaxEntryDistanceDown && cacheVwapVAB[idx].OKisLowerBreakoutCountExceeded == oKisLowerBreakoutCountExceeded && cacheVwapVAB[idx].UseDeltaSessionCloseOpenUP == useDeltaSessionCloseOpenUP && cacheVwapVAB[idx].UseDeltaSessionCloseOpenDOWN == useDeltaSessionCloseOpenDOWN && cacheVwapVAB[idx].UseDeltaSessionHighLowUP == useDeltaSessionHighLowUP && cacheVwapVAB[idx].UseDeltaSessionHighLowDOWN == useDeltaSessionHighLowDOWN && cacheVwapVAB[idx].DeltaSessionMinFilter == deltaSessionMinFilter && cacheVwapVAB[idx].DeltaSessionMaxFilter == deltaSessionMaxFilter && cacheVwapVAB[idx].EnableDistanceFromVWAPCondition == enableDistanceFromVWAPCondition && cacheVwapVAB[idx].MinDistanceFromVWAP == minDistanceFromVWAP && cacheVwapVAB[idx].MaxDistanceFromVWAP == maxDistanceFromVWAP && cacheVwapVAB[idx].EnableSTD3HighLowTracking == enableSTD3HighLowTracking && cacheVwapVAB[idx].FperiodVol == fperiodVol && cacheVwapVAB[idx].OKisVOL == oKisVOL && cacheVwapVAB[idx].EqualsInput(input))
						return cacheVwapVAB[idx];
			return CacheIndicator<ninpai.VwapVAB>(new ninpai.VwapVAB(){ EnablePreviousSessionRangeBreakout = enablePreviousSessionRangeBreakout, ResetPeriod = resetPeriod, MinBarsForSignal = minBarsForSignal, MinimumTicks = minimumTicks, MaximumTicks = maximumTicks, ShowLimusineOpenCloseUP = showLimusineOpenCloseUP, ShowLimusineOpenCloseDOWN = showLimusineOpenCloseDOWN, ShowLimusineHighLowUP = showLimusineHighLowUP, ShowLimusineHighLowDOWN = showLimusineHighLowDOWN, MinEntryDistanceUP = minEntryDistanceUP, MaxEntryDistanceUP = maxEntryDistanceUP, MaxUpperBreakouts = maxUpperBreakouts, OKisAfterBarsSinceResetUP = oKisAfterBarsSinceResetUP, OKisAboveUpperThreshold = oKisAboveUpperThreshold, OKisWithinMaxEntryDistance = oKisWithinMaxEntryDistance, OKisUpperBreakoutCountExceeded = oKisUpperBreakoutCountExceeded, MinEntryDistanceDOWN = minEntryDistanceDOWN, MaxEntryDistanceDOWN = maxEntryDistanceDOWN, MaxLowerBreakouts = maxLowerBreakouts, OKisAfterBarsSinceResetDown = oKisAfterBarsSinceResetDown, OKisBelovLowerThreshold = oKisBelovLowerThreshold, OKisWithinMaxEntryDistanceDown = oKisWithinMaxEntryDistanceDown, OKisLowerBreakoutCountExceeded = oKisLowerBreakoutCountExceeded, UseDeltaSessionCloseOpenUP = useDeltaSessionCloseOpenUP, UseDeltaSessionCloseOpenDOWN = useDeltaSessionCloseOpenDOWN, UseDeltaSessionHighLowUP = useDeltaSessionHighLowUP, UseDeltaSessionHighLowDOWN = useDeltaSessionHighLowDOWN, DeltaSessionMinFilter = deltaSessionMinFilter, DeltaSessionMaxFilter = deltaSessionMaxFilter, EnableDistanceFromVWAPCondition = enableDistanceFromVWAPCondition, MinDistanceFromVWAP = minDistanceFromVWAP, MaxDistanceFromVWAP = maxDistanceFromVWAP, EnableSTD3HighLowTracking = enableSTD3HighLowTracking, FperiodVol = fperiodVol, OKisVOL = oKisVOL }, input, ref cacheVwapVAB);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.VwapVAB VwapVAB(bool enablePreviousSessionRangeBreakout, int resetPeriod, int minBarsForSignal, int minimumTicks, int maximumTicks, bool showLimusineOpenCloseUP, bool showLimusineOpenCloseDOWN, bool showLimusineHighLowUP, bool showLimusineHighLowDOWN, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, bool oKisAfterBarsSinceResetUP, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, bool oKisAfterBarsSinceResetDown, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded, bool useDeltaSessionCloseOpenUP, bool useDeltaSessionCloseOpenDOWN, bool useDeltaSessionHighLowUP, bool useDeltaSessionHighLowDOWN, int deltaSessionMinFilter, int deltaSessionMaxFilter, bool enableDistanceFromVWAPCondition, int minDistanceFromVWAP, int maxDistanceFromVWAP, bool enableSTD3HighLowTracking, int fperiodVol, bool oKisVOL)
		{
			return indicator.VwapVAB(Input, enablePreviousSessionRangeBreakout, resetPeriod, minBarsForSignal, minimumTicks, maximumTicks, showLimusineOpenCloseUP, showLimusineOpenCloseDOWN, showLimusineHighLowUP, showLimusineHighLowDOWN, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, oKisAfterBarsSinceResetUP, oKisAboveUpperThreshold, oKisWithinMaxEntryDistance, oKisUpperBreakoutCountExceeded, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts, oKisAfterBarsSinceResetDown, oKisBelovLowerThreshold, oKisWithinMaxEntryDistanceDown, oKisLowerBreakoutCountExceeded, useDeltaSessionCloseOpenUP, useDeltaSessionCloseOpenDOWN, useDeltaSessionHighLowUP, useDeltaSessionHighLowDOWN, deltaSessionMinFilter, deltaSessionMaxFilter, enableDistanceFromVWAPCondition, minDistanceFromVWAP, maxDistanceFromVWAP, enableSTD3HighLowTracking, fperiodVol, oKisVOL);
		}

		public Indicators.ninpai.VwapVAB VwapVAB(ISeries<double> input , bool enablePreviousSessionRangeBreakout, int resetPeriod, int minBarsForSignal, int minimumTicks, int maximumTicks, bool showLimusineOpenCloseUP, bool showLimusineOpenCloseDOWN, bool showLimusineHighLowUP, bool showLimusineHighLowDOWN, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, bool oKisAfterBarsSinceResetUP, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, bool oKisAfterBarsSinceResetDown, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded, bool useDeltaSessionCloseOpenUP, bool useDeltaSessionCloseOpenDOWN, bool useDeltaSessionHighLowUP, bool useDeltaSessionHighLowDOWN, int deltaSessionMinFilter, int deltaSessionMaxFilter, bool enableDistanceFromVWAPCondition, int minDistanceFromVWAP, int maxDistanceFromVWAP, bool enableSTD3HighLowTracking, int fperiodVol, bool oKisVOL)
		{
			return indicator.VwapVAB(input, enablePreviousSessionRangeBreakout, resetPeriod, minBarsForSignal, minimumTicks, maximumTicks, showLimusineOpenCloseUP, showLimusineOpenCloseDOWN, showLimusineHighLowUP, showLimusineHighLowDOWN, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, oKisAfterBarsSinceResetUP, oKisAboveUpperThreshold, oKisWithinMaxEntryDistance, oKisUpperBreakoutCountExceeded, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts, oKisAfterBarsSinceResetDown, oKisBelovLowerThreshold, oKisWithinMaxEntryDistanceDown, oKisLowerBreakoutCountExceeded, useDeltaSessionCloseOpenUP, useDeltaSessionCloseOpenDOWN, useDeltaSessionHighLowUP, useDeltaSessionHighLowDOWN, deltaSessionMinFilter, deltaSessionMaxFilter, enableDistanceFromVWAPCondition, minDistanceFromVWAP, maxDistanceFromVWAP, enableSTD3HighLowTracking, fperiodVol, oKisVOL);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.VwapVAB VwapVAB(bool enablePreviousSessionRangeBreakout, int resetPeriod, int minBarsForSignal, int minimumTicks, int maximumTicks, bool showLimusineOpenCloseUP, bool showLimusineOpenCloseDOWN, bool showLimusineHighLowUP, bool showLimusineHighLowDOWN, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, bool oKisAfterBarsSinceResetUP, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, bool oKisAfterBarsSinceResetDown, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded, bool useDeltaSessionCloseOpenUP, bool useDeltaSessionCloseOpenDOWN, bool useDeltaSessionHighLowUP, bool useDeltaSessionHighLowDOWN, int deltaSessionMinFilter, int deltaSessionMaxFilter, bool enableDistanceFromVWAPCondition, int minDistanceFromVWAP, int maxDistanceFromVWAP, bool enableSTD3HighLowTracking, int fperiodVol, bool oKisVOL)
		{
			return indicator.VwapVAB(Input, enablePreviousSessionRangeBreakout, resetPeriod, minBarsForSignal, minimumTicks, maximumTicks, showLimusineOpenCloseUP, showLimusineOpenCloseDOWN, showLimusineHighLowUP, showLimusineHighLowDOWN, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, oKisAfterBarsSinceResetUP, oKisAboveUpperThreshold, oKisWithinMaxEntryDistance, oKisUpperBreakoutCountExceeded, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts, oKisAfterBarsSinceResetDown, oKisBelovLowerThreshold, oKisWithinMaxEntryDistanceDown, oKisLowerBreakoutCountExceeded, useDeltaSessionCloseOpenUP, useDeltaSessionCloseOpenDOWN, useDeltaSessionHighLowUP, useDeltaSessionHighLowDOWN, deltaSessionMinFilter, deltaSessionMaxFilter, enableDistanceFromVWAPCondition, minDistanceFromVWAP, maxDistanceFromVWAP, enableSTD3HighLowTracking, fperiodVol, oKisVOL);
		}

		public Indicators.ninpai.VwapVAB VwapVAB(ISeries<double> input , bool enablePreviousSessionRangeBreakout, int resetPeriod, int minBarsForSignal, int minimumTicks, int maximumTicks, bool showLimusineOpenCloseUP, bool showLimusineOpenCloseDOWN, bool showLimusineHighLowUP, bool showLimusineHighLowDOWN, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, bool oKisAfterBarsSinceResetUP, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, bool oKisAfterBarsSinceResetDown, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded, bool useDeltaSessionCloseOpenUP, bool useDeltaSessionCloseOpenDOWN, bool useDeltaSessionHighLowUP, bool useDeltaSessionHighLowDOWN, int deltaSessionMinFilter, int deltaSessionMaxFilter, bool enableDistanceFromVWAPCondition, int minDistanceFromVWAP, int maxDistanceFromVWAP, bool enableSTD3HighLowTracking, int fperiodVol, bool oKisVOL)
		{
			return indicator.VwapVAB(input, enablePreviousSessionRangeBreakout, resetPeriod, minBarsForSignal, minimumTicks, maximumTicks, showLimusineOpenCloseUP, showLimusineOpenCloseDOWN, showLimusineHighLowUP, showLimusineHighLowDOWN, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, oKisAfterBarsSinceResetUP, oKisAboveUpperThreshold, oKisWithinMaxEntryDistance, oKisUpperBreakoutCountExceeded, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts, oKisAfterBarsSinceResetDown, oKisBelovLowerThreshold, oKisWithinMaxEntryDistanceDown, oKisLowerBreakoutCountExceeded, useDeltaSessionCloseOpenUP, useDeltaSessionCloseOpenDOWN, useDeltaSessionHighLowUP, useDeltaSessionHighLowDOWN, deltaSessionMinFilter, deltaSessionMaxFilter, enableDistanceFromVWAPCondition, minDistanceFromVWAP, maxDistanceFromVWAP, enableSTD3HighLowTracking, fperiodVol, oKisVOL);
		}
	}
}

#endregion
