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
    public class VolumetriFilter : Indicator
    {
        #region Variables
        private Brush upArrowColor;
        private Brush downArrowColor;
        #endregion

        

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                                    = "Indicateur personnalisé qui affiche des flèches en fonction du BarDelta dans une plage définie.";
                Name                                           = "VolumetriFilter";
                Calculate                                      = Calculate.OnBarClose;
                IsOverlay                                      = true;
                DisplayInDataBox                               = false;
                PaintPriceMarkers                              = false;
                IsSuspendedWhileInactive                       = true;

                // Paramètres par défaut pour UP
                UpBarDeltaEnabled                              = true;
                MinBarDeltaUPThreshold                         = 500;
                MaxBarDeltaUPThreshold                         = 2000;

                // Paramètres par défaut pour DOWN
                DownBarDeltaEnabled                            = true;
                MinBarDeltaDownThreshold                       = -500;
                MaxBarDeltaDownThreshold                       = -2000;

                // Couleurs par défaut
                UpArrowColor                                   = Brushes.Green;
                DownArrowColor                                 = Brushes.Red;
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 1)
                return;

            // Vérifier si le type de barres est Volumetric
            if (!(Bars.BarsSeries.BarsType is NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType))
                return;

            var barsType = Bars.BarsSeries.BarsType as NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType;
            if (barsType == null)
                return;

            var barDelta = barsType.Volumes[CurrentBar].BarDelta;

            // Condition pour flèche UP
            if (UpBarDeltaEnabled)
            {
                if (barDelta > MinBarDeltaUPThreshold && barDelta < MaxBarDeltaUPThreshold)
                {
                    Draw.ArrowUp(this, "UpArrow" + CurrentBar, false, 0, Low[0] - TickSize, UpArrowColor);
                }
            }

            // Condition pour flèche DOWN
            if (DownBarDeltaEnabled)
            {
                if (barDelta < MinBarDeltaDownThreshold && barDelta > MaxBarDeltaDownThreshold)
                {
                    Draw.ArrowDown(this, "DownArrow" + CurrentBar, false, 0, High[0] + TickSize, DownArrowColor);
                }
            }
        }
		
		#region Properties
        // Paramètres pour la flèche UP
        [NinjaScriptProperty]
        [Display(Name = "Activer la condition UP", Order = 1, GroupName = "0.01_Paramètres UP")]
        public bool UpBarDeltaEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Seuil  supérieur UP (BarDelta)", Order = 2, GroupName = "0.01_Paramètres UP")]
        public double MinBarDeltaUPThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Seuil inférieur UP (BarDelta)", Order = 3, GroupName = "0.01_Paramètres UP")]
        public double MaxBarDeltaUPThreshold { get; set; }

        // Paramètres pour la flèche DOWN
        [NinjaScriptProperty]
        [Display(Name = "Activer la condition DOWN", Order = 1, GroupName = "0.02_Paramètres DOWN")]
        public bool DownBarDeltaEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Seuil  supérieur DOWN (BarDelta)", Order = 2, GroupName = "0.02_Paramètres DOWN")]
        public double MinBarDeltaDownThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Seuil inférieur DOWN (BarDelta)", Order = 3, GroupName = "0.02_Paramètres DOWN")]
        public double MaxBarDeltaDownThreshold { get; set; }

        // Couleurs des flèches
        [XmlIgnore]
        [Display(Name = "Couleur flèche UP", Order = 1, GroupName = "Couleurs")]
        public Brush UpArrowColor
        {
            get { return upArrowColor; }
            set { upArrowColor = value; }
        }

        [Browsable(false)]
        public string UpArrowColorSerializable
        {
            get { return Serialize.BrushToString(upArrowColor); }
            set { upArrowColor = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Couleur flèche DOWN", Order = 2, GroupName = "Couleurs")]
        public Brush DownArrowColor
        {
            get { return downArrowColor; }
            set { downArrowColor = value; }
        }

        [Browsable(false)]
        public string DownArrowColorSerializable
        {
            get { return Serialize.BrushToString(downArrowColor); }
            set { downArrowColor = Serialize.StringToBrush(value); }
        }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ninpai.VolumetriFilter[] cacheVolumetriFilter;
		public ninpai.VolumetriFilter VolumetriFilter(bool upBarDeltaEnabled, double minBarDeltaUPThreshold, double maxBarDeltaUPThreshold, bool downBarDeltaEnabled, double minBarDeltaDownThreshold, double maxBarDeltaDownThreshold)
		{
			return VolumetriFilter(Input, upBarDeltaEnabled, minBarDeltaUPThreshold, maxBarDeltaUPThreshold, downBarDeltaEnabled, minBarDeltaDownThreshold, maxBarDeltaDownThreshold);
		}

		public ninpai.VolumetriFilter VolumetriFilter(ISeries<double> input, bool upBarDeltaEnabled, double minBarDeltaUPThreshold, double maxBarDeltaUPThreshold, bool downBarDeltaEnabled, double minBarDeltaDownThreshold, double maxBarDeltaDownThreshold)
		{
			if (cacheVolumetriFilter != null)
				for (int idx = 0; idx < cacheVolumetriFilter.Length; idx++)
					if (cacheVolumetriFilter[idx] != null && cacheVolumetriFilter[idx].UpBarDeltaEnabled == upBarDeltaEnabled && cacheVolumetriFilter[idx].MinBarDeltaUPThreshold == minBarDeltaUPThreshold && cacheVolumetriFilter[idx].MaxBarDeltaUPThreshold == maxBarDeltaUPThreshold && cacheVolumetriFilter[idx].DownBarDeltaEnabled == downBarDeltaEnabled && cacheVolumetriFilter[idx].MinBarDeltaDownThreshold == minBarDeltaDownThreshold && cacheVolumetriFilter[idx].MaxBarDeltaDownThreshold == maxBarDeltaDownThreshold && cacheVolumetriFilter[idx].EqualsInput(input))
						return cacheVolumetriFilter[idx];
			return CacheIndicator<ninpai.VolumetriFilter>(new ninpai.VolumetriFilter(){ UpBarDeltaEnabled = upBarDeltaEnabled, MinBarDeltaUPThreshold = minBarDeltaUPThreshold, MaxBarDeltaUPThreshold = maxBarDeltaUPThreshold, DownBarDeltaEnabled = downBarDeltaEnabled, MinBarDeltaDownThreshold = minBarDeltaDownThreshold, MaxBarDeltaDownThreshold = maxBarDeltaDownThreshold }, input, ref cacheVolumetriFilter);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.VolumetriFilter VolumetriFilter(bool upBarDeltaEnabled, double minBarDeltaUPThreshold, double maxBarDeltaUPThreshold, bool downBarDeltaEnabled, double minBarDeltaDownThreshold, double maxBarDeltaDownThreshold)
		{
			return indicator.VolumetriFilter(Input, upBarDeltaEnabled, minBarDeltaUPThreshold, maxBarDeltaUPThreshold, downBarDeltaEnabled, minBarDeltaDownThreshold, maxBarDeltaDownThreshold);
		}

		public Indicators.ninpai.VolumetriFilter VolumetriFilter(ISeries<double> input , bool upBarDeltaEnabled, double minBarDeltaUPThreshold, double maxBarDeltaUPThreshold, bool downBarDeltaEnabled, double minBarDeltaDownThreshold, double maxBarDeltaDownThreshold)
		{
			return indicator.VolumetriFilter(input, upBarDeltaEnabled, minBarDeltaUPThreshold, maxBarDeltaUPThreshold, downBarDeltaEnabled, minBarDeltaDownThreshold, maxBarDeltaDownThreshold);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.VolumetriFilter VolumetriFilter(bool upBarDeltaEnabled, double minBarDeltaUPThreshold, double maxBarDeltaUPThreshold, bool downBarDeltaEnabled, double minBarDeltaDownThreshold, double maxBarDeltaDownThreshold)
		{
			return indicator.VolumetriFilter(Input, upBarDeltaEnabled, minBarDeltaUPThreshold, maxBarDeltaUPThreshold, downBarDeltaEnabled, minBarDeltaDownThreshold, maxBarDeltaDownThreshold);
		}

		public Indicators.ninpai.VolumetriFilter VolumetriFilter(ISeries<double> input , bool upBarDeltaEnabled, double minBarDeltaUPThreshold, double maxBarDeltaUPThreshold, bool downBarDeltaEnabled, double minBarDeltaDownThreshold, double maxBarDeltaDownThreshold)
		{
			return indicator.VolumetriFilter(input, upBarDeltaEnabled, minBarDeltaUPThreshold, maxBarDeltaUPThreshold, downBarDeltaEnabled, minBarDeltaDownThreshold, maxBarDeltaDownThreshold);
		}
	}
}

#endregion
