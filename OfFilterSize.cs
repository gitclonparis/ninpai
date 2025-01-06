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
    public class OfFilterSize : Indicator
    {
        private OrderFlowCumulativeDelta[] deltaIndicators;
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Filter Size 1", Description="Premier niveau de filtre", Order=1, GroupName="Paramètres")]
        public int FilterSize1 { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Filter Size 2", Description="Deuxième niveau de filtre", Order=2, GroupName="Paramètres")]
        public int FilterSize2 { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Filter Size 3", Description="Troisième niveau de filtre", Order=3, GroupName="Paramètres")]
        public int FilterSize3 { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Filter Size 4", Description="Quatrième niveau de filtre", Order=4, GroupName="Paramètres")]
        public int FilterSize4 { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Seuil Delta", Description="Seuil pour les signaux", Order=5, GroupName="Paramètres")]
        public int DeltaThreshold { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Indicateur de Delta avec Flèches";
                Name = "OfFilterSize Delta Arrows";
				IsOverlay = true;
                FilterSize1 = 5;
                FilterSize2 = 10;
                FilterSize3 = 50;
                FilterSize4 = 100;
                DeltaThreshold = 1000;
                
                AddPlot(new Stroke(Brushes.Transparent), PlotStyle.Dot, "Delta");
            }
            else if (State == State.Configure)
            {
                AddDataSeries(Data.BarsPeriodType.Tick, 1);
                
                deltaIndicators = new OrderFlowCumulativeDelta[4];
            }
            else if (State == State.DataLoaded)
            {
                deltaIndicators[0] = OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, FilterSize1);
                deltaIndicators[1] = OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, FilterSize2);
                deltaIndicators[2] = OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, FilterSize3);
                deltaIndicators[3] = OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, FilterSize4);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 2) return;
            if (BarsInProgress != 0) return;

            double totalDelta = 0;
            
            // Calcul de la somme des deltas pour les 3 dernières barres
            for (int i = 0; i < deltaIndicators.Length; i++)
            {
                for (int barsAgo = 0; barsAgo <= 2; barsAgo++)
                {
                    totalDelta += deltaIndicators[i].DeltaClose[barsAgo];
                }
            }

            // Signal haussier
            if (totalDelta > DeltaThreshold)
            {
                Draw.ArrowUp(this, "Up" + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Green);
            }
            // Signal baissier
            else if (totalDelta < -DeltaThreshold)
            {
                Draw.ArrowDown(this, "Down" + CurrentBar, true, 0, High[0] + TickSize, Brushes.Red);
            }
        }
    }
}
