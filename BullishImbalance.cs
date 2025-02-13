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
    public class BullishImbalance : Indicator
    {
		private double imbalanceRatio = 2.0;
        private double minVolume = 50; // Seuil minimum de volume
		private double accumulatedAskVolume = 0;
		private double accumulatedBidVolume = 0;
		
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Indicateur qui détecte des motifs de chandeliers spécifiques et place des flèches sur le graphique.";
                Name = "BullishImbalance";
                IsOverlay = true;
                IsSuspendedWhileInactive = true;
				AddPlot(Brushes.Transparent, "BullishImbalance");
            }
            else if (State == State.Configure)
            {
                // Configuration supplémentaire si nécessaire
            }
        }
		
		protected override void OnMarketData(MarketDataEventArgs e)
		{
			// Vérifier le type de donnée
			if (e.MarketDataType == MarketDataType.Ask)
			{
				accumulatedAskVolume += e.Volume;
			}
			else if (e.MarketDataType == MarketDataType.Bid)
			{
				accumulatedBidVolume += e.Volume;
			}
		}

        protected override void OnBarUpdate()
        {
			if (CurrentBar < 2 || !Bars.IsTickReplay) 
                return; // Besoin de Tick Replay pour accéder aux données Order Flow

            // Exemple de logique : si le volume ask accumulé est deux fois supérieur à celui du bid
			if (accumulatedAskVolume >= (accumulatedBidVolume * imbalanceRatio) &&
				accumulatedAskVolume >= minVolume && accumulatedBidVolume >= minVolume)
			{
				Draw.ArrowUp(this, "Imbalance" + CurrentBar, false, 0, Low[0] - TickSize, Brushes.Green);
				// Réinitialiser les volumes pour le prochain bar, si nécessaire
				accumulatedAskVolume = 0;
				accumulatedBidVolume = 0;
			}
				
		}
	}
}
