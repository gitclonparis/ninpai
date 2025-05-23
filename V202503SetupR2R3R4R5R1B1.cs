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
    public class V202503SetupR2R3R4R5R1B1 : Indicator
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
                Name = "V202503SetupR2R3R4R5R1B1";
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
				
				UseOpen0InfVwap = false;
				UseLow0InfVwap = false;
				UseBarre1CroiseVwapUp = false;
				UseBarre1RejVwapUp = false;
				UseOpen0SupVwap = false;
				UseHigh0SupVwap = false;
				UseBarre1CroiseVwapDown = false;
				UseBarre1RejVwapDown = false;
                
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
				
				EnableDownBreakoutCheck = false;
				DownBreakoutBars = 2;
				DownBreakoutOffsetTicks = 1; // Doit être >= 1
				
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
				// B-R
				EnableSetupR2UP = false;
				SetupR2UPBarsToCheck = 3;
				SetupR2UPMinPicTicks = 25;
				EnableSetupR2DOWN = false;
				SetupR2DOWNBarsToCheck = 3;
				SetupR2DOWNMinPicTicks = 25;
				
				EnableSetupR3UP = false;
				SetupR3UPBarsToCheck = 3;
				SetupR3UPMinPicTicks = 25;
				EnableSetupR3DOWN = false;
				SetupR3DOWNBarsToCheck = 3;
				SetupR3DOWNMinPicTicks = 25;
				
				EnableSetupR4UP = false;
				SetupR4UPBarsToCheck = 3;
				SetupR4UPMinPicTicks = 25;
				EnableSetupR4DOWN = false;
				SetupR4DOWNBarsToCheck = 3;
				SetupR4DOWNMinPicTicks = 25;
				
				EnableSetupR5UP = false;
				SetupR5UPBarsToCheck = 3;
				SetupR5UPMinPicTicks = 25;
				EnableSetupR5DOWN = false;
				SetupR5DOWNBarsToCheck = 3;
				SetupR5DOWNMinPicTicks = 25;
				
				EnableSetupR1UP = false;
				SetupR1UPBarsToCheck = 3;
				SetupR1UPMinPicTicks = 25;
				SetupR1UPMaxBarSizeTicks = 5;
				EnableSetupR1DOWN = false;
				SetupR1DOWNBarsToCheck = 3;
				SetupR1DOWNMinPicTicks = 25;
				SetupR1DOWNMaxBarSizeTicks = 5;
				
				EnableSetupB1UP = false;
				SetupB1UPMaxBarSizeTicks = 3;
				SetupB1UPMinBar0SizeTicks = 15;
				EnableSetupB1DOWN = false;
				SetupB1DOWNMaxBarSizeTicks = 3;
				SetupB1DOWNMinBar0SizeTicks = 15;
				
				UseVague1UP = false;
				UseVague1DOWN = false;
				FilterVagueTicks = 50;
				
				UseThreeBarBreakoutUp = false;
				UseThreeBarBreakoutDown = false;
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
		
		// ############################################## SetupR2 ################################################################# //
		private bool CheckSetupR2UP()
		{
			if (!EnableSetupR2UP)
				return true; // Si la fonctionnalité n'est pas activée, ne pas affecter les conditions existantes
			
			// Condition 1: Vérifier les Highs des barres précédentes
			bool foundHighsAboveSTD1Upper = false;
			bool foundPic = false;
			
			// Limiter le nombre de barres à vérifier à ce qui est disponible
			int barsToCheck = Math.Min(SetupR2UPBarsToCheck, CurrentBar);
			
			for (int i = 1; i <= barsToCheck; i++)
			{
				// STD1Upper de la barre précédente
				double std1UpperPrev = Values[3][i]; // STD1Upper
				
				// Si le High est supérieur au STD1Upper
				if (High[i] > std1UpperPrev)
				{
					foundHighsAboveSTD1Upper = true;
					
					// Vérifier si ce High forme un pic suffisamment haut au-dessus de STD1Upper
					double picTicks = (High[i] - std1UpperPrev) / TickSize;
					if (picTicks >= SetupR2UPMinPicTicks)
					{
						foundPic = true;
					}
				}
			}
			
			// Condition 2: Low[1] doit être inférieur à STD1Upper
			bool low1BelowSTD1Upper = Low[1] < Values[3][1]; // STD1Upper de la barre précédente
			
			// Condition 3: Low[0] et High[0] doivent être supérieurs à STD1Upper et Close[0] > Open[0]
			bool currentBarCondition = Low[0] > Values[3][0] && High[0] > Values[3][0] && Close[0] > Open[0];
			
			// Toutes les conditions doivent être satisfaites
			return foundHighsAboveSTD1Upper && foundPic && low1BelowSTD1Upper && currentBarCondition;
		}
		
		private bool CheckSetupR2DOWN()
		{
			if (!EnableSetupR2DOWN)
				return true; // Si la fonctionnalité n'est pas activée, ne pas affecter les conditions existantes
			
			// Condition 1: Vérifier les Lows des barres précédentes
			bool foundLowsBelowSTD1Lower = false;
			bool foundPic = false;
			
			// Limiter le nombre de barres à vérifier à ce qui est disponible
			int barsToCheck = Math.Min(SetupR2DOWNBarsToCheck, CurrentBar);
			
			for (int i = 1; i <= barsToCheck; i++)
			{
				// STD1Lower de la barre précédente
				double std1LowerPrev = Values[4][i]; // STD1Lower
				
				// Si le Low est inférieur au STD1Lower
				if (Low[i] < std1LowerPrev)
				{
					foundLowsBelowSTD1Lower = true;
					
					// Vérifier si ce Low forme un pic suffisamment bas en-dessous de STD1Lower
					double picTicks = (std1LowerPrev - Low[i]) / TickSize;
					if (picTicks >= SetupR2DOWNMinPicTicks)
					{
						foundPic = true;
					}
				}
			}
			
			// Condition 2: High[1] doit être supérieur à STD1Lower
			bool high1AboveSTD1Lower = High[1] > Values[4][1]; // STD1Lower de la barre précédente
			
			// Condition 3: Low[0] et High[0] doivent être inférieurs à STD1Lower et Close[0] < Open[0]
			bool currentBarCondition = Low[0] < Values[4][0] && High[0] < Values[4][0] && Close[0] < Open[0];
			
			// Toutes les conditions doivent être satisfaites
			return foundLowsBelowSTD1Lower && foundPic && high1AboveSTD1Lower && currentBarCondition;
		}
		
		// ############################################## SetupR2 ################################################################# //
		// ############################################## SetupR3 ################################################################# //
		private bool CheckSetupR3UP()
		{
			if (!EnableSetupR3UP)
				return true; // Si la fonctionnalité n'est pas activée, ne pas affecter les conditions existantes
			
			// Condition 1: Vérifier les Highs des barres précédentes
			bool foundHighsAboveSTD1Upper = false;
			bool foundPic = false;
			
			// Limiter le nombre de barres à vérifier à ce qui est disponible
			int barsToCheck = Math.Min(SetupR3UPBarsToCheck, CurrentBar);
			
			for (int i = 1; i <= barsToCheck; i++)
			{
				// STD1Upper de la barre précédente
				double std1UpperPrev = Values[3][i]; // STD1Upper
				
				// Si le High est supérieur au STD1Upper
				if (High[i] > std1UpperPrev)
				{
					foundHighsAboveSTD1Upper = true;
					
					// Vérifier si ce High forme un pic suffisamment haut au-dessus de STD1Upper
					double picTicks = (High[i] - std1UpperPrev) / TickSize;
					if (picTicks >= SetupR3UPMinPicTicks)
					{
						foundPic = true;
					}
				}
			}
			
			// Condition 2: Low[1] doit être inférieur à STD1Upper ET Close[1] < Open[1]
			bool bar1Condition = Low[1] < Values[3][1] && Close[1] < Open[1]; // STD1Upper de la barre précédente
			
			// Condition 3: Le prix resortant plus haut que STD1Upper et Close[0] > Open[0]
			// Note: Low[0] n'est pas obligatoirement supérieur à STD1Upper comme dans SetupR2UP
			bool currentBarCondition = Close[0] > Values[3][0] && Close[0] > Open[0];
			
			// Toutes les conditions doivent être satisfaites
			return foundHighsAboveSTD1Upper && foundPic && bar1Condition && currentBarCondition;
		}
		
		// Méthode pour vérifier les conditions du SetupR3DOWN
		private bool CheckSetupR3DOWN()
		{
			if (!EnableSetupR3DOWN)
				return true; // Si la fonctionnalité n'est pas activée, ne pas affecter les conditions existantes
			
			// Condition 1: Vérifier les Lows des barres précédentes
			bool foundLowsBelowSTD1Lower = false;
			bool foundPic = false;
			
			// Limiter le nombre de barres à vérifier à ce qui est disponible
			int barsToCheck = Math.Min(SetupR3DOWNBarsToCheck, CurrentBar);
			
			for (int i = 1; i <= barsToCheck; i++)
			{
				// STD1Lower de la barre précédente
				double std1LowerPrev = Values[4][i]; // STD1Lower
				
				// Si le Low est inférieur au STD1Lower
				if (Low[i] < std1LowerPrev)
				{
					foundLowsBelowSTD1Lower = true;
					
					// Vérifier si ce Low forme un pic suffisamment bas en-dessous de STD1Lower
					double picTicks = (std1LowerPrev - Low[i]) / TickSize;
					if (picTicks >= SetupR3DOWNMinPicTicks)
					{
						foundPic = true;
					}
				}
			}
			
			// Condition 2: High[1] doit être supérieur à STD1Lower ET Close[1] > Open[1]
			bool bar1Condition = High[1] > Values[4][1] && Close[1] > Open[1]; // STD1Lower de la barre précédente
			
			// Condition 3: Le prix resortant plus bas que STD1Lower et Close[0] < Open[0]
			// Note: High[0] n'est pas obligatoirement inférieur à STD1Lower comme dans SetupR2DOWN
			bool currentBarCondition = Close[0] < Values[4][0] && Close[0] < Open[0];
			
			// Toutes les conditions doivent être satisfaites
			return foundLowsBelowSTD1Lower && foundPic && bar1Condition && currentBarCondition;
		}
		// ############################################## SetupR3 ################################################################# //
		// ############################################## SetupR4 ################################################################# //
		private bool CheckSetupR4UP()
		{
			if (!EnableSetupR4UP)
				return true; // Si la fonctionnalité n'est pas activée, ne pas affecter les conditions existantes
			
			// Condition 1: Vérifier les Highs des barres précédentes
			bool foundHighsAboveSTD1Upper = false;
			bool foundPic = false;
			
			// Limiter le nombre de barres à vérifier à ce qui est disponible
			int barsToCheck = Math.Min(SetupR4UPBarsToCheck, CurrentBar);
			
			for (int i = 1; i <= barsToCheck; i++)
			{
				// STD1Upper de la barre précédente
				double std1UpperPrev = Values[3][i]; // STD1Upper
				
				// Si le High est supérieur au STD1Upper
				if (High[i] > std1UpperPrev)
				{
					foundHighsAboveSTD1Upper = true;
					
					// Vérifier si ce High forme un pic suffisamment haut au-dessus de STD1Upper
					double picTicks = (High[i] - std1UpperPrev) / TickSize;
					if (picTicks >= SetupR4UPMinPicTicks)
					{
						foundPic = true;
					}
				}
			}
			
			// Condition 2: Pour le SetupR4UP, le prix entre puis ressort au-dessus de STD1Upper
			// Low[0] < STD1Upper (entre sous le niveau) mais Close[0] > STD1Upper (ressort au-dessus)
			bool priceReentry = Low[0] < Values[3][0] && Close[0] > Values[3][0];
			
			// Toutes les conditions doivent être satisfaites
			return foundHighsAboveSTD1Upper && foundPic && priceReentry;
		}
		
		// Méthode pour vérifier les conditions du SetupR4DOWN
		private bool CheckSetupR4DOWN()
		{
			if (!EnableSetupR4DOWN)
				return true; // Si la fonctionnalité n'est pas activée, ne pas affecter les conditions existantes
			
			// Condition 1: Vérifier les Lows des barres précédentes
			bool foundLowsBelowSTD1Lower = false;
			bool foundPic = false;
			
			// Limiter le nombre de barres à vérifier à ce qui est disponible
			int barsToCheck = Math.Min(SetupR4DOWNBarsToCheck, CurrentBar);
			
			for (int i = 1; i <= barsToCheck; i++)
			{
				// STD1Lower de la barre précédente
				double std1LowerPrev = Values[4][i]; // STD1Lower
				
				// Si le Low est inférieur au STD1Lower
				if (Low[i] < std1LowerPrev)
				{
					foundLowsBelowSTD1Lower = true;
					
					// Vérifier si ce Low forme un pic suffisamment bas en-dessous de STD1Lower
					double picTicks = (std1LowerPrev - Low[i]) / TickSize;
					if (picTicks >= SetupR4DOWNMinPicTicks)
					{
						foundPic = true;
					}
				}
			}
			
			// Condition 2: Pour le SetupR4DOWN, le prix entre puis ressort en-dessous de STD1Lower
			// High[0] > STD1Lower (entre au-dessus du niveau) mais Close[0] < STD1Lower (ressort en-dessous)
			bool priceReentry = High[0] > Values[4][0] && Close[0] < Values[4][0];
			
			// Toutes les conditions doivent être satisfaites
			return foundLowsBelowSTD1Lower && foundPic && priceReentry;
		}
		
		
		// ############################################## SetupR4 ################################################################# //
		// ############################################## SetupR5 ################################################################# //
		private bool CheckSetupR5UP()
		{
			if (!EnableSetupR5UP)
				return true; // Si la fonctionnalité n'est pas activée, ne pas affecter les conditions existantes
			
			// Condition 1: Vérifier les Highs des barres précédentes
			bool foundHighsAboveSTD1Upper = false;
			bool foundPic = false;
			
			// Limiter le nombre de barres à vérifier à ce qui est disponible
			int barsToCheck = Math.Min(SetupR5UPBarsToCheck, CurrentBar);
			
			for (int i = 1; i <= barsToCheck; i++)
			{
				// STD1Upper de la barre précédente
				double std1UpperPrev = Values[3][i]; // STD1Upper
				
				// Si le High est supérieur au STD1Upper
				if (High[i] > std1UpperPrev)
				{
					foundHighsAboveSTD1Upper = true;
					
					// Vérifier si ce High forme un pic suffisamment haut au-dessus de STD1Upper
					double picTicks = (High[i] - std1UpperPrev) / TickSize;
					if (picTicks >= SetupR5UPMinPicTicks)
					{
						foundPic = true;
					}
				}
			}
			
			// Condition 2: Close[1] < Open[1]
			bool bar1Condition = Close[1] < Open[1];
			
			// Condition 3: Le prix rentre et ressort plus haut que STD1Upper
			// Low[0] < STD1Upper (entre sous le niveau) ET Close[0] > STD1Upper (ressort au-dessus) ET Close[0] > Open[0]
			bool currentBarCondition = Low[0] < Values[3][0] && Close[0] > Values[3][0] && Close[0] > Open[0];
			
			// Toutes les conditions doivent être satisfaites
			return foundHighsAboveSTD1Upper && foundPic && bar1Condition && currentBarCondition;
		}
		
		private bool CheckSetupR5DOWN()
		{
			if (!EnableSetupR5DOWN)
				return true; // Si la fonctionnalité n'est pas activée, ne pas affecter les conditions existantes
			
			// Condition 1: Vérifier les Lows des barres précédentes
			bool foundLowsBelowSTD1Lower = false;
			bool foundPic = false;
			
			// Limiter le nombre de barres à vérifier à ce qui est disponible
			int barsToCheck = Math.Min(SetupR5DOWNBarsToCheck, CurrentBar);
			
			for (int i = 1; i <= barsToCheck; i++)
			{
				// STD1Lower de la barre précédente
				double std1LowerPrev = Values[4][i]; // STD1Lower
				
				// Si le Low est inférieur au STD1Lower
				if (Low[i] < std1LowerPrev)
				{
					foundLowsBelowSTD1Lower = true;
					
					// Vérifier si ce Low forme un pic suffisamment bas en-dessous de STD1Lower
					double picTicks = (std1LowerPrev - Low[i]) / TickSize;
					if (picTicks >= SetupR5DOWNMinPicTicks)
					{
						foundPic = true;
					}
				}
			}
			
			// Condition 2: Close[1] > Open[1]
			bool bar1Condition = Close[1] > Open[1];
			
			// Condition 3: Le prix rentre et ressort plus bas que STD1Lower
			// High[0] > STD1Lower (entre au-dessus du niveau) ET Close[0] < STD1Lower (ressort en-dessous) ET Close[0] < Open[0]
			bool currentBarCondition = High[0] > Values[4][0] && Close[0] < Values[4][0] && Close[0] < Open[0];
			
			// Toutes les conditions doivent être satisfaites
			return foundLowsBelowSTD1Lower && foundPic && bar1Condition && currentBarCondition;
		}
		
		// ############################################## SetupR5 ################################################################# //
		// ############################################## SetupR1 ################################################################# //
		private bool CheckSetupR1UP()
		{
			if (!EnableSetupR1UP)
				return true; // Si la fonctionnalité n'est pas activée, ne pas affecter les conditions existantes
			
			// Condition 1: Vérifier que au moins une des barres précédentes a un High > STD1Upper
			bool foundHighsAboveSTD1Upper = false;
			bool foundPic = false;
			
			// Limiter le nombre de barres à vérifier à ce qui est disponible
			int barsToCheck = Math.Min(SetupR1UPBarsToCheck, CurrentBar);
			
			for (int i = 1; i <= barsToCheck; i++)
			{
				// STD1Upper de la barre précédente
				double std1UpperPrev = Values[3][i]; // STD1Upper
				
				// Si le High est supérieur au STD1Upper
				if (High[i] > std1UpperPrev)
				{
					foundHighsAboveSTD1Upper = true;
					
					// Vérifier si ce High forme un pic suffisamment haut au-dessus de STD1Upper
					double picTicks = (High[i] - std1UpperPrev) / TickSize;
					if (picTicks >= SetupR1UPMinPicTicks)
					{
						foundPic = true;
					}
				}
			}
			
			// Condition 2: Barre1 - Close1 et Open1 inférieurs à STD1Upper et supérieurs à STD1Lower
			// avec une petite taille de barre (différence Close1-Open1 < max ticks)
			double std1Upper = Values[3][1]; // STD1Upper pour barre1
			double std1Lower = Values[4][1]; // STD1Lower pour barre1
			
			bool bar1Condition = Close[1] < std1Upper && 
								Open[1] < std1Upper && 
								Close[1] > std1Lower && 
								Open[1] > std1Lower &&
								Math.Abs(Close[1] - Open[1]) / TickSize < SetupR1UPMaxBarSizeTicks;
			
			// Condition 3: Le Open0 inférieur à STD05Upper, Close0 supérieur à STD1Upper et Close0 supérieur à High1
			double std05Upper = Values[1][0]; // STD05Upper pour barre0
			double std1UpperCurrent = Values[3][0]; // STD1Upper pour barre0
			
			bool currentBarCondition = //Open[0] < std05Upper && 
									Close[0] > std1UpperCurrent && 
									Close[0] > High[1];
			
			// Toutes les conditions doivent être satisfaites
			return foundHighsAboveSTD1Upper && foundPic && bar1Condition && currentBarCondition;
		}
		
		private bool CheckSetupR1DOWN()
		{
			if (!EnableSetupR1DOWN)
				return true; // Si la fonctionnalité n'est pas activée, ne pas affecter les conditions existantes
			
			// Condition 1: Vérifier que au moins une des barres précédentes a un Low < STD1Lower
			bool foundLowsBelowSTD1Lower = false;
			bool foundPic = false;
			
			// Limiter le nombre de barres à vérifier à ce qui est disponible
			int barsToCheck = Math.Min(SetupR1DOWNBarsToCheck, CurrentBar);
			
			for (int i = 1; i <= barsToCheck; i++)
			{
				// STD1Lower de la barre précédente
				double std1LowerPrev = Values[4][i]; // STD1Lower
				
				// Si le Low est inférieur au STD1Lower
				if (Low[i] < std1LowerPrev)
				{
					foundLowsBelowSTD1Lower = true;
					
					// Vérifier si ce Low forme un pic suffisamment bas en-dessous de STD1Lower
					double picTicks = (std1LowerPrev - Low[i]) / TickSize;
					if (picTicks >= SetupR1DOWNMinPicTicks)
					{
						foundPic = true;
					}
				}
			}
			
			// Condition 2: Barre1 - Close1 et Open1 supérieurs à STD1Lower et inférieurs à STD1Upper
			// avec une petite taille de barre (différence Close1-Open1 < max ticks)
			double std1Upper = Values[3][1]; // STD1Upper pour barre1
			double std1Lower = Values[4][1]; // STD1Lower pour barre1
			
			bool bar1Condition = Close[1] > std1Lower && 
								Open[1] > std1Lower && 
								Close[1] < std1Upper && 
								Open[1] < std1Upper &&
								Math.Abs(Close[1] - Open[1]) / TickSize < SetupR1DOWNMaxBarSizeTicks;
			
			// Condition 3: Le Open0 supérieur à STD05Lower, Close0 inférieur à STD1Lower et Close0 inférieur à Low1
			double std05Lower = Values[2][0]; // STD05Lower pour barre0
			double std1LowerCurrent = Values[4][0]; // STD1Lower pour barre0
			
			bool currentBarCondition = //Open[0] > std05Lower && 
									Close[0] < std1LowerCurrent && 
									Close[0] < Low[1];
			
			// Toutes les conditions doivent être satisfaites
			return foundLowsBelowSTD1Lower && foundPic && bar1Condition && currentBarCondition;
		}
		
		// ############################################## SetupR1 ################################################################# //
		// ############################################## SetupB1 ################################################################# //
		private bool CheckSetupB1UP()
		{
			if (!EnableSetupB1UP)
				return true; // Si la fonctionnalité n'est pas activée, ne pas affecter les conditions existantes
			
			// Condition 1: Barre1 - Close1 et Open1 inférieurs à STD1Upper et supérieurs à STD1Lower
			// avec une petite taille de barre (différence Close1-Open1 < max ticks)
			double std1Upper = Values[3][1]; // STD1Upper pour barre1
			double std1Lower = Values[4][1]; // STD1Lower pour barre1
			
			bool bar1Condition = Close[1] < std1Upper && 
								Open[1] < std1Upper && 
								Close[1] > std1Lower && 
								Open[1] > std1Lower &&
								Math.Abs(Close[1] - Open[1]) / TickSize < SetupB1UPMaxBarSizeTicks;
			
			// Condition 2: Le Open0 inférieur à STD05Upper, Close0 supérieur à STD1Upper et Close0 supérieur à High1
			double std05Upper = Values[1][0]; // STD05Upper pour barre0
			double std1UpperCurrent = Values[3][0]; // STD1Upper pour barre0
			
			// Ajout du filtre sur la taille de la barre0 (Close0 - Open0)
			double bar0Size = (Close[0] - Open[0]) / TickSize;
			
			bool currentBarCondition = Open[0] < std05Upper && 
									Close[0] > std1UpperCurrent && 
									Close[0] > High[1] &&
									bar0Size >= SetupB1UPMinBar0SizeTicks; // Nouvelle condition pour la taille de barre0
			
			// Toutes les conditions doivent être satisfaites
			return bar1Condition && currentBarCondition;
		}
		
		private bool CheckSetupB1DOWN()
		{
			if (!EnableSetupB1DOWN)
				return true; // Si la fonctionnalité n'est pas activée, ne pas affecter les conditions existantes
			
			// Condition 1: Barre1 - Close1 et Open1 supérieurs à STD1Lower et inférieurs à STD1Upper
			// avec une petite taille de barre (différence Close1-Open1 < max ticks)
			double std1Upper = Values[3][1]; // STD1Upper pour barre1
			double std1Lower = Values[4][1]; // STD1Lower pour barre1
			
			bool bar1Condition = Close[1] > std1Lower && 
								Open[1] > std1Lower && 
								Close[1] < std1Upper && 
								Open[1] < std1Upper &&
								Math.Abs(Close[1] - Open[1]) / TickSize < SetupB1DOWNMaxBarSizeTicks;
			
			// Condition 2: Le Open0 supérieur à STD05Lower, Close0 inférieur à STD1Lower et Close0 inférieur à Low1
			double std05Lower = Values[2][0]; // STD05Lower pour barre0
			double std1LowerCurrent = Values[4][0]; // STD1Lower pour barre0
			
			// Ajout du filtre sur la taille de la barre0 (Open0 - Close0)
			double bar0Size = (Open[0] - Close[0]) / TickSize;
			
			bool currentBarCondition = Open[0] > std05Lower && 
									Close[0] < std1LowerCurrent && 
									Close[0] < Low[1] &&
									bar0Size >= SetupB1DOWNMinBar0SizeTicks; // Nouvelle condition pour la taille de barre0
			
			// Toutes les conditions doivent être satisfaites
			return bar1Condition && currentBarCondition;
		}
		
		// ############################################## SetupB1 ################################################################# //
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
		
		// ############################################## UseThreeBarBreakout ################################################################# //
		
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
				(!UseOpen0InfVwap || Open[0] < Values[0][0]) && // Open[0] inférieur à VWAP
				(!UseLow0InfVwap || Low[0] < Values[0][0]) && // Low[0] inférieur à VWAP
				(!UseBarre1CroiseVwapUp || (Open[1] < Values[0][1] && Close[1] > Values[0][1])) && 
				(!UseBarre1RejVwapUp || (Open[1] > Values[0][1] && Low[1] < Values[0][1] && Close[1] > Values[0][1])) &&
				// (!UsePrevBarInVA || (Open[1] > dynamicLowerThreshold && Open[1] < dynamicUpperThreshold)) &&
				(!UseVague1UP || (isAboveVwap && upWaveValid)) &&
				(!UseThreeBarBreakoutUp || IsThreeBarUpBreakout()) &&
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
			
			//
			if (EnableSetupR2UP || EnableSetupR3UP || EnableSetupR4UP || EnableSetupR5UP || EnableSetupR1UP || EnableSetupB1UP)
			{
				// Vérifier si au moins une des six conditions est remplie
				bool setupR2Passed = !EnableSetupR2UP || CheckSetupR2UP();
				bool setupR3Passed = !EnableSetupR3UP || CheckSetupR3UP();
				bool setupR4Passed = !EnableSetupR4UP || CheckSetupR4UP();
				bool setupR5Passed = !EnableSetupR5UP || CheckSetupR5UP();
				bool setupR1Passed = !EnableSetupR1UP || CheckSetupR1UP();
				bool setupB1Passed = !EnableSetupB1UP || CheckSetupB1UP();
				
				// Si au moins une des conditions est activée, il suffit qu'une soit vraie pour passer
				if (EnableSetupR2UP || EnableSetupR3UP || EnableSetupR4UP || EnableSetupR5UP || EnableSetupR1UP || EnableSetupB1UP)
				{
					// Vérifie si au moins une des conditions activées est satisfaite
					bool anyPassed = (EnableSetupR2UP && setupR2Passed) || 
									(EnableSetupR3UP && setupR3Passed) || 
									(EnableSetupR4UP && setupR4Passed) ||
									(EnableSetupR5UP && setupR5Passed) ||
									(EnableSetupR1UP && setupR1Passed) ||
									(EnableSetupB1UP && setupB1Passed);
					
					// Si aucune des conditions activées n'est satisfaite, bloquer le signal
					if (!anyPassed)
						showUpArrow = false;
				}
			}
	
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
				(!UseOpen0SupVwap || Open[0] > Values[0][0]) && // Open[0] supérieur à VWAP
				(!UseHigh0SupVwap || High[0] > Values[0][0]) && // High[0] supérieur à VWAP
				(!UseBarre1CroiseVwapDown || (Open[1] > Values[0][1] && Close[1] < Values[0][1])) && // Barre1 croise VWAP de haut en bas
				(!UseBarre1RejVwapDown || (Open[1] < Values[0][1] && High[1] > Values[0][1] && Close[1] < Values[0][1])) &&
				// (!UsePrevBarInVA || (Open[1] > dynamicLowerThreshold && Open[1] < dynamicUpperThreshold)) &&
				(!UseVague1DOWN || (!isAboveVwap && downWaveValid)) &&
				(!UseThreeBarBreakoutDown || IsThreeBarDownBreakout()) &&
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
			
			//
			if (EnableSetupR2DOWN || EnableSetupR3DOWN || EnableSetupR4DOWN || EnableSetupR5DOWN || EnableSetupR1DOWN || EnableSetupB1DOWN)
			{
				// Vérifier si au moins une des six conditions est remplie
				bool setupR2Passed = !EnableSetupR2DOWN || CheckSetupR2DOWN();
				bool setupR3Passed = !EnableSetupR3DOWN || CheckSetupR3DOWN();
				bool setupR4Passed = !EnableSetupR4DOWN || CheckSetupR4DOWN();
				bool setupR5Passed = !EnableSetupR5DOWN || CheckSetupR5DOWN();
				bool setupR1Passed = !EnableSetupR1DOWN || CheckSetupR1DOWN();
				bool setupB1Passed = !EnableSetupB1DOWN || CheckSetupB1DOWN();
				
				// Si au moins une des conditions est activée, il suffit qu'une soit vraie pour passer
				if (EnableSetupR2DOWN || EnableSetupR3DOWN || EnableSetupR4DOWN || EnableSetupR5DOWN || EnableSetupR1DOWN || EnableSetupB1DOWN)
				{
					// Vérifie si au moins une des conditions activées est satisfaite
					bool anyPassed = (EnableSetupR2DOWN && setupR2Passed) || 
									(EnableSetupR3DOWN && setupR3Passed) || 
									(EnableSetupR4DOWN && setupR4Passed) ||
									(EnableSetupR5DOWN && setupR5Passed) ||
									(EnableSetupR1DOWN && setupR1Passed) ||
									(EnableSetupB1DOWN && setupB1Passed);
					
					// Si aucune des conditions activées n'est satisfaite, bloquer le signal
					if (!anyPassed)
						showDownArrow = false;
				}
			}
			
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
		[Display(Name="Use Open0 Inf Vwap", Description="Si activé, Open[0] doit être inférieur à VWAP", Order=11, GroupName="0.3_Buy")]
		public bool UseOpen0InfVwap { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Use Low0 Inf Vwap", Description="Si activé, Low[0] doit être inférieur à VWAP", Order=12, GroupName="0.3_Buy")]
		public bool UseLow0InfVwap { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Use Barre1 Croise Vwap", Description="Si activé, Open[1] doit être inférieur à VWAP et Close[1] supérieur à VWAP", Order=13, GroupName="0.3_Buy")]
		public bool UseBarre1CroiseVwapUp { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use Barre1 Rej Vwap UP", Description = "Active la condition de rejet de la VWAP pour les signaux UP", Order = 14, GroupName = "0.3_Buy")]
		public bool UseBarre1RejVwapUp { get; set; }
        
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
		[Display(Name="Use Open0 Sup Vwap", Description="Si activé, Open[0] doit être supérieur à VWAP", Order=11, GroupName="0.4_Sell")]
		public bool UseOpen0SupVwap { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Use High0 Sup Vwap", Description="Si activé, High[0] doit être supérieur à VWAP", Order=12, GroupName="0.4_Sell")]
		public bool UseHigh0SupVwap { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Use Barre1 Croise Vwap Down", Description="Si activé, Open[1] doit être supérieur à VWAP et Close[1] inférieur à VWAP", Order=13, GroupName="0.4_Sell")]
		public bool UseBarre1CroiseVwapDown { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use Barre1 Rej Vwap DOWN", Description = "Active la condition de rejet de la VWAP pour les signaux DOWN", Order = 14, GroupName = "0.4_Sell")]
		public bool UseBarre1RejVwapDown { get; set; }
        
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
		
		// ############################ SetupR2 ######################################### //
		[NinjaScriptProperty]
		[Display(Name = "Enable SetupR2UP", Description = "Active l'option SetupR2UP", Order = 1, GroupName = "SetupR2UP")]
		public bool EnableSetupR2UP { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, 5)]
		[Display(Name = "Bars to Check", Description = "Nombre de barres précédentes à vérifier pour High > STD1Upper", Order = 2, GroupName = "SetupR2UP")]
		public int SetupR2UPBarsToCheck { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Minimum Pic Ticks", Description = "Minimum ticks au-dessus de STD1Upper pour au moins un des Highs", Order = 3, GroupName = "SetupR2UP")]
		public int SetupR2UPMinPicTicks { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Enable SetupR2DOWN", Description = "Active l'option SetupR2DOWN", Order = 1, GroupName = "SetupR2DOWN")]
		public bool EnableSetupR2DOWN { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, 5)]
		[Display(Name = "Bars to Check", Description = "Nombre de barres précédentes à vérifier pour Low < STD1Lower", Order = 2, GroupName = "SetupR2DOWN")]
		public int SetupR2DOWNBarsToCheck { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Minimum Pic Ticks", Description = "Minimum ticks en-dessous de STD1Lower pour au moins un des Lows", Order = 3, GroupName = "SetupR2DOWN")]
		public int SetupR2DOWNMinPicTicks { get; set; }
		
		// ############################ SetupR2 ######################################### //
		// ############################ SetupR3 ######################################### //
		[NinjaScriptProperty]
		[Display(Name = "Enable SetupR3UP", Description = "Active l'option SetupR3UP", Order = 1, GroupName = "SetupR3UP")]
		public bool EnableSetupR3UP { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, 5)]
		[Display(Name = "Bars to Check", Description = "Nombre de barres précédentes à vérifier pour High > STD1Upper", Order = 2, GroupName = "SetupR3UP")]
		public int SetupR3UPBarsToCheck { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Minimum Pic Ticks", Description = "Minimum ticks au-dessus de STD1Upper pour au moins un des Highs", Order = 3, GroupName = "SetupR3UP")]
		public int SetupR3UPMinPicTicks { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Enable SetupR3DOWN", Description = "Active l'option SetupR3DOWN", Order = 1, GroupName = "SetupR3DOWN")]
		public bool EnableSetupR3DOWN { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, 5)]
		[Display(Name = "Bars to Check", Description = "Nombre de barres précédentes à vérifier pour Low < STD1Lower", Order = 2, GroupName = "SetupR3DOWN")]
		public int SetupR3DOWNBarsToCheck { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Minimum Pic Ticks", Description = "Minimum ticks en-dessous de STD1Lower pour au moins un des Lows", Order = 3, GroupName = "SetupR3DOWN")]
		public int SetupR3DOWNMinPicTicks { get; set; }
		// ############################ SetupR3 ######################################### //
		// ############################ SetupR4 ######################################### //
		[NinjaScriptProperty]
		[Display(Name = "Enable SetupR4UP", Description = "Active l'option SetupR4UP", Order = 1, GroupName = "SetupR4UP")]
		public bool EnableSetupR4UP { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, 5)]
		[Display(Name = "Bars to Check", Description = "Nombre de barres précédentes à vérifier pour High > STD1Upper", Order = 2, GroupName = "SetupR4UP")]
		public int SetupR4UPBarsToCheck { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Minimum Pic Ticks", Description = "Minimum ticks au-dessus de STD1Upper pour au moins un des Highs", Order = 3, GroupName = "SetupR4UP")]
		public int SetupR4UPMinPicTicks { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Enable SetupR4DOWN", Description = "Active l'option SetupR4DOWN", Order = 1, GroupName = "SetupR4DOWN")]
		public bool EnableSetupR4DOWN { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, 5)]
		[Display(Name = "Bars to Check", Description = "Nombre de barres précédentes à vérifier pour Low < STD1Lower", Order = 2, GroupName = "SetupR4DOWN")]
		public int SetupR4DOWNBarsToCheck { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Minimum Pic Ticks", Description = "Minimum ticks en-dessous de STD1Lower pour au moins un des Lows", Order = 3, GroupName = "SetupR4DOWN")]
		public int SetupR4DOWNMinPicTicks { get; set; }
		
		// ############################ SetupR4 ######################################### //
		// ############################ SetupR5 ######################################### //
		// Propriétés pour SetupR5UP
		[NinjaScriptProperty]
		[Display(Name = "Enable SetupR5UP", Description = "Active l'option SetupR5UP", Order = 1, GroupName = "SetupR5UP")]
		public bool EnableSetupR5UP { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, 5)]
		[Display(Name = "Bars to Check", Description = "Nombre de barres précédentes à vérifier pour High > STD1Upper", Order = 2, GroupName = "SetupR5UP")]
		public int SetupR5UPBarsToCheck { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Minimum Pic Ticks", Description = "Minimum ticks au-dessus de STD1Upper pour au moins un des Highs", Order = 3, GroupName = "SetupR5UP")]
		public int SetupR5UPMinPicTicks { get; set; }
		
		// Propriétés pour SetupR5DOWN
		[NinjaScriptProperty]
		[Display(Name = "Enable SetupR5DOWN", Description = "Active l'option SetupR5DOWN", Order = 1, GroupName = "SetupR5DOWN")]
		public bool EnableSetupR5DOWN { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, 5)]
		[Display(Name = "Bars to Check", Description = "Nombre de barres précédentes à vérifier pour Low < STD1Lower", Order = 2, GroupName = "SetupR5DOWN")]
		public int SetupR5DOWNBarsToCheck { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Minimum Pic Ticks", Description = "Minimum ticks en-dessous de STD1Lower pour au moins un des Lows", Order = 3, GroupName = "SetupR5DOWN")]
		public int SetupR5DOWNMinPicTicks { get; set; }
		// ############################ SetupR5 ######################################### //
		// ############################ SetupR1 ######################################### //
		[NinjaScriptProperty]
		[Display(Name = "Enable SetupR1UP", Description = "Active l'option SetupR1UP", Order = 1, GroupName = "SetupR1UP")]
		public bool EnableSetupR1UP { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, 5)]
		[Display(Name = "Bars to Check", Description = "Nombre de barres précédentes à vérifier pour High > STD1Upper", Order = 2, GroupName = "SetupR1UP")]
		public int SetupR1UPBarsToCheck { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Minimum Pic Ticks", Description = "Minimum ticks au-dessus de STD1Upper pour au moins un des Highs", Order = 3, GroupName = "SetupR1UP")]
		public int SetupR1UPMinPicTicks { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Max Bar Size Ticks", Description = "Taille maximale en ticks entre Close[1] et Open[1]", Order = 4, GroupName = "SetupR1UP")]
		public int SetupR1UPMaxBarSizeTicks { get; set; }
		
		// Propriétés pour SetupR1DOWN
		[NinjaScriptProperty]
		[Display(Name = "Enable SetupR1DOWN", Description = "Active l'option SetupR1DOWN", Order = 1, GroupName = "SetupR1DOWN")]
		public bool EnableSetupR1DOWN { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, 5)]
		[Display(Name = "Bars to Check", Description = "Nombre de barres précédentes à vérifier pour Low < STD1Lower", Order = 2, GroupName = "SetupR1DOWN")]
		public int SetupR1DOWNBarsToCheck { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Minimum Pic Ticks", Description = "Minimum ticks en-dessous de STD1Lower pour au moins un des Lows", Order = 3, GroupName = "SetupR1DOWN")]
		public int SetupR1DOWNMinPicTicks { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Max Bar Size Ticks", Description = "Taille maximale en ticks entre Close[1] et Open[1]", Order = 4, GroupName = "SetupR1DOWN")]
		public int SetupR1DOWNMaxBarSizeTicks { get; set; }
		
		// ############################ SetupR1 ######################################### //
		// ############################ SetupB1 ######################################### //
		[NinjaScriptProperty]
		[Display(Name = "Enable SetupB1UP", Description = "Active l'option SetupB1UP", Order = 1, GroupName = "SetupB1UP")]
		public bool EnableSetupB1UP { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Max Bar Size Ticks", Description = "Taille maximale en ticks entre Close[1] et Open[1]", Order = 2, GroupName = "SetupB1UP")]
		public int SetupB1UPMaxBarSizeTicks { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Bar0 Minimum Size Ticks", Description = "Taille minimale en ticks de la barre courante (Close0 - Open0)", Order = 3, GroupName = "SetupB1UP")]
		public int SetupB1UPMinBar0SizeTicks { get; set; }
		
		// Propriétés pour SetupB1DOWN
		[NinjaScriptProperty]
		[Display(Name = "Enable SetupB1DOWN", Description = "Active l'option SetupB1DOWN", Order = 1, GroupName = "SetupB1DOWN")]
		public bool EnableSetupB1DOWN { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Max Bar Size Ticks", Description = "Taille maximale en ticks entre Close[1] et Open[1]", Order = 2, GroupName = "SetupB1DOWN")]
		public int SetupB1DOWNMaxBarSizeTicks { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Bar0 Minimum Size Ticks", Description = "Taille minimale en ticks de la barre courante (Open0 - Close0)", Order = 3, GroupName = "SetupB1DOWN")]
		public int SetupB1DOWNMinBar0SizeTicks { get; set; }
		// ############################ SetupB1 ######################################### //
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
		// ############################ 3-Bar Breakout ######################################### //
		[NinjaScriptProperty]
		[Display(Name = "Utiliser 3-Bar Breakout UP", Order = 1, GroupName = "3-Bar Breakout")]
		public bool UseThreeBarBreakoutUp { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Utiliser 3-Bar Breakout DOWN", Order = 2, GroupName = "3-Bar Breakout")]
		public bool UseThreeBarBreakoutDown { get; set; }
		// ############################ 3-Bar Breakout ######################################### //

        #endregion
    }
}