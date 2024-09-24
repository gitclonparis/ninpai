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
	public class CustomConfigurableCumulativeDeltaIndicator : Indicator
	{
		private OrderFlowCumulativeDelta cumulativeDelta0;
		private OrderFlowCumulativeDelta cumulativeDelta1;
		private OrderFlowCumulativeDelta cumulativeDelta2;
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Filtre achat 1 (SizeFilter 0)", Description="Valeur du filtre pour les flèches à la hausse (SizeFilter 0)", Order=1, GroupName="Filtres")]
		public int BuyFilter1 { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Filtre achat 2", Description="Valeur du filtre pour les flèches à la hausse (SizeFilter dynamique 1)", Order=2, GroupName="Filtres")]
		public int BuyFilter2 { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Filtre achat 3", Description="Valeur du filtre pour les flèches à la hausse (SizeFilter dynamique 2)", Order=3, GroupName="Filtres")]
		public int BuyFilter3 { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Filtre vente 1 (SizeFilter 0)", Description="Valeur du filtre pour les flèches à la baisse (SizeFilter 0)", Order=4, GroupName="Filtres")]
		public int SellFilter1 { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Filtre vente 2", Description="Valeur du filtre pour les flèches à la baisse (SizeFilter dynamique 1)", Order=5, GroupName="Filtres")]
		public int SellFilter2 { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Filtre vente 3", Description="Valeur du filtre pour les flèches à la baisse (SizeFilter dynamique 2)", Order=6, GroupName="Filtres")]
		public int SellFilter3 { get; set; }
	
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="SizeFilter 1", Description="Valeur du SizeFilter dynamique 1", Order=1, GroupName="SizeFilters")]
		public int DynamicSizeFilter1 { get; set; }
	
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="SizeFilter 2", Description="Valeur du SizeFilter dynamique 2", Order=2, GroupName="SizeFilters")]
		public int DynamicSizeFilter2 { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name="Activer Condition 1 (SizeFilter 0)", Description="Activer la condition pour SizeFilter 0", Order=1, GroupName="Conditions")]
		public bool EnableCondition1 { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name="Activer Condition 2 (SizeFilter 1)", Description="Activer la condition pour SizeFilter dynamique 1", Order=2, GroupName="Conditions")]
		public bool EnableCondition2 { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name="Activer Condition 3 (SizeFilter 2)", Description="Activer la condition pour SizeFilter dynamique 2", Order=3, GroupName="Conditions")]
		public bool EnableCondition3 { get; set; }
	
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = @"Indicateur personnalisé basé sur OrderFlowCumulativeDelta avec conditions configurables";
				Name = "Custom Configurable Cumulative Delta Indicator";
				Calculate = Calculate.OnBarClose;
				IsOverlay = true;
				DisplayInDataBox = true;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = true;
				ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
				
				// Définir les valeurs par défaut des paramètres
				BuyFilter1 = 1000;
				BuyFilter2 = 1500;
				BuyFilter3 = 2000;
				SellFilter1 = 1000;
				SellFilter2 = 1500;
				SellFilter3 = 2000;
				DynamicSizeFilter1 = 10;
				DynamicSizeFilter2 = 20;
				EnableCondition1 = true;
				EnableCondition2 = false;
				EnableCondition3 = false;
			}
			else if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
				cumulativeDelta0 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0);
				cumulativeDelta1 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, DynamicSizeFilter1);
				cumulativeDelta2 = OrderFlowCumulativeDelta(BarsArray[0], CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, DynamicSizeFilter2);
			}
		}
	
		protected override void OnBarUpdate()
		{
			if (BarsInProgress != 0) 
				return;
	
			// Mettre à jour les indicateurs OrderFlowCumulativeDelta
			cumulativeDelta0.Update(cumulativeDelta0.BarsArray[1].Count - 1, 1);
			cumulativeDelta1.Update(cumulativeDelta1.BarsArray[1].Count - 1, 1);
			cumulativeDelta2.Update(cumulativeDelta2.BarsArray[1].Count - 1, 1);
	
			double deltaClose0 = cumulativeDelta0.DeltaClose[0];
			double deltaClose1 = cumulativeDelta1.DeltaClose[0];
			double deltaClose2 = cumulativeDelta2.DeltaClose[0];
	
			bool buySignal = true;
			bool sellSignal = true;
	
			if (EnableCondition1)
			{
				buySignal &= (deltaClose0 > BuyFilter1);
				sellSignal &= (deltaClose0 < -SellFilter1);
			}
			if (EnableCondition2)
			{
				buySignal &= (deltaClose1 > BuyFilter2);
				sellSignal &= (deltaClose1 < -SellFilter2);
			}
			if (EnableCondition3)
			{
				buySignal &= (deltaClose2 > BuyFilter3);
				sellSignal &= (deltaClose2 < -SellFilter3);
			}
	
			if (buySignal)
			{
				Draw.ArrowUp(this, "BuyArrow" + CurrentBar, true, 0, Low[0] - 2*TickSize, Brushes.Green);
			}
			else if (sellSignal)
			{
				Draw.ArrowDown(this, "SellArrow" + CurrentBar, true, 0, High[0] + 2*TickSize, Brushes.Red);
			}
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ninpai.CustomConfigurableCumulativeDeltaIndicator[] cacheCustomConfigurableCumulativeDeltaIndicator;
		public ninpai.CustomConfigurableCumulativeDeltaIndicator CustomConfigurableCumulativeDeltaIndicator(int buyFilter1, int buyFilter2, int buyFilter3, int sellFilter1, int sellFilter2, int sellFilter3, int dynamicSizeFilter1, int dynamicSizeFilter2, bool enableCondition1, bool enableCondition2, bool enableCondition3)
		{
			return CustomConfigurableCumulativeDeltaIndicator(Input, buyFilter1, buyFilter2, buyFilter3, sellFilter1, sellFilter2, sellFilter3, dynamicSizeFilter1, dynamicSizeFilter2, enableCondition1, enableCondition2, enableCondition3);
		}

		public ninpai.CustomConfigurableCumulativeDeltaIndicator CustomConfigurableCumulativeDeltaIndicator(ISeries<double> input, int buyFilter1, int buyFilter2, int buyFilter3, int sellFilter1, int sellFilter2, int sellFilter3, int dynamicSizeFilter1, int dynamicSizeFilter2, bool enableCondition1, bool enableCondition2, bool enableCondition3)
		{
			if (cacheCustomConfigurableCumulativeDeltaIndicator != null)
				for (int idx = 0; idx < cacheCustomConfigurableCumulativeDeltaIndicator.Length; idx++)
					if (cacheCustomConfigurableCumulativeDeltaIndicator[idx] != null && cacheCustomConfigurableCumulativeDeltaIndicator[idx].BuyFilter1 == buyFilter1 && cacheCustomConfigurableCumulativeDeltaIndicator[idx].BuyFilter2 == buyFilter2 && cacheCustomConfigurableCumulativeDeltaIndicator[idx].BuyFilter3 == buyFilter3 && cacheCustomConfigurableCumulativeDeltaIndicator[idx].SellFilter1 == sellFilter1 && cacheCustomConfigurableCumulativeDeltaIndicator[idx].SellFilter2 == sellFilter2 && cacheCustomConfigurableCumulativeDeltaIndicator[idx].SellFilter3 == sellFilter3 && cacheCustomConfigurableCumulativeDeltaIndicator[idx].DynamicSizeFilter1 == dynamicSizeFilter1 && cacheCustomConfigurableCumulativeDeltaIndicator[idx].DynamicSizeFilter2 == dynamicSizeFilter2 && cacheCustomConfigurableCumulativeDeltaIndicator[idx].EnableCondition1 == enableCondition1 && cacheCustomConfigurableCumulativeDeltaIndicator[idx].EnableCondition2 == enableCondition2 && cacheCustomConfigurableCumulativeDeltaIndicator[idx].EnableCondition3 == enableCondition3 && cacheCustomConfigurableCumulativeDeltaIndicator[idx].EqualsInput(input))
						return cacheCustomConfigurableCumulativeDeltaIndicator[idx];
			return CacheIndicator<ninpai.CustomConfigurableCumulativeDeltaIndicator>(new ninpai.CustomConfigurableCumulativeDeltaIndicator(){ BuyFilter1 = buyFilter1, BuyFilter2 = buyFilter2, BuyFilter3 = buyFilter3, SellFilter1 = sellFilter1, SellFilter2 = sellFilter2, SellFilter3 = sellFilter3, DynamicSizeFilter1 = dynamicSizeFilter1, DynamicSizeFilter2 = dynamicSizeFilter2, EnableCondition1 = enableCondition1, EnableCondition2 = enableCondition2, EnableCondition3 = enableCondition3 }, input, ref cacheCustomConfigurableCumulativeDeltaIndicator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.CustomConfigurableCumulativeDeltaIndicator CustomConfigurableCumulativeDeltaIndicator(int buyFilter1, int buyFilter2, int buyFilter3, int sellFilter1, int sellFilter2, int sellFilter3, int dynamicSizeFilter1, int dynamicSizeFilter2, bool enableCondition1, bool enableCondition2, bool enableCondition3)
		{
			return indicator.CustomConfigurableCumulativeDeltaIndicator(Input, buyFilter1, buyFilter2, buyFilter3, sellFilter1, sellFilter2, sellFilter3, dynamicSizeFilter1, dynamicSizeFilter2, enableCondition1, enableCondition2, enableCondition3);
		}

		public Indicators.ninpai.CustomConfigurableCumulativeDeltaIndicator CustomConfigurableCumulativeDeltaIndicator(ISeries<double> input , int buyFilter1, int buyFilter2, int buyFilter3, int sellFilter1, int sellFilter2, int sellFilter3, int dynamicSizeFilter1, int dynamicSizeFilter2, bool enableCondition1, bool enableCondition2, bool enableCondition3)
		{
			return indicator.CustomConfigurableCumulativeDeltaIndicator(input, buyFilter1, buyFilter2, buyFilter3, sellFilter1, sellFilter2, sellFilter3, dynamicSizeFilter1, dynamicSizeFilter2, enableCondition1, enableCondition2, enableCondition3);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.CustomConfigurableCumulativeDeltaIndicator CustomConfigurableCumulativeDeltaIndicator(int buyFilter1, int buyFilter2, int buyFilter3, int sellFilter1, int sellFilter2, int sellFilter3, int dynamicSizeFilter1, int dynamicSizeFilter2, bool enableCondition1, bool enableCondition2, bool enableCondition3)
		{
			return indicator.CustomConfigurableCumulativeDeltaIndicator(Input, buyFilter1, buyFilter2, buyFilter3, sellFilter1, sellFilter2, sellFilter3, dynamicSizeFilter1, dynamicSizeFilter2, enableCondition1, enableCondition2, enableCondition3);
		}

		public Indicators.ninpai.CustomConfigurableCumulativeDeltaIndicator CustomConfigurableCumulativeDeltaIndicator(ISeries<double> input , int buyFilter1, int buyFilter2, int buyFilter3, int sellFilter1, int sellFilter2, int sellFilter3, int dynamicSizeFilter1, int dynamicSizeFilter2, bool enableCondition1, bool enableCondition2, bool enableCondition3)
		{
			return indicator.CustomConfigurableCumulativeDeltaIndicator(input, buyFilter1, buyFilter2, buyFilter3, sellFilter1, sellFilter2, sellFilter3, dynamicSizeFilter1, dynamicSizeFilter2, enableCondition1, enableCondition2, enableCondition3);
		}
	}
}

#endregion
