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
	public class IBArrowIndicator : Indicator
	{
		private DateTime ibStartTime;
		private DateTime ibEndTime;
		private double ibHigh;
		private double ibLow;
		private bool ibComplete;
	
		[NinjaScriptProperty]
		[Display(Name="Activer IB", Description="Activer l'Initial Balance", Order=1, GroupName="Paramètres")]
		public bool EnableIB { get; set; }
	
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Durée IB (minutes)", Description="Durée de l'Initial Balance en minutes", Order=2, GroupName="Paramètres")]
		public int IBDuration { get; set; }
	
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Ticks de breakout", Description="Nombre de ticks pour confirmer un breakout", Order=3, GroupName="Paramètres")]
		public int BreakoutTicks { get; set; }
	
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Indicateur Initial Balance avec flèches";
				Name = "IBArrowIndicator";
				EnableIB = true;
				IBDuration = 30;
				BreakoutTicks = 2;
				Calculate = Calculate.OnBarClose;
			}
		}
	
		protected override void OnBarUpdate()
		{
			if (!EnableIB) return;
	
			DateTime currentBarTime = Time[0];
	
			// Vérifier si c'est le début de l'IB (15:30 heure française)
			if (currentBarTime.TimeOfDay == new TimeSpan(13, 30, 0)) // 15:30 en heure locale (UTC-2)
			{
				ibStartTime = currentBarTime;
				ibEndTime = ibStartTime.AddMinutes(IBDuration);
				ibHigh = High[0];
				ibLow = Low[0];
				ibComplete = false;
			}
	
			// Pendant l'IB
			if (currentBarTime >= ibStartTime && currentBarTime <= ibEndTime)
			{
				ibHigh = Math.Max(ibHigh, High[0]);
				ibLow = Math.Min(ibLow, Low[0]);
				Draw.ArrowUp(this, "Up" + CurrentBar, false, 0, Low[0] - TickSize, Brushes.Green);
				Draw.ArrowDown(this, "Down" + CurrentBar, false, 0, High[0] + TickSize, Brushes.Red);
			}
	
			// Après l'IB
			if (currentBarTime > ibEndTime && !ibComplete)
			{
				ibComplete = true;
			}
	
			if (ibComplete)
			{
				double breakoutHighLevel = ibHigh + (BreakoutTicks * TickSize);
				double breakoutLowLevel = ibLow - (BreakoutTicks * TickSize);
	
				if (Close[0] > breakoutHighLevel)
				{
					Draw.ArrowUp(this, "UpBreakout" + CurrentBar, false, 0, Low[0] - TickSize, Brushes.Blue);
				}
				else if (Close[0] < breakoutLowLevel)
				{
					Draw.ArrowDown(this, "DownBreakout" + CurrentBar, false, 0, High[0] + TickSize, Brushes.Purple);
				}
				else
				{
					Draw.ArrowUp(this, "Up" + CurrentBar, false, 0, Low[0] - TickSize, Brushes.Green);
					Draw.ArrowDown(this, "Down" + CurrentBar, false, 0, High[0] + TickSize, Brushes.Red);
				}
			}
		}
	}
}
