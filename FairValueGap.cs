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
    public class FairValueGap : Indicator
    {
		private Brush bullishColor = Brushes.LightGreen;
        private Brush bearishColor = Brushes.LightCoral;
		
		[NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Extension des rectangles (barres)", Description="Nombre de barres sur lesquelles étendre les rectangles", Order=1, GroupName="Paramètres")]
        public int RectangleExtension { get; set; }
		
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Indicateur qui détecte des motifs de chandeliers spécifiques et place des flèches sur le graphique.";
                Name = "FairValueGap";
                IsOverlay = true;
                IsSuspendedWhileInactive = true;
				AddPlot(bullishColor, "Bullish FVG");
                AddPlot(bearishColor, "Bearish FVG");
            }
            else if (State == State.Configure)
            {
            }
        }
		
		//
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
            // La bougie 2 doit être à la hausse (close > open)
            // Le bas de la bougie 3 doit être au-dessus du haut de la bougie 1
            if (close2 > Open[1] && low3 > high1)
            {
                Draw.Rectangle(this, 
                    "BullishFVG" + CurrentBar.ToString(), 
                    false, 
                    2, high1,
                    -RectangleExtension, low3,
                    bullishColor, 
                    bullishColor, 
                    30);
            }

            // Détection du FVG baissier
            // La bougie 2 doit être à la baisse (close < open)
            // Le haut de la bougie 3 doit être en dessous du bas de la bougie 1
            if (close2 < Open[1] && high3 < low1)
            {
                Draw.Rectangle(this, 
                    "BearishFVG" + CurrentBar.ToString(), 
                    false, 
                    2, low1,
                    -RectangleExtension, high3,
                    bearishColor, 
                    bearishColor, 
                    30);
            }
        }
    }
}
