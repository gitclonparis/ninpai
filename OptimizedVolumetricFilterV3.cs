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
    public class OptimizedVolumetricFilterV3 : Indicator
    {
		private class VolumetricParameters
        {
            public bool Enabled { get; set; }
            public double Min { get; set; }
            public double Max { get; set; }
        }

        private VolumetricParameters[] upParameters;
        private VolumetricParameters[] downParameters;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Optimized indicator that displays arrows based on various volumetric parameters.";
                Name = "OptimizedVolumetricFilterV3";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = false;
                PaintPriceMarkers = false;
                IsSuspendedWhileInactive = true;

                // Initialize parameters
                InitializeParameters();

                UpArrowColor = Brushes.Green;
                DownArrowColor = Brushes.Red;
            }
        }

        private void InitializeParameters()
        {
            upParameters = new VolumetricParameters[9];
            downParameters = new VolumetricParameters[9];

            for (int i = 0; i < 9; i++)
            {
                upParameters[i] = new VolumetricParameters();
                downParameters[i] = new VolumetricParameters();
            }

            // Set default values (you can adjust these as needed)
            SetDefaultParameterValues(upParameters[0], false, 500, 2000);    // BarDelta
            SetDefaultParameterValues(upParameters[1], false, 10, 50);       // DeltaPercent
            SetDefaultParameterValues(upParameters[2], false, 1000, 5000);   // MaxPositiveDelta
            SetDefaultParameterValues(upParameters[3], false, 1000, 5000);   // MaxNegativeDelta
            SetDefaultParameterValues(upParameters[4], false, 100, 1000);    // DeltaChange
            SetDefaultParameterValues(upParameters[5], false, 1000, 10000);  // TotalBuyingVolume
            SetDefaultParameterValues(upParameters[6], false, 0, 5000);      // TotalSellingVolume
            SetDefaultParameterValues(upParameters[7], false, 100, 1000);    // Trades
            SetDefaultParameterValues(upParameters[8], false, 2000, 20000);  // TotalVolume

            // Set default values for down parameters (adjust as needed)
            SetDefaultParameterValues(downParameters[0], false, 500, 2000);
            SetDefaultParameterValues(downParameters[1], false, 10, 50);
            SetDefaultParameterValues(downParameters[2], false, 0, 1000);
            SetDefaultParameterValues(downParameters[3], false, 1000, 5000);
            SetDefaultParameterValues(downParameters[4], false, 100, 1000);
            SetDefaultParameterValues(downParameters[5], false, 0, 5000);
            SetDefaultParameterValues(downParameters[6], false, 1000, 10000);
            SetDefaultParameterValues(downParameters[7], false, 100, 1000);
            SetDefaultParameterValues(downParameters[8], false, 2000, 20000);
        }

        private void SetDefaultParameterValues(VolumetricParameters param, bool enabled, double min, double max)
        {
            param.Enabled = enabled;
            param.Min = min;
            param.Max = max;
        }
        // ... (autres parties du code inchangées)

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 2 || !(Bars.BarsSeries.BarsType is NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType barsType))
                return;

            var currentBarVolumes = barsType.Volumes[CurrentBar];
            var previousBarVolumes = barsType.Volumes[CurrentBar - 1];

            double[] volumetricValues = new double[9];
            volumetricValues[0] = currentBarVolumes.BarDelta;
            volumetricValues[1] = currentBarVolumes.GetDeltaPercent();
            volumetricValues[2] = currentBarVolumes.GetMaximumPositiveDelta();
            volumetricValues[3] = currentBarVolumes.GetMaximumNegativeDelta(); // Removed Math.Abs()
            volumetricValues[4] = volumetricValues[0] - previousBarVolumes.BarDelta;
            volumetricValues[5] = currentBarVolumes.TotalBuyingVolume;
            volumetricValues[6] = currentBarVolumes.TotalSellingVolume;
            volumetricValues[7] = currentBarVolumes.Trades;
            volumetricValues[8] = currentBarVolumes.TotalVolume;

            bool showUpArrow = CheckAllConditions(upParameters, volumetricValues, true);
            bool showDownArrow = CheckAllConditions(downParameters, volumetricValues, false);

            if (showUpArrow)
                Draw.ArrowUp(this, "UpArrow" + CurrentBar, false, 0, Low[0] - TickSize, UpArrowColor);

            if (showDownArrow)
                Draw.ArrowDown(this, "DownArrow" + CurrentBar, false, 0, High[0] + TickSize, DownArrowColor);
        }

        private bool CheckAllConditions(VolumetricParameters[] parameters, double[] values, bool isUpDirection)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Enabled)
                {
                    if (isUpDirection)
                    {
                        if (values[i] < parameters[i].Min || values[i] > parameters[i].Max)
                            return false;
                    }
                    else
                    {
                        // Pour la direction DOWN, on inverse la logique pour les valeurs négatives
                        if (i == 0 || i == 1 || i == 3 || i == 4) // BarDelta, DeltaPercent, MaxNegativeDelta, DeltaChange
                        {
                            if (values[i] > parameters[i].Min || values[i] < parameters[i].Max)
                                return false;
                        }
                        else
                        {
                            if (values[i] < parameters[i].Min || values[i] > parameters[i].Max)
                                return false;
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

        // Manually define properties for all parameters
        [NinjaScriptProperty]
        [Display(Name = "Bar Delta UP Enabled", Order = 1, GroupName = "01_BarDeltaUP")]
        public bool BarDeltaUPEnabled
        {
            get { return upParameters[0].Enabled; }
            set { upParameters[0].Enabled = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Min Bar Delta UP", Order = 2, GroupName = "01_BarDeltaUP")]
        public double MinBarDeltaUP
        {
            get { return upParameters[0].Min; }
            set { upParameters[0].Min = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Max Bar Delta UP", Order = 3, GroupName = "01_BarDeltaUP")]
        public double MaxBarDeltaUP
        {
            get { return upParameters[0].Max; }
            set { upParameters[0].Max = value; }
        }
		
		// Delta Percent UP
		[NinjaScriptProperty]
		[Display(Name = "Delta Percent UP Enabled", Order = 1, GroupName = "02_DeltaPercentUP")]
		public bool DeltaPercentUPEnabled
		{
			get { return upParameters[1].Enabled; }
			set { upParameters[1].Enabled = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Min Delta Percent UP", Order = 2, GroupName = "02_DeltaPercentUP")]
		public double MinDeltaPercentUP
		{
			get { return upParameters[1].Min; }
			set { upParameters[1].Min = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Max Delta Percent UP", Order = 3, GroupName = "02_DeltaPercentUP")]
		public double MaxDeltaPercentUP
		{
			get { return upParameters[1].Max; }
			set { upParameters[1].Max = value; }
		}
		
		// Max Positive Delta UP
		[NinjaScriptProperty]
		[Display(Name = "Max Positive Delta UP Enabled", Order = 1, GroupName = "03_MaxPositiveDeltaUP")]
		public bool MaxPositiveDeltaUPEnabled
		{
			get { return upParameters[2].Enabled; }
			set { upParameters[2].Enabled = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Min Max Positive Delta UP", Order = 2, GroupName = "03_MaxPositiveDeltaUP")]
		public double MinMaxPositiveDeltaUP
		{
			get { return upParameters[2].Min; }
			set { upParameters[2].Min = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Max Max Positive Delta UP", Order = 3, GroupName = "03_MaxPositiveDeltaUP")]
		public double MaxMaxPositiveDeltaUP
		{
			get { return upParameters[2].Max; }
			set { upParameters[2].Max = value; }
		}
		
		// Max Negative Delta UP
		[NinjaScriptProperty]
		[Display(Name = "Max Negative Delta UP Enabled", Order = 1, GroupName = "04_MaxNegativeDeltaUP")]
		public bool MaxNegativeDeltaUPEnabled
		{
			get { return upParameters[3].Enabled; }
			set { upParameters[3].Enabled = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Min Max Negative Delta UP", Order = 2, GroupName = "04_MaxNegativeDeltaUP")]
		public double MinMaxNegativeDeltaUP
		{
			get { return upParameters[3].Min; }
			set { upParameters[3].Min = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Max Max Negative Delta UP", Order = 3, GroupName = "04_MaxNegativeDeltaUP")]
		public double MaxMaxNegativeDeltaUP
		{
			get { return upParameters[3].Max; }
			set { upParameters[3].Max = value; }
		}
		
		// Delta Change UP
		[NinjaScriptProperty]
		[Display(Name = "Delta Change UP Enabled", Order = 1, GroupName = "05_DeltaChangeUP")]
		public bool DeltaChangeUPEnabled
		{
			get { return upParameters[4].Enabled; }
			set { upParameters[4].Enabled = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Min Delta Change UP", Order = 2, GroupName = "05_DeltaChangeUP")]
		public double MinDeltaChangeUP
		{
			get { return upParameters[4].Min; }
			set { upParameters[4].Min = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Max Delta Change UP", Order = 3, GroupName = "05_DeltaChangeUP")]
		public double MaxDeltaChangeUP
		{
			get { return upParameters[4].Max; }
			set { upParameters[4].Max = value; }
		}
		
		// Total Buying Volume UP
		[NinjaScriptProperty]
		[Display(Name = "Total Buying Volume UP Enabled", Order = 1, GroupName = "06_TotalBuyingVolumeUP")]
		public bool TotalBuyingVolumeUPEnabled
		{
			get { return upParameters[5].Enabled; }
			set { upParameters[5].Enabled = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Min Total Buying Volume UP", Order = 2, GroupName = "06_TotalBuyingVolumeUP")]
		public double MinTotalBuyingVolumeUP
		{
			get { return upParameters[5].Min; }
			set { upParameters[5].Min = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Max Total Buying Volume UP", Order = 3, GroupName = "06_TotalBuyingVolumeUP")]
		public double MaxTotalBuyingVolumeUP
		{
			get { return upParameters[5].Max; }
			set { upParameters[5].Max = value; }
		}
		
		// Total Selling Volume UP
		[NinjaScriptProperty]
		[Display(Name = "Total Selling Volume UP Enabled", Order = 1, GroupName = "07_TotalSellingVolumeUP")]
		public bool TotalSellingVolumeUPEnabled
		{
			get { return upParameters[6].Enabled; }
			set { upParameters[6].Enabled = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Min Total Selling Volume UP", Order = 2, GroupName = "07_TotalSellingVolumeUP")]
		public double MinTotalSellingVolumeUP
		{
			get { return upParameters[6].Min; }
			set { upParameters[6].Min = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Max Total Selling Volume UP", Order = 3, GroupName = "07_TotalSellingVolumeUP")]
		public double MaxTotalSellingVolumeUP
		{
			get { return upParameters[6].Max; }
			set { upParameters[6].Max = value; }
		}
		
		// Trades UP
		[NinjaScriptProperty]
		[Display(Name = "Trades UP Enabled", Order = 1, GroupName = "08_TradesUP")]
		public bool TradesUPEnabled
		{
			get { return upParameters[7].Enabled; }
			set { upParameters[7].Enabled = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Min Trades UP", Order = 2, GroupName = "08_TradesUP")]
		public double MinTradesUP
		{
			get { return upParameters[7].Min; }
			set { upParameters[7].Min = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Max Trades UP", Order = 3, GroupName = "08_TradesUP")]
		public double MaxTradesUP
		{
			get { return upParameters[7].Max; }
			set { upParameters[7].Max = value; }
		}
		
		// Total Volume UP
		[NinjaScriptProperty]
		[Display(Name = "Total Volume UP Enabled", Order = 1, GroupName = "09_TotalVolumeUP")]
		public bool TotalVolumeUPEnabled
		{
			get { return upParameters[8].Enabled; }
			set { upParameters[8].Enabled = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Min Total Volume UP", Order = 2, GroupName = "09_TotalVolumeUP")]
		public double MinTotalVolumeUP
		{
			get { return upParameters[8].Min; }
			set { upParameters[8].Min = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Max Total Volume UP", Order = 3, GroupName = "09_TotalVolumeUP")]
		public double MaxTotalVolumeUP
		{
			get { return upParameters[8].Max; }
			set { upParameters[8].Max = value; }
		}
		
		// Now, let's add the DOWN parameters
		
		// Bar Delta DOWN
		[NinjaScriptProperty]
		[Display(Name = "Bar Delta DOWN Enabled", Order = 1, GroupName = "10_BarDeltaDOWN")]
		public bool BarDeltaDOWNEnabled
		{
			get { return downParameters[0].Enabled; }
			set { downParameters[0].Enabled = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Min Bar Delta DOWN", Order = 2, GroupName = "10_BarDeltaDOWN")]
		public double MinBarDeltaDOWN
		{
			get { return downParameters[0].Min; }
			set { downParameters[0].Min = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Max Bar Delta DOWN", Order = 3, GroupName = "10_BarDeltaDOWN")]
		public double MaxBarDeltaDOWN
		{
			get { return downParameters[0].Max; }
			set { downParameters[0].Max = value; }
		}
		
		// Delta Percent DOWN
		[NinjaScriptProperty]
		[Display(Name = "Delta Percent DOWN Enabled", Order = 1, GroupName = "11_DeltaPercentDOWN")]
		public bool DeltaPercentDOWNEnabled
		{
			get { return downParameters[1].Enabled; }
			set { downParameters[1].Enabled = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Min Delta Percent DOWN", Order = 2, GroupName = "11_DeltaPercentDOWN")]
		public double MinDeltaPercentDOWN
		{
			get { return downParameters[1].Min; }
			set { downParameters[1].Min = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Max Delta Percent DOWN", Order = 3, GroupName = "11_DeltaPercentDOWN")]
		public double MaxDeltaPercentDOWN
		{
			get { return downParameters[1].Max; }
			set { downParameters[1].Max = value; }
		}
		
		// Max Positive Delta DOWN
		[NinjaScriptProperty]
		[Display(Name = "Max Positive Delta DOWN Enabled", Order = 1, GroupName = "12_MaxPositiveDeltaDOWN")]
		public bool MaxPositiveDeltaDOWNEnabled
		{
			get { return downParameters[2].Enabled; }
			set { downParameters[2].Enabled = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Min Max Positive Delta DOWN", Order = 2, GroupName = "12_MaxPositiveDeltaDOWN")]
		public double MinMaxPositiveDeltaDOWN
		{
			get { return downParameters[2].Min; }
			set { downParameters[2].Min = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Max Max Positive Delta DOWN", Order = 3, GroupName = "12_MaxPositiveDeltaDOWN")]
		public double MaxMaxPositiveDeltaDOWN
		{
			get { return downParameters[2].Max; }
			set { downParameters[2].Max = value; }
		}
		
		// Max Negative Delta DOWN
		[NinjaScriptProperty]
		[Display(Name = "Max Negative Delta DOWN Enabled", Order = 1, GroupName = "13_MaxNegativeDeltaDOWN")]
		public bool MaxNegativeDeltaDOWNEnabled
		{
			get { return downParameters[3].Enabled; }
			set { downParameters[3].Enabled = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Min Max Negative Delta DOWN", Order = 2, GroupName = "13_MaxNegativeDeltaDOWN")]
		public double MinMaxNegativeDeltaDOWN
		{
			get { return downParameters[3].Min; }
			set { downParameters[3].Min = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Max Max Negative Delta DOWN", Order = 3, GroupName = "13_MaxNegativeDeltaDOWN")]
		public double MaxMaxNegativeDeltaDOWN
		{
			get { return downParameters[3].Max; }
			set { downParameters[3].Max = value; }
		}
		
		// Delta Change DOWN
		[NinjaScriptProperty]
		[Display(Name = "Delta Change DOWN Enabled", Order = 1, GroupName = "14_DeltaChangeDOWN")]
		public bool DeltaChangeDOWNEnabled
		{
			get { return downParameters[4].Enabled; }
			set { downParameters[4].Enabled = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Min Delta Change DOWN", Order = 2, GroupName = "14_DeltaChangeDOWN")]
		public double MinDeltaChangeDOWN
		{
			get { return downParameters[4].Min; }
			set { downParameters[4].Min = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Max Delta Change DOWN", Order = 3, GroupName = "14_DeltaChangeDOWN")]
		public double MaxDeltaChangeDOWN
		{
			get { return downParameters[4].Max; }
			set { downParameters[4].Max = value; }
		}
		
		// Total Buying Volume DOWN
		[NinjaScriptProperty]
		[Display(Name = "Total Buying Volume DOWN Enabled", Order = 1, GroupName = "15_TotalBuyingVolumeDOWN")]
		public bool TotalBuyingVolumeDOWNEnabled
		{
			get { return downParameters[5].Enabled; }
			set { downParameters[5].Enabled = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Min Total Buying Volume DOWN", Order = 2, GroupName = "15_TotalBuyingVolumeDOWN")]
		public double MinTotalBuyingVolumeDOWN
		{
			get { return downParameters[5].Min; }
			set { downParameters[5].Min = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Max Total Buying Volume DOWN", Order = 3, GroupName = "15_TotalBuyingVolumeDOWN")]
		public double MaxTotalBuyingVolumeDOWN
		{
			get { return downParameters[5].Max; }
			set { downParameters[5].Max = value; }
		}
		
		// Total Selling Volume DOWN
		[NinjaScriptProperty]
		[Display(Name = "Total Selling Volume DOWN Enabled", Order = 1, GroupName = "16_TotalSellingVolumeDOWN")]
		public bool TotalSellingVolumeDOWNEnabled
		{
			get { return downParameters[6].Enabled; }
			set { downParameters[6].Enabled = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Min Total Selling Volume DOWN", Order = 2, GroupName = "16_TotalSellingVolumeDOWN")]
		public double MinTotalSellingVolumeDOWN
		{
			get { return downParameters[6].Min; }
			set { downParameters[6].Min = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Max Total Selling Volume DOWN", Order = 3, GroupName = "16_TotalSellingVolumeDOWN")]
		public double MaxTotalSellingVolumeDOWN
		{
			get { return downParameters[6].Max; }
			set { downParameters[6].Max = value; }
		}
		
		// Trades DOWN
		[NinjaScriptProperty]
		[Display(Name = "Trades DOWN Enabled", Order = 1, GroupName = "17_TradesDOWN")]
		public bool TradesDOWNEnabled
		{
			get { return downParameters[7].Enabled; }
			set { downParameters[7].Enabled = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Min Trades DOWN", Order = 2, GroupName = "17_TradesDOWN")]
		public double MinTradesDOWN
		{
			get { return downParameters[7].Min; }
			set { downParameters[7].Min = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Max Trades DOWN", Order = 3, GroupName = "17_TradesDOWN")]
		public double MaxTradesDOWN
		{
			get { return downParameters[7].Max; }
			set { downParameters[7].Max = value; }
		}

		
		// Total Volume DOWN
		[NinjaScriptProperty]
		[Display(Name = "Total Volume DOWN Enabled", Order = 1, GroupName = "18_TotalVolumeDOWN")]
		public bool TotalVolumeDOWNEnabled
		{
			get { return downParameters[8].Enabled; }
			set { downParameters[8].Enabled = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Min Total Volume DOWN", Order = 2, GroupName = "18_TotalVolumeDOWN")]
		public double MinTotalVolumeDOWN
		{
			get { return downParameters[8].Min; }
			set { downParameters[8].Min = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Max Total Volume DOWN", Order = 3, GroupName = "18_TotalVolumeDOWN")]
		public double MaxTotalVolumeDOWN
		{
			get { return downParameters[8].Max; }
			set { downParameters[8].Max = value; }
		}

        #endregion

        // ... (reste du code inchangé)
    }
}
