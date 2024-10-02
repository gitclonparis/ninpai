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
    public class VolumetricFilterLastV8 : Indicator
    {
        private class VolumetricParameters
        {
            public bool Enabled { get; set; }
            public double Min { get; set; }
            public double Max { get; set; }
        }

        private VolumetricParameters[] upParameters;
        private VolumetricParameters[] downParameters;
		
		// new //
		private bool pocConditionEnabled;
		private int pocTicksDistance;
		private Series<double> pocSeries;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Optimized indicator that displays arrows based on various volumetric parameters.";
                Name = "VolumetricFilterLastV8";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = false;
                PaintPriceMarkers = false;
                IsSuspendedWhileInactive = true;

                InitializeParameters();

                UpArrowColor = Brushes.Green;
                DownArrowColor = Brushes.Red;
				POCColor = Brushes.Blue;
				// POCSize = 1;
				// New parameters
				pocConditionEnabled = false;
				pocTicksDistance = 2;
            }
			else if (State == State.Configure)
			{
				AddPlot(new Stroke(POCColor, 2), PlotStyle.Dot, "POC");
			}
			else if (State == State.DataLoaded)
			{
				pocSeries = new Series<double>(this);
			}
        }

        private void InitializeParameters()
        {
            upParameters = new VolumetricParameters[7];
            downParameters = new VolumetricParameters[7];

            for (int i = 0; i < 7; i++)
            {
                upParameters[i] = new VolumetricParameters();
                downParameters[i] = new VolumetricParameters();
            }

            // Set default values (you can adjust these as needed)
            SetDefaultParameterValues(upParameters[0], false, 200, 2000);    // BarDelta
            SetDefaultParameterValues(upParameters[1], false, 10, 50);       // DeltaPercent
            SetDefaultParameterValues(upParameters[2], false, 100, 1000);    // DeltaChange
            SetDefaultParameterValues(upParameters[3], false, 1000, 10000);  // TotalBuyingVolume
            SetDefaultParameterValues(upParameters[4], false, 0, 5000);      // TotalSellingVolume
            SetDefaultParameterValues(upParameters[5], false, 100, 1000);    // Trades
            SetDefaultParameterValues(upParameters[6], false, 2000, 20000);  // TotalVolume

            // Set default values for down parameters (adjust as needed)
            SetDefaultParameterValues(downParameters[0], false, 200, 2000);  // BarDelta (abs value)
            SetDefaultParameterValues(downParameters[1], false, 10, 50);     // DeltaPercent (abs value)
            SetDefaultParameterValues(downParameters[2], false, 100, 1000);  // DeltaChange (abs value)
            SetDefaultParameterValues(downParameters[3], false, 0, 5000);    // TotalBuyingVolume
            SetDefaultParameterValues(downParameters[4], false, 1000, 10000);// TotalSellingVolume
            SetDefaultParameterValues(downParameters[5], false, 100, 1000);  // Trades
            SetDefaultParameterValues(downParameters[6], false, 2000, 20000);// TotalVolume
        }

        private void SetDefaultParameterValues(VolumetricParameters param, bool enabled, double min, double max)
        {
            param.Enabled = enabled;
            param.Min = min;
            param.Max = max;
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 2 || !(Bars.BarsSeries.BarsType is NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType barsType))
                return;

            var currentBarVolumes = barsType.Volumes[CurrentBar];
            var previousBarVolumes = barsType.Volumes[CurrentBar - 1];

            double[] volumetricValues = new double[7];
            volumetricValues[0] = currentBarVolumes.BarDelta;
            volumetricValues[1] = currentBarVolumes.GetDeltaPercent();
            volumetricValues[2] = volumetricValues[0] - previousBarVolumes.BarDelta;
            volumetricValues[3] = currentBarVolumes.TotalBuyingVolume;
            volumetricValues[4] = currentBarVolumes.TotalSellingVolume;
            volumetricValues[5] = currentBarVolumes.Trades;
            volumetricValues[6] = currentBarVolumes.TotalVolume;
			
			// new //
			double pocPrice;
			long maxVolume = currentBarVolumes.GetMaximumVolume(null, out pocPrice);
			pocSeries[0] = pocPrice;
			Values[0][0] = pocPrice;

            bool showUpArrow = CheckAllConditions(upParameters, volumetricValues, true);
            bool showDownArrow = CheckAllConditions(downParameters, volumetricValues, false);
			
			// new //
			if (pocConditionEnabled)
			{
				double closePrice = Close[0];
				double tickSize = TickSize;
	
				showUpArrow = showUpArrow && (pocPrice <= closePrice - pocTicksDistance * tickSize);
				showDownArrow = showDownArrow && (pocPrice >= closePrice + pocTicksDistance * tickSize);
			}

            if (showUpArrow)
			{
                Draw.ArrowUp(this, "UpArrow" + CurrentBar, false, 0, Low[0] - TickSize, UpArrowColor);
				Draw.Dot(this, "POCUP" + CurrentBar, false, 0, pocPrice - pocTicksDistance * TickSize, POCColor);
			}

            if (showDownArrow)
			{
                Draw.ArrowDown(this, "DownArrow" + CurrentBar, false, 0, High[0] + TickSize, DownArrowColor);
				Draw.Dot(this, "POCDOWN" + CurrentBar, false, 0, pocPrice + pocTicksDistance * TickSize, POCColor);
			}
        }

        private bool CheckAllConditions(VolumetricParameters[] parameters, double[] values, bool isUpDirection)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Enabled)
                {
                    if (isUpDirection)
                    {
                        switch (i)
                        {
                            case 0: // BarDelta
                            case 1: // DeltaPercent
                            case 2: // DeltaChange
                                if (values[i] < parameters[i].Min || values[i] > parameters[i].Max)
                                    return false;
                                break;
                            default:
                                if (values[i] < parameters[i].Min || values[i] > parameters[i].Max)
                                    return false;
                                break;
                        }
                    }
                    else // Down direction
                    {
                        switch (i)
                        {
                            case 0: // BarDelta
                            case 1: // DeltaPercent
                            case 2: // DeltaChange
                                if (values[i] > -parameters[i].Min || values[i] < -parameters[i].Max)
                                    return false;
                                break;
                            default:
                                if (values[i] < parameters[i].Min || values[i] > parameters[i].Max)
                                    return false;
                                break;
                        }
                    }
                }
            }
            return true;
        }

        #region Properties
        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Up Arrow Color", Order = 1, GroupName = "Visuals")]
        public Brush UpArrowColor { get; set; }

        [Browsable(false)]
        public string UpArrowColorSerializable
        {
            get { return Serialize.BrushToString(UpArrowColor); }
            set { UpArrowColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Down Arrow Color", Order = 2, GroupName = "Visuals")]
        public Brush DownArrowColor { get; set; }

        [Browsable(false)]
        public string DownArrowColorSerializable
        {
            get { return Serialize.BrushToString(DownArrowColor); }
            set { DownArrowColor = Serialize.StringToBrush(value); }
        }
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="POC Color", Description="Color for POC", Order=3, GroupName="Visuals")]
		public Brush POCColor { get; set; }
	
		[Browsable(false)]
		public Series<double> POC
		{
			get { return Values[0]; }
		}
		
		// [NinjaScriptProperty]
		// [Range(1, 10)]
		// [Display(Name="POC Point Size", Description="Size of the POC point", Order=4, GroupName="Visuals")]
		// public int POCSize { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Enable POC Condition", Description="Enable the Point of Control condition", Order=1, GroupName="POC Parameters")]
		public bool POCConditionEnabled
		{
			get { return pocConditionEnabled; }
			set { pocConditionEnabled = value; }
		}
	
		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name="POC Ticks Distance", Description="Number of ticks for POC distance from close", Order=2, GroupName="POC Parameters")]
		public int POCTicksDistance
		{
			get { return pocTicksDistance; }
			set { pocTicksDistance = Math.Max(1, value); }
		}

        // UP Parameters
        [NinjaScriptProperty]
        [Display(Name = "Bar Delta UP Enabled", Order = 1, GroupName = "0.01_BarDeltaUP")]
        public bool BarDeltaUPEnabled
        {
            get { return upParameters[0].Enabled; }
            set { upParameters[0].Enabled = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Min Bar Delta UP", Order = 2, GroupName = "0.01_BarDeltaUP")]
        public double MinBarDeltaUP
        {
            get { return upParameters[0].Min; }
            set { upParameters[0].Min = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Max Bar Delta UP", Order = 3, GroupName = "0.01_BarDeltaUP")]
        public double MaxBarDeltaUP
        {
            get { return upParameters[0].Max; }
            set { upParameters[0].Max = value; }
        }

        // Delta Percent UP
        [NinjaScriptProperty]
        [Display(Name = "Delta Percent UP Enabled", Order = 1, GroupName = "0.02_DeltaPercentUP")]
        public bool DeltaPercentUPEnabled
        {
            get { return upParameters[1].Enabled; }
            set { upParameters[1].Enabled = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Min Delta Percent UP", Order = 2, GroupName = "0.02_DeltaPercentUP")]
        public double MinDeltaPercentUP
        {
            get { return upParameters[1].Min; }
            set { upParameters[1].Min = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Max Delta Percent UP", Order = 3, GroupName = "0.02_DeltaPercentUP")]
        public double MaxDeltaPercentUP
        {
            get { return upParameters[1].Max; }
            set { upParameters[1].Max = value; }
        }

        // Delta Change UP
        [NinjaScriptProperty]
        [Display(Name = "Delta Change UP Enabled", Order = 1, GroupName = "0.03_DeltaChangeUP")]
        public bool DeltaChangeUPEnabled
        {
            get { return upParameters[2].Enabled; }
            set { upParameters[2].Enabled = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Min Delta Change UP", Order = 2, GroupName = "0.03_DeltaChangeUP")]
        public double MinDeltaChangeUP
        {
            get { return upParameters[2].Min; }
            set { upParameters[2].Min = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Max Delta Change UP", Order = 3, GroupName = "0.03_DeltaChangeUP")]
        public double MaxDeltaChangeUP
        {
            get { return upParameters[2].Max; }
            set { upParameters[2].Max = value; }
        }

        // Total Buying Volume UP
        [NinjaScriptProperty]
        [Display(Name = "Total Buying Volume UP Enabled", Order = 1, GroupName = "0.04_TotalBuyingVolumeUP")]
        public bool TotalBuyingVolumeUPEnabled
        {
            get { return upParameters[3].Enabled; }
            set { upParameters[3].Enabled = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Min Total Buying Volume UP", Order = 2, GroupName = "0.04_TotalBuyingVolumeUP")]
        public double MinTotalBuyingVolumeUP
        {
            get { return upParameters[3].Min; }
            set { upParameters[3].Min = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Max Total Buying Volume UP", Order = 3, GroupName = "0.04_TotalBuyingVolumeUP")]
        public double MaxTotalBuyingVolumeUP
        {
            get { return upParameters[3].Max; }
            set { upParameters[3].Max = value; }
        }

        // Total Selling Volume UP
        [NinjaScriptProperty]
        [Display(Name = "Total Selling Volume UP Enabled", Order = 1, GroupName = "0.05_TotalSellingVolumeUP")]
        public bool TotalSellingVolumeUPEnabled
        {
            get { return upParameters[4].Enabled; }
            set { upParameters[4].Enabled = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Min Total Selling Volume UP", Order = 2, GroupName = "0.05_TotalSellingVolumeUP")]
        public double MinTotalSellingVolumeUP
        {
            get { return upParameters[4].Min; }
            set { upParameters[4].Min = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Max Total Selling Volume UP", Order = 3, GroupName = "0.05_TotalSellingVolumeUP")]
        public double MaxTotalSellingVolumeUP
        {
            get { return upParameters[4].Max; }
            set { upParameters[4].Max = value; }
        }

        // Trades UP
        [NinjaScriptProperty]
        [Display(Name = "Trades UP Enabled", Order = 1, GroupName = "0.06_TradesUP")]
        public bool TradesUPEnabled
        {
            get { return upParameters[5].Enabled; }
            set { upParameters[5].Enabled = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Min Trades UP", Order = 2, GroupName = "0.06_TradesUP")]
        public double MinTradesUP
        {
            get { return upParameters[5].Min; }
            set { upParameters[5].Min = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Max Trades UP", Order = 3, GroupName = "0.06_TradesUP")]
        public double MaxTradesUP
        {
            get { return upParameters[5].Max; }
            set { upParameters[5].Max = value; }
        }

        // Total Volume UP
        [NinjaScriptProperty]
        [Display(Name = "Total Volume UP Enabled", Order = 1, GroupName = "0.07_TotalVolumeUP")]
        public bool TotalVolumeUPEnabled
        {
            get { return upParameters[6].Enabled; }
            set { upParameters[6].Enabled = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Min Total Volume UP", Order = 2, GroupName = "0.07_TotalVolumeUP")]
        public double MinTotalVolumeUP
        {
            get { return upParameters[6].Min; }
            set { upParameters[6].Min = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Max Total Volume UP", Order = 3, GroupName = "0.07_TotalVolumeUP")]
        public double MaxTotalVolumeUP
        {
            get { return upParameters[6].Max; }
            set { upParameters[6].Max = value; }
        }

        // DOWN Parameters
        // Bar Delta DOWN
        [NinjaScriptProperty]
        [Display(Name = "Bar Delta DOWN Enabled", Order = 1, GroupName = "1.01_BarDeltaDOWN")]
        public bool BarDeltaDOWNEnabled
        {
            get { return downParameters[0].Enabled; }
            set { downParameters[0].Enabled = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Min Bar Delta DOWN", Order = 2, GroupName = "1.01_BarDeltaDOWN")]
        public double MinBarDeltaDOWN
        {
            get { return downParameters[0].Min; }
            set { downParameters[0].Min = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Max Bar Delta DOWN", Order = 3, GroupName = "1.01_BarDeltaDOWN")]
        public double MaxBarDeltaDOWN
        {
            get { return downParameters[0].Max; }
            set { downParameters[0].Max = value; }
        }
        
        // Delta Percent DOWN
        [NinjaScriptProperty]
        [Display(Name = "Delta Percent DOWN Enabled", Order = 1, GroupName = "1.02_DeltaPercentDOWN")]
        public bool DeltaPercentDOWNEnabled
        {
            get { return downParameters[1].Enabled; }
            set { downParameters[1].Enabled = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Min Delta Percent DOWN", Order = 2, GroupName = "1.02_DeltaPercentDOWN")]
        public double MinDeltaPercentDOWN
        {
            get { return downParameters[1].Min; }
            set { downParameters[1].Min = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Max Delta Percent DOWN", Order = 3, GroupName = "1.02_DeltaPercentDOWN")]
        public double MaxDeltaPercentDOWN
        {
            get { return downParameters[1].Max; }
            set { downParameters[1].Max = value; }
        }
        
        // Delta Change DOWN
        [NinjaScriptProperty]
        [Display(Name = "Delta Change DOWN Enabled", Order = 1, GroupName = "1.03_DeltaChangeDOWN")]
        public bool DeltaChangeDOWNEnabled
        {
            get { return downParameters[2].Enabled; }
            set { downParameters[2].Enabled = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Min Delta Change DOWN", Order = 2, GroupName = "1.03_DeltaChangeDOWN")]
        public double MinDeltaChangeDOWN
        {
            get { return downParameters[2].Min; }
            set { downParameters[2].Min = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Max Delta Change DOWN", Order = 3, GroupName = "1.03_DeltaChangeDOWN")]
        public double MaxDeltaChangeDOWN
        {
            get { return downParameters[2].Max; }
            set { downParameters[2].Max = value; }
        }
        
        // Total Buying Volume DOWN
        [NinjaScriptProperty]
        [Display(Name = "Total Buying Volume DOWN Enabled", Order = 1, GroupName = "1.04_TotalBuyingVolumeDOWN")]
        public bool TotalBuyingVolumeDOWNEnabled
        {
            get { return downParameters[3].Enabled; }
            set { downParameters[3].Enabled = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Min Total Buying Volume DOWN", Order = 2, GroupName = "1.04_TotalBuyingVolumeDOWN")]
        public double MinTotalBuyingVolumeDOWN
        {
            get { return downParameters[3].Min; }
            set { downParameters[3].Min = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Max Total Buying Volume DOWN", Order = 3, GroupName = "1.04_TotalBuyingVolumeDOWN")]
        public double MaxTotalBuyingVolumeDOWN
        {
            get { return downParameters[3].Max; }
            set { downParameters[3].Max = value; }
        }
        
        // Total Selling Volume DOWN
        [NinjaScriptProperty]
        [Display(Name = "Total Selling Volume DOWN Enabled", Order = 1, GroupName = "1.05_TotalSellingVolumeDOWN")]
        public bool TotalSellingVolumeDOWNEnabled
        {
            get { return downParameters[4].Enabled; }
            set { downParameters[4].Enabled = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Min Total Selling Volume DOWN", Order = 2, GroupName = "1.05_TotalSellingVolumeDOWN")]
        public double MinTotalSellingVolumeDOWN
        {
            get { return downParameters[4].Min; }
            set { downParameters[4].Min = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Max Total Selling Volume DOWN", Order = 3, GroupName = "1.05_TotalSellingVolumeDOWN")]
        public double MaxTotalSellingVolumeDOWN
        {
            get { return downParameters[4].Max; }
            set { downParameters[4].Max = value; }
        }
        
        // Trades DOWN
        [NinjaScriptProperty]
        [Display(Name = "Trades DOWN Enabled", Order = 1, GroupName = "1.06_TradesDOWN")]
        public bool TradesDOWNEnabled
        {
            get { return downParameters[5].Enabled; }
            set { downParameters[5].Enabled = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Min Trades DOWN", Order = 2, GroupName = "1.06_TradesDOWN")]
        public double MinTradesDOWN
        {
            get { return downParameters[5].Min; }
            set { downParameters[5].Min = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Max Trades DOWN", Order = 3, GroupName = "1.06_TradesDOWN")]
        public double MaxTradesDOWN
        {
            get { return downParameters[5].Max; }
            set { downParameters[5].Max = value; }
        }
        
        // Total Volume DOWN
        [NinjaScriptProperty]
        [Display(Name = "Total Volume DOWN Enabled", Order = 1, GroupName = "1.07_TotalVolumeDOWN")]
        public bool TotalVolumeDOWNEnabled
        {
            get { return downParameters[6].Enabled; }
            set { downParameters[6].Enabled = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Min Total Volume DOWN", Order = 2, GroupName = "1.07_TotalVolumeDOWN")]
        public double MinTotalVolumeDOWN
        {
            get { return downParameters[6].Min; }
            set { downParameters[6].Min = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Max Total Volume DOWN", Order = 3, GroupName = "1.07_TotalVolumeDOWN")]
        public double MaxTotalVolumeDOWN
        {
            get { return downParameters[6].Max; }
            set { downParameters[6].Max = value; }
        }

        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ninpai.VolumetricFilterLastV8[] cacheVolumetricFilterLastV8;
		public ninpai.VolumetricFilterLastV8 VolumetricFilterLastV8(Brush upArrowColor, Brush downArrowColor, Brush pOCColor, bool pOCConditionEnabled, int pOCTicksDistance, bool barDeltaUPEnabled, double minBarDeltaUP, double maxBarDeltaUP, bool deltaPercentUPEnabled, double minDeltaPercentUP, double maxDeltaPercentUP, bool deltaChangeUPEnabled, double minDeltaChangeUP, double maxDeltaChangeUP, bool totalBuyingVolumeUPEnabled, double minTotalBuyingVolumeUP, double maxTotalBuyingVolumeUP, bool totalSellingVolumeUPEnabled, double minTotalSellingVolumeUP, double maxTotalSellingVolumeUP, bool tradesUPEnabled, double minTradesUP, double maxTradesUP, bool totalVolumeUPEnabled, double minTotalVolumeUP, double maxTotalVolumeUP, bool barDeltaDOWNEnabled, double minBarDeltaDOWN, double maxBarDeltaDOWN, bool deltaPercentDOWNEnabled, double minDeltaPercentDOWN, double maxDeltaPercentDOWN, bool deltaChangeDOWNEnabled, double minDeltaChangeDOWN, double maxDeltaChangeDOWN, bool totalBuyingVolumeDOWNEnabled, double minTotalBuyingVolumeDOWN, double maxTotalBuyingVolumeDOWN, bool totalSellingVolumeDOWNEnabled, double minTotalSellingVolumeDOWN, double maxTotalSellingVolumeDOWN, bool tradesDOWNEnabled, double minTradesDOWN, double maxTradesDOWN, bool totalVolumeDOWNEnabled, double minTotalVolumeDOWN, double maxTotalVolumeDOWN)
		{
			return VolumetricFilterLastV8(Input, upArrowColor, downArrowColor, pOCColor, pOCConditionEnabled, pOCTicksDistance, barDeltaUPEnabled, minBarDeltaUP, maxBarDeltaUP, deltaPercentUPEnabled, minDeltaPercentUP, maxDeltaPercentUP, deltaChangeUPEnabled, minDeltaChangeUP, maxDeltaChangeUP, totalBuyingVolumeUPEnabled, minTotalBuyingVolumeUP, maxTotalBuyingVolumeUP, totalSellingVolumeUPEnabled, minTotalSellingVolumeUP, maxTotalSellingVolumeUP, tradesUPEnabled, minTradesUP, maxTradesUP, totalVolumeUPEnabled, minTotalVolumeUP, maxTotalVolumeUP, barDeltaDOWNEnabled, minBarDeltaDOWN, maxBarDeltaDOWN, deltaPercentDOWNEnabled, minDeltaPercentDOWN, maxDeltaPercentDOWN, deltaChangeDOWNEnabled, minDeltaChangeDOWN, maxDeltaChangeDOWN, totalBuyingVolumeDOWNEnabled, minTotalBuyingVolumeDOWN, maxTotalBuyingVolumeDOWN, totalSellingVolumeDOWNEnabled, minTotalSellingVolumeDOWN, maxTotalSellingVolumeDOWN, tradesDOWNEnabled, minTradesDOWN, maxTradesDOWN, totalVolumeDOWNEnabled, minTotalVolumeDOWN, maxTotalVolumeDOWN);
		}

		public ninpai.VolumetricFilterLastV8 VolumetricFilterLastV8(ISeries<double> input, Brush upArrowColor, Brush downArrowColor, Brush pOCColor, bool pOCConditionEnabled, int pOCTicksDistance, bool barDeltaUPEnabled, double minBarDeltaUP, double maxBarDeltaUP, bool deltaPercentUPEnabled, double minDeltaPercentUP, double maxDeltaPercentUP, bool deltaChangeUPEnabled, double minDeltaChangeUP, double maxDeltaChangeUP, bool totalBuyingVolumeUPEnabled, double minTotalBuyingVolumeUP, double maxTotalBuyingVolumeUP, bool totalSellingVolumeUPEnabled, double minTotalSellingVolumeUP, double maxTotalSellingVolumeUP, bool tradesUPEnabled, double minTradesUP, double maxTradesUP, bool totalVolumeUPEnabled, double minTotalVolumeUP, double maxTotalVolumeUP, bool barDeltaDOWNEnabled, double minBarDeltaDOWN, double maxBarDeltaDOWN, bool deltaPercentDOWNEnabled, double minDeltaPercentDOWN, double maxDeltaPercentDOWN, bool deltaChangeDOWNEnabled, double minDeltaChangeDOWN, double maxDeltaChangeDOWN, bool totalBuyingVolumeDOWNEnabled, double minTotalBuyingVolumeDOWN, double maxTotalBuyingVolumeDOWN, bool totalSellingVolumeDOWNEnabled, double minTotalSellingVolumeDOWN, double maxTotalSellingVolumeDOWN, bool tradesDOWNEnabled, double minTradesDOWN, double maxTradesDOWN, bool totalVolumeDOWNEnabled, double minTotalVolumeDOWN, double maxTotalVolumeDOWN)
		{
			if (cacheVolumetricFilterLastV8 != null)
				for (int idx = 0; idx < cacheVolumetricFilterLastV8.Length; idx++)
					if (cacheVolumetricFilterLastV8[idx] != null && cacheVolumetricFilterLastV8[idx].UpArrowColor == upArrowColor && cacheVolumetricFilterLastV8[idx].DownArrowColor == downArrowColor && cacheVolumetricFilterLastV8[idx].POCColor == pOCColor && cacheVolumetricFilterLastV8[idx].POCConditionEnabled == pOCConditionEnabled && cacheVolumetricFilterLastV8[idx].POCTicksDistance == pOCTicksDistance && cacheVolumetricFilterLastV8[idx].BarDeltaUPEnabled == barDeltaUPEnabled && cacheVolumetricFilterLastV8[idx].MinBarDeltaUP == minBarDeltaUP && cacheVolumetricFilterLastV8[idx].MaxBarDeltaUP == maxBarDeltaUP && cacheVolumetricFilterLastV8[idx].DeltaPercentUPEnabled == deltaPercentUPEnabled && cacheVolumetricFilterLastV8[idx].MinDeltaPercentUP == minDeltaPercentUP && cacheVolumetricFilterLastV8[idx].MaxDeltaPercentUP == maxDeltaPercentUP && cacheVolumetricFilterLastV8[idx].DeltaChangeUPEnabled == deltaChangeUPEnabled && cacheVolumetricFilterLastV8[idx].MinDeltaChangeUP == minDeltaChangeUP && cacheVolumetricFilterLastV8[idx].MaxDeltaChangeUP == maxDeltaChangeUP && cacheVolumetricFilterLastV8[idx].TotalBuyingVolumeUPEnabled == totalBuyingVolumeUPEnabled && cacheVolumetricFilterLastV8[idx].MinTotalBuyingVolumeUP == minTotalBuyingVolumeUP && cacheVolumetricFilterLastV8[idx].MaxTotalBuyingVolumeUP == maxTotalBuyingVolumeUP && cacheVolumetricFilterLastV8[idx].TotalSellingVolumeUPEnabled == totalSellingVolumeUPEnabled && cacheVolumetricFilterLastV8[idx].MinTotalSellingVolumeUP == minTotalSellingVolumeUP && cacheVolumetricFilterLastV8[idx].MaxTotalSellingVolumeUP == maxTotalSellingVolumeUP && cacheVolumetricFilterLastV8[idx].TradesUPEnabled == tradesUPEnabled && cacheVolumetricFilterLastV8[idx].MinTradesUP == minTradesUP && cacheVolumetricFilterLastV8[idx].MaxTradesUP == maxTradesUP && cacheVolumetricFilterLastV8[idx].TotalVolumeUPEnabled == totalVolumeUPEnabled && cacheVolumetricFilterLastV8[idx].MinTotalVolumeUP == minTotalVolumeUP && cacheVolumetricFilterLastV8[idx].MaxTotalVolumeUP == maxTotalVolumeUP && cacheVolumetricFilterLastV8[idx].BarDeltaDOWNEnabled == barDeltaDOWNEnabled && cacheVolumetricFilterLastV8[idx].MinBarDeltaDOWN == minBarDeltaDOWN && cacheVolumetricFilterLastV8[idx].MaxBarDeltaDOWN == maxBarDeltaDOWN && cacheVolumetricFilterLastV8[idx].DeltaPercentDOWNEnabled == deltaPercentDOWNEnabled && cacheVolumetricFilterLastV8[idx].MinDeltaPercentDOWN == minDeltaPercentDOWN && cacheVolumetricFilterLastV8[idx].MaxDeltaPercentDOWN == maxDeltaPercentDOWN && cacheVolumetricFilterLastV8[idx].DeltaChangeDOWNEnabled == deltaChangeDOWNEnabled && cacheVolumetricFilterLastV8[idx].MinDeltaChangeDOWN == minDeltaChangeDOWN && cacheVolumetricFilterLastV8[idx].MaxDeltaChangeDOWN == maxDeltaChangeDOWN && cacheVolumetricFilterLastV8[idx].TotalBuyingVolumeDOWNEnabled == totalBuyingVolumeDOWNEnabled && cacheVolumetricFilterLastV8[idx].MinTotalBuyingVolumeDOWN == minTotalBuyingVolumeDOWN && cacheVolumetricFilterLastV8[idx].MaxTotalBuyingVolumeDOWN == maxTotalBuyingVolumeDOWN && cacheVolumetricFilterLastV8[idx].TotalSellingVolumeDOWNEnabled == totalSellingVolumeDOWNEnabled && cacheVolumetricFilterLastV8[idx].MinTotalSellingVolumeDOWN == minTotalSellingVolumeDOWN && cacheVolumetricFilterLastV8[idx].MaxTotalSellingVolumeDOWN == maxTotalSellingVolumeDOWN && cacheVolumetricFilterLastV8[idx].TradesDOWNEnabled == tradesDOWNEnabled && cacheVolumetricFilterLastV8[idx].MinTradesDOWN == minTradesDOWN && cacheVolumetricFilterLastV8[idx].MaxTradesDOWN == maxTradesDOWN && cacheVolumetricFilterLastV8[idx].TotalVolumeDOWNEnabled == totalVolumeDOWNEnabled && cacheVolumetricFilterLastV8[idx].MinTotalVolumeDOWN == minTotalVolumeDOWN && cacheVolumetricFilterLastV8[idx].MaxTotalVolumeDOWN == maxTotalVolumeDOWN && cacheVolumetricFilterLastV8[idx].EqualsInput(input))
						return cacheVolumetricFilterLastV8[idx];
			return CacheIndicator<ninpai.VolumetricFilterLastV8>(new ninpai.VolumetricFilterLastV8(){ UpArrowColor = upArrowColor, DownArrowColor = downArrowColor, POCColor = pOCColor, POCConditionEnabled = pOCConditionEnabled, POCTicksDistance = pOCTicksDistance, BarDeltaUPEnabled = barDeltaUPEnabled, MinBarDeltaUP = minBarDeltaUP, MaxBarDeltaUP = maxBarDeltaUP, DeltaPercentUPEnabled = deltaPercentUPEnabled, MinDeltaPercentUP = minDeltaPercentUP, MaxDeltaPercentUP = maxDeltaPercentUP, DeltaChangeUPEnabled = deltaChangeUPEnabled, MinDeltaChangeUP = minDeltaChangeUP, MaxDeltaChangeUP = maxDeltaChangeUP, TotalBuyingVolumeUPEnabled = totalBuyingVolumeUPEnabled, MinTotalBuyingVolumeUP = minTotalBuyingVolumeUP, MaxTotalBuyingVolumeUP = maxTotalBuyingVolumeUP, TotalSellingVolumeUPEnabled = totalSellingVolumeUPEnabled, MinTotalSellingVolumeUP = minTotalSellingVolumeUP, MaxTotalSellingVolumeUP = maxTotalSellingVolumeUP, TradesUPEnabled = tradesUPEnabled, MinTradesUP = minTradesUP, MaxTradesUP = maxTradesUP, TotalVolumeUPEnabled = totalVolumeUPEnabled, MinTotalVolumeUP = minTotalVolumeUP, MaxTotalVolumeUP = maxTotalVolumeUP, BarDeltaDOWNEnabled = barDeltaDOWNEnabled, MinBarDeltaDOWN = minBarDeltaDOWN, MaxBarDeltaDOWN = maxBarDeltaDOWN, DeltaPercentDOWNEnabled = deltaPercentDOWNEnabled, MinDeltaPercentDOWN = minDeltaPercentDOWN, MaxDeltaPercentDOWN = maxDeltaPercentDOWN, DeltaChangeDOWNEnabled = deltaChangeDOWNEnabled, MinDeltaChangeDOWN = minDeltaChangeDOWN, MaxDeltaChangeDOWN = maxDeltaChangeDOWN, TotalBuyingVolumeDOWNEnabled = totalBuyingVolumeDOWNEnabled, MinTotalBuyingVolumeDOWN = minTotalBuyingVolumeDOWN, MaxTotalBuyingVolumeDOWN = maxTotalBuyingVolumeDOWN, TotalSellingVolumeDOWNEnabled = totalSellingVolumeDOWNEnabled, MinTotalSellingVolumeDOWN = minTotalSellingVolumeDOWN, MaxTotalSellingVolumeDOWN = maxTotalSellingVolumeDOWN, TradesDOWNEnabled = tradesDOWNEnabled, MinTradesDOWN = minTradesDOWN, MaxTradesDOWN = maxTradesDOWN, TotalVolumeDOWNEnabled = totalVolumeDOWNEnabled, MinTotalVolumeDOWN = minTotalVolumeDOWN, MaxTotalVolumeDOWN = maxTotalVolumeDOWN }, input, ref cacheVolumetricFilterLastV8);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.VolumetricFilterLastV8 VolumetricFilterLastV8(Brush upArrowColor, Brush downArrowColor, Brush pOCColor, bool pOCConditionEnabled, int pOCTicksDistance, bool barDeltaUPEnabled, double minBarDeltaUP, double maxBarDeltaUP, bool deltaPercentUPEnabled, double minDeltaPercentUP, double maxDeltaPercentUP, bool deltaChangeUPEnabled, double minDeltaChangeUP, double maxDeltaChangeUP, bool totalBuyingVolumeUPEnabled, double minTotalBuyingVolumeUP, double maxTotalBuyingVolumeUP, bool totalSellingVolumeUPEnabled, double minTotalSellingVolumeUP, double maxTotalSellingVolumeUP, bool tradesUPEnabled, double minTradesUP, double maxTradesUP, bool totalVolumeUPEnabled, double minTotalVolumeUP, double maxTotalVolumeUP, bool barDeltaDOWNEnabled, double minBarDeltaDOWN, double maxBarDeltaDOWN, bool deltaPercentDOWNEnabled, double minDeltaPercentDOWN, double maxDeltaPercentDOWN, bool deltaChangeDOWNEnabled, double minDeltaChangeDOWN, double maxDeltaChangeDOWN, bool totalBuyingVolumeDOWNEnabled, double minTotalBuyingVolumeDOWN, double maxTotalBuyingVolumeDOWN, bool totalSellingVolumeDOWNEnabled, double minTotalSellingVolumeDOWN, double maxTotalSellingVolumeDOWN, bool tradesDOWNEnabled, double minTradesDOWN, double maxTradesDOWN, bool totalVolumeDOWNEnabled, double minTotalVolumeDOWN, double maxTotalVolumeDOWN)
		{
			return indicator.VolumetricFilterLastV8(Input, upArrowColor, downArrowColor, pOCColor, pOCConditionEnabled, pOCTicksDistance, barDeltaUPEnabled, minBarDeltaUP, maxBarDeltaUP, deltaPercentUPEnabled, minDeltaPercentUP, maxDeltaPercentUP, deltaChangeUPEnabled, minDeltaChangeUP, maxDeltaChangeUP, totalBuyingVolumeUPEnabled, minTotalBuyingVolumeUP, maxTotalBuyingVolumeUP, totalSellingVolumeUPEnabled, minTotalSellingVolumeUP, maxTotalSellingVolumeUP, tradesUPEnabled, minTradesUP, maxTradesUP, totalVolumeUPEnabled, minTotalVolumeUP, maxTotalVolumeUP, barDeltaDOWNEnabled, minBarDeltaDOWN, maxBarDeltaDOWN, deltaPercentDOWNEnabled, minDeltaPercentDOWN, maxDeltaPercentDOWN, deltaChangeDOWNEnabled, minDeltaChangeDOWN, maxDeltaChangeDOWN, totalBuyingVolumeDOWNEnabled, minTotalBuyingVolumeDOWN, maxTotalBuyingVolumeDOWN, totalSellingVolumeDOWNEnabled, minTotalSellingVolumeDOWN, maxTotalSellingVolumeDOWN, tradesDOWNEnabled, minTradesDOWN, maxTradesDOWN, totalVolumeDOWNEnabled, minTotalVolumeDOWN, maxTotalVolumeDOWN);
		}

		public Indicators.ninpai.VolumetricFilterLastV8 VolumetricFilterLastV8(ISeries<double> input , Brush upArrowColor, Brush downArrowColor, Brush pOCColor, bool pOCConditionEnabled, int pOCTicksDistance, bool barDeltaUPEnabled, double minBarDeltaUP, double maxBarDeltaUP, bool deltaPercentUPEnabled, double minDeltaPercentUP, double maxDeltaPercentUP, bool deltaChangeUPEnabled, double minDeltaChangeUP, double maxDeltaChangeUP, bool totalBuyingVolumeUPEnabled, double minTotalBuyingVolumeUP, double maxTotalBuyingVolumeUP, bool totalSellingVolumeUPEnabled, double minTotalSellingVolumeUP, double maxTotalSellingVolumeUP, bool tradesUPEnabled, double minTradesUP, double maxTradesUP, bool totalVolumeUPEnabled, double minTotalVolumeUP, double maxTotalVolumeUP, bool barDeltaDOWNEnabled, double minBarDeltaDOWN, double maxBarDeltaDOWN, bool deltaPercentDOWNEnabled, double minDeltaPercentDOWN, double maxDeltaPercentDOWN, bool deltaChangeDOWNEnabled, double minDeltaChangeDOWN, double maxDeltaChangeDOWN, bool totalBuyingVolumeDOWNEnabled, double minTotalBuyingVolumeDOWN, double maxTotalBuyingVolumeDOWN, bool totalSellingVolumeDOWNEnabled, double minTotalSellingVolumeDOWN, double maxTotalSellingVolumeDOWN, bool tradesDOWNEnabled, double minTradesDOWN, double maxTradesDOWN, bool totalVolumeDOWNEnabled, double minTotalVolumeDOWN, double maxTotalVolumeDOWN)
		{
			return indicator.VolumetricFilterLastV8(input, upArrowColor, downArrowColor, pOCColor, pOCConditionEnabled, pOCTicksDistance, barDeltaUPEnabled, minBarDeltaUP, maxBarDeltaUP, deltaPercentUPEnabled, minDeltaPercentUP, maxDeltaPercentUP, deltaChangeUPEnabled, minDeltaChangeUP, maxDeltaChangeUP, totalBuyingVolumeUPEnabled, minTotalBuyingVolumeUP, maxTotalBuyingVolumeUP, totalSellingVolumeUPEnabled, minTotalSellingVolumeUP, maxTotalSellingVolumeUP, tradesUPEnabled, minTradesUP, maxTradesUP, totalVolumeUPEnabled, minTotalVolumeUP, maxTotalVolumeUP, barDeltaDOWNEnabled, minBarDeltaDOWN, maxBarDeltaDOWN, deltaPercentDOWNEnabled, minDeltaPercentDOWN, maxDeltaPercentDOWN, deltaChangeDOWNEnabled, minDeltaChangeDOWN, maxDeltaChangeDOWN, totalBuyingVolumeDOWNEnabled, minTotalBuyingVolumeDOWN, maxTotalBuyingVolumeDOWN, totalSellingVolumeDOWNEnabled, minTotalSellingVolumeDOWN, maxTotalSellingVolumeDOWN, tradesDOWNEnabled, minTradesDOWN, maxTradesDOWN, totalVolumeDOWNEnabled, minTotalVolumeDOWN, maxTotalVolumeDOWN);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.VolumetricFilterLastV8 VolumetricFilterLastV8(Brush upArrowColor, Brush downArrowColor, Brush pOCColor, bool pOCConditionEnabled, int pOCTicksDistance, bool barDeltaUPEnabled, double minBarDeltaUP, double maxBarDeltaUP, bool deltaPercentUPEnabled, double minDeltaPercentUP, double maxDeltaPercentUP, bool deltaChangeUPEnabled, double minDeltaChangeUP, double maxDeltaChangeUP, bool totalBuyingVolumeUPEnabled, double minTotalBuyingVolumeUP, double maxTotalBuyingVolumeUP, bool totalSellingVolumeUPEnabled, double minTotalSellingVolumeUP, double maxTotalSellingVolumeUP, bool tradesUPEnabled, double minTradesUP, double maxTradesUP, bool totalVolumeUPEnabled, double minTotalVolumeUP, double maxTotalVolumeUP, bool barDeltaDOWNEnabled, double minBarDeltaDOWN, double maxBarDeltaDOWN, bool deltaPercentDOWNEnabled, double minDeltaPercentDOWN, double maxDeltaPercentDOWN, bool deltaChangeDOWNEnabled, double minDeltaChangeDOWN, double maxDeltaChangeDOWN, bool totalBuyingVolumeDOWNEnabled, double minTotalBuyingVolumeDOWN, double maxTotalBuyingVolumeDOWN, bool totalSellingVolumeDOWNEnabled, double minTotalSellingVolumeDOWN, double maxTotalSellingVolumeDOWN, bool tradesDOWNEnabled, double minTradesDOWN, double maxTradesDOWN, bool totalVolumeDOWNEnabled, double minTotalVolumeDOWN, double maxTotalVolumeDOWN)
		{
			return indicator.VolumetricFilterLastV8(Input, upArrowColor, downArrowColor, pOCColor, pOCConditionEnabled, pOCTicksDistance, barDeltaUPEnabled, minBarDeltaUP, maxBarDeltaUP, deltaPercentUPEnabled, minDeltaPercentUP, maxDeltaPercentUP, deltaChangeUPEnabled, minDeltaChangeUP, maxDeltaChangeUP, totalBuyingVolumeUPEnabled, minTotalBuyingVolumeUP, maxTotalBuyingVolumeUP, totalSellingVolumeUPEnabled, minTotalSellingVolumeUP, maxTotalSellingVolumeUP, tradesUPEnabled, minTradesUP, maxTradesUP, totalVolumeUPEnabled, minTotalVolumeUP, maxTotalVolumeUP, barDeltaDOWNEnabled, minBarDeltaDOWN, maxBarDeltaDOWN, deltaPercentDOWNEnabled, minDeltaPercentDOWN, maxDeltaPercentDOWN, deltaChangeDOWNEnabled, minDeltaChangeDOWN, maxDeltaChangeDOWN, totalBuyingVolumeDOWNEnabled, minTotalBuyingVolumeDOWN, maxTotalBuyingVolumeDOWN, totalSellingVolumeDOWNEnabled, minTotalSellingVolumeDOWN, maxTotalSellingVolumeDOWN, tradesDOWNEnabled, minTradesDOWN, maxTradesDOWN, totalVolumeDOWNEnabled, minTotalVolumeDOWN, maxTotalVolumeDOWN);
		}

		public Indicators.ninpai.VolumetricFilterLastV8 VolumetricFilterLastV8(ISeries<double> input , Brush upArrowColor, Brush downArrowColor, Brush pOCColor, bool pOCConditionEnabled, int pOCTicksDistance, bool barDeltaUPEnabled, double minBarDeltaUP, double maxBarDeltaUP, bool deltaPercentUPEnabled, double minDeltaPercentUP, double maxDeltaPercentUP, bool deltaChangeUPEnabled, double minDeltaChangeUP, double maxDeltaChangeUP, bool totalBuyingVolumeUPEnabled, double minTotalBuyingVolumeUP, double maxTotalBuyingVolumeUP, bool totalSellingVolumeUPEnabled, double minTotalSellingVolumeUP, double maxTotalSellingVolumeUP, bool tradesUPEnabled, double minTradesUP, double maxTradesUP, bool totalVolumeUPEnabled, double minTotalVolumeUP, double maxTotalVolumeUP, bool barDeltaDOWNEnabled, double minBarDeltaDOWN, double maxBarDeltaDOWN, bool deltaPercentDOWNEnabled, double minDeltaPercentDOWN, double maxDeltaPercentDOWN, bool deltaChangeDOWNEnabled, double minDeltaChangeDOWN, double maxDeltaChangeDOWN, bool totalBuyingVolumeDOWNEnabled, double minTotalBuyingVolumeDOWN, double maxTotalBuyingVolumeDOWN, bool totalSellingVolumeDOWNEnabled, double minTotalSellingVolumeDOWN, double maxTotalSellingVolumeDOWN, bool tradesDOWNEnabled, double minTradesDOWN, double maxTradesDOWN, bool totalVolumeDOWNEnabled, double minTotalVolumeDOWN, double maxTotalVolumeDOWN)
		{
			return indicator.VolumetricFilterLastV8(input, upArrowColor, downArrowColor, pOCColor, pOCConditionEnabled, pOCTicksDistance, barDeltaUPEnabled, minBarDeltaUP, maxBarDeltaUP, deltaPercentUPEnabled, minDeltaPercentUP, maxDeltaPercentUP, deltaChangeUPEnabled, minDeltaChangeUP, maxDeltaChangeUP, totalBuyingVolumeUPEnabled, minTotalBuyingVolumeUP, maxTotalBuyingVolumeUP, totalSellingVolumeUPEnabled, minTotalSellingVolumeUP, maxTotalSellingVolumeUP, tradesUPEnabled, minTradesUP, maxTradesUP, totalVolumeUPEnabled, minTotalVolumeUP, maxTotalVolumeUP, barDeltaDOWNEnabled, minBarDeltaDOWN, maxBarDeltaDOWN, deltaPercentDOWNEnabled, minDeltaPercentDOWN, maxDeltaPercentDOWN, deltaChangeDOWNEnabled, minDeltaChangeDOWN, maxDeltaChangeDOWN, totalBuyingVolumeDOWNEnabled, minTotalBuyingVolumeDOWN, maxTotalBuyingVolumeDOWN, totalSellingVolumeDOWNEnabled, minTotalSellingVolumeDOWN, maxTotalSellingVolumeDOWN, tradesDOWNEnabled, minTradesDOWN, maxTradesDOWN, totalVolumeDOWNEnabled, minTotalVolumeDOWN, maxTotalVolumeDOWN);
		}
	}
}

#endregion
