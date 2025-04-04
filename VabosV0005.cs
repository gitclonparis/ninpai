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
    public class VabosV0005 : Indicator
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
		private double volumeMaxS;
		
        // Variables for tracking STD3 highs and lows
        private double highestSTD3Upper;
        private double lowestSTD3Lower;
        private bool isFirstBarSinceReset;
		
        private double previousSessionHighStd1Upper;
        private double previousSessionLowStd1Lower;
        private DateTime currentSessionStartTime;
        private double highestStd1Upper;
        private double lowestStd1Lower;

        private int figVA;
        private bool figVAPointsDrawn;
		
		private double previousSessionVAUpperLevel = double.MinValue;
		private double previousSessionVALowerLevel = double.MaxValue;
		
		// Ajoutez ces variables au début de la classe VvabSkel03
		private double ibHigh = double.MinValue;
		private double ibLow = double.MaxValue;
		private bool ibPeriod = true;
		private SessionIterator sessionIterator;
		private DateTime currentDate = DateTime.MinValue;
		
		public enum DynamicAreaLevel
		{
			STD05,  // Standard Deviation 0.5
			STD1,   // Standard Deviation 1
			STD2,   // Standard Deviation 2
			STD3    // Standard Deviation 3
		}
		
		// Ajoutez ces variables privées
		private double previousSessionDynamicUpperLevel = double.MinValue;
		private double previousSessionDynamicLowerLevel = double.MaxValue;
		private bool dynamicAreaPointsDrawn;
		private int dynamicAreaDrawDelayMinutes;
		
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Indicateur BVA-Limusine combiné";
                Name = "VabosV0005";
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
                ResetPeriod = 240;
				figVA = ResetPeriod - 1;
                figVAPointsDrawn = false;
                MinBarsForSignal = 5;
                MaxBarsForSignal = 80;
                MinEntryDistanceUP = 3;
                MaxEntryDistanceUP = 40;
                MaxUpperBreakouts = 3;
                MinEntryDistanceDOWN = 3;
                MaxEntryDistanceDOWN = 40;
                MaxLowerBreakouts = 3;
				BlockSignalsInPreviousValueArea = false;
				ValueAreaOffsetTicks = 0;
				UsePrevBarInVA = false;
                FperiodVol = 9;
				UseVolumeS = false;
				UseVolumeIncrease = false;
				VolumeBarsToCompare = 1;
				EnableVolumeAnalysisPeriod = false;

                // Paramètres Limusine
				ActiveBuy = true;
				ActiveSell = true;
                MinimumTicks = 10;
                MaximumTicks = 30;
                ShowLimusineOpenCloseUP = true;
                ShowLimusineOpenCloseDOWN = true;
                ShowLimusineHighLowUP = false;
                ShowLimusineHighLowDOWN = false;
                
                EnableDistanceFromVWAPCondition = false;
                MinDistanceFromVWAP = 10;
                MaxDistanceFromVWAP = 50;
                
                EnableSTD3HighLowTracking = false;
                EnablePreviousSessionRangeBreakout = false;
				EnableSTD1RangeCheck = false;
				MinSTD1Range = 20;
				MaxSTD1Range = 100;

                AddPlot(Brushes.Orange, "VWAP");
                AddPlot(Brushes.Purple, "StdDev0.5Upper");
                AddPlot(Brushes.Purple, "StdDev0.5Lower");
                AddPlot(Brushes.Red, "StdDev1Upper");
                AddPlot(Brushes.Red, "StdDev1Lower");
                AddPlot(Brushes.Green, "StdDev2Upper");
                AddPlot(Brushes.Green, "StdDev2Lower");
                AddPlot(Brushes.Blue, "StdDev3Upper");
                AddPlot(Brushes.Blue, "StdDev3Lower");

                SignalTimingMode = SignalTimeMode.Bars;
                MinMinutesForSignal = 5;
                MaxMinutesForSignal = 30;
                SelectedValueArea = ValueAreaLevel.STD1;
                useOpenForVAConditionUP = false;
                useOpenForVAConditionDown = false;
                useLowForVAConditionUP = false;
                useHighForVAConditionDown = false;
				SelectedEntryLevelUp = EntryLevelChoice.STD1;    // Au lieu de STD05
				SelectedEntryLevelDown = EntryLevelChoice.STD1;  // Au lieu de STD05
				
				// Ajoutez les valeurs par défaut pour Initial Balance
				EnableIBLogic = false;
				IBStartTime = DateTime.Parse("15:30", System.Globalization.CultureInfo.InvariantCulture);
				IBEndTime = DateTime.Parse("16:00", System.Globalization.CultureInfo.InvariantCulture);
				IBOffsetTicks = 0;
				
				// Paramètres par défaut pour les breakout checks
				EnableUpBreakoutCheck = false;
				UpBreakoutBars = 2;
				UpBreakoutOffsetTicks = 1; // Doit être >= 1
				UseClose0SupMaxVA = false;
				MaxOffsetClose0VAUP = 5;
				UseClose0infSTD2 = false;
				STD2OffsetTicksUP = 0;
				SkipUseClose0infSTD2 = false;
				SkipFiltreClose0infSTD2 = 50;
				
				EnableDownBreakoutCheck = false;
				DownBreakoutBars = 2;
				DownBreakoutOffsetTicks = 1; // Doit être >= 1
				UseClose0InfMaxVA = false;
				MaxOffsetClose0VADOWN = 5;
				UseClose0supSTD2 = false;
				STD2OffsetTicksDOWN = 0;
				SkipUseClose0supSTD2 = false;
				SkipFiltreClose0supSTD2 = 50;
				
				// Dots
				SelectedDotPlotMode = DotPlotMode.STD1; // Valeur par défaut
				SelectedBarSizeType = BarSizeType.CloseOpen;
				RedDotMultiplier = 1.0;
				BlueDotMultiplier = 1.0;
				STD1RedDotMultiplier = 0.5;
				STD1BlueDotMultiplier = 1.5;
				
				SelectedDynamicArea = DynamicAreaLevel.STD1;
				BlockSignalsInPreviousDynamicArea = false;
				DynamicAreaOffsetTicks = 0;
				dynamicAreaPointsDrawn = false;
				DynamicAreaDrawDelayMinutes = 59;
				
				// Prior VA Vwap
				UpperOffsetTicks = 5;
				LowerOffsetTicks = 5;
				UsePriorSvaUP = false;
				UsePriorSvaDown = false;
				BlockInPriorSVA = false;
				
				// Slope Filter defaults
				SlopeStartBars = 5;
				SlopeEndBars = 0;
				MinVwapSessionSlopeUp = 0.30;
				MinVwapResetSlopeUp = 0.30;
				MinStdUpperSlopeUp = 0.30;
				MinStdLowerSlopeUp = 0.30;
				
				MaxVwapSessionSlopeDown = -0.30;
				MaxVwapResetSlopeDown = -0.30;
				MaxStdUpperSlopeDown = -0.30;
				MaxStdLowerSlopeDown = -0.30;
				
				// 0.7_Barre0_Property
				UseOpen0inVA05 = false;
				UserBarre0FiltreWik = false;
				FilterWickBarre0 = 5;
				UseBarre0FiltreVolume = false;
				MinVolBarre0 = 10000;
				MaxVolBarre0 = 100000;
				UseCrossVwapBarre0 = false;
				UseRejVwapBarre0 = false;
				UseCrossStd1Barre0 = false;
				Std1CrossOffsetTicksBarre0 = 0;
				UseRejStd1Barre0 = false;
				Std1RejOffsetTicksBarre0 = 0;
				UseRejStd05Barre0 = false;
				Std05RejOffsetTicksBarre0 = 0;
				// 0.8_Barre1_Property
				UseOpen1inVA05 = false;
				UseOpen1inVAstd1 = false;
				UserBarre1FiltreWik = false;
				FilterWickBarre1 = 5;
				UseBarre1FiltreVolume = false;
				MinVolBarre1 = 10000;
				MaxVolBarre1 = 100000;
				UseBarre1FilterSizeOC = false;
				FilterMinSizeBarre1OC = 0;
				FilterMaxSizeBarre1OC = 200;
				UseCrossVwapBarre1 = false;
				UseRejVwapBarre1 = false;
				UseCrossStd1Barre1 = false;
				Std1CrossOffsetTicks = 0;
				UseRejStd1Barre1 = false;
				Std1RejOffsetTicks = 0;
				UseRejStd05Barre1 = false;
				Std05RejOffsetTicksBarre1 = 0;
				
				UseVague1UP = false;
				UseVague1DOWN = false;
				FilterVagueTicks = 50;
				
				UseThreeBarBreakoutUp = false;
				UseThreeBarBreakoutDown = false;
				UseInsideB0B1 = false;
				
				// Setup V3B
				UseSetupV3B = false;
				UseSetupV3BOC = false;
				UseSetupV3BHL = false;
				IgnoreCondition1V3BOC = false;
				IgnoreCondition1V3BHL = false;
				
				// Setup D3BHLup
				UseSetupD3BHLup = false;
				UseSetupD3BHLup1 = false;
				UseSetupD3BHLup2 = false;
				
				// Setup DOWN
				UseSetupV3BDown = false;
				UseSetupV3BOCDown = false;
				UseSetupV3BHLDown = false;
				UseSetupD3BHLdown = false;
				UseSetupD3BHLdown1 = false;
				UseSetupD3BHLdown2 = false;
				
				// Setup ALPHAUP
				UseSetupALPHAUP = false;
				AlphaUseV3BOC = false;
				AlphaUseV3BHL = false;
				AlphaUseD3BHLup1 = false;
				AlphaUseD3BHLup2 = false;
				AlphaUseThreeBarBreakout = false;
				// ALPHADOWN
				UseSetupALPHADOWN = false;
				AlphaUseV3BOCDown = false;
				AlphaUseV3BHLDown = false;
				AlphaUseD3BHLdown1 = false;
				AlphaUseD3BHLdown2 = false;
				AlphaUseThreeBarBreakoutDown = false;
				
				UseSetupU4Bup = false;
				UseSetupU4BOCup = false;
				UseSetupU4BHLup = false;
				UseSetupU4BDown = false;
				UseSetupU4BOCDown = false;
				UseSetupU4BHLDown = false;
				
            }
            else if (State == State.Configure)
            {
                ResetValues(DateTime.MinValue);
				newSession = true;
            }
            else if (State == State.DataLoaded)
            {
                VOL1 = VOL(Close);
                VOLMA1 = VOLMA(Close, Convert.ToInt32(FperiodVol));
				sessionIterator = new SessionIterator(Bars);
				
				vwap = OrderFlowVWAP(VWAPResolution.Standard, Bars.TradingHours, VWAPStandardDeviations.Three, 1, 2, 3);
				
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBars[0] < 20)
                return;
			if (BarsInProgress != 0) return;
			
			UpdatePriorSessionBands();
			if (!newSession) return;
			
			figVA = ResetPeriod - 1;
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

            // Assigner les valeurs aux plots
            Values[0][0] = vwap;
            Values[1][0] = vwap + 0.5 * stdDev;
            Values[2][0] = vwap - 0.5 * stdDev;
            Values[3][0] = vwap + stdDev;
            Values[4][0] = vwap - stdDev;
            Values[5][0] = vwap + 2 * stdDev;
            Values[6][0] = vwap - 2 * stdDev;
            Values[7][0] = vwap + 3 * stdDev;
            Values[8][0] = vwap - 3 * stdDev;
			
			// if (Volume[0] > volumeMaxS)
			// {
				// volumeMaxS = Volume[0];
			// }
            
            if (EnablePreviousSessionRangeBreakout)
            {
                highestStd1Upper = Math.Max(highestStd1Upper, Values[3][0]);
                lowestStd1Lower = Math.Min(lowestStd1Lower, Values[4][0]);
            }

            if (EnableSTD3HighLowTracking)
            {
                if (isFirstBarSinceReset)
                {
                    highestSTD3Upper = Values[7][0];
                    lowestSTD3Lower = Values[8][0];
                    isFirstBarSinceReset = false;
                }
                else
                {
                    highestSTD3Upper = Math.Max(highestSTD3Upper, Values[7][0]);
                    lowestSTD3Lower = Math.Min(lowestSTD3Lower, Values[8][0]);
                }
            }
            
            barsSinceReset++;

            TimeSpan timeSinceReset = Time[0] - lastResetTime;
            if (timeSinceReset.TotalMinutes >= figVA && !figVAPointsDrawn)
            {
                double upperLevel = 0;
                double lowerLevel = 0;
                
                // Sélectionner les niveaux en fonction de ValueAreaLevel
                switch (SelectedValueArea)
                {
                    case ValueAreaLevel.STD05:
                        upperLevel = Values[1][0]; // StdDev0.5 Upper
                        lowerLevel = Values[2][0]; // StdDev0.5 Lower
                        break;
                    case ValueAreaLevel.STD1:
                        upperLevel = Values[3][0]; // StdDev1 Upper
                        lowerLevel = Values[4][0]; // StdDev1 Lower
                        break;
                    case ValueAreaLevel.STD2:
                        upperLevel = Values[5][0]; // StdDev2 Upper
                        lowerLevel = Values[6][0]; // StdDev2 Lower
                        break;
                    case ValueAreaLevel.STD3:
                        upperLevel = Values[7][0]; // StdDev3 Upper
                        lowerLevel = Values[8][0]; // StdDev3 Lower
                        break;
                }
				// Appliquer l'offset aux niveaux
				upperLevel += ValueAreaOffsetTicks * TickSize;
				lowerLevel -= ValueAreaOffsetTicks * TickSize;
                // Dessiner les points
                Draw.Dot(this, "FigVAUpper" + CurrentBar, true, 0, upperLevel, Brushes.Yellow);
                Draw.Dot(this, "FigVALower" + CurrentBar, true, 0, lowerLevel, Brushes.Yellow);
                previousSessionVAUpperLevel = upperLevel;
				previousSessionVALowerLevel = lowerLevel;
                figVAPointsDrawn = true;
            }
			
			//
			// if (timeSinceReset.TotalMinutes >= figVA && !dynamicAreaPointsDrawn)
			if (timeSinceReset.TotalMinutes >= DynamicAreaDrawDelayMinutes && !dynamicAreaPointsDrawn)
			{
				double dynamicUpperLevel = 0;
				double dynamicLowerLevel = 0;
				
				switch (SelectedDynamicArea)
				{
					case DynamicAreaLevel.STD05:
						dynamicUpperLevel = Values[1][0];
						dynamicLowerLevel = Values[2][0];
						break;
					case DynamicAreaLevel.STD1:
						dynamicUpperLevel = Values[3][0];
						dynamicLowerLevel = Values[4][0];
						break;
					case DynamicAreaLevel.STD2:
						dynamicUpperLevel = Values[5][0];
						dynamicLowerLevel = Values[6][0];
						break;
					case DynamicAreaLevel.STD3:
						dynamicUpperLevel = Values[7][0];
						dynamicLowerLevel = Values[8][0];
						break;
				}
			
				dynamicUpperLevel += DynamicAreaOffsetTicks * TickSize;
				dynamicLowerLevel -= DynamicAreaOffsetTicks * TickSize;
			
				Draw.Dot(this, "DynamicAreaUpper" + CurrentBar, true, 0, dynamicUpperLevel, Brushes.Orange);
				Draw.Dot(this, "DynamicAreaLower" + CurrentBar, true, 0, dynamicLowerLevel, Brushes.Orange);
				previousSessionDynamicUpperLevel = dynamicUpperLevel;
				previousSessionDynamicLowerLevel = dynamicLowerLevel;
				dynamicAreaPointsDrawn = true;
			}
			// ######################################################## //
			
			bool isWithinVolumeAnalysisPeriod;
			if (SignalTimingMode == SignalTimeMode.Minutes)
			{
				isWithinVolumeAnalysisPeriod = timeSinceReset.TotalMinutes >= MinMinutesForSignal && 
										timeSinceReset.TotalMinutes <= MaxMinutesForSignal;
			}
			else
			{
				isWithinVolumeAnalysisPeriod = barsSinceReset >= MinBarsForSignal && 
										barsSinceReset <= MaxBarsForSignal;
			}
			
			if (!EnableVolumeAnalysisPeriod || isWithinVolumeAnalysisPeriod)
			{
				if (Volume[0] > volumeMaxS)
				{
					volumeMaxS = Volume[0];
				}
			}
            // ######################################################## //		
			UpdateWaveTrackingUp();
			UpdateWaveTrackingDown();
			
            // Réinitialiser figVAPointsDrawn lors d'un reset
            if (shouldReset)
            {
                figVAPointsDrawn = false;
            }

            if (ActiveBuy && ShouldDrawUpArrow())
            {
                Draw.ArrowUp(this, "UpArrow" + CurrentBar, true, 0, Low[0] - 2 * TickSize, Brushes.Green);
                upperBreakoutCount++;
				var (redDotPrice, blueDotPrice) = CalculateDotLevels(true, Close[0]);
				Draw.Dot(this, "RedDotUp" + CurrentBar, true, 0, redDotPrice, Brushes.Red);
				Draw.Dot(this, "BlueDotUp" + CurrentBar, true, 0, blueDotPrice, Brushes.Blue);
				Draw.Dot(this, "WhiteDotUp" + CurrentBar, true, 0, Close[0], Brushes.White);
            }
            else if (ActiveSell && ShouldDrawDownArrow())
            {
                Draw.ArrowDown(this, "DownArrow" + CurrentBar, true, 0, High[0] + 2 * TickSize, Brushes.Red);
                lowerBreakoutCount++;
				var (redDotPrice, blueDotPrice) = CalculateDotLevels(false, Close[0]);
				Draw.Dot(this, "RedDotDown" + CurrentBar, true, 0, redDotPrice, Brushes.Red);
				Draw.Dot(this, "BlueDotDown" + CurrentBar, true, 0, blueDotPrice, Brushes.Blue);
				Draw.Dot(this, "WhiteDotDown" + CurrentBar, true, 0, Close[0], Brushes.White);
            }
        }
		// ############################################################################################################### //
		// ############################################################################################################### //
		//
		 private double GetSelectedLevel(EntryLevelChoice choice, bool isUpper)
		{
			switch (choice)
			{
				case EntryLevelChoice.STD05:
					return isUpper ? Values[1][0] : Values[2][0];
				case EntryLevelChoice.STD1:
					return isUpper ? Values[3][0] : Values[4][0];
				case EntryLevelChoice.STD2:
					return isUpper ? Values[5][0] : Values[6][0];
				case EntryLevelChoice.STD3:
					return isUpper ? Values[7][0] : Values[8][0];
				default:
					return isUpper ? Values[3][0] : Values[4][0];
			}
		}
		//
		private bool IsPriceInPreviousValueArea()
		{
			if (!BlockSignalsInPreviousValueArea || previousSessionVAUpperLevel == double.MinValue || previousSessionVALowerLevel == double.MaxValue)
				return false;
		
			// Ajoute l'offset à la Value Area
			double upperLevelWithOffset = previousSessionVAUpperLevel + (ValueAreaOffsetTicks * TickSize);
			double lowerLevelWithOffset = previousSessionVALowerLevel - (ValueAreaOffsetTicks * TickSize);
		
			return Close[0] <= upperLevelWithOffset && Close[0] >= lowerLevelWithOffset;
			// if (!BlockSignalsInPreviousValueArea || previousSessionVAUpperLevel == double.MinValue || previousSessionVALowerLevel == double.MaxValue)
				// return false;
			// return Close[0] <= previousSessionVAUpperLevel && Close[0] >= previousSessionVALowerLevel;
		}
		
		
		private bool IsPriceInPreviousDynamicArea()
		{
			if (!BlockSignalsInPreviousDynamicArea || previousSessionDynamicUpperLevel == double.MinValue || previousSessionDynamicLowerLevel == double.MaxValue)
				return false;
		
			double upperLevelWithOffset = previousSessionDynamicUpperLevel + (DynamicAreaOffsetTicks * TickSize);
			double lowerLevelWithOffset = previousSessionDynamicLowerLevel - (DynamicAreaOffsetTicks * TickSize);
		
			return Close[0] <= upperLevelWithOffset && Close[0] >= lowerLevelWithOffset;
		}

		// ############################################################################################################### //
		// Ajoutez cette nouvelle méthode pour gérer la logique IB
		private void ApplyIBLogic(ref bool showUpArrow, ref bool showDownArrow)
		{
			if (!EnableIBLogic)
				return;
		
			DateTime barTime = Time[0];
			DateTime tradingDay = sessionIterator.GetTradingDay(barTime);
		
			// Détection du début de session
			if (currentDate != tradingDay)
			{
				currentDate = tradingDay;
				ibHigh = double.MinValue;
				ibLow = double.MaxValue;
				ibPeriod = true;
			}
		
			// Déterminer l'heure de début et de fin de l'IB pour la session actuelle
			DateTime ibStart = tradingDay.AddHours(IBStartTime.Hour).AddMinutes(IBStartTime.Minute);
			DateTime ibEnd = tradingDay.AddHours(IBEndTime.Hour).AddMinutes(IBEndTime.Minute);
		
			// Gérer le cas où l'heure de fin est inférieure à l'heure de début (IB traversant minuit)
			if (ibEnd <= ibStart)
				ibEnd = ibEnd.AddDays(1);
		
			// Pendant la période IB
			if (barTime >= ibStart && barTime <= ibEnd)
			{
				ibHigh = Math.Max(ibHigh, High[0]);
				ibLow = Math.Min(ibLow, Low[0]);
			}
			// Après la période IB
			else if (barTime > ibEnd && ibPeriod)
			{
				ibPeriod = false;
			}
		
			// Appliquer la logique IB aux conditions existantes
			if (!ibPeriod && ibHigh != double.MinValue && ibLow != double.MaxValue)
			{
				double upperBreak = ibHigh + IBOffsetTicks * TickSize;
				double lowerBreak = ibLow - IBOffsetTicks * TickSize;
		
				// Modifier les conditions showUpArrow et showDownArrow
				showUpArrow = showUpArrow && (Close[0] >= lowerBreak);
				showDownArrow = showDownArrow && (Close[0] <= upperBreak);
			}
		}
		// ############################################################################################################### //
		
		// ############################################################################################################### //
		//
		private bool CheckVolumeIncrease()
		{
			if (!UseVolumeIncrease || CurrentBar < VolumeBarsToCompare)
				return true;
		
			double currentVolume = Volume[0];
			
			// Vérifier que le volume actuel est supérieur à tous les volumes précédents
			for (int i = 1; i <= VolumeBarsToCompare; i++)
			{
				if (currentVolume <= Volume[i])
					return false;
			}
			
			return true;
		}
		
		
		// ############################################## Prior VA Vwap ################################################################# //
		private void UpdatePriorSessionBands()
		{
			if (Bars.IsFirstBarOfSession)
			{
				newSession = true;
				priorSessionUpperBand = vwap.StdDev1Upper[1] + (TickSize * UpperOffsetTicks);
				priorSessionLowerBand = vwap.StdDev1Lower[1] - (TickSize * LowerOffsetTicks);
			}
		}
	
		private bool IsPriceWithinValueArea(double price)
		{
			return price >= priorSessionLowerBand && price <= priorSessionUpperBand;
		}
		
		private bool ShouldAllowSignals(double price, bool isUpSignal)
		{
			bool isWithinVA = IsPriceWithinValueArea(price);
			
			// Si BlockInPriorSVA est activé et que le prix est dans la Value Area, bloquer tous les signaux
			if (BlockInPriorSVA && isWithinVA)
				return false;
				
			// Si le prix est dans la Value Area et qu'aucune option spéciale n'est activée
			if (isWithinVA && !UsePriorSvaUP && !UsePriorSvaDown)
				return true;
				
			// Pour les signaux UP
			if (isUpSignal)
			{
				// Si UsePriorSvaUP est activé
				if (UsePriorSvaUP)
				{
					// Autoriser dans la Value Area ou au-dessus
					return isWithinVA || price > priorSessionUpperBand;
				}
				// Si UsePriorSvaDown est activé
				else if (UsePriorSvaDown)
				{
					// Autoriser seulement dans la Value Area
					return isWithinVA;
				}
			}
			// Pour les signaux DOWN
			else
			{
				// Si UsePriorSvaDown est activé
				if (UsePriorSvaDown)
				{
					// Autoriser dans la Value Area ou en-dessous
					return isWithinVA || price < priorSessionLowerBand;
				}
				// Si UsePriorSvaUP est activé
				else if (UsePriorSvaUP)
				{
					// Autoriser seulement dans la Value Area
					return isWithinVA;
				}
			}
			
			// Par défaut, autoriser les signaux
			return true;
		}
		
		// ############################################## Prior VA Vwap ################################################################# //
		// ############################################## Vague Filter ################################################################# //
		private void UpdateWaveTrackingUp()
		{
			double vwap = Values[0][0];
			
			// Check if price is above VWAP
			bool isAboveVwapNow = Close[0] > vwap;
			
			// Detect crossing from below to above VWAP
			bool crossedUp = isAboveVwapNow && !isAboveVwap && Low[0] <= vwap;
			
			// If crossed up, start a new upward wave
			if (crossedUp)
			{
				upWaveStart = Low[0];
				upWaveExtreme = High[0];
				isUpWaveActive = true;
				upWaveValid = false;
				upWaveBarCount = 0;
			}
			// If already in an upward wave, update the extreme value
			else if (isUpWaveActive && isAboveVwapNow)
			{
				upWaveExtreme = Math.Max(upWaveExtreme, High[0]);
				upWaveBarCount++;
				
				// Calculate wave amplitude in ticks
				double waveAmplitude = (upWaveExtreme - upWaveStart) / TickSize;
				
				// Mark the wave as valid if it exceeds the filter threshold
				// But don't validate on the crossing bar itself
				if (waveAmplitude >= FilterVagueTicks && upWaveBarCount > 0)
				{
					upWaveValid = true;
				}
			}
			// If price goes below VWAP, end the upward wave
			else if (!isAboveVwapNow)
			{
				isUpWaveActive = false;
			}
			
			// Update the global flag for VWAP position
			isAboveVwap = isAboveVwapNow;
		}
		
		private void UpdateWaveTrackingDown()
		{
			double vwap = Values[0][0]; // État actuel : la barre est-elle en dessous de la VWAP ? 
			bool isBelowVwapNow = Close[0] < vwap; // Utiliser l'état de la barre précédente pour détecter la transition 
			bool wasAboveVwap = CurrentBar > 0 && Close[1] > Values[0][1];
		
			// Détecter le passage de la barre précédente (au-dessus) à la barre actuelle (en-dessous)
			bool crossedDown = isBelowVwapNow && wasAboveVwap && High[0] >= vwap;
			
			if (crossedDown)
			{
				downWaveStart = High[0];
				downWaveExtreme = Low[0];
				isDownWaveActive = true;
				downWaveValid = false;
				downWaveBarCount = 0;
			}
			else if (isDownWaveActive && isBelowVwapNow)
			{
				downWaveExtreme = Math.Min(downWaveExtreme, Low[0]);
				downWaveBarCount++;
				
				// Calculer l'amplitude de la vague en ticks
				double waveAmplitude = (downWaveStart - downWaveExtreme) / TickSize;
				
				// Valider la vague si l'amplitude dépasse le seuil
				if (waveAmplitude >= FilterVagueTicks && downWaveBarCount > 0)
				{
					downWaveValid = true;
				}
			}
			else if (!isBelowVwapNow)
			{
				isDownWaveActive = false;
			}
		}
		
		// ############################################## Vague Filter ################################################################# //
		// ############################################## UseThreeBarBreakout ################################################################# //
		// Ajoutez ces méthodes à votre classe principale
		private bool IsThreeBarUpBreakout()
		{
			if (CurrentBar < 2)
				return false;
				
			// Déterminer les extrémités hautes et basses basées sur Open et Close pour barre1 et barre2
			double high1 = Math.Max(Open[1], Close[1]);
			double low1 = Math.Min(Open[1], Close[1]);
			double high2 = Math.Max(Open[2], Close[2]);
			double low2 = Math.Min(Open[2], Close[2]);
			
			// Vérifier si l'une est inside ou outside par rapport à l'autre
			bool isInsideBarPattern = (high1 <= high2 && low1 >= low2) || (high2 <= high1 && low2 >= low1);
			
			if (isInsideBarPattern)
			{
				// Définir le range entre Barre1 et Barre2
				double highestHighOfRange = Math.Max(high1, high2);
				
				// Vérifier si Barre0 casse le high du range
				if (Close[0] > Close[1] && Close[0] > highestHighOfRange)
				{
					return true;
				}
			}
			
			return false;
		}
		
		private bool IsThreeBarDownBreakout()
		{
			if (CurrentBar < 2)
				return false;
				
			// Déterminer les extrémités hautes et basses basées sur Open et Close pour barre1 et barre2
			double high1 = Math.Max(Open[1], Close[1]);
			double low1 = Math.Min(Open[1], Close[1]);
			double high2 = Math.Max(Open[2], Close[2]);
			double low2 = Math.Min(Open[2], Close[2]);
			
			// Vérifier si l'une est inside ou outside par rapport à l'autre
			bool isInsideBarPattern = (high1 <= high2 && low1 >= low2) || (high2 <= high1 && low2 >= low1);
			
			if (isInsideBarPattern)
			{
				// Définir le range entre Barre1 et Barre2
				double lowestLowOfRange = Math.Min(low1, low2);
				
				// Vérifier si Barre0 casse le low du range
				if (Close[0] < Close[1] && Close[0] < lowestLowOfRange)
				{
					return true;
				}
			}
			
			return false;
		}
		
		private bool IsInsidePatternB0B1()
		{
			if (CurrentBar < 1)
				return false;
				
			// Déterminer les extrémités hautes et basses basées sur Open et Close pour barre0 et barre1
			double high0 = Math.Max(Open[0], Close[0]);
			double low0 = Math.Min(Open[0], Close[0]);
			double high1 = Math.Max(Open[1], Close[1]);
			double low1 = Math.Min(Open[1], Close[1]);
			
			// Vérifier si l'une est inside ou outside par rapport à l'autre
			bool isInsideBarPattern = (high0 <= high1 && low0 >= low1) || (high1 <= high0 && low1 >= low0);
			
			return isInsideBarPattern;
		}
		
		// ############################################## UseThreeBarBreakout ################################################################# //
		
		// ############################################## Use Setup V3B UP ################################################################# //
		private bool IsSetupV3BOCPattern()
		{
			if (CurrentBar < 2)
				return false;
				
			// Conditions communes
			bool condition1 = Open[2] > Close[2];
			bool condition2 = Close[0] > Open[2];
			
			// Conditions spécifiques au pattern OC
			bool condition3 = Open[1] < Open[2] && Close[1] < Open[2];
			bool condition4 = Close[1] < Close[2] || Open[1] < Close[2];
			bool condition5 = Close[1] < Open[0] || Open[1] < Open[0];
			
			// return condition1 && condition2 && condition3 && condition4 && condition5;
			if (IgnoreCondition1V3BOC)
				//
				return condition2 && condition3 && condition4 && condition5;
			else
				return condition1 && condition2 && condition3 && condition4 && condition5;
		}
		
		private bool IsSetupV3BHLPattern()
		{
			if (CurrentBar < 2)
				return false;
				
			// Conditions communes
			bool condition1 = Open[2] > Close[2];
			bool condition2 = Close[0] > Open[2];
			
			// Conditions spécifiques au pattern HL
			bool condition3 = High[1] < High[2];
			bool condition4 = Low[1] < Low[2];
			bool condition5 = Low[1] < Low[0];
			
			// return condition1 && condition2 && condition3 && condition4 && condition5;
			if (IgnoreCondition1V3BHL)
				//
				return condition2 && condition3 && condition4 && condition5;
			else
				return condition1 && condition2 && condition3 && condition4 && condition5;
		}
		// ############################################## Use Setup V3B UP ################################################################# //
		
		// ############################################## Use Setup V3B DOWN ################################################################# //
		private bool IsSetupV3BOCPatternDown()
		{
			if (CurrentBar < 2)
				return false;
				
			// Conditions communes pour la version DOWN
			bool condition1 = Open[2] < Close[2];
			bool condition2 = Close[0] < Open[2];
			
			// Conditions spécifiques au pattern OC pour DOWN
			bool condition3 = Open[1] > Open[2] && Close[1] > Open[2];
			bool condition4 = Close[1] > Close[2] || Open[1] > Close[2];
			bool condition5 = Close[1] > Open[0] || Open[1] > Open[0];
			
			// return condition1 && condition2 && condition3 && condition4 && condition5;
			if (IgnoreCondition1V3BOC)
				return condition2 && condition3 && condition4 && condition5;
			else
				return condition1 && condition2 && condition3 && condition4 && condition5;
		}
		
		private bool IsSetupV3BHLPatternDown()
		{
			if (CurrentBar < 2)
				return false;
				
			// Conditions communes pour la version DOWN
			bool condition1 = Open[2] < Close[2];
			bool condition2 = Close[0] < Open[2];
			
			// Conditions spécifiques au pattern HL pour DOWN
			bool condition3 = Low[1] > Low[2];
			bool condition4 = High[1] > High[2];
			bool condition5 = High[1] > High[0];
			
			// return condition1 && condition2 && condition3 && condition4 && condition5;
			if (IgnoreCondition1V3BHL)
				//
				return condition2 && condition3 && condition4 && condition5;
			else
				return condition1 && condition2 && condition3 && condition4 && condition5;
		}
		
		// ############################################## Use Setup V3B DOWN ################################################################# //
		
		// ############################################## Use Setup D3BHLup ################################################################# //
		private bool IsSetupD3BHLup1Pattern()
		{
			if (CurrentBar < 2)
				return false;
				
			bool condition1 = High[2] > High[1];
			bool condition2 = Low[2] < Low[1];
			bool condition3 = Close[0] > High[2];
			
			return condition1 && condition2 && condition3;
		}
		
		private bool IsSetupD3BHLup2Pattern()
		{
			if (CurrentBar < 2)
				return false;
				
			bool condition1 = High[2] < High[1];
			bool condition2 = Low[2] > Low[1];
			bool condition3 = Close[0] > High[1];
			
			return condition1 && condition2 && condition3;
		}
		
		private bool IsSetupD3BHLupPattern()
		{
			// Le setup principal vérifie si l'un des deux sous-setups est valide
			return IsSetupD3BHLup1Pattern() || IsSetupD3BHLup2Pattern();
		}
		
		// ############################################## Use Setup D3BHLup ################################################################# //
		
		
		// ############################################## Use Setup D3BHL DOWN ################################################################# //
		
		private bool IsSetupD3BHLdown1Pattern()
		{
			if (CurrentBar < 2)
				return false;
				
			bool condition1 = Low[2] < Low[1];
			bool condition2 = High[2] > High[1];
			bool condition3 = Close[0] < Low[2];
			
			return condition1 && condition2 && condition3;
		}
		
		private bool IsSetupD3BHLdown2Pattern()
		{
			if (CurrentBar < 2)
				return false;
				
			bool condition1 = Low[2] > Low[1];
			bool condition2 = High[2] < High[1];
			bool condition3 = Close[0] < Low[1];
			
			return condition1 && condition2 && condition3;
		}
		
		private bool IsSetupD3BHLdownPattern()
		{
			// Le setup principal vérifie si l'un des deux sous-setups est valide
			return IsSetupD3BHLdown1Pattern() || IsSetupD3BHLdown2Pattern();
		}
		
		// ############################################## Use Setup D3BHL DOWN ################################################################# //
		// ############################################## Use Setup UseSetupU4B ################################################################# //
		private bool IsSetupU4BOCupPattern()
		{
			if (CurrentBar < 3)
				return false;
			
			double high3 = Math.Max(Open[3], Close[3]);
			double high2 = Math.Max(Open[2], Close[2]);
			double high1 = Math.Max(Open[1], Close[1]);
			double high0 = Math.Max(Open[0], Close[0]);
			double low3 = Math.Min(Open[3], Close[3]);
			double low2 = Math.Min(Open[2], Close[2]);
			double low1 = Math.Min(Open[1], Close[1]);
			double low0 = Math.Min(Open[0], Close[0]);
			
			bool condition1 = high2 < high3 && high1 < high3;
			bool condition2 = low2 < low3 && low1 < low3 && low2 < low0 && low1 < low0;
			bool condition3 = high0 > high3;
			
			return condition1 && condition2 && condition3;
		}
		
		private bool IsSetupU4BHLupPattern()
		{
			if (CurrentBar < 3)
				return false;
			
			double high3 = High[3];
			double high2 = High[2];
			double high1 = High[1];
			double high0 = High[0];
			double low3 = Low[3];
			double low2 = Low[2];
			double low1 = Low[1];
			double low0 = Low[0];
			
			bool condition1 = high2 < high3 && high1 < high3;
			bool condition2 = low2 < low3 && low1 < low3 && low2 < low0 && low1 < low0;
			bool condition3 = high0 > high3;
			
			return condition1 && condition2 && condition3;
		}
		
		private bool IsSetupU4BupPattern()
		{
			// Le setup principal vérifie si l'un des deux sous-setups est valide
			return IsSetupU4BOCupPattern() || IsSetupU4BHLupPattern();
		}
		
		// Méthodes pour les setups DOWN
		private bool IsSetupU4BOCDownPattern()
		{
			if (CurrentBar < 3)
				return false;
			
			double high3 = Math.Max(Open[3], Close[3]);
			double high2 = Math.Max(Open[2], Close[2]);
			double high1 = Math.Max(Open[1], Close[1]);
			double high0 = Math.Max(Open[0], Close[0]);
			double low3 = Math.Min(Open[3], Close[3]);
			double low2 = Math.Min(Open[2], Close[2]);
			double low1 = Math.Min(Open[1], Close[1]);
			double low0 = Math.Min(Open[0], Close[0]);
			
			bool condition1 = low2 > low3 && low1 > low3;
			bool condition2 = high2 > high3 && high1 > high3 && high2 > high0 && high1 > high0;
			bool condition3 = low0 < low3;
			
			return condition1 && condition2 && condition3;
		}
		
		private bool IsSetupU4BHLDownPattern()
		{
			if (CurrentBar < 3)
				return false;
			
			double high3 = High[3];
			double high2 = High[2];
			double high1 = High[1];
			double high0 = High[0];
			double low3 = Low[3];
			double low2 = Low[2];
			double low1 = Low[1];
			double low0 = Low[0];
			
			bool condition1 = low2 > low3 && low1 > low3;
			bool condition2 = high2 > high3 && high1 > high3 && high2 > high0 && high1 > high0;
			bool condition3 = low0 < low3;
			
			return condition1 && condition2 && condition3;
		}
		
		private bool IsSetupU4BDownPattern()
		{
			// Le setup principal vérifie si l'un des deux sous-setups est valide
			return IsSetupU4BOCDownPattern() || IsSetupU4BHLDownPattern();
		}
		
		// ############################################## Use Setup UseSetupU4B ################################################################# //
		// ############################################## Use Setup ALPHA ################################################################# //
		private bool IsSetupALPHAUPPattern()
		{
			// Vérifier chaque setup individuellement en fonction de son activation
			bool patternDetected = false;
			
			if (AlphaUseV3BOC && IsSetupV3BOCPattern())
				patternDetected = true;
			
			if (AlphaUseV3BHL && IsSetupV3BHLPattern())
				patternDetected = true;
			
			if (AlphaUseD3BHLup1 && IsSetupD3BHLup1Pattern())
				patternDetected = true;
			
			if (AlphaUseD3BHLup2 && IsSetupD3BHLup2Pattern())
				patternDetected = true;
			
			if (AlphaUseThreeBarBreakout && IsThreeBarUpBreakout())
				patternDetected = true;
			
			if (AlphaUseU4Bup && IsSetupU4BupPattern())
				patternDetected = true;
			
			return patternDetected;
		}
		
		private bool IsSetupALPHADOWNPattern()
		{
			// Vérifier chaque setup individuellement en fonction de son activation
			bool patternDetected = false;
			
			if (AlphaUseV3BOCDown && IsSetupV3BOCPatternDown())
				patternDetected = true;
			
			if (AlphaUseV3BHLDown && IsSetupV3BHLPatternDown())
				patternDetected = true;
			
			if (AlphaUseD3BHLdown1 && IsSetupD3BHLdown1Pattern())
				patternDetected = true;
			
			if (AlphaUseD3BHLdown2 && IsSetupD3BHLdown2Pattern())
				patternDetected = true;
			
			if (AlphaUseThreeBarBreakoutDown && IsThreeBarDownBreakout())
				patternDetected = true;
			
			if (AlphaUseU4BDown && IsSetupU4BDownPattern())
				patternDetected = true;
			
			return patternDetected;
		}
		
		// ############################################## Use Setup ALPHA ################################################################# //
		
		
		// ############################################## ShouldDrawUpArrow ################################################################# //
        private bool ShouldDrawUpArrow()
        {
			// Vérifier d'abord si les signaux sont autorisés
			if (!ShouldAllowSignals(Close[0], true))
				return false;
			
			// Vérifier si le prix est dans la Value Area précédente
			if (BlockSignalsInPreviousValueArea && IsPriceInPreviousValueArea())
				return false;
			
			// Vérifier si le prix est dans la Dynamic Area précédente
			if (BlockSignalsInPreviousDynamicArea && IsPriceInPreviousDynamicArea())
				return false;
			
			// Vérifier la condition de range STD1 si activée
			if (EnableSTD1RangeCheck)
			{
				double std1Range = (Values[3][0] - Values[4][0]) / TickSize; // STD1Upper - STD1Lower en ticks
				if (std1Range < MinSTD1Range || std1Range > MaxSTD1Range)
					return false;
			}
			
            double vwap = Values[0][0];
            double distanceInTicks = (Close[0] - vwap) / TickSize;
			double selectedUpperLevel = GetSelectedLevel(SelectedEntryLevelUp, true);
            double upperThreshold, lowerThreshold;
            
			//
			// Définir les seuils basés sur le DynamicAreaLevel sélectionné
			double dynamicUpperThreshold, dynamicLowerThreshold;
			switch (SelectedDynamicArea)
			{
				case DynamicAreaLevel.STD05:
					dynamicUpperThreshold = Values[1][0];
					dynamicLowerThreshold = Values[2][0];
					break;
				case DynamicAreaLevel.STD1:
					dynamicUpperThreshold = Values[3][0];
					dynamicLowerThreshold = Values[4][0];
					break;
				case DynamicAreaLevel.STD2:
					dynamicUpperThreshold = Values[5][0];
					dynamicLowerThreshold = Values[6][0];
					break;
				case DynamicAreaLevel.STD3:
					dynamicUpperThreshold = Values[7][0];
					dynamicLowerThreshold = Values[8][0];
					break;
				default:
					dynamicUpperThreshold = Values[3][0];
					dynamicLowerThreshold = Values[4][0];
					break;
			}
			
			// Ajuster les seuils avec l'offset
			dynamicUpperThreshold += DynamicAreaOffsetTicks * TickSize;
			dynamicLowerThreshold -= DynamicAreaOffsetTicks * TickSize;
			
            switch (SelectedValueArea)
            {
                case ValueAreaLevel.STD05:
                    upperThreshold = Values[1][0]; // StdDev0.5 Upper
                    lowerThreshold = Values[2][0]; // StdDev0.5 Lower
                    break;
                case ValueAreaLevel.STD1:
                    upperThreshold = Values[3][0]; // StdDev1 Upper
                    lowerThreshold = Values[4][0]; // StdDev1 Lower
                    break;
                case ValueAreaLevel.STD2:
                    upperThreshold = Values[5][0]; // StdDev2 Upper
                    lowerThreshold = Values[6][0]; // StdDev2 Lower
                    break;
                case ValueAreaLevel.STD3:
                    upperThreshold = Values[7][0]; // StdDev3 Upper
                    lowerThreshold = Values[8][0]; // StdDev3 Lower
                    break;
                default:
                    upperThreshold = Values[3][0]; // Par défaut StdDev1 Upper
                    lowerThreshold = Values[4][0]; // Par défaut StdDev1 Lower
                    break;
            }

            bool withinSignalTime;
            if (SignalTimingMode == SignalTimeMode.Minutes)
            {
                TimeSpan timeSinceReset = Time[0] - lastResetTime;
                withinSignalTime = timeSinceReset.TotalMinutes >= MinMinutesForSignal && 
                                  timeSinceReset.TotalMinutes <= MaxMinutesForSignal;
            }
            else
            {
                withinSignalTime = barsSinceReset >= MinBarsForSignal && 
                                  barsSinceReset <= MaxBarsForSignal;
            }

            double selectedUpperThreshold;
            switch (SelectedValueArea)
            {
                case ValueAreaLevel.STD05:
                    selectedUpperThreshold = Values[1][0]; // StdDev0.5 Upper
                    break;
                case ValueAreaLevel.STD1:
                    selectedUpperThreshold = Values[3][0]; // StdDev1 Upper
                    break;
                case ValueAreaLevel.STD2:
                    selectedUpperThreshold = Values[5][0]; // StdDev2 Upper
                    break;
                case ValueAreaLevel.STD3:
                    selectedUpperThreshold = Values[7][0]; // StdDev3 Upper
                    break;
                default:
                    selectedUpperThreshold = Values[3][0]; // Par défaut StdDev1 Upper
                    break;
            }
			
			bool shouldCheckSTD2UpCondition = UseClose0infSTD2;
			if (SkipUseClose0infSTD2)
			{
				double std2Range = (Values[5][0] - Values[6][0]) / TickSize; // STD2Upper - STD2Lower en ticks
				if (std2Range < SkipFiltreClose0infSTD2)
					shouldCheckSTD2UpCondition = false;
			}
			
			double barre1Size = Math.Abs(Close[1] - Open[1]) / TickSize;

            bool bvaCondition = (Close[0] > Open[0]) &&
                (!OKisVOL || (VOL1[0] > VOLMA1[0])) &&
				(!UseVolumeS || Volume[0] >= volumeMaxS) &&				
				(!UseVolumeIncrease || CheckVolumeIncrease()) &&
                (!OKisAfterBarsSinceResetUP || withinSignalTime) &&
				(!OKisAboveUpperThreshold || Close[0] > (selectedUpperLevel + MinEntryDistanceUP * TickSize)) &&
				(!OKisWithinMaxEntryDistance || Close[0] <= (selectedUpperLevel + MaxEntryDistanceUP * TickSize)) &&
                (!OKisUpperBreakoutCountExceeded || upperBreakoutCount < MaxUpperBreakouts) &&
                (!useOpenForVAConditionUP || (Open[0] > lowerThreshold && Open[0] < upperThreshold)) &&
                (!useLowForVAConditionUP || (Low[0] > lowerThreshold && Low[0] < upperThreshold)) &&
				(!UsePrevBarInVA || (Open[1] > lowerThreshold && Open[1] < upperThreshold)) &&
				//(!useOpenForVAConditionUP || (Open[0] > dynamicLowerThreshold && Open[0] < dynamicUpperThreshold)) &&
				//(!useLowForVAConditionUP || (Low[0] > dynamicLowerThreshold && Low[0] < dynamicUpperThreshold)) &&
				// (!UsePrevBarInVA || (Open[1] > dynamicLowerThreshold && Open[1] < dynamicUpperThreshold)) &&
				(!UseClose0SupMaxVA || Close[0] <= (selectedUpperLevel + MaxOffsetClose0VAUP * TickSize)) &&
				(!UseOpen0inVA05 || (Open[0] >= Values[2][0] && Open[0] <= Values[1][0])) &&
				(!UserBarre0FiltreWik || (High[0] - Close[0]) / TickSize <= FilterWickBarre0) &&
				(!shouldCheckSTD2UpCondition || Close[0] <= (Values[5][0] + STD2OffsetTicksUP * TickSize)) &&
				(!UseBarre0FiltreVolume || (Volume[0] >= MinVolBarre0 && Volume[0] <= MaxVolBarre0)) &&
				(!UseCrossVwapBarre0 || (Open[0] < Values[0][0] && Close[0] > Values[0][0])) &&
				(!UseRejVwapBarre0 || (Open[0] > Values[0][0] && Close[0] > Values[0][0] && Low[0] < Values[0][0])) &&
				(!UseCrossStd1Barre0 || (Open[0] < Values[3][0] && Close[0] > (Values[3][0] + Std1CrossOffsetTicksBarre0 * TickSize))) &&
				(!UseRejStd1Barre0 || (Open[0] > Values[3][0] && Close[0] > (Values[3][0] + Std1RejOffsetTicksBarre0 * TickSize) && Low[0] < Values[3][0])) &&
				(!UseOpen1inVA05 || (Open[1] >= Values[2][1] && Open[1] <= Values[1][1])) &&
				(!UseOpen1inVAstd1 || (Open[1] >= Values[4][1] && Open[1] <= Values[3][1])) &&
				(!UserBarre1FiltreWik || (High[1] - Close[1]) / TickSize <= FilterWickBarre1) &&
				(!UseBarre1FiltreVolume || (Volume[1] >= MinVolBarre1 && Volume[1] <= MaxVolBarre1)) &&
				(!UseBarre1FilterSizeOC || (barre1Size >= FilterMinSizeBarre1OC && barre1Size <= FilterMaxSizeBarre1OC)) &&
				(!UseCrossVwapBarre1 || (Open[1] < Values[0][1] && Close[1] > Values[0][1])) &&
				(!UseRejVwapBarre1 || (Open[1] > Values[0][1] && Close[1] > Values[0][1] && Low[1] < Values[0][1])) &&
				(!UseCrossStd1Barre1 || (Open[1] < Values[3][1] && Close[1] > (Values[3][1] + Std1CrossOffsetTicks * TickSize))) &&
				(!UseRejStd1Barre1 || (Open[1] > Values[3][1] && Close[1] > (Values[3][1] + Std1RejOffsetTicks * TickSize) && Low[1] < Values[3][1])) &&
				(!UseVague1UP || (isAboveVwap && upWaveValid)) &&
				(!UseRejStd05Barre0 || (Open[0] > Values[1][0] && Close[0] > (Values[1][0] + Std05RejOffsetTicksBarre0 * TickSize) && Low[0] < Values[1][0])) &&
				(!UseRejStd05Barre1 || (Open[1] > Values[1][1] && Close[1] > (Values[1][1] + Std05RejOffsetTicksBarre1 * TickSize) && Low[1] < Values[1][1])) &&
				(!UseSetupV3B || IsSetupV3BOCPattern() || IsSetupV3BHLPattern()) &&
				(!UseSetupV3BOC || IsSetupV3BOCPattern()) &&
				(!UseSetupV3BHL || IsSetupV3BHLPattern()) &&
				(!UseSetupD3BHLup || IsSetupD3BHLupPattern()) &&
				(!UseSetupD3BHLup1 || IsSetupD3BHLup1Pattern()) &&
				(!UseSetupD3BHLup2 || IsSetupD3BHLup2Pattern()) &&
				(!UseThreeBarBreakoutUp || IsThreeBarUpBreakout()) &&
				(!UseInsideB0B1 || IsInsidePatternB0B1()) &&
				(!UseSetupU4Bup || IsSetupU4BupPattern()) &&
				(!UseSetupU4BOCup || IsSetupU4BOCupPattern()) &&
				(!UseSetupU4BHLup || IsSetupU4BHLupPattern()) &&
				(!UseSetupALPHAUP || IsSetupALPHAUPPattern()) &&
				(!EnableDistanceFromVWAPCondition || (distanceInTicks >= MinDistanceFromVWAP && distanceInTicks <= MaxDistanceFromVWAP));

            double openCloseDiff = Math.Abs(Open[0] - Close[0]) / TickSize;
            double highLowDiff = Math.Abs(High[0] - Low[0]) / TickSize;
            bool limusineCondition = (ShowLimusineOpenCloseUP && openCloseDiff >= MinimumTicks && openCloseDiff <= MaximumTicks && Close[0] > Open[0]) ||
                                    (ShowLimusineHighLowUP && highLowDiff >= MinimumTicks && highLowDiff <= MaximumTicks && Close[0] > Open[0]);

            bool std3Condition = !EnableSTD3HighLowTracking || Values[7][0] >= highestSTD3Upper;
            bool rangeBreakoutCondition = !EnablePreviousSessionRangeBreakout || 
                (previousSessionHighStd1Upper != double.MinValue && Close[0] > previousSessionHighStd1Upper);
            
            // return bvaCondition && limusineCondition && std3Condition && rangeBreakoutCondition;
			bool showUpArrow = bvaCondition && limusineCondition && std3Condition && rangeBreakoutCondition;
			
			if (CurrentBar >= SlopeStartBars)
			{
				// VWAP Session
				if (UseVwapSessioSlopeFilterUp)
				{
					double vwapSessionSlope = Slope(VWAP, SlopeStartBars, SlopeEndBars);
					if (vwapSessionSlope < MinVwapSessionSlopeUp)
						showUpArrow = false;
				}
				
				// VWAP Reset
				if (UseVwapSlopeFilterUp)
				{
					double vwapResetSlope = Slope(Values[0], SlopeStartBars, SlopeEndBars);
					if (vwapResetSlope < MinVwapResetSlopeUp)
						showUpArrow = false;
				}
				
				// STD1 Upper
				if (UseStdUpperSloperUP)
				{
					double stdUpperSlope = Slope(Values[3], SlopeStartBars, SlopeEndBars);
					if (stdUpperSlope < MinStdUpperSlopeUp)
						showUpArrow = false;
				}
				
				// STD1 Lower
				if (UseStdLowerUP)
				{
					double stdLowerSlope = Slope(Values[4], SlopeStartBars, SlopeEndBars);
					if (stdLowerSlope < MinStdLowerSlopeUp)
						showUpArrow = false;
				}
			}
			
			// Condition de cassure du plus haut des X dernières barres si activé
			if (EnableUpBreakoutCheck)
			{
				double highestHigh = double.MinValue;
				// Récupérer le plus haut des UpBreakoutBars dernières barres (par ex. de High[1] à High[UpBreakoutBars])
				for (int i = 1; i <= UpBreakoutBars; i++)
				{
					if (CurrentBar - i < 0) break; 
				
					// On prend la valeur la plus haute entre High, Open, Close, Low
					double barHighest = Math.Max(High[i], Math.Max(Open[i], Math.Max(Close[i], Low[i])));
					highestHigh = Math.Max(highestHigh, barHighest);
				}
		
				// Vérifier que Close[0] casse ce plus haut + offset en ticks
				if (!(Close[0] > highestHigh + UpBreakoutOffsetTicks * TickSize))
				{
					showUpArrow = false;
				}
			}
			
			// Appliquer la logique IB
			bool showDownArrow = false; // dummy variable nécessaire pour la méthode
			ApplyIBLogic(ref showUpArrow, ref showDownArrow);
			
			return showUpArrow;
        }

        private bool ShouldDrawDownArrow()
        {
			if (!ShouldAllowSignals(Close[0], false))
				return false;
			
			// Vérifier si le prix est dans la Value Area précédente
			if (BlockSignalsInPreviousValueArea && IsPriceInPreviousValueArea())
				return false;
			
			// Vérifier si le prix est dans la Dynamic Area précédente
			if (BlockSignalsInPreviousDynamicArea && IsPriceInPreviousDynamicArea())
				return false;
			
			// Vérifier la condition de range STD1 si activée
			if (EnableSTD1RangeCheck)
			{
				double std1Range = (Values[3][0] - Values[4][0]) / TickSize; // STD1Upper - STD1Lower en ticks
				if (std1Range < MinSTD1Range || std1Range > MaxSTD1Range)
					return false;
			}
			
            double vwap = Values[0][0];
            double distanceInTicks = (vwap - Close[0]) / TickSize;
            double selectedLowerLevel = GetSelectedLevel(SelectedEntryLevelDown, false);
            double upperThreshold, lowerThreshold;
            
			//
			// Définir les seuils basés sur le DynamicAreaLevel sélectionné
			double dynamicUpperThreshold, dynamicLowerThreshold;
			switch (SelectedDynamicArea)
			{
				case DynamicAreaLevel.STD05:
					dynamicUpperThreshold = Values[1][0];
					dynamicLowerThreshold = Values[2][0];
					break;
				case DynamicAreaLevel.STD1:
					dynamicUpperThreshold = Values[3][0];
					dynamicLowerThreshold = Values[4][0];
					break;
				case DynamicAreaLevel.STD2:
					dynamicUpperThreshold = Values[5][0];
					dynamicLowerThreshold = Values[6][0];
					break;
				case DynamicAreaLevel.STD3:
					dynamicUpperThreshold = Values[7][0];
					dynamicLowerThreshold = Values[8][0];
					break;
				default:
					dynamicUpperThreshold = Values[3][0];
					dynamicLowerThreshold = Values[4][0];
					break;
			}
			
			// Ajuster les seuils avec l'offset
			dynamicUpperThreshold += DynamicAreaOffsetTicks * TickSize;
			dynamicLowerThreshold -= DynamicAreaOffsetTicks * TickSize;
			
            switch (SelectedValueArea)
            {
                case ValueAreaLevel.STD05:
                    upperThreshold = Values[1][0]; // StdDev0.5 Upper
                    lowerThreshold = Values[2][0]; // StdDev0.5 Lower
                    break;
                case ValueAreaLevel.STD1:
                    upperThreshold = Values[3][0]; // StdDev1 Upper
                    lowerThreshold = Values[4][0]; // StdDev1 Lower
                    break;
                case ValueAreaLevel.STD2:
                    upperThreshold = Values[5][0]; // StdDev2 Upper
                    lowerThreshold = Values[6][0]; // StdDev2 Lower
                    break;
                case ValueAreaLevel.STD3:
                    upperThreshold = Values[7][0]; // StdDev3 Upper
                    lowerThreshold = Values[8][0]; // StdDev3 Lower
                    break;
                default:
                    upperThreshold = Values[3][0]; // Par défaut StdDev1 Upper
                    lowerThreshold = Values[4][0]; // Par défaut StdDev1 Lower
                    break;
            }

            bool withinSignalTime;
            if (SignalTimingMode == SignalTimeMode.Minutes)
            {
                TimeSpan timeSinceReset = Time[0] - lastResetTime;
                withinSignalTime = timeSinceReset.TotalMinutes >= MinMinutesForSignal && 
                                  timeSinceReset.TotalMinutes <= MaxMinutesForSignal;
            }
            else
            {
                withinSignalTime = barsSinceReset >= MinBarsForSignal && 
                                  barsSinceReset <= MaxBarsForSignal;
            }

            double selectedLowerThreshold;
            switch (SelectedValueArea)
            {
                case ValueAreaLevel.STD05:
                    selectedLowerThreshold = Values[2][0]; // StdDev0.5 Lower
                    break;
                case ValueAreaLevel.STD1:
                    selectedLowerThreshold = Values[4][0]; // StdDev1 Lower
                    break;
                case ValueAreaLevel.STD2:
                    selectedLowerThreshold = Values[6][0]; // StdDev2 Lower
                    break;
                case ValueAreaLevel.STD3:
                    selectedLowerThreshold = Values[8][0]; // StdDev3 Lower
                    break;
                default:
                    selectedLowerThreshold = Values[4][0]; // Par défaut StdDev1 Lower
                    break;
            }
			
			bool shouldCheckSTD2DownCondition = UseClose0supSTD2;
			if (SkipUseClose0supSTD2)
			{
				double std2Range = (Values[5][0] - Values[6][0]) / TickSize; // STD2Upper - STD2Lower en ticks
				if (std2Range < SkipFiltreClose0supSTD2)
					shouldCheckSTD2DownCondition = false;
			}
			
			double barre1Size = Math.Abs(Close[1] - Open[1]) / TickSize;

            bool bvaCondition = (Close[0] < Open[0]) &&
                (!OKisVOL || (VOL1[0] > VOLMA1[0])) &&
				(!UseVolumeS || Volume[0] >= volumeMaxS) &&
				(!UseVolumeIncrease || CheckVolumeIncrease()) &&
                (!OKisAfterBarsSinceResetDown || withinSignalTime) &&
				(!OKisBelovLowerThreshold || Close[0] < (selectedLowerLevel - MinEntryDistanceDOWN * TickSize)) &&
				(!OKisWithinMaxEntryDistanceDown || Close[0] >= (selectedLowerLevel - MaxEntryDistanceDOWN * TickSize)) &&
                (!OKisLowerBreakoutCountExceeded || lowerBreakoutCount < MaxLowerBreakouts) &&
                (!useOpenForVAConditionDown || (Open[0] > lowerThreshold && Open[0] < upperThreshold)) &&
                (!useHighForVAConditionDown || (High[0] > lowerThreshold && High[0] < upperThreshold)) &&
				(!UsePrevBarInVA || (Open[1] > lowerThreshold && Open[1] < upperThreshold)) &&
				//(!useOpenForVAConditionDown || (Open[0] > dynamicLowerThreshold && Open[0] < dynamicUpperThreshold)) &&
				//(!useHighForVAConditionDown || (High[0] > dynamicLowerThreshold && High[0] < dynamicUpperThreshold)) &&
				// (!UsePrevBarInVA || (Open[1] > dynamicLowerThreshold && Open[1] < dynamicUpperThreshold)) &&
				(!UseClose0InfMaxVA || Close[0] >= (selectedLowerLevel - MaxOffsetClose0VADOWN * TickSize)) &&
				(!UseOpen0inVA05 || (Open[0] >= Values[2][0] && Open[0] <= Values[1][0])) &&
				(!UserBarre0FiltreWik || (Close[0] - Low[0]) / TickSize <= FilterWickBarre0) &&
				(!shouldCheckSTD2DownCondition || Close[0] >= (Values[6][0] - STD2OffsetTicksDOWN * TickSize)) &&
				(!UseBarre0FiltreVolume || (Volume[0] >= MinVolBarre0 && Volume[0] <= MaxVolBarre0)) &&
				(!UseCrossVwapBarre0 || (Open[0] > Values[0][0] && Close[0] < Values[0][0])) &&
				(!UseRejVwapBarre0 || (Open[0] < Values[0][0] && Close[0] < Values[0][0] && High[0] > Values[0][0])) &&
				(!UseCrossStd1Barre0 || (Open[0] > Values[4][0] && Close[0] < (Values[4][0] - Std1CrossOffsetTicksBarre0 * TickSize))) &&
				(!UseRejStd1Barre0 || (Open[0] < Values[4][0] && Close[0] < (Values[4][0] - Std1RejOffsetTicksBarre0 * TickSize) && High[0] > Values[4][0])) &&
				(!UseOpen1inVA05 || (Open[1] >= Values[2][1] && Open[1] <= Values[1][1])) &&
				(!UseOpen1inVAstd1 || (Open[1] >= Values[4][1] && Open[1] <= Values[3][1])) &&
				(!UserBarre1FiltreWik || (Close[1] - Low[1]) / TickSize <= FilterWickBarre1) &&
				(!UseBarre1FiltreVolume || (Volume[1] >= MinVolBarre1 && Volume[1] <= MaxVolBarre1)) &&
				(!UseBarre1FilterSizeOC || (barre1Size >= FilterMinSizeBarre1OC && barre1Size <= FilterMaxSizeBarre1OC)) &&
				(!UseCrossVwapBarre1 || (Open[1] > Values[0][1] && Close[1] < Values[0][1])) &&
				(!UseRejVwapBarre1 || (Open[1] < Values[0][1] && Close[1] < Values[0][1] && High[1] > Values[0][1])) &&
				(!UseCrossStd1Barre1 || (Open[1] > Values[4][1] && Close[1] < (Values[4][1] + Std1CrossOffsetTicks * TickSize))) &&
				(!UseRejStd1Barre1 || (Open[1] < Values[4][1]  && Close[1] < (Values[4][1] + Std1RejOffsetTicks * TickSize) && High[1] > Values[4][1])) &&
				(!UseVague1DOWN || (!isAboveVwap && downWaveValid)) &&
				(!UseRejStd05Barre0 || (Open[0] < Values[2][0] && Close[0] < (Values[2][0] - Std05RejOffsetTicksBarre0 * TickSize) && High[0] > Values[2][0])) &&
				(!UseRejStd05Barre1 || (Open[1] < Values[2][1] && Close[1] < (Values[2][1] - Std05RejOffsetTicksBarre1 * TickSize) && High[1] > Values[2][1])) &&
				(!UseSetupV3BDown || IsSetupV3BOCPatternDown() || IsSetupV3BHLPatternDown()) &&
				(!UseSetupV3BOCDown || IsSetupV3BOCPatternDown()) &&
				(!UseSetupV3BHLDown || IsSetupV3BHLPatternDown()) &&
				(!UseSetupD3BHLdown || IsSetupD3BHLdownPattern()) &&
				(!UseSetupD3BHLdown1 || IsSetupD3BHLdown1Pattern()) &&
				(!UseSetupD3BHLdown2 || IsSetupD3BHLdown2Pattern()) &&
				(!UseThreeBarBreakoutDown || IsThreeBarDownBreakout()) &&
				(!UseInsideB0B1 || IsInsidePatternB0B1()) &&
				(!UseSetupU4BDown || IsSetupU4BDownPattern()) &&
				(!UseSetupU4BOCDown || IsSetupU4BOCDownPattern()) &&
				(!UseSetupU4BHLDown || IsSetupU4BHLDownPattern()) &&
				(!UseSetupALPHADOWN || IsSetupALPHADOWNPattern()) &&
				(!EnableDistanceFromVWAPCondition || (distanceInTicks >= MinDistanceFromVWAP && distanceInTicks <= MaxDistanceFromVWAP)); 

            double openCloseDiff = Math.Abs(Open[0] - Close[0]) / TickSize;
            double highLowDiff = Math.Abs(High[0] - Low[0]) / TickSize;
            bool limusineCondition = (ShowLimusineOpenCloseDOWN && openCloseDiff >= MinimumTicks && openCloseDiff <= MaximumTicks && Close[0] < Open[0]) ||
                                    (ShowLimusineHighLowDOWN && highLowDiff >= MinimumTicks && highLowDiff <= MaximumTicks && Close[0] < Open[0]);

            bool std3Condition = !EnableSTD3HighLowTracking || Values[8][0] <= lowestSTD3Lower;
            bool rangeBreakoutCondition = !EnablePreviousSessionRangeBreakout || 
                (previousSessionLowStd1Lower != double.MaxValue && Close[0] < previousSessionLowStd1Lower);
            
            // return bvaCondition && limusineCondition && std3Condition && rangeBreakoutCondition;
			bool showDownArrow = bvaCondition && limusineCondition && std3Condition && rangeBreakoutCondition;
			
			if (CurrentBar >= SlopeStartBars)
			{
				// VWAP Session
				if (UseVwapSessioSlopeFilterDown)
				{
					double vwapSessionSlope = Slope(VWAP, SlopeStartBars, SlopeEndBars);
					if (vwapSessionSlope > MaxVwapSessionSlopeDown)
						showDownArrow = false;
				}
				
				// VWAP Reset
				if (UseVwapSlopeFilterDown)
				{
					double vwapResetSlope = Slope(Values[0], SlopeStartBars, SlopeEndBars);
					if (vwapResetSlope > MaxVwapResetSlopeDown)
						showDownArrow = false;
				}
				
				// STD1 Upper
				if (UseStdUpperSloperDown)
				{
					double stdUpperSlope = Slope(Values[3], SlopeStartBars, SlopeEndBars);
					if (stdUpperSlope > MaxStdUpperSlopeDown)
						showDownArrow = false;
				}
				
				// STD1 Lower
				if (UseStdLowerDown)
				{
					double stdLowerSlope = Slope(Values[4], SlopeStartBars, SlopeEndBars);
					if (stdLowerSlope > MaxStdLowerSlopeDown)
						showDownArrow = false;
				}
			}
			
			if (EnableDownBreakoutCheck)
			{
				double lowestLow = double.MaxValue;
				// Récupérer le plus bas des DownBreakoutBars dernières barres (par ex. de Low[1] à Low[DownBreakoutBars])
				for (int i = 1; i <= DownBreakoutBars; i++)
				{
					if (CurrentBar - i < 0) break;
				
					// On prend la valeur la plus basse parmi Open, High, Low, Close
					double barLowest = Math.Min(Low[i], Math.Min(Open[i], Math.Min(Close[i], High[i])));
					lowestLow = Math.Min(lowestLow, barLowest);
				}
		
				// Vérifier que Close[0] casse ce plus bas - offset en ticks
				if (!(Close[0] < lowestLow - DownBreakoutOffsetTicks * TickSize))
				{
					showDownArrow = false;
				}
			}
			
			// Appliquer la logique IB
			bool showUpArrow = false; // dummy variable nécessaire pour la méthode
			ApplyIBLogic(ref showUpArrow, ref showDownArrow);
			
			return showDownArrow;
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
			
			dynamicAreaPointsDrawn = false;
			volumeMaxS = 0;
			// previousVAUpperLevel = double.MinValue;
			// previousVALowerLevel = double.MaxValue;
        }
		
		// ############################################################################# //
		private (double redDotPrice, double blueDotPrice) CalculateDotLevels(bool isUpSignal, double closePrice)
		{
			double redDotPrice, blueDotPrice;
			
			switch (SelectedDotPlotMode)
			{
				case DotPlotMode.STD1:
					if (isUpSignal)
					{
						redDotPrice = Close[0] - (Values[0][0] - Values[4][0]); // VWAP - (STD1Upper - STD1Lower)
						blueDotPrice = Close[0] + (Values[3][0] -Values[0][0]); // VWAP + (STD1Upper - VWAP)
					}
					else
					{
						redDotPrice = Close[0] + (Values[3][0] - Values[0][0]); // VWAP + (STD1Upper - STD1Lower)
						blueDotPrice = Close[0] - (Values[0][0] - Values[4][0]); // VWAP - (VWAP - STD1Lower)
					}
					break;
					
				case DotPlotMode.STD2:
					if (isUpSignal)
					{
						redDotPrice = Close[0] - (Values[0][0] - Values[6][0]); // VWAP - (STD2Upper - STD2Lower)
						blueDotPrice = Close[0] + (Values[5][0] - Values[0][0]); // VWAP + (STD2Upper - VWAP)
					}
					else
					{
						redDotPrice = Close[0] + (Values[5][0] - Values[0][0]); // VWAP + (STD2Upper - STD2Lower)
						blueDotPrice = Close[0] - (Values[0][0] - Values[6][0]); // VWAP - (VWAP - STD2Lower)
					}
					break;
					
				//
				case DotPlotMode.UseSTD1Multiple:
					double std1Range = Values[3][0] - Values[4][0]; // STD1Upper - STD1Lower
					if (isUpSignal)
					{
						redDotPrice = Close[0] - (std1Range * STD1RedDotMultiplier); // Close - (STD1 range * multiplier)
						blueDotPrice = Close[0] + (std1Range * STD1BlueDotMultiplier); // Close + (STD1 range * multiplier)
					}
					else
					{
						redDotPrice = Close[0] + (std1Range * STD1RedDotMultiplier); // Close + (STD1 range * multiplier)
						blueDotPrice = Close[0] - (std1Range * STD1BlueDotMultiplier); // Close - (STD1 range * multiplier)
					}
					break;
				// 
				case DotPlotMode.BarSize:
					double barSize;
					
					// Calculer la taille de la barre selon le type sélectionné
					if (SelectedBarSizeType == BarSizeType.CloseOpen)
					{
						barSize = Math.Abs(Close[0] - Open[0]);
					}
					else // HighLow
					{
						barSize = Math.Abs(High[0] - Low[0]);
					}
					
					if (isUpSignal)
					{
						redDotPrice = closePrice - (barSize * RedDotMultiplier); // Close - (bar size * multiplier) (stoploss)
						blueDotPrice = closePrice + (barSize * BlueDotMultiplier); // Close + (bar size * multiplier) (takeprofit)
					}
					else
					{
						redDotPrice = closePrice + (barSize * RedDotMultiplier); // Close + (bar size * multiplier) (stoploss)
						blueDotPrice = closePrice - (barSize * BlueDotMultiplier); // Close - (bar size * multiplier) (takeprofit)
					}
					break;
				
				default:
					redDotPrice = closePrice;
					blueDotPrice = closePrice;
					break;
			}
			
			return (redDotPrice, blueDotPrice);
		}
		// ############################################################################# //
		
		#region Properties
        // ###################### Propriétés BVA ###############################
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Reset Period (Minutes)", Order = 1, GroupName = "0.1_BVA Parameters")]
        public int ResetPeriod { get; set; }
		
		[NinjaScriptProperty]
        [Display(Name="Signal Time Mode", Description="Choose between Bars or Minutes for signal timing", Order=2, GroupName="0.1_BVA Parameters")]
        public SignalTimeMode SignalTimingMode { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Min Bars for Signal", Order = 3, GroupName = "0.1_BVA Parameters")]
        public int MinBarsForSignal { get; set; }
        
        [Range(1, int.MaxValue)]
        [Display(Name = "Max Bars for Signal", Description = "Nombre maximum de barres depuis la réinitialisation pour un signal", Order = 4, GroupName = "0.1_BVA Parameters")]
        public int MaxBarsForSignal { get; set; }
		
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Min Minutes for Signal", Order=5, GroupName="0.1_BVA Parameters")]
        public int MinMinutesForSignal { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Max Minutes for Signal", Order=6, GroupName="0.1_BVA Parameters")]
        public int MaxMinutesForSignal { get; set; }
		
		public enum SignalTimeMode
        {
            Bars,
            Minutes
        }
		
		public enum ValueAreaLevel
        {
            STD05,  // Standard Deviation 0.5
            STD1,   // Standard Deviation 1
            STD2,   // Standard Deviation 2
            STD3    // Standard Deviation 3
        }
		
        [NinjaScriptProperty]
        [Display(Name="Value Area Level", Description="Choose which Standard Deviation level to use", Order=7, GroupName="0.1_BVA Parameters")]
        public ValueAreaLevel SelectedValueArea { get; set; }

        [NinjaScriptProperty]
        [Display(Name="use Open in VA Condition UP", Order=8, GroupName="0.1_BVA Parameters")]
        public bool useOpenForVAConditionUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name="use Open in VA Condition Down", Order=9, GroupName="0.1_BVA Parameters")]
        public bool useOpenForVAConditionDown { get; set; }

        [NinjaScriptProperty]
        [Display(Name="use Low in VA Condition UP", Order=10, GroupName="0.1_BVA Parameters")]
        public bool useLowForVAConditionUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name="use High in VA Condition Down", Order=11, GroupName="0.1_BVA Parameters")]
        public bool useHighForVAConditionDown { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Block Signals in Previous Value Area", Description="Block signals when price is inside previous session's Value Area", Order=12, GroupName="0.1_BVA Parameters")]
		public bool BlockSignalsInPreviousValueArea { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Value Area Offset Ticks", Description="Offset en ticks pour la Value Area précédente", Order=13, GroupName="0.1_BVA Parameters")]
		public int ValueAreaOffsetTicks { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Use Previous Bar Open in VA", Description="Check if previous bar's Open is within Value Area", Order=14, GroupName="0.1_BVA Parameters")]
		public bool UsePrevBarInVA { get; set; }
        
        // ################# Propriétés Limusine ##########################
		[NinjaScriptProperty]
		[Display(Name = "Active Buy", Description = "Activer les signaux d'achat (flèches UP)", Order = 1, GroupName = "0.2_Limusine Parameters")]
		public bool ActiveBuy { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Active Sell", Description = "Activer les signaux de vente (flèches DOWN)", Order = 2, GroupName = "0.2_Limusine Parameters")]
		public bool ActiveSell { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Minimum Ticks", Description = "Nombre minimum de ticks pour une limusine", Order = 3, GroupName = "0.2_Limusine Parameters")]
        public int MinimumTicks { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Maximum Ticks", Description = "Nombre maximum de ticks pour une limusine", Order = 4, GroupName = "0.2_Limusine Parameters")]
        public int MaximumTicks { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Afficher Limusine Open-Close UP", Description = "Afficher les limusines Open-Close UP", Order = 5, GroupName = "0.2_Limusine Parameters")]
        public bool ShowLimusineOpenCloseUP { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Afficher Limusine Open-Close DOWN", Description = "Afficher les limusines Open-Close DOWN", Order = 6, GroupName = "0.2_Limusine Parameters")]
        public bool ShowLimusineOpenCloseDOWN { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Afficher Limusine High-Low UP", Description = "Afficher les limusines High-Low UP", Order = 7, GroupName = "0.2_Limusine Parameters")]
        public bool ShowLimusineHighLowUP { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Afficher Limusine High-Low DOWN", Description = "Afficher les limusines High-Low DOWN", Order = 8, GroupName = "0.2_Limusine Parameters")]
        public bool ShowLimusineHighLowDOWN { get; set; }
		
		// ############ Buy & Sell #############
		public enum EntryLevelChoice
		{
			STD05,
			STD1,
			STD2,
			STD3
		}
        // ############# Buy ############## //
		[NinjaScriptProperty]
		[Display(Name="Entry Level Choice UP", Description="Choose which Standard Deviation level to use for UP entries", Order=0, GroupName="0.3_Buy")]
		public EntryLevelChoice SelectedEntryLevelUp { get; set; }
	
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
        [Display(Name = "OKisAfterBarsSinceResetUP", Description = "Check Bars Since Reset UP", Order = 4, GroupName = "0.3_Buy")]
        public bool OKisAfterBarsSinceResetUP { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, 1)]
        [Display(Name = "OKisAboveUpperThreshold", Description = "Check Above Upper Threshold", Order = 5, GroupName = "0.3_Buy")]
        public bool OKisAboveUpperThreshold { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, 1)]
        [Display(Name = "OKisWithinMaxEntryDistance", Description = "Check Within Max Entry Distance", Order = 6, GroupName = "0.3_Buy")]
        public bool OKisWithinMaxEntryDistance { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, 1)]
        [Display(Name = "OKisUpperBreakoutCountExceeded", Description = "Check Upper Breakout Count Exceeded", Order = 7, GroupName = "0.3_Buy")]
        public bool OKisUpperBreakoutCountExceeded { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Enable Up Breakout Check", Description="Active la condition de cassure du plus haut des dernières barres pour un signal UP", Order=8, GroupName="0.3_Buy")]
		public bool EnableUpBreakoutCheck { get; set; }
		
		[NinjaScriptProperty]
		[Range(1,10)]
		[Display(Name="Up Breakout Bars", Description="Nombre de barres à considérer pour le plus haut (2 à 5)", Order=9, GroupName="0.3_Buy")]
		public int UpBreakoutBars { get; set; }
		
		[NinjaScriptProperty]
		[Range(0,int.MaxValue)]
		[Display(Name="Up Breakout Offset Ticks", Description="Offset en ticks au-dessus du plus haut pour confirmer la cassure", Order=10, GroupName="0.3_Buy")]
		public int UpBreakoutOffsetTicks { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "UseClose0SupMaxVA", Description = "Vérifier que Close[0] est inférieur à la VA", Order = 11, GroupName = "0.3_Buy")]
		public bool UseClose0SupMaxVA { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "MaxOffsetClose0VAUP", Description = "Offset en ticks pour le niveau maximum de la VA", Order = 12, GroupName = "0.3_Buy")]
		public int MaxOffsetClose0VAUP { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "UseClose0infSTD2", Description = "Vérifier que Close[0] est inférieur à STD2 Upper + offset", Order = 13, GroupName = "0.3_Buy")]
		public bool UseClose0infSTD2 { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "STD2OffsetTicksUP", Description = "Offset en ticks pour STD2 Upper", Order = 14, GroupName = "0.3_Buy")]
		public int STD2OffsetTicksUP { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "SkipUseClose0infSTD2", Description = "Ignore UseClose0infSTD2 si la distance STD2 est trop petite", Order = 15, GroupName = "0.3_Buy")]
		public bool SkipUseClose0infSTD2 { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "SkipFiltreClose0infSTD2", Description = "Distance minimale en ticks entre STD2 Upper et Lower", Order = 16, GroupName = "0.3_Buy")]
		public int SkipFiltreClose0infSTD2 { get; set; }
        
        // ############ Sell #############
        // Sell
		[NinjaScriptProperty]
		[Display(Name="Entry Level Choice DOWN", Description="Choose which Standard Deviation level to use for DOWN entries", Order=0, GroupName="0.4_Sell")]
		public EntryLevelChoice SelectedEntryLevelDown { get; set; }
	
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
        [Display(Name = "OKisAfterBarsSinceResetDown", Description = "Check Bars Since Reset Down", Order = 4, GroupName = "0.4_Sell")]
        public bool OKisAfterBarsSinceResetDown { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, 1)]
        [Display(Name = "OKisBelovLowerThreshold", Description = "Check Below Lower Threshold", Order = 5, GroupName = "0.4_Sell")]
        public bool OKisBelovLowerThreshold { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, 1)]
        [Display(Name = "OKisWithinMaxEntryDistanceDown", Description = "Check Within Max Entry Distance Down", Order = 6, GroupName = "0.4_Sell")]
        public bool OKisWithinMaxEntryDistanceDown { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, 1)]
        [Display(Name = "OKisLowerBreakoutCountExceeded", Description = "Check Lower Breakout Count Exceeded", Order = 7, GroupName = "0.4_Sell")]
        public bool OKisLowerBreakoutCountExceeded { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Enable Down Breakout Check", Description="Active la condition de cassure du plus bas des dernières barres pour un signal DOWN", Order=8, GroupName="0.4_Sell")]
		public bool EnableDownBreakoutCheck { get; set; }
		
		[NinjaScriptProperty]
		[Range(1,10)]
		[Display(Name="Down Breakout Bars", Description="Nombre de barres à considérer pour le plus bas (2 à 5)", Order=9, GroupName="0.4_Sell")]
		public int DownBreakoutBars { get; set; }
		
		[NinjaScriptProperty]
		[Range(0,int.MaxValue)]
		[Display(Name="Down Breakout Offset Ticks", Description="Offset en ticks en-dessous du plus bas pour confirmer la cassure", Order=10, GroupName="0.4_Sell")]
		public int DownBreakoutOffsetTicks { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "UseClose0InfMaxVA", Description = "Vérifier que Close[0] est supérieur à la VA", Order = 11, GroupName = "0.4_Sell")]
		public bool UseClose0InfMaxVA { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "MaxOffsetClose0VADOWN", Description = "Offset en ticks pour le niveau minimum de la VA", Order = 12, GroupName = "0.4_Sell")]
		public int MaxOffsetClose0VADOWN { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "UseClose0supSTD2", Description = "Vérifier que Close[0] est supérieur à STD2 Lower - offset", Order = 13, GroupName = "0.4_Sell")]
		public bool UseClose0supSTD2 { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "STD2OffsetTicksDOWN", Description = "Offset en ticks pour STD2 Lower", Order = 14, GroupName = "0.4_Sell")]
		public int STD2OffsetTicksDOWN { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "SkipUseClose0supSTD2", Description = "Ignore UseClose0supSTD2 si la distance STD2 est trop petite", Order = 15, GroupName = "0.4_Sell")]
		public bool SkipUseClose0supSTD2 { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "SkipFiltreClose0supSTD2", Description = "Distance minimale en ticks entre STD2 Upper et Lower", Order = 16, GroupName = "0.4_Sell")]
		public int SkipFiltreClose0supSTD2 { get; set; }
		
		// ############################ 0.7_Barre0_Property ####################################### // 
		[NinjaScriptProperty]
		[Display(Name = "UseOpen0inVA05", Description = "Vérifier que Open[0] est compris entre STD05 Upper et STD05 Lower", Order = 1, GroupName = "0.7_Barre0_Property")]
		public bool UseOpen0inVA05 { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "UserBarre0FiltreWik", Description = "Filtrer la mèche haute de la barre 0", Order = 2, GroupName = "0.7_Barre0_Property")]
		public bool UserBarre0FiltreWik { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "FilterWickBarre0", Description = "Taille maximum de la mèche haute en ticks", Order = 3, GroupName = "0.7_Barre0_Property")]
		public int FilterWickBarre0 { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "UseBarre0FiltreVolume", Description = "Filtrer le volume de la barre 0", Order = 4, GroupName = "0.7_Barre0_Property")]
		public bool UseBarre0FiltreVolume { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "MinVolBarre0", Description = "Volume minimum pour la barre 0", Order = 5, GroupName = "0.7_Barre0_Property")]
		public int MinVolBarre0 { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "MaxVolBarre0", Description = "Volume maximum pour la barre 0", Order = 6, GroupName = "0.7_Barre0_Property")]
		public int MaxVolBarre0 { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "UseCrossVwapBarre0", Description = "Vérifier le croisement du VWAP par la barre 0", Order = 7, GroupName = "0.7_Barre0_Property")]
		public bool UseCrossVwapBarre0 { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "UseRejVwapBarre0", Description = "Vérifier le rejet du VWAP par la barre 0", Order = 8, GroupName = "0.7_Barre0_Property")]
		public bool UseRejVwapBarre0 { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "UseCrossStd1Barre0", Description = "Vérifier le croisement de STD1 par la barre 0", Order = 9, GroupName = "0.7_Barre0_Property")]
		public bool UseCrossStd1Barre0 { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Std1CrossOffsetTicksBarre0", Description = "Offset en ticks pour le niveau STD1", Order = 10, GroupName = "0.7_Barre0_Property")]
		public int Std1CrossOffsetTicksBarre0 { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "UseRejStd1Barre0", Description = "Vérifier le rejet de STD1 par la barre 0", Order = 11, GroupName = "0.7_Barre0_Property")]
		public bool UseRejStd1Barre0 { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Std1RejOffsetTicksBarre0", Description = "Offset en ticks pour le niveau STD1 (rejet)", Order = 12, GroupName = "0.7_Barre0_Property")]
		public int Std1RejOffsetTicksBarre0 { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "UseRejStd05Barre0", Description = "Vérifier le rejet de STD05 par la barre 0", Order = 13, GroupName = "0.7_Barre0_Property")]
		public bool UseRejStd05Barre0 { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Std05RejOffsetTicksBarre0", Description = "Offset en ticks pour le niveau STD05 (rejet)", Order = 14, GroupName = "0.7_Barre0_Property")]
		public int Std05RejOffsetTicksBarre0 { get; set; }

		// ############################ 0.7_Barre0_Property ####################################### //
		
		// ############################ 0.8_Barre1_Property ####################################### //
		
		[NinjaScriptProperty]
		[Display(Name = "UseOpen1inVA05", Description = "Vérifier que Open[1] est compris entre STD05 Upper et STD05 Lower", Order = 1, GroupName = "0.8_Barre1_Property")]
		public bool UseOpen1inVA05 { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "UseOpen1inVAstd1", Description = "Vérifier que Open[1] est compris entre STD1 Upper et STD1 Lower", Order = 2, GroupName = "0.8_Barre1_Property")]
		public bool UseOpen1inVAstd1 { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "UserBarre1FiltreWik", Description = "Filtrer la mèche de la barre 1", Order = 3, GroupName = "0.8_Barre1_Property")]
		public bool UserBarre1FiltreWik { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "FilterWickBarre1", Description = "Taille maximum de la mèche en ticks pour la barre 1", Order = 4, GroupName = "0.8_Barre1_Property")]
		public int FilterWickBarre1 { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "UseBarre1FiltreVolume", Description = "Filtrer le volume de la barre 1", Order = 5, GroupName = "0.8_Barre1_Property")]
		public bool UseBarre1FiltreVolume { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "MinVolBarre1", Description = "Volume minimum pour la barre 1", Order = 6, GroupName = "0.8_Barre1_Property")]
		public int MinVolBarre1 { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "MaxVolBarre1", Description = "Volume maximum pour la barre 1", Order = 7, GroupName = "0.8_Barre1_Property")]
		public int MaxVolBarre1 { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "UseBarre1FilterSizeOC", Description = "Filtrer la taille Open-Close de la barre 1", Order = 8, GroupName = "0.8_Barre1_Property")]
		public bool UseBarre1FilterSizeOC { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "FilterMinSizeBarre1OC", Description = "Taille minimum en ticks entre Open et Close pour la barre 1", Order = 9, GroupName = "0.8_Barre1_Property")]
		public int FilterMinSizeBarre1OC { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "FilterMaxSizeBarre1OC", Description = "Taille maximum en ticks entre Open et Close pour la barre 1", Order = 10, GroupName = "0.8_Barre1_Property")]
		public int FilterMaxSizeBarre1OC { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "UseCrossVwapBarre1", Description = "Vérifier le croisement du VWAP par la barre 1", Order = 11, GroupName = "0.8_Barre1_Property")]
		public bool UseCrossVwapBarre1 { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "UseRejVwapBarre1", Description = "Vérifier le rejet du VWAP par la barre 1", Order = 12, GroupName = "0.8_Barre1_Property")]
		public bool UseRejVwapBarre1 { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "UseCrossStd1Barre1", Description = "Vérifier le croisement de STD1 par la barre 1", Order = 13, GroupName = "0.8_Barre1_Property")]
		public bool UseCrossStd1Barre1 { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Std1CrossOffsetTicks", Description = "Offset en ticks pour le niveau STD1", Order = 14, GroupName = "0.8_Barre1_Property")]
		public int Std1CrossOffsetTicks { get; set; }
		
		// Ajout à la rubrique 0.8_Barre1_Property pour rejet STD1
		[NinjaScriptProperty]
		[Display(Name = "UseRejStd1Barre1", Description = "Vérifier le rejet de STD1 par la barre 1", Order = 15, GroupName = "0.8_Barre1_Property")]
		public bool UseRejStd1Barre1 { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Std1RejOffsetTicks", Description = "Offset en ticks pour le niveau STD1 (rejet)", Order = 16, GroupName = "0.8_Barre1_Property")]
		public int Std1RejOffsetTicks { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "UseRejStd05Barre1", Description = "Vérifier le rejet de STD05 par la barre 1", Order = 17, GroupName = "0.8_Barre1_Property")]
		public bool UseRejStd05Barre1 { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Std05RejOffsetTicksBarre1", Description = "Offset en ticks pour le niveau STD05 (rejet)", Order = 18, GroupName = "0.8_Barre1_Property")]
		public int Std05RejOffsetTicksBarre1 { get; set; }
	   
	   // ############################ 0.8_Barre1_Property ####################################### //
        // ################ Distance VWAP ####################### // 
        [NinjaScriptProperty]
        [Display(Name = "Enable Distance From VWAP Condition", Order = 1, GroupName = "1.01_Distance_VWAP")]
        public bool EnableDistanceFromVWAPCondition { get; set; }
        
        [Range(1, int.MaxValue)]
        [NinjaScriptProperty]
        [Display(Name = "Minimum Distance From VWAP (Ticks)", Order = 2, GroupName = "1.01_Distance_VWAP")]
        public int MinDistanceFromVWAP { get; set; }
        
        [Range(1, int.MaxValue)]
        [NinjaScriptProperty]
        [Display(Name = "Maximum Distance From VWAP (Ticks)", Order = 3, GroupName = "1.01_Distance_VWAP")]
        public int MaxDistanceFromVWAP { get; set; }
		
		// #################### 1.02_STD1_Range ################### //
		[NinjaScriptProperty]
		[Display(Name = "Enable STD1 Range Check", Description = "Enable checking for minimum/maximum range between STD1 Upper and Lower", Order = 1, GroupName = "1.02_STD1_Range")]
		public bool EnableSTD1RangeCheck { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Min STD1 Range (Ticks)", Description = "Minimum range between STD1 Upper and Lower in ticks", Order = 2, GroupName = "1.02_STD1_Range")]
		public int MinSTD1Range { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Max STD1 Range (Ticks)", Description = "Maximum range between STD1 Upper and Lower in ticks", Order = 3, GroupName = "1.02_STD1_Range")]
		public int MaxSTD1Range { get; set; }
		// ################ 1.03_STD3 Tracking ############# //
        [NinjaScriptProperty]
        [Display(Name="Enable STD3 High/Low Tracking", Description="Track highest STD3 Upper and lowest STD3 Lower since last reset", Order=1000, GroupName="1.03_STD3 Tracking")]
        public bool EnableSTD3HighLowTracking { get; set; }
		// ################## 1.04_Enable Previous Session RangeBreakout ########################
		[NinjaScriptProperty]
        [Display(Name = "Enable Previous Session Range Breakout", Description = "Enable checking for breakouts of the previous session's StdDev1 range", Order = 1, GroupName = "1.04_Enable Previous Session RangeBreakout")]
        public bool EnablePreviousSessionRangeBreakout { get; set; }
		
		//
		// Ajoutez ces propriétés dans la région Properties
		[NinjaScriptProperty]
		[Display(Name="Enable Initial Balance Logic", Description="Enable the Initial Balance logic", Order=1, GroupName="1.05_Initial Balance")]
		public bool EnableIBLogic { get; set; }
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="IB Start Time", Description="Start time of the Initial Balance period", Order=2, GroupName="1.05_Initial Balance")]
		public DateTime IBStartTime { get; set; }
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="IB End Time", Description="End time of the Initial Balance period", Order=3, GroupName="1.05_Initial Balance")]
		public DateTime IBEndTime { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="IB Offset Ticks", Description="Number of ticks to offset the IB levels", Order=4, GroupName="1.05_Initial Balance")]
		public int IBOffsetTicks { get; set; }
		
		// ######################### 1.6_Dynamic Area Parameters ########################################## //
		[NinjaScriptProperty]
		[Display(Name="Dynamic Area Level", Description="Choose which Standard Deviation level to use for Dynamic Area", Order=1, GroupName="1.6_Dynamic Area Parameters")]
		public DynamicAreaLevel SelectedDynamicArea { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Block Signals in Previous Dynamic Area", Description="Block signals when price is inside previous session's Dynamic Area", Order=2, GroupName="1.6_Dynamic Area Parameters")]
		public bool BlockSignalsInPreviousDynamicArea { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Dynamic Area Offset Ticks", Description="Offset en ticks pour la Dynamic Area précédente", Order=3, GroupName="1.6_Dynamic Area Parameters")]
		public int DynamicAreaOffsetTicks { get; set; }
		
		[NinjaScriptProperty]
        [Display(Name = "DynamicAreaDrawDelayMinutes", Order = 4, GroupName = "1.6_Dynamic Area Parameters")]
        public int DynamicAreaDrawDelayMinutes
        {
            get { return dynamicAreaDrawDelayMinutes; }
            set { dynamicAreaDrawDelayMinutes = value; }
        }
		
		// ######################## "Dot Plot Settings" #################################### //
		public enum DotPlotMode
		{
			STD1,
			STD2,
			BarSize,
			UseSTD1Multiple 
		}
		
		[NinjaScriptProperty]
		[Display(Name="Dot Plot Mode", Description="Choose the mode for plotting dots", Order=1, GroupName="Dot Plot Settings")]
		public DotPlotMode SelectedDotPlotMode { get; set; }
		
		public enum BarSizeType
		{
			CloseOpen,
			HighLow
		}
		
		// Ajoutez ces propriétés dans la région Properties
		[NinjaScriptProperty]
		[Display(Name="Bar Size Type", Description="Choose between Close-Open or High-Low for bar size calculation", Order=2, GroupName="Dot Plot Settings")]
		public BarSizeType SelectedBarSizeType { get; set; }
		
		[NinjaScriptProperty]
		[Range(0.1, 10.0)]
		[Display(Name="Red Dot Multiplier", Description="Multiplier for the red dot distance", Order=3, GroupName="Dot Plot Settings")]
		public double RedDotMultiplier { get; set; }
		
		[NinjaScriptProperty]
		[Range(0.1, 10.0)]
		[Display(Name="Blue Dot Multiplier", Description="Multiplier for the blue dot distance", Order=4, GroupName="Dot Plot Settings")]
		public double BlueDotMultiplier { get; set; }
		
		[NinjaScriptProperty]
		[Range(0.1, 10.0)]
		[Display(Name="STD1 Red Dot Multiplier", Description="Multiplier for the red dot distance when using STD1 Multiple mode", Order=5, GroupName="Dot Plot Settings")]
		public double STD1RedDotMultiplier { get; set; }
		
		[NinjaScriptProperty]
		[Range(0.1, 10.0)]
		[Display(Name="STD1 Blue Dot Multiplier", Description="Multiplier for the blue dot distance when using STD1 Multiple mode", Order=6, GroupName="Dot Plot Settings")]
		public double STD1BlueDotMultiplier { get; set; }

        // ############ Volume #############
        // Volume
        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Fperiod Vol", Order = 1, GroupName = "Volume")]
        public int FperiodVol { get; set; }
        
        [NinjaScriptProperty]
        [Range(0, 1)]
        [Display(Name = "OKisVOL", Description = "Check Volume", Order = 2, GroupName = "Volume")]
        public bool OKisVOL { get; set; }
		//
		[NinjaScriptProperty]
		[Display(Name="Use Volume S", Description="Active la comparaison avec le volume maximum de la période", Order=3, GroupName="Volume")]
		public bool UseVolumeS { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Enable Volume Analysis Period", Description="Active la période d'analyse du volume maximum", Order=4, GroupName="Volume")]
		public bool EnableVolumeAnalysisPeriod { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use Volume Increase", Description = "Enable volume increase check", Order = 5, GroupName = "Volume")]
		public bool UseVolumeIncrease { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Volume Bars to Compare", Description = "Number of previous bars to compare volume with", Order = 6, GroupName = "Volume")]
		[Range(1, 10)]
		public int VolumeBarsToCompare { get; set; }


		//
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> VWAP => Values[0];

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> StdDev0_5Upper => Values[1];

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> StdDev0_5Lower => Values[2];

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> StdDev1Upper => Values[3];

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> StdDev1Lower => Values[4];
		
		
		// ############################ Prior VA Vwap ######################################### //
		private OrderFlowVWAP vwap;
		private double priorSessionUpperBand;
		private double priorSessionLowerBand;
		private bool newSession;
		
		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Upper Offset Ticks", Description = "Number of ticks above upper band", Order=1, GroupName="Prior VA Vwap")]
		public int UpperOffsetTicks { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Lower Offset Ticks", Description = "Number of ticks below lower band", Order=2, GroupName="Prior VA Vwap")]
		public int LowerOffsetTicks { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name = "Use Prior SVA UP", Description = "Only up arrows when price above prior session VA", Order=3, GroupName="Prior VA Vwap")]
		public bool UsePriorSvaUP { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name = "Use Prior SVA Down", Description = "Only down arrows when price below prior session VA", Order=4, GroupName="Prior VA Vwap")]
		public bool UsePriorSvaDown { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name = "Block In Prior SVA", Description = "Block arrows inside prior session Value Area", Order=5, GroupName="Prior VA Vwap")]
		public bool BlockInPriorSVA { get; set; }
		// ############################ Prior VA Vwap ######################################### //
		
		// ############################ Slope Filter Properties ######################################### //
		[NinjaScriptProperty]
		[Range(1, 20)]
		[Display(Name = "Slope Start Bars", Description = "Number of bars ago to start slope calculation", Order = 1, GroupName = "Slope Filter")]
		public int SlopeStartBars { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 10)]
		[Display(Name = "Slope End Bars", Description = "Number of bars ago to end slope calculation", Order = 2, GroupName = "Slope Filter")]
		public int SlopeEndBars { get; set; }
		
		// Pour UP
		[NinjaScriptProperty]
		[Display(Name = "Use VWAP Session Slope Filter UP", GroupName = "Slope Filter")]
		public bool UseVwapSessioSlopeFilterUp { get; set; }
		
		[NinjaScriptProperty]
		[Range(0.0, 10.0)]
		[Display(Name = "Min VWAP Session Slope UP", Description = "Minimum VWAP session slope value for UP signals", GroupName = "Slope Filter")]
		public double MinVwapSessionSlopeUp { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use VWAP Reset Slope Filter UP", GroupName = "Slope Filter")]
		public bool UseVwapSlopeFilterUp { get; set; }
		
		[NinjaScriptProperty]
		[Range(0.0, 10.0)]
		[Display(Name = "Min VWAP Reset Slope UP", Description = "Minimum VWAP reset slope value for UP signals", GroupName = "Slope Filter")]
		public double MinVwapResetSlopeUp { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use STD1 Upper Slope Filter UP", GroupName = "Slope Filter")]
		public bool UseStdUpperSloperUP { get; set; }
		
		[NinjaScriptProperty]
		[Range(0.0, 10.0)]
		[Display(Name = "Min STD1 Upper Slope UP", Description = "Minimum STD1 Upper slope value for UP signals", GroupName = "Slope Filter")]
		public double MinStdUpperSlopeUp { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use STD1 Lower Slope Filter UP", GroupName = "Slope Filter")]
		public bool UseStdLowerUP { get; set; }
		
		[NinjaScriptProperty]
		[Range(0.0, 10.0)]
		[Display(Name = "Min STD1 Lower Slope UP", Description = "Minimum STD1 Lower slope value for UP signals", GroupName = "Slope Filter")]
		public double MinStdLowerSlopeUp { get; set; }
		
		// Pour DOWN
		[NinjaScriptProperty]
		[Display(Name = "Use VWAP Session Slope Filter DOWN", GroupName = "Slope Filter")]
		public bool UseVwapSessioSlopeFilterDown { get; set; }
		
		[NinjaScriptProperty]
		[Range(-10.0, 0.0)]
		[Display(Name = "Max VWAP Session Slope DOWN", Description = "Maximum VWAP session slope value for DOWN signals", GroupName = "Slope Filter")]
		public double MaxVwapSessionSlopeDown { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use VWAP Reset Slope Filter DOWN", GroupName = "Slope Filter")]
		public bool UseVwapSlopeFilterDown { get; set; }
		
		[NinjaScriptProperty]
		[Range(-10.0, 0.0)]
		[Display(Name = "Max VWAP Reset Slope DOWN", Description = "Maximum VWAP reset slope value for DOWN signals", GroupName = "Slope Filter")]
		public double MaxVwapResetSlopeDown { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use STD1 Upper Slope Filter DOWN", GroupName = "Slope Filter")]
		public bool UseStdUpperSloperDown { get; set; }
		
		[NinjaScriptProperty]
		[Range(-10.0, 0.0)]
		[Display(Name = "Max STD1 Upper Slope DOWN", Description = "Maximum STD1 Upper slope value for DOWN signals", GroupName = "Slope Filter")]
		public double MaxStdUpperSlopeDown { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use STD1 Lower Slope Filter DOWN", GroupName = "Slope Filter")]
		public bool UseStdLowerDown { get; set; }
		
		[NinjaScriptProperty]
		[Range(-10.0, 0.0)]
		[Display(Name = "Max STD1 Lower Slope DOWN", Description = "Maximum STD1 Lower slope value for DOWN signals", GroupName = "Slope Filter")]
		public double MaxStdLowerSlopeDown { get; set; }
		// ############################ Slope Filter Properties ######################################### //
		// ############################ Vague Filter ######################################### //
		private bool isAboveVwap = false; // Indique si le prix est au-dessus de la VWAP
		private bool isUpWaveActive = false;
		private double upWaveStart = 0;
		private double upWaveExtreme = 0;
		private bool upWaveValid = false;
		private int upWaveBarCount = 0;
		private bool isDownWaveActive = false;
		private double downWaveStart = 0;
		private double downWaveExtreme = 0;
		private bool downWaveValid = false;
		private int downWaveBarCount = 0;
		
		[NinjaScriptProperty]
		[Display(Name = "Use Vague 1 UP", Description = "Active la condition de vague 1 pour les signaux UP", Order = 1, GroupName = "Vague Filter")]
		public bool UseVague1UP { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use Vague 1 DOWN", Description = "Active la condition de vague 1 pour les signaux DOWN", Order = 2, GroupName = "Vague Filter")]
		public bool UseVague1DOWN { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Filtre Vague (Ticks)", Description = "Amplitude minimale de la vague en ticks", Order = 3, GroupName = "Vague Filter")]
		public int FilterVagueTicks { get; set; }

		// ############################ Vague Filter ######################################### //
		
		// ############################ Setup V3B UP ######################################### //
		[NinjaScriptProperty]
		[Display(Name = "Use SetupV3B", Description = "Activer le pattern de setup V3B pour les signaux UP", Order = 1, GroupName = "20.01_Setup V3B UP")]
		public bool UseSetupV3B { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use SetupV3BOC", Description = "Activer le pattern de setup V3B basé sur Open/Close", Order = 2, GroupName = "20.01_Setup V3B UP")]
		public bool UseSetupV3BOC { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use SetupV3BHL", Description = "Activer le pattern de setup V3B basé sur High/Low", Order = 3, GroupName = "20.01_Setup V3B UP")]
		public bool UseSetupV3BHL { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Ignore Condition1 V3BOC", Description = "Ignorer la condition1 (Open[2] > Close[2]) dans le pattern V3BOC", Order = 4, GroupName = "20.01_Setup V3B UP")]
		public bool IgnoreCondition1V3BOC { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Ignore Condition1 V3BHL", Description = "Ignorer la condition1 (Open[2] > Close[2]) dans le pattern V3BHL", Order = 5, GroupName = "20.01_Setup V3B UP")]
		public bool IgnoreCondition1V3BHL { get; set; }

		// ############################ Setup V3B UP ######################################### //
		// ############################ Setup V3B DOWN ######################################### //
		[NinjaScriptProperty]
		[Display(Name = "Use SetupV3BDown", Description = "Activer le pattern de setup V3B pour les signaux DOWN", Order = 1, GroupName = "20.02_Setup V3B DOWN")]
		public bool UseSetupV3BDown { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use SetupV3BOCDown", Description = "Activer le pattern de setup V3B basé sur Open/Close pour DOWN", Order = 2, GroupName = "20.02_Setup V3B DOWN")]
		public bool UseSetupV3BOCDown { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use SetupV3BHLDown", Description = "Activer le pattern de setup V3B basé sur High/Low pour DOWN", Order = 3, GroupName = "20.02_Setup V3B DOWN")]
		public bool UseSetupV3BHLDown { get; set; }
		// ############################ Setup V3B DOWN ######################################### //
		
		// ############################ Setup D3BHL UP ######################################### //
		[NinjaScriptProperty]
		[Display(Name = "Use Setup D3BHLup", Description = "Activer la combinaison des patterns D3BHLup1 et D3BHLup2 pour les signaux d'achat", Order = 1, GroupName = "21.01_Setup D3BHL UP")]
		public bool UseSetupD3BHLup { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use Setup D3BHLup1", Description = "Activer le pattern D3BHLup1 (High2>High1, Low2<Low1, Close0>High2)", Order = 2, GroupName = "21.01_Setup D3BHL UP")]
		public bool UseSetupD3BHLup1 { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use Setup D3BHLup2", Description = "Activer le pattern D3BHLup2 (High2<High1, Low2<Low1, Close0>High1)", Order = 3, GroupName = "21.01_Setup D3BHL UP")]
		public bool UseSetupD3BHLup2 { get; set; }
		// ############################ Setup D3BHL UP ######################################### //
		// ############################ Setup D3BHL DOWN ######################################### //
		[NinjaScriptProperty]
		[Display(Name = "Use Setup D3BHLdown", Description = "Activer la combinaison des patterns D3BHLdown1 et D3BHLdown2 pour les signaux de vente", Order = 4, GroupName = "21.02_Setup D3BHL DOWN")]
		public bool UseSetupD3BHLdown { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use Setup D3BHLdown1", Description = "Activer le pattern D3BHLdown1 (Low2<Low1, High2>High1, Close0<Low2)", Order = 5, GroupName = "21.02_Setup D3BHL DOWN")]
		public bool UseSetupD3BHLdown1 { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use Setup D3BHLdown2", Description = "Activer le pattern D3BHLdown2 (Low2>Low1, High2>High1, Close0<Low1)", Order = 6, GroupName = "21.02_Setup D3BHL DOWN")]
		public bool UseSetupD3BHLdown2 { get; set; }
		// ############################ Setup D3BHL DOWN ######################################### //
		// ############################ 3-Bar Breakout ######################################### //
		[NinjaScriptProperty]
		[Display(Name = "Utiliser 3-Bar Breakout UP", Order = 1, GroupName = "22.01_Bar Breakout")]
		public bool UseThreeBarBreakoutUp { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Utiliser 3-Bar Breakout DOWN", Order = 2, GroupName = "22.01_Bar Breakout")]
		public bool UseThreeBarBreakoutDown { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use Inside B0B1 Pattern", Description = "Vérifier si les barres 0 et 1 forment un pattern de barre intérieure", Order = 3, GroupName = "22.01_Bar Breakout")]
		public bool UseInsideB0B1 { get; set; }
		// ############################ 3-Bar Breakout ######################################### //
		
		// ############################ Setup ALPHAUP ######################################### //
		[NinjaScriptProperty]
		[Display(Name = "Use SetupALPHAUP", Description = "Activer la combinaison des patterns V3B, V3BV2, U4BUP ou N4BUP pour les signaux d'achat", Order = 1, GroupName = "23.01_Setup ALPHAUP")]
		public bool UseSetupALPHAUP { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "ALPHAUP: V3BOC", Description = "Activer le pattern V3BOC dans ALPHAUP", Order = 2, GroupName = "23.01_Setup ALPHAUP")]
		public bool AlphaUseV3BOC { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "ALPHAUP: V3BHL", Description = "Activer le pattern V3BHL dans ALPHAUP", Order = 3, GroupName = "23.01_Setup ALPHAUP")]
		public bool AlphaUseV3BHL { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "ALPHAUP: D3BHLup1", Description = "Activer le pattern D3BHLup1 dans ALPHAUP", Order = 4, GroupName = "23.01_Setup ALPHAUP")]
		public bool AlphaUseD3BHLup1 { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "ALPHAUP: D3BHLup2", Description = "Activer le pattern D3BHLup2 dans ALPHAUP", Order = 5, GroupName = "23.01_Setup ALPHAUP")]
		public bool AlphaUseD3BHLup2 { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "ALPHAUP: ThreeBarBreakout", Description = "Activer le pattern ThreeBarBreakout dans ALPHAUP", Order = 6, GroupName = "23.01_Setup ALPHAUP")]
		public bool AlphaUseThreeBarBreakout { get; set; }
		// ############################ Setup ALPHAUP ######################################### //
		// ############################ Setup ALPHADOWN ######################################### //
		[NinjaScriptProperty]
		[Display(Name = "Use SetupALPHADOWN", Description = "Activer la combinaison des patterns pour les signaux de vente", Order = 1, GroupName = "23.02_Setup ALPHADOWN")]
		public bool UseSetupALPHADOWN { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "ALPHADOWN: V3BOCDown", Description = "Activer le pattern V3BOCDown dans ALPHADOWN", Order = 2, GroupName = "23.02_Setup ALPHADOWN")]
		public bool AlphaUseV3BOCDown { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "ALPHADOWN: V3BHLDown", Description = "Activer le pattern V3BHLDown dans ALPHADOWN", Order = 3, GroupName = "23.02_Setup ALPHADOWN")]
		public bool AlphaUseV3BHLDown { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "ALPHADOWN: D3BHLdown1", Description = "Activer le pattern D3BHLdown1 dans ALPHADOWN", Order = 4, GroupName = "23.02_Setup ALPHADOWN")]
		public bool AlphaUseD3BHLdown1 { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "ALPHADOWN: D3BHLdown2", Description = "Activer le pattern D3BHLdown2 dans ALPHADOWN", Order = 5, GroupName = "23.02_Setup ALPHADOWN")]
		public bool AlphaUseD3BHLdown2 { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "ALPHADOWN: ThreeBarBreakoutDown", Description = "Activer le pattern ThreeBarBreakoutDown dans ALPHADOWN", Order = 6, GroupName = "23.02_Setup ALPHADOWN")]
		public bool AlphaUseThreeBarBreakoutDown { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "ALPHAUP: U4Bup", Description = "Activer le pattern U4Bup dans ALPHAUP", Order = 7, GroupName = "23.01_Setup ALPHAUP")]
		public bool AlphaUseU4Bup { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "ALPHADOWN: U4BDown", Description = "Activer le pattern U4BDown dans ALPHADOWN", Order = 7, GroupName = "23.02_Setup ALPHADOWN")]
		public bool AlphaUseU4BDown { get; set; }

		// ############################ Setup ALPHADOWN ######################################### //
		// ############################ Setup UseSetupU4B ######################################### //
		[NinjaScriptProperty]
		[Display(Name = "Use Setup U4Bup", Description = "Activer le setup U4B pour les signaux UP", Order = 1, GroupName = "24.01_Setup U4B UP")]
		public bool UseSetupU4Bup { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use Setup U4BOCup", Description = "Activer le pattern U4B basé sur Open/Close pour UP", Order = 2, GroupName = "24.01_Setup U4B UP")]
		public bool UseSetupU4BOCup { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use Setup U4BHLup", Description = "Activer le pattern U4B basé sur High/Low pour UP", Order = 3, GroupName = "24.01_Setup U4B UP")]
		public bool UseSetupU4BHLup { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use Setup U4BDown", Description = "Activer le setup U4B pour les signaux DOWN", Order = 1, GroupName = "24.02_Setup U4B DOWN")]
		public bool UseSetupU4BDown { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use Setup U4BOCDown", Description = "Activer le pattern U4B basé sur Open/Close pour DOWN", Order = 2, GroupName = "24.02_Setup U4B DOWN")]
		public bool UseSetupU4BOCDown { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use Setup U4BHLDown", Description = "Activer le pattern U4B basé sur High/Low pour DOWN", Order = 3, GroupName = "24.02_Setup U4B DOWN")]
		public bool UseSetupU4BHLDown { get; set; }
		
		// ############################ Setup UseSetupU4B ######################################### //
		
        #endregion
    }
}