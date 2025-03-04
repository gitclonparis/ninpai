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

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators.ninpai
{
    public class SwingDetector : Indicator
    {
        private Swing swing;
        private List<int> upSwingStartBars;
        private List<int> upSwingEndBars;
        private List<int> downSwingStartBars;
        private List<int> downSwingEndBars;
        
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                 = @"Détecte les swings qui dépassent un filtre de ticks spécifié";
                Name                        = "SwingDetector";
                Calculate                   = Calculate.OnBarClose;
                IsOverlay                   = true;
                DisplayInDataBox            = true;
                DrawOnPricePanel            = true;
                DrawHorizontalGridLines     = true;
                DrawVerticalGridLines       = true;
                PaintPriceMarkers           = true;
                ScaleJustification          = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                //Disable this property if your indicator requires custom values that cumulate with each new market data event. 
                //See Help Guide for additional information.
                IsSuspendedWhileInactive    = true;
                
                // Paramètres par défaut
                SwingStrength               = 5;
                TickFilter                  = 50;
                UpArrowColor                = Brushes.Green;
                DownArrowColor              = Brushes.Red;
            }
            else if (State == State.Configure)
            {
                // Initialiser l'indicateur Swing
                swing = Swing(SwingStrength);
                
                // Initialiser les listes pour garder trace des swings détectés
                upSwingStartBars = new List<int>();
                upSwingEndBars = new List<int>();
                downSwingStartBars = new List<int>();
                downSwingEndBars = new List<int>();
            }
        }

        protected override void OnBarUpdate()
        {
            // Nous avons besoin d'un minimum de barres pour commencer
            if (CurrentBar < SwingStrength * 2)
                return;
                
            // Vérifie s'il y a un nouveau swing haut
            if (swing.SwingHigh[0] != 0 && CurrentBar > SwingStrength)
            {
                // Chercher le dernier swing bas précédent
                int lastLowBar = -1;
                double lastLowValue = 0;
                
                for (int i = 0; i < 100; i++)  // Cherche dans les 100 dernières barres maximum
                {
                    int lowBar = swing.SwingLowBar(i, 1, 100);
                    if (lowBar >= 0 && lowBar < CurrentBar)
                    {
                        lastLowBar = lowBar;
                        lastLowValue = swing.SwingLow[CurrentBar - lowBar];
                        break;
                    }
                }
                
                // Si nous avons trouvé un swing bas précédent
                if (lastLowBar >= 0)
                {
                    // Calculer la différence en ticks
                    double priceDifference = swing.SwingHigh[0] - lastLowValue;
                    double tickDifference = priceDifference / TickSize;
                    
                    // Si la différence dépasse notre filtre
                    if (tickDifference >= TickFilter)
                    {
                        // Stocker les barres de début et de fin du swing
                        upSwingStartBars.Add(lastLowBar);
                        upSwingEndBars.Add(CurrentBar);
                        
                        // Dessiner des flèches sur toutes les barres de ce swing
                        for (int i = lastLowBar; i <= CurrentBar; i++)
                        {
                            Draw.ArrowUp(this, "UpArrow_" + i, false, i, Low[CurrentBar - i] - 2 * TickSize, UpArrowColor);
                        }
                    }
                }
            }
            
            // Vérifie s'il y a un nouveau swing bas
            if (swing.SwingLow[0] != 0 && CurrentBar > SwingStrength)
            {
                // Chercher le dernier swing haut précédent
                int lastHighBar = -1;
                double lastHighValue = 0;
                
                for (int i = 0; i < 100; i++)  // Cherche dans les 100 dernières barres maximum
                {
                    int highBar = swing.SwingHighBar(i, 1, 100);
                    if (highBar >= 0 && highBar < CurrentBar)
                    {
                        lastHighBar = highBar;
                        lastHighValue = swing.SwingHigh[CurrentBar - highBar];
                        break;
                    }
                }
                
                // Si nous avons trouvé un swing haut précédent
                if (lastHighBar >= 0)
                {
                    // Calculer la différence en ticks
                    double priceDifference = lastHighValue - swing.SwingLow[0];
                    double tickDifference = priceDifference / TickSize;
                    
                    // Si la différence dépasse notre filtre
                    if (tickDifference >= TickFilter)
                    {
                        // Stocker les barres de début et de fin du swing
                        downSwingStartBars.Add(lastHighBar);
                        downSwingEndBars.Add(CurrentBar);
                        
                        // Dessiner des flèches sur toutes les barres de ce swing
                        for (int i = lastHighBar; i <= CurrentBar; i++)
                        {
                            Draw.ArrowDown(this, "DownArrow_" + i, false, i, High[CurrentBar - i] + 2 * TickSize, DownArrowColor);
                        }
                    }
                }
            }
        }

        #region Properties
        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name="Swing Strength", Description="Nombre de barres nécessaires à gauche et à droite du point de swing", Order=1, GroupName="Paramètres")]
        public int SwingStrength
        { get; set; }
        
        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name="Tick Filter", Description="Nombre minimum de ticks pour identifier un swing significatif", Order=2, GroupName="Paramètres")]
        public int TickFilter
        { get; set; }
        
        [XmlIgnore]
        [Display(Name="Up Arrow Color", Description="Couleur des flèches montantes", Order=3, GroupName="Paramètres Visuels")]
        public System.Windows.Media.Brush UpArrowColor
        { get; set; }

        [Browsable(false)]
        public string UpArrowColorSerializable
        {
            get { return Serialize.BrushToString(UpArrowColor); }
            set { UpArrowColor = Serialize.StringToBrush(value); }
        }
        
        [XmlIgnore]
        [Display(Name="Down Arrow Color", Description="Couleur des flèches descendantes", Order=4, GroupName="Paramètres Visuels")]
        public System.Windows.Media.Brush DownArrowColor
        { get; set; }

        [Browsable(false)]
        public string DownArrowColorSerializable
        {
            get { return Serialize.BrushToString(DownArrowColor); }
            set { DownArrowColor = Serialize.StringToBrush(value); }
        }
        #endregion
    }
}
