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
    public class VABVolumetric501 : Indicator
    {
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
        
        private double highestSTD3Upper;
        private double lowestSTD3Lower;
        private bool isFirstBarSinceReset;

        private class VolumetricParameters
        {
            public bool Enabled { get; set; }
            public double Min { get; set; }
            public double Max { get; set; }
        }

        private VolumetricParameters[] upParameters;
        private VolumetricParameters[] downParameters;
		
		private bool pocConditionEnabled;
		private int pocTicksDistance;
		private Series<double> pocSeries;
		
		// New parameter for Value Area condition
        private bool enableValueAreaCondition;
        private bool useOpenForVACondition;
		
		// New parameters for cumulative delta condition
        private bool enableCumulativeDeltaConditionUP;
        private bool enableCumulativeDeltaConditionDOWN;
        private int cumulativeDeltaBarsRangeUP;
        private int cumulativeDeltaBarsRangeDOWN;
        private int cumulativeDeltaJumpUP;
        private int cumulativeDeltaJumpDOWN;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Indicateur combiné VAB et Volumetric Filter";
                Name = "VABVolumetric501";
                Calculate = Calculate.OnEachTick;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive = true;

                // Paramètres VAB
                ResetPeriod = 120;
                MinBarsForSignal = 10;
                MaxBarsForSignal = 100;
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

                // Paramètres Limusine
                MinimumTicks = 10;
                MaximumTicks = 30;
                ShowLimusineOpenCloseUP = true;
                ShowLimusineOpenCloseDOWN = true;
                ShowLimusineHighLowUP = true;
                ShowLimusineHighLowDOWN = true;

                // Nouveaux paramètres
                EnableSlopeFilterUP = false;
                MinSlopeValueUP = 0.0;
                SlopeBarsCountUP = 5;

                EnableSlopeFilterDOWN = false;
                MinSlopeValueDOWN = 0.0;
                SlopeBarsCountDOWN = 5;

                EnableDistanceFromVWAPCondition = false;
                MinDistanceFromVWAP = 10;
                MaxDistanceFromVWAP = 50;

                EnableSTD3HighLowTracking = false;

                // Paramètres Volumetric Filter
                UpArrowColor = Brushes.Green;
                DownArrowColor = Brushes.Red;
				POCColor = Brushes.Blue;
				pocConditionEnabled = false;
				pocTicksDistance = 2;
				// Initialize new parameter for Value Area condition
                enableValueAreaCondition = false;
                useOpenForVACondition = true;
				// Initialize new parameters for cumulative delta condition
                enableCumulativeDeltaConditionUP = false;
                enableCumulativeDeltaConditionDOWN = false;
                cumulativeDeltaBarsRangeUP = 3;
                cumulativeDeltaBarsRangeDOWN = 3;
                cumulativeDeltaJumpUP = 100;
                cumulativeDeltaJumpDOWN = 100;

                InitializeVolumetricParameters();

                AddPlot(Brushes.Orange, "VWAP");
                AddPlot(Brushes.Red, "StdDev1Upper");
                AddPlot(Brushes.Red, "StdDev1Lower");
                AddPlot(Brushes.Green, "StdDev2Upper");
                AddPlot(Brushes.Green, "StdDev2Lower");
                AddPlot(Brushes.Blue, "StdDev3Upper");
                AddPlot(Brushes.Blue, "StdDev3Lower");
				AddPlot(new Stroke(POCColor, 2), PlotStyle.Dot, "POC");
            }
            else if (State == State.Configure)
            {
                ResetValues(DateTime.MinValue);
            }
            else if (State == State.DataLoaded)
            {
                ADX1 = ADX(Close, 14);
                ATR1 = ATR(Close, 14);
                VOL1 = VOL(Close);
                VOLMA1 = VOLMA(Close, Convert.ToInt32(FperiodVol));
				pocSeries = new Series<double>(this);
            }
        }

        private void InitializeVolumetricParameters()
        {
            upParameters = new VolumetricParameters[7];
            downParameters = new VolumetricParameters[7];

            for (int i = 0; i < 7; i++)
            {
                upParameters[i] = new VolumetricParameters();
                downParameters[i] = new VolumetricParameters();
            }

            // Set default values (you can adjust these as needed)
            SetDefaultParameterValues(upParameters[0], false, 200, 2000);    // BarDelta
            SetDefaultParameterValues(upParameters[1], false, 10, 50);       // DeltaPercent
            SetDefaultParameterValues(upParameters[2], false, 100, 1000);    // DeltaChange
            SetDefaultParameterValues(upParameters[3], false, 1000, 10000);  // TotalBuyingVolume
            SetDefaultParameterValues(upParameters[4], false, 0, 5000);      // TotalSellingVolume
            SetDefaultParameterValues(upParameters[5], false, 100, 1000);    // Trades
            SetDefaultParameterValues(upParameters[6], false, 2000, 20000);  // TotalVolume

            // Set default values for down parameters (adjust as needed)
            SetDefaultParameterValues(downParameters[0], false, 200, 2000);  // BarDelta (abs value)
            SetDefaultParameterValues(downParameters[1], false, 10, 50);     // DeltaPercent (abs value)
            SetDefaultParameterValues(downParameters[2], false, 100, 1000);  // DeltaChange (abs value)
            SetDefaultParameterValues(downParameters[3], false, 0, 5000);    // TotalBuyingVolume
            SetDefaultParameterValues(downParameters[4], false, 1000, 10000);// TotalSellingVolume
            SetDefaultParameterValues(downParameters[5], false, 100, 1000);  // Trades
            SetDefaultParameterValues(downParameters[6], false, 2000, 20000);// TotalVolume
        }

        private void SetDefaultParameterValues(VolumetricParameters param, bool enabled, double min, double max)
        {
            param.Enabled = enabled;
            param.Min = min;
            param.Max = max;
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 20 || !(Bars.BarsSeries.BarsType is NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType barsType))
				return;
			
			var currentBarVolumes = barsType.Volumes[CurrentBar];
			// Calcul du POC
			double pocPrice;
			long maxVolume = currentBarVolumes.GetMaximumVolume(null, out pocPrice);
			pocSeries[0] = pocPrice;
			Values[0][0] = pocPrice;

            DateTime currentBarTime = Time[0];

            if (Bars.IsFirstBarOfSession)
            {
                ResetValues(currentBarTime);
            }
            else if (lastResetTime != DateTime.MinValue && (currentBarTime - lastResetTime).TotalMinutes >= ResetPeriod)
            {
                ResetValues(currentBarTime);
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

            // Update STD3 high/low tracking
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

            // Vérification des conditions combinées
            bool showUpArrow = ShouldDrawUpArrow() && CheckVolumetricConditions(true);
            bool showDownArrow = ShouldDrawDownArrow() && CheckVolumetricConditions(false);
			
			// Condition POC
			if (pocConditionEnabled)
			{
				double closePrice = Close[0];
				double tickSize = TickSize;
		
				showUpArrow = showUpArrow && (pocPrice <= closePrice - pocTicksDistance * tickSize);
				showDownArrow = showDownArrow && (pocPrice >= closePrice + pocTicksDistance * tickSize);
			}
			
			// New Value Area condition
            if (enableValueAreaCondition)
            {
                if (useOpenForVACondition)
                {
                    showUpArrow = showUpArrow && (Open[0] > Values[2][0] && Open[0] < Values[1][0]);
                    showDownArrow = showDownArrow && (Open[0] > Values[2][0] && Open[0] < Values[1][0]);
                }
                else
                {
                    showUpArrow = showUpArrow && (Low[0] > Values[2][0] && Low[0] < Values[1][0]);
                    showDownArrow = showDownArrow && (High[0] > Values[2][0] && High[0] < Values[1][0]);
                }
            }
			
			 // New Cumulative Delta condition
            if (enableCumulativeDeltaConditionUP && showUpArrow)
            {
                for (int i = 1; i <= cumulativeDeltaBarsRangeUP; i++)
                {
                    if (CurrentBar - i < 0 || barsType.Volumes[CurrentBar - i].CumulativeDelta + cumulativeDeltaJumpUP > barsType.Volumes[CurrentBar - i + 1].CumulativeDelta)
                    {
                        showUpArrow = false;
                        break;
                    }
                }
            }

            if (enableCumulativeDeltaConditionDOWN && showDownArrow)
            {
                for (int i = 1; i <= cumulativeDeltaBarsRangeDOWN; i++)
                {
                    if (CurrentBar - i < 0 || barsType.Volumes[CurrentBar - i].CumulativeDelta - cumulativeDeltaJumpDOWN < barsType.Volumes[CurrentBar - i + 1].CumulativeDelta)
                    {
                        showDownArrow = false;
                        break;
                    }
                }
            }

            if (showUpArrow)
            {
                Draw.ArrowUp(this, "UpArrow" + CurrentBar, true, 0, Low[0] - 2 * TickSize, UpArrowColor);
                upperBreakoutCount++;

                // Calcul de la distance entre VWAP et StdDev1 Lower (StdDev-1)
                double distanceRed = Values[0][0] - Values[2][0]; // VWAP - StdDev1 Lower
                double priceForRedDot = Close[0] - distanceRed;

                // Dessiner le point rouge
                Draw.Dot(this, "RedDotUp" + CurrentBar, true, 0, priceForRedDot, Brushes.Red);

                // Calcul de la distance pour le point bleu (comme précédemment)
                double distanceBlue = Values[1][0] - Values[0][0]; // StdDev1 Upper - VWAP
                double priceForBlueDot = Close[0] + distanceBlue;

                // Dessiner le point bleu
                Draw.Dot(this, "BlueDotUp" + CurrentBar, true, 0, priceForBlueDot, Brushes.Blue);

                // Dessiner le point blanc au prix actuel
                Draw.Dot(this, "WhiteDotUp" + CurrentBar, true, 0, Close[0], Brushes.White);
				Draw.Dot(this, "POCUP" + CurrentBar, false, 0, pocPrice - pocTicksDistance * TickSize, POCColor);
            }
            else if (showDownArrow)
            {
                Draw.ArrowDown(this, "DownArrow" + CurrentBar, true, 0, High[0] + 2 * TickSize, DownArrowColor);
                lowerBreakoutCount++;

                // Calcul de la distance entre StdDev1 Upper (StdDev+1) et VWAP
                double distanceRed = Values[1][0] - Values[0][0]; // StdDev1 Upper - VWAP
                double priceForRedDot = Close[0] + distanceRed;

                // Dessiner le point rouge
                Draw.Dot(this, "RedDotDown" + CurrentBar, true, 0, priceForRedDot, Brushes.Red);

                // Calcul de la distance pour le point bleu (comme précédemment)
                double distanceBlue = Values[0][0] - Values[2][0]; // VWAP - StdDev1 Lower
                double priceForBlueDot = Close[0] - distanceBlue;

                // Dessiner le point bleu
                Draw.Dot(this, "BlueDotDown" + CurrentBar, true, 0, priceForBlueDot, Brushes.Blue);

                // Dessiner le point blanc au prix actuel
                Draw.Dot(this, "WhiteDotDown" + CurrentBar, true, 0, Close[0], Brushes.White);
				Draw.Dot(this, "POCDOWN" + CurrentBar, false, 0, pocPrice + pocTicksDistance * TickSize, POCColor);
            }
        }

        private bool ShouldDrawUpArrow()
        {
            // Calculate the distance from VWAP
            double vwap = Values[0][0];
            double distanceInTicks = (Close[0] - vwap) / TickSize;

            bool bvaCondition = (Close[0] > Open[0]) &&
                   (!OKisADX || (ADX1[0] > FminADX && ADX1[0] < FmaxADX)) &&
                   (!OKisATR || (ATR1[0] > FminATR && ATR1[0] < FmaxATR)) &&
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
									
            // New Slope Condition for UP Arrows
            if (EnableSlopeFilterUP)
            {
                if (CurrentBar < SlopeBarsCountUP)
                    return false; // Not enough bars to calculate slope

                double oldValue = Values[5][SlopeBarsCountUP - 1]; // StdDev3Upper value SlopeBarsCountUP bars ago
                double newValue = Values[5][0]; // Current StdDev3Upper value

                double slopePerBar = (newValue - oldValue) / SlopeBarsCountUP;

                if (slopePerBar < MinSlopeValueUP)
                    return false; // The slope is not steep enough upwards
            }

            // New condition for STD3 Upper at its highest
            bool std3Condition = !EnableSTD3HighLowTracking || Values[5][0] >= highestSTD3Upper;

            return bvaCondition && limusineCondition && std3Condition;
        }

        private bool ShouldDrawDownArrow()
        {
            // Calculate the distance from VWAP
            double vwap = Values[0][0];
            double distanceInTicks = (vwap - Close[0]) / TickSize;

            bool bvaCondition = (Close[0] < Open[0]) &&
                   (!OKisADX || (ADX1[0] > FminADX && ADX1[0] < FmaxADX)) &&
                   (!OKisATR || (ATR1[0] > FminATR && ATR1[0] < FmaxATR)) &&
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

            // New Slope Condition for DOWN Arrows
            if (EnableSlopeFilterDOWN)
            {
                if (CurrentBar < SlopeBarsCountDOWN)
                    return false; // Not enough bars to calculate slope

                double oldValue = Values[6][SlopeBarsCountDOWN - 1]; // StdDev3Lower value SlopeBarsCountDOWN bars ago
                double newValue = Values[6][0]; // Current StdDev3Lower value

                double slopePerBar = (newValue - oldValue) / SlopeBarsCountDOWN;

                if (slopePerBar > -MinSlopeValueDOWN)
                    return false; // The slope is not steep enough downwards
            }

            // New condition for STD3 Lower at its lowest
            bool std3Condition = !EnableSTD3HighLowTracking || Values[6][0] <= lowestSTD3Lower;

            return bvaCondition && limusineCondition && std3Condition;
        }

        private bool CheckVolumetricConditions(bool isUpDirection)
        {
            if (!(Bars.BarsSeries.BarsType is NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType barsType))
                return false;

            var currentBarVolumes = barsType.Volumes[CurrentBar];
            var previousBarVolumes = barsType.Volumes[CurrentBar - 1];

            double[] volumetricValues = new double[7];
            volumetricValues[0] = currentBarVolumes.BarDelta;
            volumetricValues[1] = currentBarVolumes.GetDeltaPercent();
            volumetricValues[2] = volumetricValues[0] - previousBarVolumes.BarDelta;
            volumetricValues[3] = currentBarVolumes.TotalBuyingVolume;
            volumetricValues[4] = currentBarVolumes.TotalSellingVolume;
            volumetricValues[5] = currentBarVolumes.Trades;
            volumetricValues[6] = currentBarVolumes.TotalVolume;

            VolumetricParameters[] parameters = isUpDirection ? upParameters : downParameters;

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Enabled)
                {
                    if (isUpDirection)
                    {
                        switch (i)
                        {
                            case 0: // BarDelta
                            case 1: // DeltaPercent
                            case 2: // DeltaChange
                                if (volumetricValues[i] < parameters[i].Min || volumetricValues[i] > parameters[i].Max)
                                    return false;
                                break;
                            default:
                                if (volumetricValues[i] < parameters[i].Min || volumetricValues[i] > parameters[i].Max)
                                    return false;
                                break;
                        }
                    }
                    else // Down direction
                    {
                        switch (i)
                        {
                            case 0: // BarDelta
                            case 1: // DeltaPercent
                            case 2: // DeltaChange
                                if (volumetricValues[i] > -parameters[i].Min || volumetricValues[i] < -parameters[i].Max)
                                    return false;
                                break;
                            default:
                                if (volumetricValues[i] < parameters[i].Min || volumetricValues[i] > parameters[i].Max)
                                    return false;
                                break;
                        }
                    }
                }
            }
            return true;
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
            // Reset STD3 high/low tracking
            isFirstBarSinceReset = true;
            highestSTD3Upper = double.MinValue;
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
		public int MaxBarsForSignal
		{ get; set; }
		
		[NinjaScriptProperty]
        [Display(Name="Enable Value Area Condition", Order=4, GroupName="0.1_BVA Parameters")]
        public bool EnableValueAreaCondition
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Use Open for Value Area Condition", Order=5, GroupName="0.1_BVA Parameters")]
        public bool UseOpenForVACondition
        { get; set; }
		
		
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
		
		
        
		
		// New Parameters for Slope Filter (updated)
        [Display(Name = "Enable Slope Filter UP", Order = 1, GroupName = "0.5_Slope Filter UP")]
        public bool EnableSlopeFilterUP { get; set; }

        [Display(Name = "Minimum Slope Value UP", Order = 2, GroupName = "0.5_Slope Filter UP")]
        public double MinSlopeValueUP { get; set; }

        [Display(Name = "Slope Bars Count UP", Order = 3, GroupName = "0.5_Slope Filter UP")]
        public int SlopeBarsCountUP { get; set; }

        [Display(Name = "Enable Slope Filter DOWN", Order = 1, GroupName = "0.6_Slope Filter DOWN")]
        public bool EnableSlopeFilterDOWN { get; set; }

        [Display(Name = "Minimum Slope Value DOWN", Order = 2, GroupName = "0.6_Slope Filter DOWN")]
        public double MinSlopeValueDOWN { get; set; }

        [Display(Name = "Slope Bars Count DOWN", Order = 3, GroupName = "0.6_Slope Filter DOWN")]
        public int SlopeBarsCountDOWN { get; set; }
		
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
		
		
		
		
		
		
		// ############ ADX #############
		// ADX
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Fmin ADX", Order = 1, GroupName = "ADX")]
		public double FminADX { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Fmax ADX", Order = 2, GroupName = "ADX")]
		public double FmaxADX { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 1)]
		[Display(Name = "OKisADX", Description = "Check ADX", Order = 1, GroupName = "ADX")]
		public bool OKisADX { get; set; }
		
		// ############ ATR #############
		// ATR
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Fmin ATR", Order = 1, GroupName = "ATR")]
		public double FminATR { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name = "Fmax ATR", Order = 2, GroupName = "ATR")]
		public double FmaxATR { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 1)]
		[Display(Name = "OKisATR", Description = "Check ATR", Order = 1, GroupName = "ATR")]
		public bool OKisATR { get; set; }
		
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

	   // ... (autres propriétés)
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> VWAP => Values[0];

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> StdDev1Upper => Values[1];

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> StdDev1Lower => Values[2];

        
		// ##################### Propriétés Volumetric Filter #################################
		// Propriétés Volumetric Filter
        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Up Arrow Color", Order = 1, GroupName = "Visuals")]
        public Brush UpArrowColor { get; set; }

        [Browsable(false)]
        public string UpArrowColorSerializable
        {
            get { return Serialize.BrushToString(UpArrowColor); }
            set { UpArrowColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Down Arrow Color", Order = 2, GroupName = "Visuals")]
        public Brush DownArrowColor { get; set; }

        [Browsable(false)]
        public string DownArrowColorSerializable
        {
            get { return Serialize.BrushToString(DownArrowColor); }
            set { DownArrowColor = Serialize.StringToBrush(value); }
        }
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="POC Color", Description="Color for POC", Order=3, GroupName="Visuals")]
		public Brush POCColor { get; set; }
		
		[Browsable(false)]
		public Series<double> POC
		{
			get { return Values[0]; }
		}
		
		[NinjaScriptProperty]
		[Display(Name="Enable POC Condition", Description="Enable the Point of Control condition", Order=1, GroupName="POC Parameters")]
		public bool POCConditionEnabled
		{
			get { return pocConditionEnabled; }
			set { pocConditionEnabled = value; }
		}
		
		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name="POC Ticks Distance", Description="Number of ticks for POC distance from close", Order=2, GroupName="POC Parameters")]
		public int POCTicksDistance
		{
			get { return pocTicksDistance; }
			set { pocTicksDistance = Math.Max(1, value); }
		}

        // UP Parameters
        [NinjaScriptProperty]
        [Display(Name = "Bar Delta UP Enabled", Order = 1, GroupName = "2.01_BarDeltaUP")]
        public bool BarDeltaUPEnabled
        {
            get { return upParameters[0].Enabled; }
            set { upParameters[0].Enabled = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Min Bar Delta UP", Order = 2, GroupName = "2.01_BarDeltaUP")]
        public double MinBarDeltaUP
        {
            get { return upParameters[0].Min; }
            set { upParameters[0].Min = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Max Bar Delta UP", Order = 3, GroupName = "2.01_BarDeltaUP")]
        public double MaxBarDeltaUP
        {
            get { return upParameters[0].Max; }
            set { upParameters[0].Max = value; }
        }

        // Delta Percent UP
        [NinjaScriptProperty]
        [Display(Name = "Delta Percent UP Enabled", Order = 1, GroupName = "2.02_DeltaPercentUP")]
        public bool DeltaPercentUPEnabled
        {
            get { return upParameters[1].Enabled; }
            set { upParameters[1].Enabled = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Min Delta Percent UP", Order = 2, GroupName = "2.02_DeltaPercentUP")]
        public double MinDeltaPercentUP
        {
            get { return upParameters[1].Min; }
            set { upParameters[1].Min = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Max Delta Percent UP", Order = 3, GroupName = "2.02_DeltaPercentUP")]
        public double MaxDeltaPercentUP
        {
            get { return upParameters[1].Max; }
            set { upParameters[1].Max = value; }
        }

        // Delta Change UP
        [NinjaScriptProperty]
        [Display(Name = "Delta Change UP Enabled", Order = 1, GroupName = "2.03_DeltaChangeUP")]
        public bool DeltaChangeUPEnabled
        {
            get { return upParameters[2].Enabled; }
            set { upParameters[2].Enabled = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Min Delta Change UP", Order = 2, GroupName = "2.03_DeltaChangeUP")]
        public double MinDeltaChangeUP
        {
            get { return upParameters[2].Min; }
            set { upParameters[2].Min = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Max Delta Change UP", Order = 3, GroupName = "2.03_DeltaChangeUP")]
        public double MaxDeltaChangeUP
        {
            get { return upParameters[2].Max; }
            set { upParameters[2].Max = value; }
        }

        // Total Buying Volume UP
        [NinjaScriptProperty]
        [Display(Name = "Total Buying Volume UP Enabled", Order = 1, GroupName = "2.04_TotalBuyingVolumeUP")]
        public bool TotalBuyingVolumeUPEnabled
        {
            get { return upParameters[3].Enabled; }
            set { upParameters[3].Enabled = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Min Total Buying Volume UP", Order = 2, GroupName = "2.04_TotalBuyingVolumeUP")]
        public double MinTotalBuyingVolumeUP
        {
            get { return upParameters[3].Min; }
            set { upParameters[3].Min = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Max Total Buying Volume UP", Order = 3, GroupName = "2.04_TotalBuyingVolumeUP")]
        public double MaxTotalBuyingVolumeUP
        {
            get { return upParameters[3].Max; }
            set { upParameters[3].Max = value; }
        }

        // Total Selling Volume UP
        [NinjaScriptProperty]
        [Display(Name = "Total Selling Volume UP Enabled", Order = 1, GroupName = "2.05_TotalSellingVolumeUP")]
        public bool TotalSellingVolumeUPEnabled
        {
            get { return upParameters[4].Enabled; }
            set { upParameters[4].Enabled = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Min Total Selling Volume UP", Order = 2, GroupName = "2.05_TotalSellingVolumeUP")]
        public double MinTotalSellingVolumeUP
        {
            get { return upParameters[4].Min; }
            set { upParameters[4].Min = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Max Total Selling Volume UP", Order = 3, GroupName = "2.05_TotalSellingVolumeUP")]
        public double MaxTotalSellingVolumeUP
        {
            get { return upParameters[4].Max; }
            set { upParameters[4].Max = value; }
        }

        // Trades UP
        [NinjaScriptProperty]
        [Display(Name = "Trades UP Enabled", Order = 1, GroupName = "2.06_TradesUP")]
        public bool TradesUPEnabled
        {
            get { return upParameters[5].Enabled; }
            set { upParameters[5].Enabled = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Min Trades UP", Order = 2, GroupName = "2.06_TradesUP")]
        public double MinTradesUP
        {
            get { return upParameters[5].Min; }
            set { upParameters[5].Min = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Max Trades UP", Order = 3, GroupName = "2.06_TradesUP")]
        public double MaxTradesUP
        {
            get { return upParameters[5].Max; }
            set { upParameters[5].Max = value; }
        }

        // Total Volume UP
        [NinjaScriptProperty]
        [Display(Name = "Total Volume UP Enabled", Order = 1, GroupName = "2.07_TotalVolumeUP")]
        public bool TotalVolumeUPEnabled
        {
            get { return upParameters[6].Enabled; }
            set { upParameters[6].Enabled = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Min Total Volume UP", Order = 2, GroupName = "2.07_TotalVolumeUP")]
        public double MinTotalVolumeUP
        {
            get { return upParameters[6].Min; }
            set { upParameters[6].Min = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Max Total Volume UP", Order = 3, GroupName = "2.07_TotalVolumeUP")]
        public double MaxTotalVolumeUP
        {
            get { return upParameters[6].Max; }
            set { upParameters[6].Max = value; }
        }

        // DOWN Parameters
        // Bar Delta DOWN
        [NinjaScriptProperty]
        [Display(Name = "Bar Delta DOWN Enabled", Order = 1, GroupName = "3.01_BarDeltaDOWN")]
        public bool BarDeltaDOWNEnabled
        {
            get { return downParameters[0].Enabled; }
            set { downParameters[0].Enabled = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Min Bar Delta DOWN", Order = 2, GroupName = "3.01_BarDeltaDOWN")]
        public double MinBarDeltaDOWN
        {
            get { return downParameters[0].Min; }
            set { downParameters[0].Min = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Max Bar Delta DOWN", Order = 3, GroupName = "3.01_BarDeltaDOWN")]
        public double MaxBarDeltaDOWN
        {
            get { return downParameters[0].Max; }
            set { downParameters[0].Max = value; }
        }
        
        // Delta Percent DOWN
        [NinjaScriptProperty]
        [Display(Name = "Delta Percent DOWN Enabled", Order = 1, GroupName = "3.02_DeltaPercentDOWN")]
        public bool DeltaPercentDOWNEnabled
        {
            get { return downParameters[1].Enabled; }
            set { downParameters[1].Enabled = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Min Delta Percent DOWN", Order = 2, GroupName = "3.02_DeltaPercentDOWN")]
        public double MinDeltaPercentDOWN
        {
            get { return downParameters[1].Min; }
            set { downParameters[1].Min = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Max Delta Percent DOWN", Order = 3, GroupName = "3.02_DeltaPercentDOWN")]
        public double MaxDeltaPercentDOWN
        {
            get { return downParameters[1].Max; }
            set { downParameters[1].Max = value; }
        }
        
        // Delta Change DOWN
        [NinjaScriptProperty]
        [Display(Name = "Delta Change DOWN Enabled", Order = 1, GroupName = "3.03_DeltaChangeDOWN")]
        public bool DeltaChangeDOWNEnabled
        {
            get { return downParameters[2].Enabled; }
            set { downParameters[2].Enabled = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Min Delta Change DOWN", Order = 2, GroupName = "3.03_DeltaChangeDOWN")]
        public double MinDeltaChangeDOWN
        {
            get { return downParameters[2].Min; }
            set { downParameters[2].Min = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Max Delta Change DOWN", Order = 3, GroupName = "3.03_DeltaChangeDOWN")]
        public double MaxDeltaChangeDOWN
        {
            get { return downParameters[2].Max; }
            set { downParameters[2].Max = value; }
        }
        
        // Total Buying Volume DOWN
        [NinjaScriptProperty]
        [Display(Name = "Total Buying Volume DOWN Enabled", Order = 1, GroupName = "3.04_TotalBuyingVolumeDOWN")]
        public bool TotalBuyingVolumeDOWNEnabled
        {
            get { return downParameters[3].Enabled; }
            set { downParameters[3].Enabled = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Min Total Buying Volume DOWN", Order = 2, GroupName = "3.04_TotalBuyingVolumeDOWN")]
        public double MinTotalBuyingVolumeDOWN
        {
            get { return downParameters[3].Min; }
            set { downParameters[3].Min = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Max Total Buying Volume DOWN", Order = 3, GroupName = "3.04_TotalBuyingVolumeDOWN")]
        public double MaxTotalBuyingVolumeDOWN
        {
            get { return downParameters[3].Max; }
            set { downParameters[3].Max = value; }
        }
        
        // Total Selling Volume DOWN
        [NinjaScriptProperty]
        [Display(Name = "Total Selling Volume DOWN Enabled", Order = 1, GroupName = "3.05_TotalSellingVolumeDOWN")]
        public bool TotalSellingVolumeDOWNEnabled
        {
            get { return downParameters[4].Enabled; }
            set { downParameters[4].Enabled = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Min Total Selling Volume DOWN", Order = 2, GroupName = "3.05_TotalSellingVolumeDOWN")]
        public double MinTotalSellingVolumeDOWN
        {
            get { return downParameters[4].Min; }
            set { downParameters[4].Min = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Max Total Selling Volume DOWN", Order = 3, GroupName = "3.05_TotalSellingVolumeDOWN")]
        public double MaxTotalSellingVolumeDOWN
        {
            get { return downParameters[4].Max; }
            set { downParameters[4].Max = value; }
        }
        
        // Trades DOWN
        [NinjaScriptProperty]
        [Display(Name = "Trades DOWN Enabled", Order = 1, GroupName = "3.06_TradesDOWN")]
        public bool TradesDOWNEnabled
        {
            get { return downParameters[5].Enabled; }
            set { downParameters[5].Enabled = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Min Trades DOWN", Order = 2, GroupName = "3.06_TradesDOWN")]
        public double MinTradesDOWN
        {
            get { return downParameters[5].Min; }
            set { downParameters[5].Min = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Max Trades DOWN", Order = 3, GroupName = "3.06_TradesDOWN")]
        public double MaxTradesDOWN
        {
            get { return downParameters[5].Max; }
            set { downParameters[5].Max = value; }
        }
        
        // Total Volume DOWN
        [NinjaScriptProperty]
        [Display(Name = "Total Volume DOWN Enabled", Order = 1, GroupName = "3.07_TotalVolumeDOWN")]
        public bool TotalVolumeDOWNEnabled
        {
            get { return downParameters[6].Enabled; }
            set { downParameters[6].Enabled = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Min Total Volume DOWN", Order = 2, GroupName = "3.07_TotalVolumeDOWN")]
        public double MinTotalVolumeDOWN
        {
            get { return downParameters[6].Min; }
            set { downParameters[6].Min = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Max Total Volume DOWN", Order = 3, GroupName = "3.07_TotalVolumeDOWN")]
        public double MaxTotalVolumeDOWN
        {
            get { return downParameters[6].Max; }
            set { downParameters[6].Max = value; }
        }
		
		[NinjaScriptProperty]
        [Display(Name="Enable Cumulative Delta Condition UP", Order=1, GroupName="4.01_Cumulative_Delta_Parameters")]
        public bool EnableCumulativeDeltaConditionUP
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Cumulative Delta Bars Range UP", Order=2, GroupName="4.01_Cumulative_Delta_Parameters")]
        public int CumulativeDeltaBarsRangeUP
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Cumulative Delta Jump UP", Order=3, GroupName="4.01_Cumulative_Delta_Parameters")]
        public int CumulativeDeltaJumpUP
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Enable Cumulative Delta Condition DOWN", Order=4, GroupName="4.01_Cumulative_Delta_Parameters")]
        public bool EnableCumulativeDeltaConditionDOWN
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Cumulative Delta Bars Range DOWN", Order=5, GroupName="4.01_Cumulative_Delta_Parameters")]
        public int CumulativeDeltaBarsRangeDOWN
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Cumulative Delta Jump DOWN", Order=6, GroupName="4.01_Cumulative_Delta_Parameters")]
        public int CumulativeDeltaJumpDOWN
        { get; set; }

        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ninpai.VABVolumetric501[] cacheVABVolumetric501;
		public ninpai.VABVolumetric501 VABVolumetric501(int resetPeriod, int minBarsForSignal, bool enableValueAreaCondition, bool useOpenForVACondition, int minimumTicks, int maximumTicks, bool showLimusineOpenCloseUP, bool showLimusineOpenCloseDOWN, bool showLimusineHighLowUP, bool showLimusineHighLowDOWN, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, bool oKisAfterBarsSinceResetUP, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, bool oKisAfterBarsSinceResetDown, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded, bool enableDistanceFromVWAPCondition, int minDistanceFromVWAP, int maxDistanceFromVWAP, bool enableSTD3HighLowTracking, double fminADX, double fmaxADX, bool oKisADX, double fminATR, double fmaxATR, bool oKisATR, int fperiodVol, bool oKisVOL, Brush upArrowColor, Brush downArrowColor, Brush pOCColor, bool pOCConditionEnabled, int pOCTicksDistance, bool barDeltaUPEnabled, double minBarDeltaUP, double maxBarDeltaUP, bool deltaPercentUPEnabled, double minDeltaPercentUP, double maxDeltaPercentUP, bool deltaChangeUPEnabled, double minDeltaChangeUP, double maxDeltaChangeUP, bool totalBuyingVolumeUPEnabled, double minTotalBuyingVolumeUP, double maxTotalBuyingVolumeUP, bool totalSellingVolumeUPEnabled, double minTotalSellingVolumeUP, double maxTotalSellingVolumeUP, bool tradesUPEnabled, double minTradesUP, double maxTradesUP, bool totalVolumeUPEnabled, double minTotalVolumeUP, double maxTotalVolumeUP, bool barDeltaDOWNEnabled, double minBarDeltaDOWN, double maxBarDeltaDOWN, bool deltaPercentDOWNEnabled, double minDeltaPercentDOWN, double maxDeltaPercentDOWN, bool deltaChangeDOWNEnabled, double minDeltaChangeDOWN, double maxDeltaChangeDOWN, bool totalBuyingVolumeDOWNEnabled, double minTotalBuyingVolumeDOWN, double maxTotalBuyingVolumeDOWN, bool totalSellingVolumeDOWNEnabled, double minTotalSellingVolumeDOWN, double maxTotalSellingVolumeDOWN, bool tradesDOWNEnabled, double minTradesDOWN, double maxTradesDOWN, bool totalVolumeDOWNEnabled, double minTotalVolumeDOWN, double maxTotalVolumeDOWN, bool enableCumulativeDeltaConditionUP, int cumulativeDeltaBarsRangeUP, int cumulativeDeltaJumpUP, bool enableCumulativeDeltaConditionDOWN, int cumulativeDeltaBarsRangeDOWN, int cumulativeDeltaJumpDOWN)
		{
			return VABVolumetric501(Input, resetPeriod, minBarsForSignal, enableValueAreaCondition, useOpenForVACondition, minimumTicks, maximumTicks, showLimusineOpenCloseUP, showLimusineOpenCloseDOWN, showLimusineHighLowUP, showLimusineHighLowDOWN, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, oKisAfterBarsSinceResetUP, oKisAboveUpperThreshold, oKisWithinMaxEntryDistance, oKisUpperBreakoutCountExceeded, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts, oKisAfterBarsSinceResetDown, oKisBelovLowerThreshold, oKisWithinMaxEntryDistanceDown, oKisLowerBreakoutCountExceeded, enableDistanceFromVWAPCondition, minDistanceFromVWAP, maxDistanceFromVWAP, enableSTD3HighLowTracking, fminADX, fmaxADX, oKisADX, fminATR, fmaxATR, oKisATR, fperiodVol, oKisVOL, upArrowColor, downArrowColor, pOCColor, pOCConditionEnabled, pOCTicksDistance, barDeltaUPEnabled, minBarDeltaUP, maxBarDeltaUP, deltaPercentUPEnabled, minDeltaPercentUP, maxDeltaPercentUP, deltaChangeUPEnabled, minDeltaChangeUP, maxDeltaChangeUP, totalBuyingVolumeUPEnabled, minTotalBuyingVolumeUP, maxTotalBuyingVolumeUP, totalSellingVolumeUPEnabled, minTotalSellingVolumeUP, maxTotalSellingVolumeUP, tradesUPEnabled, minTradesUP, maxTradesUP, totalVolumeUPEnabled, minTotalVolumeUP, maxTotalVolumeUP, barDeltaDOWNEnabled, minBarDeltaDOWN, maxBarDeltaDOWN, deltaPercentDOWNEnabled, minDeltaPercentDOWN, maxDeltaPercentDOWN, deltaChangeDOWNEnabled, minDeltaChangeDOWN, maxDeltaChangeDOWN, totalBuyingVolumeDOWNEnabled, minTotalBuyingVolumeDOWN, maxTotalBuyingVolumeDOWN, totalSellingVolumeDOWNEnabled, minTotalSellingVolumeDOWN, maxTotalSellingVolumeDOWN, tradesDOWNEnabled, minTradesDOWN, maxTradesDOWN, totalVolumeDOWNEnabled, minTotalVolumeDOWN, maxTotalVolumeDOWN, enableCumulativeDeltaConditionUP, cumulativeDeltaBarsRangeUP, cumulativeDeltaJumpUP, enableCumulativeDeltaConditionDOWN, cumulativeDeltaBarsRangeDOWN, cumulativeDeltaJumpDOWN);
		}

		public ninpai.VABVolumetric501 VABVolumetric501(ISeries<double> input, int resetPeriod, int minBarsForSignal, bool enableValueAreaCondition, bool useOpenForVACondition, int minimumTicks, int maximumTicks, bool showLimusineOpenCloseUP, bool showLimusineOpenCloseDOWN, bool showLimusineHighLowUP, bool showLimusineHighLowDOWN, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, bool oKisAfterBarsSinceResetUP, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, bool oKisAfterBarsSinceResetDown, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded, bool enableDistanceFromVWAPCondition, int minDistanceFromVWAP, int maxDistanceFromVWAP, bool enableSTD3HighLowTracking, double fminADX, double fmaxADX, bool oKisADX, double fminATR, double fmaxATR, bool oKisATR, int fperiodVol, bool oKisVOL, Brush upArrowColor, Brush downArrowColor, Brush pOCColor, bool pOCConditionEnabled, int pOCTicksDistance, bool barDeltaUPEnabled, double minBarDeltaUP, double maxBarDeltaUP, bool deltaPercentUPEnabled, double minDeltaPercentUP, double maxDeltaPercentUP, bool deltaChangeUPEnabled, double minDeltaChangeUP, double maxDeltaChangeUP, bool totalBuyingVolumeUPEnabled, double minTotalBuyingVolumeUP, double maxTotalBuyingVolumeUP, bool totalSellingVolumeUPEnabled, double minTotalSellingVolumeUP, double maxTotalSellingVolumeUP, bool tradesUPEnabled, double minTradesUP, double maxTradesUP, bool totalVolumeUPEnabled, double minTotalVolumeUP, double maxTotalVolumeUP, bool barDeltaDOWNEnabled, double minBarDeltaDOWN, double maxBarDeltaDOWN, bool deltaPercentDOWNEnabled, double minDeltaPercentDOWN, double maxDeltaPercentDOWN, bool deltaChangeDOWNEnabled, double minDeltaChangeDOWN, double maxDeltaChangeDOWN, bool totalBuyingVolumeDOWNEnabled, double minTotalBuyingVolumeDOWN, double maxTotalBuyingVolumeDOWN, bool totalSellingVolumeDOWNEnabled, double minTotalSellingVolumeDOWN, double maxTotalSellingVolumeDOWN, bool tradesDOWNEnabled, double minTradesDOWN, double maxTradesDOWN, bool totalVolumeDOWNEnabled, double minTotalVolumeDOWN, double maxTotalVolumeDOWN, bool enableCumulativeDeltaConditionUP, int cumulativeDeltaBarsRangeUP, int cumulativeDeltaJumpUP, bool enableCumulativeDeltaConditionDOWN, int cumulativeDeltaBarsRangeDOWN, int cumulativeDeltaJumpDOWN)
		{
			if (cacheVABVolumetric501 != null)
				for (int idx = 0; idx < cacheVABVolumetric501.Length; idx++)
					if (cacheVABVolumetric501[idx] != null && cacheVABVolumetric501[idx].ResetPeriod == resetPeriod && cacheVABVolumetric501[idx].MinBarsForSignal == minBarsForSignal && cacheVABVolumetric501[idx].EnableValueAreaCondition == enableValueAreaCondition && cacheVABVolumetric501[idx].UseOpenForVACondition == useOpenForVACondition && cacheVABVolumetric501[idx].MinimumTicks == minimumTicks && cacheVABVolumetric501[idx].MaximumTicks == maximumTicks && cacheVABVolumetric501[idx].ShowLimusineOpenCloseUP == showLimusineOpenCloseUP && cacheVABVolumetric501[idx].ShowLimusineOpenCloseDOWN == showLimusineOpenCloseDOWN && cacheVABVolumetric501[idx].ShowLimusineHighLowUP == showLimusineHighLowUP && cacheVABVolumetric501[idx].ShowLimusineHighLowDOWN == showLimusineHighLowDOWN && cacheVABVolumetric501[idx].MinEntryDistanceUP == minEntryDistanceUP && cacheVABVolumetric501[idx].MaxEntryDistanceUP == maxEntryDistanceUP && cacheVABVolumetric501[idx].MaxUpperBreakouts == maxUpperBreakouts && cacheVABVolumetric501[idx].OKisAfterBarsSinceResetUP == oKisAfterBarsSinceResetUP && cacheVABVolumetric501[idx].OKisAboveUpperThreshold == oKisAboveUpperThreshold && cacheVABVolumetric501[idx].OKisWithinMaxEntryDistance == oKisWithinMaxEntryDistance && cacheVABVolumetric501[idx].OKisUpperBreakoutCountExceeded == oKisUpperBreakoutCountExceeded && cacheVABVolumetric501[idx].MinEntryDistanceDOWN == minEntryDistanceDOWN && cacheVABVolumetric501[idx].MaxEntryDistanceDOWN == maxEntryDistanceDOWN && cacheVABVolumetric501[idx].MaxLowerBreakouts == maxLowerBreakouts && cacheVABVolumetric501[idx].OKisAfterBarsSinceResetDown == oKisAfterBarsSinceResetDown && cacheVABVolumetric501[idx].OKisBelovLowerThreshold == oKisBelovLowerThreshold && cacheVABVolumetric501[idx].OKisWithinMaxEntryDistanceDown == oKisWithinMaxEntryDistanceDown && cacheVABVolumetric501[idx].OKisLowerBreakoutCountExceeded == oKisLowerBreakoutCountExceeded && cacheVABVolumetric501[idx].EnableDistanceFromVWAPCondition == enableDistanceFromVWAPCondition && cacheVABVolumetric501[idx].MinDistanceFromVWAP == minDistanceFromVWAP && cacheVABVolumetric501[idx].MaxDistanceFromVWAP == maxDistanceFromVWAP && cacheVABVolumetric501[idx].EnableSTD3HighLowTracking == enableSTD3HighLowTracking && cacheVABVolumetric501[idx].FminADX == fminADX && cacheVABVolumetric501[idx].FmaxADX == fmaxADX && cacheVABVolumetric501[idx].OKisADX == oKisADX && cacheVABVolumetric501[idx].FminATR == fminATR && cacheVABVolumetric501[idx].FmaxATR == fmaxATR && cacheVABVolumetric501[idx].OKisATR == oKisATR && cacheVABVolumetric501[idx].FperiodVol == fperiodVol && cacheVABVolumetric501[idx].OKisVOL == oKisVOL && cacheVABVolumetric501[idx].UpArrowColor == upArrowColor && cacheVABVolumetric501[idx].DownArrowColor == downArrowColor && cacheVABVolumetric501[idx].POCColor == pOCColor && cacheVABVolumetric501[idx].POCConditionEnabled == pOCConditionEnabled && cacheVABVolumetric501[idx].POCTicksDistance == pOCTicksDistance && cacheVABVolumetric501[idx].BarDeltaUPEnabled == barDeltaUPEnabled && cacheVABVolumetric501[idx].MinBarDeltaUP == minBarDeltaUP && cacheVABVolumetric501[idx].MaxBarDeltaUP == maxBarDeltaUP && cacheVABVolumetric501[idx].DeltaPercentUPEnabled == deltaPercentUPEnabled && cacheVABVolumetric501[idx].MinDeltaPercentUP == minDeltaPercentUP && cacheVABVolumetric501[idx].MaxDeltaPercentUP == maxDeltaPercentUP && cacheVABVolumetric501[idx].DeltaChangeUPEnabled == deltaChangeUPEnabled && cacheVABVolumetric501[idx].MinDeltaChangeUP == minDeltaChangeUP && cacheVABVolumetric501[idx].MaxDeltaChangeUP == maxDeltaChangeUP && cacheVABVolumetric501[idx].TotalBuyingVolumeUPEnabled == totalBuyingVolumeUPEnabled && cacheVABVolumetric501[idx].MinTotalBuyingVolumeUP == minTotalBuyingVolumeUP && cacheVABVolumetric501[idx].MaxTotalBuyingVolumeUP == maxTotalBuyingVolumeUP && cacheVABVolumetric501[idx].TotalSellingVolumeUPEnabled == totalSellingVolumeUPEnabled && cacheVABVolumetric501[idx].MinTotalSellingVolumeUP == minTotalSellingVolumeUP && cacheVABVolumetric501[idx].MaxTotalSellingVolumeUP == maxTotalSellingVolumeUP && cacheVABVolumetric501[idx].TradesUPEnabled == tradesUPEnabled && cacheVABVolumetric501[idx].MinTradesUP == minTradesUP && cacheVABVolumetric501[idx].MaxTradesUP == maxTradesUP && cacheVABVolumetric501[idx].TotalVolumeUPEnabled == totalVolumeUPEnabled && cacheVABVolumetric501[idx].MinTotalVolumeUP == minTotalVolumeUP && cacheVABVolumetric501[idx].MaxTotalVolumeUP == maxTotalVolumeUP && cacheVABVolumetric501[idx].BarDeltaDOWNEnabled == barDeltaDOWNEnabled && cacheVABVolumetric501[idx].MinBarDeltaDOWN == minBarDeltaDOWN && cacheVABVolumetric501[idx].MaxBarDeltaDOWN == maxBarDeltaDOWN && cacheVABVolumetric501[idx].DeltaPercentDOWNEnabled == deltaPercentDOWNEnabled && cacheVABVolumetric501[idx].MinDeltaPercentDOWN == minDeltaPercentDOWN && cacheVABVolumetric501[idx].MaxDeltaPercentDOWN == maxDeltaPercentDOWN && cacheVABVolumetric501[idx].DeltaChangeDOWNEnabled == deltaChangeDOWNEnabled && cacheVABVolumetric501[idx].MinDeltaChangeDOWN == minDeltaChangeDOWN && cacheVABVolumetric501[idx].MaxDeltaChangeDOWN == maxDeltaChangeDOWN && cacheVABVolumetric501[idx].TotalBuyingVolumeDOWNEnabled == totalBuyingVolumeDOWNEnabled && cacheVABVolumetric501[idx].MinTotalBuyingVolumeDOWN == minTotalBuyingVolumeDOWN && cacheVABVolumetric501[idx].MaxTotalBuyingVolumeDOWN == maxTotalBuyingVolumeDOWN && cacheVABVolumetric501[idx].TotalSellingVolumeDOWNEnabled == totalSellingVolumeDOWNEnabled && cacheVABVolumetric501[idx].MinTotalSellingVolumeDOWN == minTotalSellingVolumeDOWN && cacheVABVolumetric501[idx].MaxTotalSellingVolumeDOWN == maxTotalSellingVolumeDOWN && cacheVABVolumetric501[idx].TradesDOWNEnabled == tradesDOWNEnabled && cacheVABVolumetric501[idx].MinTradesDOWN == minTradesDOWN && cacheVABVolumetric501[idx].MaxTradesDOWN == maxTradesDOWN && cacheVABVolumetric501[idx].TotalVolumeDOWNEnabled == totalVolumeDOWNEnabled && cacheVABVolumetric501[idx].MinTotalVolumeDOWN == minTotalVolumeDOWN && cacheVABVolumetric501[idx].MaxTotalVolumeDOWN == maxTotalVolumeDOWN && cacheVABVolumetric501[idx].EnableCumulativeDeltaConditionUP == enableCumulativeDeltaConditionUP && cacheVABVolumetric501[idx].CumulativeDeltaBarsRangeUP == cumulativeDeltaBarsRangeUP && cacheVABVolumetric501[idx].CumulativeDeltaJumpUP == cumulativeDeltaJumpUP && cacheVABVolumetric501[idx].EnableCumulativeDeltaConditionDOWN == enableCumulativeDeltaConditionDOWN && cacheVABVolumetric501[idx].CumulativeDeltaBarsRangeDOWN == cumulativeDeltaBarsRangeDOWN && cacheVABVolumetric501[idx].CumulativeDeltaJumpDOWN == cumulativeDeltaJumpDOWN && cacheVABVolumetric501[idx].EqualsInput(input))
						return cacheVABVolumetric501[idx];
			return CacheIndicator<ninpai.VABVolumetric501>(new ninpai.VABVolumetric501(){ ResetPeriod = resetPeriod, MinBarsForSignal = minBarsForSignal, EnableValueAreaCondition = enableValueAreaCondition, UseOpenForVACondition = useOpenForVACondition, MinimumTicks = minimumTicks, MaximumTicks = maximumTicks, ShowLimusineOpenCloseUP = showLimusineOpenCloseUP, ShowLimusineOpenCloseDOWN = showLimusineOpenCloseDOWN, ShowLimusineHighLowUP = showLimusineHighLowUP, ShowLimusineHighLowDOWN = showLimusineHighLowDOWN, MinEntryDistanceUP = minEntryDistanceUP, MaxEntryDistanceUP = maxEntryDistanceUP, MaxUpperBreakouts = maxUpperBreakouts, OKisAfterBarsSinceResetUP = oKisAfterBarsSinceResetUP, OKisAboveUpperThreshold = oKisAboveUpperThreshold, OKisWithinMaxEntryDistance = oKisWithinMaxEntryDistance, OKisUpperBreakoutCountExceeded = oKisUpperBreakoutCountExceeded, MinEntryDistanceDOWN = minEntryDistanceDOWN, MaxEntryDistanceDOWN = maxEntryDistanceDOWN, MaxLowerBreakouts = maxLowerBreakouts, OKisAfterBarsSinceResetDown = oKisAfterBarsSinceResetDown, OKisBelovLowerThreshold = oKisBelovLowerThreshold, OKisWithinMaxEntryDistanceDown = oKisWithinMaxEntryDistanceDown, OKisLowerBreakoutCountExceeded = oKisLowerBreakoutCountExceeded, EnableDistanceFromVWAPCondition = enableDistanceFromVWAPCondition, MinDistanceFromVWAP = minDistanceFromVWAP, MaxDistanceFromVWAP = maxDistanceFromVWAP, EnableSTD3HighLowTracking = enableSTD3HighLowTracking, FminADX = fminADX, FmaxADX = fmaxADX, OKisADX = oKisADX, FminATR = fminATR, FmaxATR = fmaxATR, OKisATR = oKisATR, FperiodVol = fperiodVol, OKisVOL = oKisVOL, UpArrowColor = upArrowColor, DownArrowColor = downArrowColor, POCColor = pOCColor, POCConditionEnabled = pOCConditionEnabled, POCTicksDistance = pOCTicksDistance, BarDeltaUPEnabled = barDeltaUPEnabled, MinBarDeltaUP = minBarDeltaUP, MaxBarDeltaUP = maxBarDeltaUP, DeltaPercentUPEnabled = deltaPercentUPEnabled, MinDeltaPercentUP = minDeltaPercentUP, MaxDeltaPercentUP = maxDeltaPercentUP, DeltaChangeUPEnabled = deltaChangeUPEnabled, MinDeltaChangeUP = minDeltaChangeUP, MaxDeltaChangeUP = maxDeltaChangeUP, TotalBuyingVolumeUPEnabled = totalBuyingVolumeUPEnabled, MinTotalBuyingVolumeUP = minTotalBuyingVolumeUP, MaxTotalBuyingVolumeUP = maxTotalBuyingVolumeUP, TotalSellingVolumeUPEnabled = totalSellingVolumeUPEnabled, MinTotalSellingVolumeUP = minTotalSellingVolumeUP, MaxTotalSellingVolumeUP = maxTotalSellingVolumeUP, TradesUPEnabled = tradesUPEnabled, MinTradesUP = minTradesUP, MaxTradesUP = maxTradesUP, TotalVolumeUPEnabled = totalVolumeUPEnabled, MinTotalVolumeUP = minTotalVolumeUP, MaxTotalVolumeUP = maxTotalVolumeUP, BarDeltaDOWNEnabled = barDeltaDOWNEnabled, MinBarDeltaDOWN = minBarDeltaDOWN, MaxBarDeltaDOWN = maxBarDeltaDOWN, DeltaPercentDOWNEnabled = deltaPercentDOWNEnabled, MinDeltaPercentDOWN = minDeltaPercentDOWN, MaxDeltaPercentDOWN = maxDeltaPercentDOWN, DeltaChangeDOWNEnabled = deltaChangeDOWNEnabled, MinDeltaChangeDOWN = minDeltaChangeDOWN, MaxDeltaChangeDOWN = maxDeltaChangeDOWN, TotalBuyingVolumeDOWNEnabled = totalBuyingVolumeDOWNEnabled, MinTotalBuyingVolumeDOWN = minTotalBuyingVolumeDOWN, MaxTotalBuyingVolumeDOWN = maxTotalBuyingVolumeDOWN, TotalSellingVolumeDOWNEnabled = totalSellingVolumeDOWNEnabled, MinTotalSellingVolumeDOWN = minTotalSellingVolumeDOWN, MaxTotalSellingVolumeDOWN = maxTotalSellingVolumeDOWN, TradesDOWNEnabled = tradesDOWNEnabled, MinTradesDOWN = minTradesDOWN, MaxTradesDOWN = maxTradesDOWN, TotalVolumeDOWNEnabled = totalVolumeDOWNEnabled, MinTotalVolumeDOWN = minTotalVolumeDOWN, MaxTotalVolumeDOWN = maxTotalVolumeDOWN, EnableCumulativeDeltaConditionUP = enableCumulativeDeltaConditionUP, CumulativeDeltaBarsRangeUP = cumulativeDeltaBarsRangeUP, CumulativeDeltaJumpUP = cumulativeDeltaJumpUP, EnableCumulativeDeltaConditionDOWN = enableCumulativeDeltaConditionDOWN, CumulativeDeltaBarsRangeDOWN = cumulativeDeltaBarsRangeDOWN, CumulativeDeltaJumpDOWN = cumulativeDeltaJumpDOWN }, input, ref cacheVABVolumetric501);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.VABVolumetric501 VABVolumetric501(int resetPeriod, int minBarsForSignal, bool enableValueAreaCondition, bool useOpenForVACondition, int minimumTicks, int maximumTicks, bool showLimusineOpenCloseUP, bool showLimusineOpenCloseDOWN, bool showLimusineHighLowUP, bool showLimusineHighLowDOWN, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, bool oKisAfterBarsSinceResetUP, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, bool oKisAfterBarsSinceResetDown, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded, bool enableDistanceFromVWAPCondition, int minDistanceFromVWAP, int maxDistanceFromVWAP, bool enableSTD3HighLowTracking, double fminADX, double fmaxADX, bool oKisADX, double fminATR, double fmaxATR, bool oKisATR, int fperiodVol, bool oKisVOL, Brush upArrowColor, Brush downArrowColor, Brush pOCColor, bool pOCConditionEnabled, int pOCTicksDistance, bool barDeltaUPEnabled, double minBarDeltaUP, double maxBarDeltaUP, bool deltaPercentUPEnabled, double minDeltaPercentUP, double maxDeltaPercentUP, bool deltaChangeUPEnabled, double minDeltaChangeUP, double maxDeltaChangeUP, bool totalBuyingVolumeUPEnabled, double minTotalBuyingVolumeUP, double maxTotalBuyingVolumeUP, bool totalSellingVolumeUPEnabled, double minTotalSellingVolumeUP, double maxTotalSellingVolumeUP, bool tradesUPEnabled, double minTradesUP, double maxTradesUP, bool totalVolumeUPEnabled, double minTotalVolumeUP, double maxTotalVolumeUP, bool barDeltaDOWNEnabled, double minBarDeltaDOWN, double maxBarDeltaDOWN, bool deltaPercentDOWNEnabled, double minDeltaPercentDOWN, double maxDeltaPercentDOWN, bool deltaChangeDOWNEnabled, double minDeltaChangeDOWN, double maxDeltaChangeDOWN, bool totalBuyingVolumeDOWNEnabled, double minTotalBuyingVolumeDOWN, double maxTotalBuyingVolumeDOWN, bool totalSellingVolumeDOWNEnabled, double minTotalSellingVolumeDOWN, double maxTotalSellingVolumeDOWN, bool tradesDOWNEnabled, double minTradesDOWN, double maxTradesDOWN, bool totalVolumeDOWNEnabled, double minTotalVolumeDOWN, double maxTotalVolumeDOWN, bool enableCumulativeDeltaConditionUP, int cumulativeDeltaBarsRangeUP, int cumulativeDeltaJumpUP, bool enableCumulativeDeltaConditionDOWN, int cumulativeDeltaBarsRangeDOWN, int cumulativeDeltaJumpDOWN)
		{
			return indicator.VABVolumetric501(Input, resetPeriod, minBarsForSignal, enableValueAreaCondition, useOpenForVACondition, minimumTicks, maximumTicks, showLimusineOpenCloseUP, showLimusineOpenCloseDOWN, showLimusineHighLowUP, showLimusineHighLowDOWN, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, oKisAfterBarsSinceResetUP, oKisAboveUpperThreshold, oKisWithinMaxEntryDistance, oKisUpperBreakoutCountExceeded, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts, oKisAfterBarsSinceResetDown, oKisBelovLowerThreshold, oKisWithinMaxEntryDistanceDown, oKisLowerBreakoutCountExceeded, enableDistanceFromVWAPCondition, minDistanceFromVWAP, maxDistanceFromVWAP, enableSTD3HighLowTracking, fminADX, fmaxADX, oKisADX, fminATR, fmaxATR, oKisATR, fperiodVol, oKisVOL, upArrowColor, downArrowColor, pOCColor, pOCConditionEnabled, pOCTicksDistance, barDeltaUPEnabled, minBarDeltaUP, maxBarDeltaUP, deltaPercentUPEnabled, minDeltaPercentUP, maxDeltaPercentUP, deltaChangeUPEnabled, minDeltaChangeUP, maxDeltaChangeUP, totalBuyingVolumeUPEnabled, minTotalBuyingVolumeUP, maxTotalBuyingVolumeUP, totalSellingVolumeUPEnabled, minTotalSellingVolumeUP, maxTotalSellingVolumeUP, tradesUPEnabled, minTradesUP, maxTradesUP, totalVolumeUPEnabled, minTotalVolumeUP, maxTotalVolumeUP, barDeltaDOWNEnabled, minBarDeltaDOWN, maxBarDeltaDOWN, deltaPercentDOWNEnabled, minDeltaPercentDOWN, maxDeltaPercentDOWN, deltaChangeDOWNEnabled, minDeltaChangeDOWN, maxDeltaChangeDOWN, totalBuyingVolumeDOWNEnabled, minTotalBuyingVolumeDOWN, maxTotalBuyingVolumeDOWN, totalSellingVolumeDOWNEnabled, minTotalSellingVolumeDOWN, maxTotalSellingVolumeDOWN, tradesDOWNEnabled, minTradesDOWN, maxTradesDOWN, totalVolumeDOWNEnabled, minTotalVolumeDOWN, maxTotalVolumeDOWN, enableCumulativeDeltaConditionUP, cumulativeDeltaBarsRangeUP, cumulativeDeltaJumpUP, enableCumulativeDeltaConditionDOWN, cumulativeDeltaBarsRangeDOWN, cumulativeDeltaJumpDOWN);
		}

		public Indicators.ninpai.VABVolumetric501 VABVolumetric501(ISeries<double> input , int resetPeriod, int minBarsForSignal, bool enableValueAreaCondition, bool useOpenForVACondition, int minimumTicks, int maximumTicks, bool showLimusineOpenCloseUP, bool showLimusineOpenCloseDOWN, bool showLimusineHighLowUP, bool showLimusineHighLowDOWN, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, bool oKisAfterBarsSinceResetUP, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, bool oKisAfterBarsSinceResetDown, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded, bool enableDistanceFromVWAPCondition, int minDistanceFromVWAP, int maxDistanceFromVWAP, bool enableSTD3HighLowTracking, double fminADX, double fmaxADX, bool oKisADX, double fminATR, double fmaxATR, bool oKisATR, int fperiodVol, bool oKisVOL, Brush upArrowColor, Brush downArrowColor, Brush pOCColor, bool pOCConditionEnabled, int pOCTicksDistance, bool barDeltaUPEnabled, double minBarDeltaUP, double maxBarDeltaUP, bool deltaPercentUPEnabled, double minDeltaPercentUP, double maxDeltaPercentUP, bool deltaChangeUPEnabled, double minDeltaChangeUP, double maxDeltaChangeUP, bool totalBuyingVolumeUPEnabled, double minTotalBuyingVolumeUP, double maxTotalBuyingVolumeUP, bool totalSellingVolumeUPEnabled, double minTotalSellingVolumeUP, double maxTotalSellingVolumeUP, bool tradesUPEnabled, double minTradesUP, double maxTradesUP, bool totalVolumeUPEnabled, double minTotalVolumeUP, double maxTotalVolumeUP, bool barDeltaDOWNEnabled, double minBarDeltaDOWN, double maxBarDeltaDOWN, bool deltaPercentDOWNEnabled, double minDeltaPercentDOWN, double maxDeltaPercentDOWN, bool deltaChangeDOWNEnabled, double minDeltaChangeDOWN, double maxDeltaChangeDOWN, bool totalBuyingVolumeDOWNEnabled, double minTotalBuyingVolumeDOWN, double maxTotalBuyingVolumeDOWN, bool totalSellingVolumeDOWNEnabled, double minTotalSellingVolumeDOWN, double maxTotalSellingVolumeDOWN, bool tradesDOWNEnabled, double minTradesDOWN, double maxTradesDOWN, bool totalVolumeDOWNEnabled, double minTotalVolumeDOWN, double maxTotalVolumeDOWN, bool enableCumulativeDeltaConditionUP, int cumulativeDeltaBarsRangeUP, int cumulativeDeltaJumpUP, bool enableCumulativeDeltaConditionDOWN, int cumulativeDeltaBarsRangeDOWN, int cumulativeDeltaJumpDOWN)
		{
			return indicator.VABVolumetric501(input, resetPeriod, minBarsForSignal, enableValueAreaCondition, useOpenForVACondition, minimumTicks, maximumTicks, showLimusineOpenCloseUP, showLimusineOpenCloseDOWN, showLimusineHighLowUP, showLimusineHighLowDOWN, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, oKisAfterBarsSinceResetUP, oKisAboveUpperThreshold, oKisWithinMaxEntryDistance, oKisUpperBreakoutCountExceeded, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts, oKisAfterBarsSinceResetDown, oKisBelovLowerThreshold, oKisWithinMaxEntryDistanceDown, oKisLowerBreakoutCountExceeded, enableDistanceFromVWAPCondition, minDistanceFromVWAP, maxDistanceFromVWAP, enableSTD3HighLowTracking, fminADX, fmaxADX, oKisADX, fminATR, fmaxATR, oKisATR, fperiodVol, oKisVOL, upArrowColor, downArrowColor, pOCColor, pOCConditionEnabled, pOCTicksDistance, barDeltaUPEnabled, minBarDeltaUP, maxBarDeltaUP, deltaPercentUPEnabled, minDeltaPercentUP, maxDeltaPercentUP, deltaChangeUPEnabled, minDeltaChangeUP, maxDeltaChangeUP, totalBuyingVolumeUPEnabled, minTotalBuyingVolumeUP, maxTotalBuyingVolumeUP, totalSellingVolumeUPEnabled, minTotalSellingVolumeUP, maxTotalSellingVolumeUP, tradesUPEnabled, minTradesUP, maxTradesUP, totalVolumeUPEnabled, minTotalVolumeUP, maxTotalVolumeUP, barDeltaDOWNEnabled, minBarDeltaDOWN, maxBarDeltaDOWN, deltaPercentDOWNEnabled, minDeltaPercentDOWN, maxDeltaPercentDOWN, deltaChangeDOWNEnabled, minDeltaChangeDOWN, maxDeltaChangeDOWN, totalBuyingVolumeDOWNEnabled, minTotalBuyingVolumeDOWN, maxTotalBuyingVolumeDOWN, totalSellingVolumeDOWNEnabled, minTotalSellingVolumeDOWN, maxTotalSellingVolumeDOWN, tradesDOWNEnabled, minTradesDOWN, maxTradesDOWN, totalVolumeDOWNEnabled, minTotalVolumeDOWN, maxTotalVolumeDOWN, enableCumulativeDeltaConditionUP, cumulativeDeltaBarsRangeUP, cumulativeDeltaJumpUP, enableCumulativeDeltaConditionDOWN, cumulativeDeltaBarsRangeDOWN, cumulativeDeltaJumpDOWN);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.VABVolumetric501 VABVolumetric501(int resetPeriod, int minBarsForSignal, bool enableValueAreaCondition, bool useOpenForVACondition, int minimumTicks, int maximumTicks, bool showLimusineOpenCloseUP, bool showLimusineOpenCloseDOWN, bool showLimusineHighLowUP, bool showLimusineHighLowDOWN, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, bool oKisAfterBarsSinceResetUP, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, bool oKisAfterBarsSinceResetDown, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded, bool enableDistanceFromVWAPCondition, int minDistanceFromVWAP, int maxDistanceFromVWAP, bool enableSTD3HighLowTracking, double fminADX, double fmaxADX, bool oKisADX, double fminATR, double fmaxATR, bool oKisATR, int fperiodVol, bool oKisVOL, Brush upArrowColor, Brush downArrowColor, Brush pOCColor, bool pOCConditionEnabled, int pOCTicksDistance, bool barDeltaUPEnabled, double minBarDeltaUP, double maxBarDeltaUP, bool deltaPercentUPEnabled, double minDeltaPercentUP, double maxDeltaPercentUP, bool deltaChangeUPEnabled, double minDeltaChangeUP, double maxDeltaChangeUP, bool totalBuyingVolumeUPEnabled, double minTotalBuyingVolumeUP, double maxTotalBuyingVolumeUP, bool totalSellingVolumeUPEnabled, double minTotalSellingVolumeUP, double maxTotalSellingVolumeUP, bool tradesUPEnabled, double minTradesUP, double maxTradesUP, bool totalVolumeUPEnabled, double minTotalVolumeUP, double maxTotalVolumeUP, bool barDeltaDOWNEnabled, double minBarDeltaDOWN, double maxBarDeltaDOWN, bool deltaPercentDOWNEnabled, double minDeltaPercentDOWN, double maxDeltaPercentDOWN, bool deltaChangeDOWNEnabled, double minDeltaChangeDOWN, double maxDeltaChangeDOWN, bool totalBuyingVolumeDOWNEnabled, double minTotalBuyingVolumeDOWN, double maxTotalBuyingVolumeDOWN, bool totalSellingVolumeDOWNEnabled, double minTotalSellingVolumeDOWN, double maxTotalSellingVolumeDOWN, bool tradesDOWNEnabled, double minTradesDOWN, double maxTradesDOWN, bool totalVolumeDOWNEnabled, double minTotalVolumeDOWN, double maxTotalVolumeDOWN, bool enableCumulativeDeltaConditionUP, int cumulativeDeltaBarsRangeUP, int cumulativeDeltaJumpUP, bool enableCumulativeDeltaConditionDOWN, int cumulativeDeltaBarsRangeDOWN, int cumulativeDeltaJumpDOWN)
		{
			return indicator.VABVolumetric501(Input, resetPeriod, minBarsForSignal, enableValueAreaCondition, useOpenForVACondition, minimumTicks, maximumTicks, showLimusineOpenCloseUP, showLimusineOpenCloseDOWN, showLimusineHighLowUP, showLimusineHighLowDOWN, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, oKisAfterBarsSinceResetUP, oKisAboveUpperThreshold, oKisWithinMaxEntryDistance, oKisUpperBreakoutCountExceeded, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts, oKisAfterBarsSinceResetDown, oKisBelovLowerThreshold, oKisWithinMaxEntryDistanceDown, oKisLowerBreakoutCountExceeded, enableDistanceFromVWAPCondition, minDistanceFromVWAP, maxDistanceFromVWAP, enableSTD3HighLowTracking, fminADX, fmaxADX, oKisADX, fminATR, fmaxATR, oKisATR, fperiodVol, oKisVOL, upArrowColor, downArrowColor, pOCColor, pOCConditionEnabled, pOCTicksDistance, barDeltaUPEnabled, minBarDeltaUP, maxBarDeltaUP, deltaPercentUPEnabled, minDeltaPercentUP, maxDeltaPercentUP, deltaChangeUPEnabled, minDeltaChangeUP, maxDeltaChangeUP, totalBuyingVolumeUPEnabled, minTotalBuyingVolumeUP, maxTotalBuyingVolumeUP, totalSellingVolumeUPEnabled, minTotalSellingVolumeUP, maxTotalSellingVolumeUP, tradesUPEnabled, minTradesUP, maxTradesUP, totalVolumeUPEnabled, minTotalVolumeUP, maxTotalVolumeUP, barDeltaDOWNEnabled, minBarDeltaDOWN, maxBarDeltaDOWN, deltaPercentDOWNEnabled, minDeltaPercentDOWN, maxDeltaPercentDOWN, deltaChangeDOWNEnabled, minDeltaChangeDOWN, maxDeltaChangeDOWN, totalBuyingVolumeDOWNEnabled, minTotalBuyingVolumeDOWN, maxTotalBuyingVolumeDOWN, totalSellingVolumeDOWNEnabled, minTotalSellingVolumeDOWN, maxTotalSellingVolumeDOWN, tradesDOWNEnabled, minTradesDOWN, maxTradesDOWN, totalVolumeDOWNEnabled, minTotalVolumeDOWN, maxTotalVolumeDOWN, enableCumulativeDeltaConditionUP, cumulativeDeltaBarsRangeUP, cumulativeDeltaJumpUP, enableCumulativeDeltaConditionDOWN, cumulativeDeltaBarsRangeDOWN, cumulativeDeltaJumpDOWN);
		}

		public Indicators.ninpai.VABVolumetric501 VABVolumetric501(ISeries<double> input , int resetPeriod, int minBarsForSignal, bool enableValueAreaCondition, bool useOpenForVACondition, int minimumTicks, int maximumTicks, bool showLimusineOpenCloseUP, bool showLimusineOpenCloseDOWN, bool showLimusineHighLowUP, bool showLimusineHighLowDOWN, int minEntryDistanceUP, int maxEntryDistanceUP, int maxUpperBreakouts, bool oKisAfterBarsSinceResetUP, bool oKisAboveUpperThreshold, bool oKisWithinMaxEntryDistance, bool oKisUpperBreakoutCountExceeded, int minEntryDistanceDOWN, int maxEntryDistanceDOWN, int maxLowerBreakouts, bool oKisAfterBarsSinceResetDown, bool oKisBelovLowerThreshold, bool oKisWithinMaxEntryDistanceDown, bool oKisLowerBreakoutCountExceeded, bool enableDistanceFromVWAPCondition, int minDistanceFromVWAP, int maxDistanceFromVWAP, bool enableSTD3HighLowTracking, double fminADX, double fmaxADX, bool oKisADX, double fminATR, double fmaxATR, bool oKisATR, int fperiodVol, bool oKisVOL, Brush upArrowColor, Brush downArrowColor, Brush pOCColor, bool pOCConditionEnabled, int pOCTicksDistance, bool barDeltaUPEnabled, double minBarDeltaUP, double maxBarDeltaUP, bool deltaPercentUPEnabled, double minDeltaPercentUP, double maxDeltaPercentUP, bool deltaChangeUPEnabled, double minDeltaChangeUP, double maxDeltaChangeUP, bool totalBuyingVolumeUPEnabled, double minTotalBuyingVolumeUP, double maxTotalBuyingVolumeUP, bool totalSellingVolumeUPEnabled, double minTotalSellingVolumeUP, double maxTotalSellingVolumeUP, bool tradesUPEnabled, double minTradesUP, double maxTradesUP, bool totalVolumeUPEnabled, double minTotalVolumeUP, double maxTotalVolumeUP, bool barDeltaDOWNEnabled, double minBarDeltaDOWN, double maxBarDeltaDOWN, bool deltaPercentDOWNEnabled, double minDeltaPercentDOWN, double maxDeltaPercentDOWN, bool deltaChangeDOWNEnabled, double minDeltaChangeDOWN, double maxDeltaChangeDOWN, bool totalBuyingVolumeDOWNEnabled, double minTotalBuyingVolumeDOWN, double maxTotalBuyingVolumeDOWN, bool totalSellingVolumeDOWNEnabled, double minTotalSellingVolumeDOWN, double maxTotalSellingVolumeDOWN, bool tradesDOWNEnabled, double minTradesDOWN, double maxTradesDOWN, bool totalVolumeDOWNEnabled, double minTotalVolumeDOWN, double maxTotalVolumeDOWN, bool enableCumulativeDeltaConditionUP, int cumulativeDeltaBarsRangeUP, int cumulativeDeltaJumpUP, bool enableCumulativeDeltaConditionDOWN, int cumulativeDeltaBarsRangeDOWN, int cumulativeDeltaJumpDOWN)
		{
			return indicator.VABVolumetric501(input, resetPeriod, minBarsForSignal, enableValueAreaCondition, useOpenForVACondition, minimumTicks, maximumTicks, showLimusineOpenCloseUP, showLimusineOpenCloseDOWN, showLimusineHighLowUP, showLimusineHighLowDOWN, minEntryDistanceUP, maxEntryDistanceUP, maxUpperBreakouts, oKisAfterBarsSinceResetUP, oKisAboveUpperThreshold, oKisWithinMaxEntryDistance, oKisUpperBreakoutCountExceeded, minEntryDistanceDOWN, maxEntryDistanceDOWN, maxLowerBreakouts, oKisAfterBarsSinceResetDown, oKisBelovLowerThreshold, oKisWithinMaxEntryDistanceDown, oKisLowerBreakoutCountExceeded, enableDistanceFromVWAPCondition, minDistanceFromVWAP, maxDistanceFromVWAP, enableSTD3HighLowTracking, fminADX, fmaxADX, oKisADX, fminATR, fmaxATR, oKisATR, fperiodVol, oKisVOL, upArrowColor, downArrowColor, pOCColor, pOCConditionEnabled, pOCTicksDistance, barDeltaUPEnabled, minBarDeltaUP, maxBarDeltaUP, deltaPercentUPEnabled, minDeltaPercentUP, maxDeltaPercentUP, deltaChangeUPEnabled, minDeltaChangeUP, maxDeltaChangeUP, totalBuyingVolumeUPEnabled, minTotalBuyingVolumeUP, maxTotalBuyingVolumeUP, totalSellingVolumeUPEnabled, minTotalSellingVolumeUP, maxTotalSellingVolumeUP, tradesUPEnabled, minTradesUP, maxTradesUP, totalVolumeUPEnabled, minTotalVolumeUP, maxTotalVolumeUP, barDeltaDOWNEnabled, minBarDeltaDOWN, maxBarDeltaDOWN, deltaPercentDOWNEnabled, minDeltaPercentDOWN, maxDeltaPercentDOWN, deltaChangeDOWNEnabled, minDeltaChangeDOWN, maxDeltaChangeDOWN, totalBuyingVolumeDOWNEnabled, minTotalBuyingVolumeDOWN, maxTotalBuyingVolumeDOWN, totalSellingVolumeDOWNEnabled, minTotalSellingVolumeDOWN, maxTotalSellingVolumeDOWN, tradesDOWNEnabled, minTradesDOWN, maxTradesDOWN, totalVolumeDOWNEnabled, minTotalVolumeDOWN, maxTotalVolumeDOWN, enableCumulativeDeltaConditionUP, cumulativeDeltaBarsRangeUP, cumulativeDeltaJumpUP, enableCumulativeDeltaConditionDOWN, cumulativeDeltaBarsRangeDOWN, cumulativeDeltaJumpDOWN);
		}
	}
}

#endregion