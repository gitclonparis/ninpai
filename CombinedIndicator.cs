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
    public class CombinedIndicator : Indicator
    {
        // Variables privées pour BVAv11092024
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

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                // Paramètres par défaut pour l'indicateur combiné
                Description = @"Indicateur combiné de LimusineIndicatorV2 et BVAv11092024";
                Name = "CombinedIndicator";
                Calculate = Calculate.OnEachTick;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive = true;

                // Paramètres de LimusineIndicatorV2
                MinimumTicks = 20;
                ShowLimusineOpenCloseUP = true;
                ShowLimusineOpenCloseDOWN = true;
                ShowLimusineHighLowUP = true;
                ShowLimusineHighLowDOWN = true;

                // Paramètres de BVAv11092024
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
                OKisADX = false;

                FminATR = 0;
                FmaxATR = 0;
                OKisATR = false;

                FperiodVol = 9;
                OKisVOL = false;

                // Ajout des plots pour BVAv11092024
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

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 1) return;

            // Logique de LimusineIndicatorV2
            double openCloseDiff = Math.Abs(Open[0] - Close[0]) / TickSize;
            double highLowDiff = Math.Abs(High[0] - Low[0]) / TickSize;

            bool isLimusineOpenCloseUP = ShowLimusineOpenCloseUP && openCloseDiff >= MinimumTicks && Close[0] > Open[0];
            bool isLimusineOpenCloseDOWN = ShowLimusineOpenCloseDOWN && openCloseDiff >= MinimumTicks && Close[0] < Open[0];
            bool isLimusineHighLowUP = ShowLimusineHighLowUP && highLowDiff >= MinimumTicks && Close[0] > Open[0];
            bool isLimusineHighLowDOWN = ShowLimusineHighLowDOWN && highLowDiff >= MinimumTicks && Close[0] < Open[0];

            if (isLimusineOpenCloseUP || isLimusineHighLowUP)
            {
                Draw.ArrowUp(this, "LimusineUP_" + CurrentBar, true, 0, Low[0] - 2 * TickSize, Brushes.Green);
            }
            else if (isLimusineOpenCloseDOWN || isLimusineHighLowDOWN)
            {
                Draw.ArrowDown(this, "LimusineDown_" + CurrentBar, true, 0, High[0] + 2 * TickSize, Brushes.Red);
            }

            // Logique de BVAv11092024
            if (BarsInProgress == 1)
            {
                // Mettez à jour les données nécessaires pour le calcul
                // Cette partie peut nécessiter des ajustements selon vos besoins
                return;
            }

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

            // Calcul du VWAP et des écarts types
            double typicalPrice = (High[0] + Low[0] + Close[0]) / 3;
            double volume = Volume[0];

            sumPriceVolume += typicalPrice * volume;
            sumVolume += volume;
            sumSquaredPriceVolume += typicalPrice * typicalPrice * volume;

            double vwap = sumPriceVolume / sumVolume;
            double variance = (sumSquaredPriceVolume / sumVolume) - (vwap * vwap);
            double stdDev = Math.Sqrt(variance);

            VWAP[0] = vwap;
            StdDev1Upper[0] = vwap + stdDev;
            StdDev1Lower[0] = vwap - stdDev;
            StdDev2Upper[0] = vwap + 2 * stdDev;
            StdDev2Lower[0] = vwap - 2 * stdDev;
            StdDev3Upper[0] = vwap + 3 * stdDev;
            StdDev3Lower[0] = vwap - 3 * stdDev;

            barsSinceReset++;

            // Conditions d'achat/vente
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

        private bool ShouldBuy()
        {
            return (Close[0] > Open[0]) &&
                   (!OKisADX || (ADX1[0] > FminADX && ADX1[0] < FmaxADX)) &&
                   (!OKisATR || (ATR1[0] > FminATR && ATR1[0] < FmaxATR)) &&
                   (!OKisVOL || (VOL1[0] > VOLMA1[0])) &&
                   (barsSinceReset > MinBarsForSignal) &&
                   (Close[0] > (StdDev1Upper[0] + MinEntryDistanceUP * TickSize)) &&
                   (Close[0] <= (StdDev1Upper[0] + MaxEntryDistanceUP * TickSize)) &&
                   (upperBreakoutCount < MaxUpperBreakouts);
        }

        private bool ShouldSell()
        {
            return (Close[0] < Open[0]) &&
                   (!OKisADX || (ADX1[0] > FminADX && ADX1[0] < FmaxADX)) &&
                   (!OKisATR || (ATR1[0] > FminATR && ATR1[0] < FmaxATR)) &&
                   (!OKisVOL || (VOL1[0] > VOLMA1[0])) &&
                   (barsSinceReset > MinBarsForSignal) &&
                   (Close[0] < (StdDev1Lower[0] - MinEntryDistanceDOWN * TickSize)) &&
                   (Close[0] >= (StdDev1Lower[0] - MaxEntryDistanceDOWN * TickSize)) &&
                   (lowerBreakoutCount < MaxLowerBreakouts);
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

        #region Propriétés

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
        [Display(Name = "Reset Period (Minutes)", Order = 1, GroupName = "VWAP Parameters")]
        public int ResetPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Min Bars for Signal", Order = 2, GroupName = "VWAP Parameters")]
        public int MinBarsForSignal { get; set; }

        // Propriétés d'achat
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Min Entry Distance UP", Order = 1, GroupName = "Buy Parameters")]
        public int MinEntryDistanceUP { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Max Entry Distance UP", Order = 2, GroupName = "Buy Parameters")]
        public int MaxEntryDistanceUP { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Max Upper Breakouts", Order = 3, GroupName = "Buy Parameters")]
        public int MaxUpperBreakouts { get; set; }

        // Propriétés de vente
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Min Entry Distance DOWN", Order = 1, GroupName = "Sell Parameters")]
        public int MinEntryDistanceDOWN { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Max Entry Distance DOWN", Order = 2, GroupName = "Sell Parameters")]
        public int MaxEntryDistanceDOWN { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Max Lower Breakouts", Order = 3, GroupName = "Sell Parameters")]
        public int MaxLowerBreakouts { get; set; }

        // Propriétés ADX
        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "Fmin ADX", Order = 1, GroupName = "ADX Parameters")]
        public double FminADX { get; set; }

        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "Fmax ADX", Order = 2, GroupName = "ADX Parameters")]
        public double FmaxADX { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "OKisADX", Description = "Check ADX", Order = 3, GroupName = "ADX Parameters")]
        public bool OKisADX { get; set; }

        // Propriétés ATR
        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "Fmin ATR", Order = 1, GroupName = "ATR Parameters")]
        public double FminATR { get; set; }

        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "Fmax ATR", Order = 2, GroupName = "ATR Parameters")]
        public double FmaxATR { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "OKisATR", Description = "Check ATR", Order = 3, GroupName = "ATR Parameters")]
        public bool OKisATR { get; set; }

        // Propriétés Volume
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Fperiod Vol", Order = 1, GroupName = "Volume Parameters")]
        public int FperiodVol { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "OKisVOL", Description = "Check Volume", Order = 2, GroupName = "Volume Parameters")]
        public bool OKisVOL { get; set; }

        // Séries pour les plots
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> VWAP => Values[0];

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> StdDev1Upper => Values[1];

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> StdDev1Lower => Values[2];

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> StdDev2Upper => Values[3];

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> StdDev2Lower => Values[4];

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> StdDev3Upper => Values[5];

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> StdDev3Lower => Values[6];

        #endregion
    }
}
