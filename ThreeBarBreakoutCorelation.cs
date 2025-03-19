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
//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators.ninpai
{
    public class ThreeBarBreakoutCorelation : Indicator
    {
        private int setupFound;
        private double highestHighOfRange;
        private double lowestLowOfRange;
		private Indicator correlation;
        
        // Propriétés publiques pour accéder aux signaux
        public bool IsUpBreakout { get; private set; }
        public bool IsDownBreakout { get; private set; }
        public int SetupFound { get; private set; }
        
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                 = @"Détecte un setup de 3 barres avec breakout";
                Name                        = "ThreeBarBreakoutCorelation";
                Calculate                   = Calculate.OnBarClose;
                IsOverlay                   = true;
                DisplayInDataBox            = true;
                DrawOnPricePanel            = true;
                DrawHorizontalGridLines     = true;
                DrawVerticalGridLines       = true;
                PaintPriceMarkers           = true;
                ScaleJustification          = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive    = true;
                
                UpArrowColor                = Brushes.LimeGreen;
                DownArrowColor              = Brushes.Red;
                ShowUpArrows                = true;
                ShowDownArrows              = true;
				
				UseCorrelation              = false;
                CorrelationSymbol           = "NQ 03-25";
                CorrelationPeriod           = 10;
                MinCorrelationFilter        = 0.95;
            }
            else if (State == State.Configure)
            {
                AddPlot(Brushes.Transparent, "Plot");
				if (UseCorrelation)
                {
                    AddDataSeries(CorrelationSymbol);
                }
            }
			else if (State == State.DataLoaded)
            {
                // Initialiser l'indicateur de corrélation
                if (UseCorrelation)
                {
                    correlation = Correlation(CorrelationPeriod, CorrelationSymbol);
                }
            }
        }
        
        protected override void OnBarUpdate()
        {
            // Vérifier qu'on a suffisamment de barres
            if (CurrentBar < 3)
                return;
			
            bool isUpBreakout = false;
            bool isDownBreakout = false;
			
			double currentCorrelation = correlation[0];
			
			if (UseCorrelation)
			{
				if (currentCorrelation > MinCorrelationFilter)
				{
					CheckThreeBarPattern(0, out isUpBreakout, out isDownBreakout);
				}
				
			}
			else 
			{
				CheckThreeBarPattern(0, out isUpBreakout, out isDownBreakout);
			}
    
            // Appeler la méthode pour détecter le pattern
            // CheckThreeBarPattern(0, out isUpBreakout, out isDownBreakout);
            
            // Mettre à jour les valeurs de l'indicateur et les propriétés publiques
            Values[0][0] = setupFound;
            
            // Mettre à jour les propriétés publiques pour l'accès externe
            SetupFound = setupFound;
            IsUpBreakout = isUpBreakout;
            IsDownBreakout = isDownBreakout;
        }
        
        // Méthode pour vérifier le pattern sur 3 barres
        private void CheckThreeBarPattern(int barsAgo, out bool isUpBreakout, out bool isDownBreakout)
        {
            isUpBreakout = false;
            isDownBreakout = false;
            
            // Déterminer les extrémités hautes et basses basées sur Open et Close pour barre1 et barre2
            double high1 = Math.Max(Open[barsAgo + 1], Close[barsAgo + 1]);
            double low1 = Math.Min(Open[barsAgo + 1], Close[barsAgo + 1]);
            double high2 = Math.Max(Open[barsAgo + 2], Close[barsAgo + 2]);
            double low2 = Math.Min(Open[barsAgo + 2], Close[barsAgo + 2]);
            
            // Vérifier si l'une est inside ou outside par rapport à l'autre
            bool isInsideBarPattern = (high1 <= high2 && low1 >= low2) || (high2 <= high1 && low2 >= low1);
            
            if (isInsideBarPattern)
            {
                // Définir le range entre Barre1 et Barre2
                highestHighOfRange = Math.Max(high1, high2);
                lowestLowOfRange = Math.Min(low1, low2);
                
                // Vérifier si Barre0 casse le close de Barre1
                if (Close[barsAgo] > Close[barsAgo + 1] && Close[barsAgo] > highestHighOfRange)
                {
                    // Cassure par le haut
                    isUpBreakout = true;
                    if (ShowUpArrows)
                    {
                        Draw.ArrowUp(this, "UpArrow" + CurrentBar.ToString(), true, barsAgo, Math.Min(Open[barsAgo], Close[barsAgo]), UpArrowColor);
                    }
                    setupFound = 1;
                }
                else if (Close[barsAgo] < Close[barsAgo + 1] && Close[barsAgo] < lowestLowOfRange)
                {
                    // Cassure par le bas
                    isDownBreakout = true;
                    if (ShowDownArrows)
                    {
                        Draw.ArrowDown(this, "DownArrow" + CurrentBar.ToString(), true, barsAgo, Math.Max(Open[barsAgo], Close[barsAgo]), DownArrowColor);
                    }
                    setupFound = -1;
                }
                else
                {
                    setupFound = 0;
                }
            }
            else
            {
                setupFound = 0;
            }
        }

        #region Properties
        [XmlIgnore]
        [Display(Name = "Couleur de la flèche haussière", Order = 1, GroupName = "Paramètres")]
        public Brush UpArrowColor { get; set; }
        
        [Browsable(false)]
        public string UpArrowColorSerializable
        {
            get { return Serialize.BrushToString(UpArrowColor); }
            set { UpArrowColor = Serialize.StringToBrush(value); }
        }
        
        [XmlIgnore]
        [Display(Name = "Couleur de la flèche baissière", Order = 2, GroupName = "Paramètres")]
        public Brush DownArrowColor { get; set; }
        
        [Browsable(false)]
        public string DownArrowColorSerializable
        {
            get { return Serialize.BrushToString(DownArrowColor); }
            set { DownArrowColor = Serialize.StringToBrush(value); }
        }
        
        [Display(Name = "Afficher les flèches haussières", Order = 3, GroupName = "Paramètres")]
        public bool ShowUpArrows { get; set; }
        
        [Display(Name = "Afficher les flèches baissières", Order = 4, GroupName = "Paramètres")]
        public bool ShowDownArrows { get; set; }
		
		// Nouveaux paramètres pour la corrélation
		[NinjaScriptProperty]
		[Display(Name = "Utiliser la corrélation", Order = 1, GroupName = "Paramètres de Corrélation")]
        public bool UseCorrelation { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Symbole pour la corrélation", Order = 2, GroupName = "Paramètres de Corrélation")]
        public string CorrelationSymbol { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Période de corrélation", Order = 3, GroupName = "Paramètres de Corrélation")]
        public int CorrelationPeriod { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Filtre minimum de corrélation", Order = 4, GroupName = "Paramètres de Corrélation", Description = "Valeur entre 0 et 1")]
        public double MinCorrelationFilter { get; set; }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ninpai.ThreeBarBreakoutCorelation[] cacheThreeBarBreakoutCorelation;
		public ninpai.ThreeBarBreakoutCorelation ThreeBarBreakoutCorelation(bool useCorrelation, string correlationSymbol, int correlationPeriod, double minCorrelationFilter)
		{
			return ThreeBarBreakoutCorelation(Input, useCorrelation, correlationSymbol, correlationPeriod, minCorrelationFilter);
		}

		public ninpai.ThreeBarBreakoutCorelation ThreeBarBreakoutCorelation(ISeries<double> input, bool useCorrelation, string correlationSymbol, int correlationPeriod, double minCorrelationFilter)
		{
			if (cacheThreeBarBreakoutCorelation != null)
				for (int idx = 0; idx < cacheThreeBarBreakoutCorelation.Length; idx++)
					if (cacheThreeBarBreakoutCorelation[idx] != null && cacheThreeBarBreakoutCorelation[idx].UseCorrelation == useCorrelation && cacheThreeBarBreakoutCorelation[idx].CorrelationSymbol == correlationSymbol && cacheThreeBarBreakoutCorelation[idx].CorrelationPeriod == correlationPeriod && cacheThreeBarBreakoutCorelation[idx].MinCorrelationFilter == minCorrelationFilter && cacheThreeBarBreakoutCorelation[idx].EqualsInput(input))
						return cacheThreeBarBreakoutCorelation[idx];
			return CacheIndicator<ninpai.ThreeBarBreakoutCorelation>(new ninpai.ThreeBarBreakoutCorelation(){ UseCorrelation = useCorrelation, CorrelationSymbol = correlationSymbol, CorrelationPeriod = correlationPeriod, MinCorrelationFilter = minCorrelationFilter }, input, ref cacheThreeBarBreakoutCorelation);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.ThreeBarBreakoutCorelation ThreeBarBreakoutCorelation(bool useCorrelation, string correlationSymbol, int correlationPeriod, double minCorrelationFilter)
		{
			return indicator.ThreeBarBreakoutCorelation(Input, useCorrelation, correlationSymbol, correlationPeriod, minCorrelationFilter);
		}

		public Indicators.ninpai.ThreeBarBreakoutCorelation ThreeBarBreakoutCorelation(ISeries<double> input , bool useCorrelation, string correlationSymbol, int correlationPeriod, double minCorrelationFilter)
		{
			return indicator.ThreeBarBreakoutCorelation(input, useCorrelation, correlationSymbol, correlationPeriod, minCorrelationFilter);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.ThreeBarBreakoutCorelation ThreeBarBreakoutCorelation(bool useCorrelation, string correlationSymbol, int correlationPeriod, double minCorrelationFilter)
		{
			return indicator.ThreeBarBreakoutCorelation(Input, useCorrelation, correlationSymbol, correlationPeriod, minCorrelationFilter);
		}

		public Indicators.ninpai.ThreeBarBreakoutCorelation ThreeBarBreakoutCorelation(ISeries<double> input , bool useCorrelation, string correlationSymbol, int correlationPeriod, double minCorrelationFilter)
		{
			return indicator.ThreeBarBreakoutCorelation(input, useCorrelation, correlationSymbol, correlationPeriod, minCorrelationFilter);
		}
	}
}

#endregion
