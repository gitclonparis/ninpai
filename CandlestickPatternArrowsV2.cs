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
    public class CandlestickPatternArrowsV2 : Indicator
    {
        // Déclaration des variables
        private int trendStrength = 0; // Par défaut, aucune exigence de tendance

        // Paramètres utilisateur pour chaque motif haussier
        [NinjaScriptProperty]
        [Display(Name = "Bullish Engulfing", Order = 2, GroupName = "Motifs Haussiers")]
        public bool BullishEngulfing { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Three White Soldiers", Order = 10, GroupName = "Motifs Haussiers")]
        public bool ThreeWhiteSoldiers { get; set; }
		
        // Paramètres utilisateur pour chaque motif baissier
      
        [NinjaScriptProperty]
        [Display(Name = "Bearish Engulfing", Order = 2, GroupName = "Motifs Baissiers")]
        public bool BearishEngulfing { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Three Black Crows", Order = 11, GroupName = "Motifs Baissiers")]
        public bool ThreeBlackCrows { get; set; }

        // Paramètre pour la force de la tendance
        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Force de Tendance", Order = 0, GroupName = "Paramètres")]
        public int TrendStrength { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Indicateur qui détecte des motifs de chandeliers spécifiques et place des flèches sur le graphique.";
                Name = "CandlestickPatternArrowsV2";
                IsOverlay = true;
                IsSuspendedWhileInactive = true;

                // Valeurs par défaut des propriétés
                TrendStrength = 0;

                // Motifs haussiers par défaut (désactivés)
           
                BullishEngulfing = false;
                ThreeWhiteSoldiers = false;
				
                // Motifs baissiers par défaut (désactivés)
                BearishEngulfing = false;
                ThreeBlackCrows = false;
            }
            else if (State == State.Configure)
            {
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 20) // S'assurer qu'il y a suffisamment de barres pour la détection
                return;

            // Détection des motifs haussiers
            
            if (BullishEngulfing && CandlestickPattern(ChartPattern.BullishEngulfing, TrendStrength)[0] == 1)
            {
                Draw.ArrowUp(this, "BullishEngulfing" + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Green);
            }
            if (ThreeWhiteSoldiers && CandlestickPattern(ChartPattern.ThreeWhiteSoldiers, TrendStrength)[0] == 1)
            {
                Draw.ArrowUp(this, "ThreeWhiteSoldiers" + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Green);
            }
			
            // Détection des motifs baissiers
            if (BearishEngulfing && CandlestickPattern(ChartPattern.BearishEngulfing, TrendStrength)[0] == 1)
            {
                Draw.ArrowDown(this, "BearishEngulfing" + CurrentBar, true, 0, High[0] + TickSize, Brushes.Red);
            }
            if (ThreeBlackCrows && CandlestickPattern(ChartPattern.ThreeBlackCrows, TrendStrength)[0] == 1)
            {
                Draw.ArrowDown(this, "ThreeBlackCrows" + CurrentBar, true, 0, High[0] + TickSize, Brushes.Red);
            }
        }
    }
}
