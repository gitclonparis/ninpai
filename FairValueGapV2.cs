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
    public class FairValueGapV2 : Indicator
    {
        private Brush bullishColor = Brushes.LightGreen;
        private Brush bearishColor = Brushes.LightCoral;
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Extension des rectangles (barres)", Description="Nombre de barres sur lesquelles étendre les rectangles", Order=1, GroupName="Paramètres")]
        public int RectangleExtension { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Afficher flèche FVG haussier", Description="Active l'affichage des flèches pour les FVG haussiers", Order=2, GroupName="Paramètres")]
        public bool UseFVGup { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Afficher flèche FVG baissier", Description="Active l'affichage des flèches pour les FVG baissiers", Order=3, GroupName="Paramètres")]
        public bool UseFVGdown { get; set; }
        
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Indicateur qui détecte des motifs de chandeliers spécifiques et place des flèches sur le graphique.";
                Name = "FairValueGapV2";
                IsOverlay = true;
                IsSuspendedWhileInactive = true;
                RectangleExtension = 5; // Valeur par défaut
                UseFVGup = true;        // Valeur par défaut
                UseFVGdown = true;      // Valeur par défaut
                AddPlot(bullishColor, "Bullish FVG");
                AddPlot(bearishColor, "Bearish FVG");
            }
        }

        private void DrawBullishFVG(double high1, double low3)
        {
            Draw.Rectangle(this, 
                "BullishFVG" + CurrentBar.ToString(), 
                false, 
                2, high1,
                -RectangleExtension, low3,
                bullishColor, 
                bullishColor, 
                30);
            
            if (UseFVGup)
            {
                Draw.ArrowUp(this, 
                    "BullishArrow" + CurrentBar.ToString(),
                    false,
                    0,
                    Low[0] - TickSize * 5,
                    bullishColor);
            }
        }

        private void DrawBearishFVG(double low1, double high3)
        {
            Draw.Rectangle(this, 
                "BearishFVG" + CurrentBar.ToString(), 
                false, 
                2, low1,
                -RectangleExtension, high3,
                bearishColor, 
                bearishColor, 
                30);
            
            if (UseFVGdown)
            {
                Draw.ArrowDown(this, 
                    "BearishArrow" + CurrentBar.ToString(),
                    false,
                    0,
                    High[0] + TickSize * 5,
                    bearishColor);
            }
        }
        
        protected override void OnBarUpdate()
        {
            if (CurrentBar < 2) return;

            double high1 = High[2]; // Bougie 1
            double low1 = Low[2];
            double close1 = Close[2];

            double high2 = High[1]; // Bougie 2
            double low2 = Low[1];
            double close2 = Close[1];

            double high3 = High[0]; // Bougie 3
            double low3 = Low[0];
            double open3 = Open[0];

            // Détection du FVG haussier
            if (close2 > Open[1] && low3 > high1)
            {
                DrawBullishFVG(high1, low3);
            }

            // Détection du FVG baissier
            if (close2 < Open[1] && high3 < low1)
            {
                DrawBearishFVG(low1, high3);
            }
        }
    }
}