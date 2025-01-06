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
	public class TAPDetector : Indicator
	{
		private const int ARROW_SIZE = 12;
		private bool isTickReplayEnabled = false;
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Taille minimum du lot", Description = "Taille minimum du lot à détecter", Order = 1, GroupName = "Paramètres")]
		public int MinLotSize { get; set; }
	
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Détecte les transactions TAP d'une taille spécifique (nécessite TickReplay)";
				Name = "TAP Detector";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				MinLotSize = 50;
			}
			else if (State == State.Configure)
			{
				isTickReplayEnabled = true;
				
				if (!isTickReplayEnabled)
				{
					Draw.TextFixed(this, "TickReplayWarning", 
						"TickReplay doit être activé pour l'analyse historique\nDonnées en temps réel uniquement disponibles", 
						TextPosition.BottomRight, 
						Brushes.Red, 
						new SimpleFont("Arial", 12), 
						Brushes.White, 
						Brushes.Transparent, 
						0);
				}
			}
		}
	
		//
		protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
		{
			if (marketDataUpdate.MarketDataType != MarketDataType.Last)
				return;
		
			// Modification ici pour utiliser GetCurrentBid() et GetCurrentAsk()
			ProcessTrade(marketDataUpdate.Price, marketDataUpdate.Volume, Time[0], 
				GetCurrentBid(), GetCurrentAsk(), "Live");
		}
		//
	
		protected override void OnBarUpdate()
		{
			// Ne traite les données historiques que si TickReplay est activé
			if (!isTickReplayEnabled || CurrentBar < 1)
				return;
	
			// Traitement des trades historiques via TickReplay
			if (Count > 0)  // Vérifie si des données sont disponibles
			{
				double price = Close[0];
				double volume = Volume[0];
				
				ProcessTrade(price, volume, Time[0], GetCurrentBid(), GetCurrentAsk(), "Hist");
			}
		}
	
		//
		private double GetCurrentBid()
		{
			if (Bars != null && Bars.Count > 0)
			{
				return Low[0];  // Utilise le prix bas comme approximation du bid
			}
			return 0;
		}
		
		private double GetCurrentAsk()
		{
			if (Bars != null && Bars.Count > 0)
			{
				return High[0];  // Utilise le prix haut comme approximation du ask
			}
			return 0;
		}
		
		private void ProcessTrade(double price, double volume, DateTime timestamp, 
			double bid, double ask, string prefix)
		{
			if (volume >= MinLotSize)
			{
				string timeKey = timestamp.Ticks.ToString();
		
				if (price >= ask)  // Achat au-dessus du ask
				{
					Draw.ArrowUp(this, prefix + "_Up_" + timeKey, false, 0, 
						Low[0] - TickSize * 2, Brushes.Green, false);
				}
				else if (price <= bid)  // Vente en-dessous du bid
				{
					Draw.ArrowDown(this, prefix + "_Down_" + timeKey, false, 0,
						High[0] + TickSize * 2, Brushes.Red, false);
				}
			}
		}
		//
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ninpai.TAPDetector[] cacheTAPDetector;
		public ninpai.TAPDetector TAPDetector(int minLotSize)
		{
			return TAPDetector(Input, minLotSize);
		}

		public ninpai.TAPDetector TAPDetector(ISeries<double> input, int minLotSize)
		{
			if (cacheTAPDetector != null)
				for (int idx = 0; idx < cacheTAPDetector.Length; idx++)
					if (cacheTAPDetector[idx] != null && cacheTAPDetector[idx].MinLotSize == minLotSize && cacheTAPDetector[idx].EqualsInput(input))
						return cacheTAPDetector[idx];
			return CacheIndicator<ninpai.TAPDetector>(new ninpai.TAPDetector(){ MinLotSize = minLotSize }, input, ref cacheTAPDetector);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.TAPDetector TAPDetector(int minLotSize)
		{
			return indicator.TAPDetector(Input, minLotSize);
		}

		public Indicators.ninpai.TAPDetector TAPDetector(ISeries<double> input , int minLotSize)
		{
			return indicator.TAPDetector(input, minLotSize);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.TAPDetector TAPDetector(int minLotSize)
		{
			return indicator.TAPDetector(Input, minLotSize);
		}

		public Indicators.ninpai.TAPDetector TAPDetector(ISeries<double> input , int minLotSize)
		{
			return indicator.TAPDetector(input, minLotSize);
		}
	}
}

#endregion
