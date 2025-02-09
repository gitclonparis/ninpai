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

// Changement du namespace pour éviter les conflits
namespace NinjaTrader.NinjaScript.Indicators.ninpai
{
    public class CandlestickPatternArrowsV3 : Indicator
    {
        private int trendStrength = 0;

        [NinjaScriptProperty]
        [Display(Name = "Bullish Engulfing", Order = 2, GroupName = "Motifs Haussiers")]
        public bool BullishEngulfing { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Three White Soldiers", Order = 10, GroupName = "Motifs Haussiers")]
        public bool ThreeWhiteSoldiers { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Bearish Engulfing", Order = 2, GroupName = "Motifs Baissiers")]
        public bool BearishEngulfing { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Three Black Crows", Order = 11, GroupName = "Motifs Baissiers")]
        public bool ThreeBlackCrows { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Force de Tendance", Order = 0, GroupName = "Paramètres")]
        public int TrendStrength { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Indicateur qui détecte des motifs de chandeliers spécifiques et place des flèches sur le graphique.";
                Name = "CandlestickPatternArrowsV3";
                IsOverlay = true;
                IsSuspendedWhileInactive = true;
				
				TrendStrength = 0;
                BullishEngulfing = false;
                ThreeWhiteSoldiers = false;
                BearishEngulfing = false;
                ThreeBlackCrows = false;
            }
            else if (State == State.Configure)
            {
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 20) return;

            CheckBullishPatterns();
            CheckBearishPatterns();
        }
		
		// Méthode pour vérifier les patterns haussiers
        public bool CheckBullishPatterns()
        {
            bool hasSignal = false;

            if (BullishEngulfing && IsBullishEngulfing())
            {
                DrawBullishSignal("BullishEngulfing");
                hasSignal = true;
            }

            if (ThreeWhiteSoldiers && IsThreeWhiteSoldiers())
            {
                DrawBullishSignal("ThreeWhiteSoldiers");
                hasSignal = true;
            }

            return hasSignal;
        }

        // Méthode pour vérifier les patterns baissiers
        public bool CheckBearishPatterns()
        {
            bool hasSignal = false;

            if (BearishEngulfing && IsBearishEngulfing())
            {
                DrawBearishSignal("BearishEngulfing");
                hasSignal = true;
            }

            if (ThreeBlackCrows && IsThreeBlackCrows())
            {
                DrawBearishSignal("ThreeBlackCrows");
                hasSignal = true;
            }

            return hasSignal;
        }

        // Méthodes de vérification des patterns individuels
        private bool IsBullishEngulfing()
        {
            return CandlestickPattern(ChartPattern.BullishEngulfing, TrendStrength)[0] == 1;
        }

        private bool IsThreeWhiteSoldiers()
        {
            return CandlestickPattern(ChartPattern.ThreeWhiteSoldiers, TrendStrength)[0] == 1;
        }

        private bool IsBearishEngulfing()
        {
            return CandlestickPattern(ChartPattern.BearishEngulfing, TrendStrength)[0] == 1;
        }

        private bool IsThreeBlackCrows()
        {
            return CandlestickPattern(ChartPattern.ThreeBlackCrows, TrendStrength)[0] == 1;
        }

        // Méthodes de dessin
        private void DrawBullishSignal(string patternName)
        {
            Draw.ArrowUp(this, patternName + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Green);
        }

        private void DrawBearishSignal(string patternName)
        {
            Draw.ArrowDown(this, patternName + CurrentBar, true, 0, High[0] + TickSize, Brushes.Red);
        }
    }
}
