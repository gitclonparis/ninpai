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
    public class DiagonalVolumeAbsortionV02 : Indicator
    {
        private double tickSize;
        private SolidColorBrush transRed;
        private SolidColorBrush transGreen;

        #region Paramètres
        [NinjaScriptProperty]
        [Display(Name = "Use Trapped Sellers For Buy", 
                 Description = "Activer la détection des vendeurs piégés pour signal d'achat", 
                 Order = 1, GroupName = "Détection Absorption Vendeurs")]
        public bool UseTrapedSellersForBuy { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Negative Delta", 
                 Description = "Delta négatif minimum requis pour considérer une absorption (ex: -100)", 
                 Order = 2, GroupName = "Détection Absorption Vendeurs")]
        public long MinNegativeDelta { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Trapped Buyers For Sell", 
                 Description = "Activer la détection des acheteurs piégés pour signal de vente", 
                 Order = 1, GroupName = "Détection Absorption Acheteurs")]
        public bool UseTrappedBuyersForSell { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Positive Delta", 
                 Description = "Delta positif minimum requis pour considérer une absorption (ex: 100)", 
                 Order = 2, GroupName = "Détection Absorption Acheteurs")]
        public long MinPositiveDelta { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Trapped Levels", 
                 Description = "Nombre minimum de niveaux avec traders piégés requis", 
                 Order = 3, GroupName = "Paramètres Généraux")]
        public int MinTrappedLevels { get; set; }
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Indicateur de Diagonal Volume Imbalance avec détection des traders piégés et absorption.";
                Name = "DiagonalVolumeAbsortionV02";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;

                // Valeurs par défaut
                UseTrapedSellersForBuy = true;
                UseTrappedBuyersForSell = true;
                MinPositiveDelta = -100;
                MinNegativeDelta = -100;
                MinTrappedLevels = 3;

                AddPlot(Brushes.Transparent, "DummyPlot");
            }
            else if (State == State.DataLoaded)
            {
                tickSize = Instrument.MasterInstrument.TickSize;
                transRed = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
                transRed.Freeze();
                transGreen = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));
                transGreen.Freeze();
            }
        }

        protected override void OnBarUpdate()
        {
            List<double> trappedSellerLevels = new List<double>();
            List<double> trappedBuyerLevels = new List<double>();

            // Vérifier les vendeurs piégés
            if (UseTrapedSellersForBuy && CheckTrappedSellers(out trappedSellerLevels))
            {
                // Dessiner une flèche verte pour l'absorption des vendeurs
                Draw.ArrowUp(this, "AbsorptionArrowBuy_" + CurrentBar, true, 0, Low[0] - (3 * tickSize), Brushes.LimeGreen);
            }

            // Vérifier les acheteurs piégés
            if (UseTrappedBuyersForSell && CheckTrappedBuyers(out trappedBuyerLevels))
            {
                // Dessiner une flèche rouge pour l'absorption des acheteurs
                Draw.ArrowDown(this, "AbsorptionArrowSell_" + CurrentBar, true, 0, High[0] + (3 * tickSize), Brushes.Red);
            }
        }

        private bool CheckTrappedSellers(out List<double> trappedLevels)
        {
            trappedLevels = new List<double>();
            
            var volBarType = Bars.BarsSeries.BarsType as NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType;
            if (volBarType == null)
                return false;

            double closePrice = Close[0];

            // Parcourir tous les niveaux de prix de la barre
            for (double price = Low[0]; price <= High[0]; price += tickSize)
            {
                double askLevel = price + tickSize;
                long bidVol = volBarType.Volumes[CurrentBar].GetBidVolumeForPrice(price);
                long askVol = volBarType.Volumes[CurrentBar].GetAskVolumeForPrice(askLevel);

                if (bidVol == 0 && askVol == 0)
                    continue;

                // Calculer le delta
                long delta = askVol - bidVol;

                // Vérifier si le delta est suffisamment négatif (beaucoup de vendeurs)
                // et si le prix de clôture est au-dessus de ce niveau (vendeurs piégés)
                if (delta <= MinNegativeDelta && closePrice > price)
                {
                    trappedLevels.Add(price);
					Draw.Dot(this, "TrappedSellerLevel_" + CurrentBar + "_" + price, true, 0, price, transRed);
                }
            }

            return trappedLevels.Count >= MinTrappedLevels;
        }

        private bool CheckTrappedBuyers(out List<double> trappedLevels)
        {
            trappedLevels = new List<double>();
            
            var volBarType = Bars.BarsSeries.BarsType as NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType;
            if (volBarType == null)
                return false;

            double closePrice = Close[0];

            // Parcourir tous les niveaux de prix de la barre
            for (double price = Low[0]; price <= High[0]; price += tickSize)
            {
                double askLevel = price + tickSize;
                long bidVol = volBarType.Volumes[CurrentBar].GetBidVolumeForPrice(price);
                long askVol = volBarType.Volumes[CurrentBar].GetAskVolumeForPrice(askLevel);

                if (bidVol == 0 && askVol == 0)
                    continue;

                // Calculer le delta (bidVol - askVol pour les acheteurs piégés)
                long delta = bidVol - askVol;

                // Vérifier si le delta est suffisamment positif (beaucoup d'acheteurs)
                // et si le prix de clôture est en-dessous de ce niveau (acheteurs piégés)
                if (delta <= MinPositiveDelta && closePrice < price)
                {
                    trappedLevels.Add(price);
					Draw.Dot(this, "TrappedBuyerLevel_" + CurrentBar + "_" + price, true, 0, price, transGreen);
                }
            }

            return trappedLevels.Count >= MinTrappedLevels;
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ninpai.DiagonalVolumeAbsortionV02[] cacheDiagonalVolumeAbsortionV02;
		public ninpai.DiagonalVolumeAbsortionV02 DiagonalVolumeAbsortionV02(bool useTrapedSellersForBuy, long minNegativeDelta, bool useTrappedBuyersForSell, long minPositiveDelta, int minTrappedLevels)
		{
			return DiagonalVolumeAbsortionV02(Input, useTrapedSellersForBuy, minNegativeDelta, useTrappedBuyersForSell, minPositiveDelta, minTrappedLevels);
		}

		public ninpai.DiagonalVolumeAbsortionV02 DiagonalVolumeAbsortionV02(ISeries<double> input, bool useTrapedSellersForBuy, long minNegativeDelta, bool useTrappedBuyersForSell, long minPositiveDelta, int minTrappedLevels)
		{
			if (cacheDiagonalVolumeAbsortionV02 != null)
				for (int idx = 0; idx < cacheDiagonalVolumeAbsortionV02.Length; idx++)
					if (cacheDiagonalVolumeAbsortionV02[idx] != null && cacheDiagonalVolumeAbsortionV02[idx].UseTrapedSellersForBuy == useTrapedSellersForBuy && cacheDiagonalVolumeAbsortionV02[idx].MinNegativeDelta == minNegativeDelta && cacheDiagonalVolumeAbsortionV02[idx].UseTrappedBuyersForSell == useTrappedBuyersForSell && cacheDiagonalVolumeAbsortionV02[idx].MinPositiveDelta == minPositiveDelta && cacheDiagonalVolumeAbsortionV02[idx].MinTrappedLevels == minTrappedLevels && cacheDiagonalVolumeAbsortionV02[idx].EqualsInput(input))
						return cacheDiagonalVolumeAbsortionV02[idx];
			return CacheIndicator<ninpai.DiagonalVolumeAbsortionV02>(new ninpai.DiagonalVolumeAbsortionV02(){ UseTrapedSellersForBuy = useTrapedSellersForBuy, MinNegativeDelta = minNegativeDelta, UseTrappedBuyersForSell = useTrappedBuyersForSell, MinPositiveDelta = minPositiveDelta, MinTrappedLevels = minTrappedLevels }, input, ref cacheDiagonalVolumeAbsortionV02);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.DiagonalVolumeAbsortionV02 DiagonalVolumeAbsortionV02(bool useTrapedSellersForBuy, long minNegativeDelta, bool useTrappedBuyersForSell, long minPositiveDelta, int minTrappedLevels)
		{
			return indicator.DiagonalVolumeAbsortionV02(Input, useTrapedSellersForBuy, minNegativeDelta, useTrappedBuyersForSell, minPositiveDelta, minTrappedLevels);
		}

		public Indicators.ninpai.DiagonalVolumeAbsortionV02 DiagonalVolumeAbsortionV02(ISeries<double> input , bool useTrapedSellersForBuy, long minNegativeDelta, bool useTrappedBuyersForSell, long minPositiveDelta, int minTrappedLevels)
		{
			return indicator.DiagonalVolumeAbsortionV02(input, useTrapedSellersForBuy, minNegativeDelta, useTrappedBuyersForSell, minPositiveDelta, minTrappedLevels);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.DiagonalVolumeAbsortionV02 DiagonalVolumeAbsortionV02(bool useTrapedSellersForBuy, long minNegativeDelta, bool useTrappedBuyersForSell, long minPositiveDelta, int minTrappedLevels)
		{
			return indicator.DiagonalVolumeAbsortionV02(Input, useTrapedSellersForBuy, minNegativeDelta, useTrappedBuyersForSell, minPositiveDelta, minTrappedLevels);
		}

		public Indicators.ninpai.DiagonalVolumeAbsortionV02 DiagonalVolumeAbsortionV02(ISeries<double> input , bool useTrapedSellersForBuy, long minNegativeDelta, bool useTrappedBuyersForSell, long minPositiveDelta, int minTrappedLevels)
		{
			return indicator.DiagonalVolumeAbsortionV02(input, useTrapedSellersForBuy, minNegativeDelta, useTrappedBuyersForSell, minPositiveDelta, minTrappedLevels);
		}
	}
}

#endregion
