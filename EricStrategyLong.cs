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
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class EricStrategyLong : Strategy
	{
		
		private bool firstHighDrawed = false;
		private string orderName = "LongLimit";
		private Order order = null;
		private double lastPrice = 0.0;
		private bool isTrailing = false;
		private bool isOpened = false;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "EricStrategyLong";
				Calculate									= Calculate.OnEachTick;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				LookBackValue					= 20;
				MainTickSize					= 75;
				ContractSize					= 1;
				tp								= 500;
				sl								= 75;
				TriggeredTrailing				= 75;
				TrailingValue					= 150;
				
				AddPlot(Brushes.Orange, "RecentHigh");
			}
			else if (State == State.Configure)
			{
				SetProfitTarget(orderName, CalculationMode.Ticks, tp, false);
			}
		}

		protected override void OnBarUpdate()
		{
//			if (CurrentBar < LookBackValue) return;
			
			if (State == State.Historical) return;
			
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				isTrailing = false;
			}
			
			int highestBarsAgo 		= HighestBar(High, LookBackValue);
			double highValueAgo 	= High[highestBarsAgo];
			
			if (!firstHighDrawed)
			{
				for (int i=0; i<highestBarsAgo; i++)
				{
					RecentHigh[i] = highValueAgo;
				}
				firstHighDrawed = true;
			}
			else
			{
				if (highValueAgo > RecentHigh[1])
				{
					RecentHigh[0] = highValueAgo;
					ChangeOrder(order, ContractSize, highValueAgo - MainTickSize * TickSize, 0);
				}
				else
					RecentHigh[0] = RecentHigh[1];
			}
			
			if (order == null && !isOpened)
			{
				order = EnterLongLimit(0, true, ContractSize, RecentHigh[0] - MainTickSize * TickSize, orderName);
				SetStopLoss(orderName, CalculationMode.Ticks, sl, false);
				isOpened = true;
			}
			
			if (Position.MarketPosition == MarketPosition.Long && !isTrailing)
		    {
		        double profitTicks = (Close[0] - order.AverageFillPrice) / TickSize;
		        
		        if (profitTicks >= TriggeredTrailing)
		        {
		            isTrailing = true;
					lastPrice = Close[0];
		        }
		    }
			
			if (isTrailing && Close[0] > lastPrice)
		    {
		        double newStopPrice = Close[0] - TrailingValue * TickSize;
		        SetStopLoss(orderName, CalculationMode.Price, newStopPrice, false);
				lastPrice = Close[0];
		    }
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="LookBackValue", Description="Number of bars to find the most recent high value", Order=1, GroupName="Parameters")]
		public int LookBackValue
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MainTickSize", Description="Main Tick Size", Order=2, GroupName="Parameters")]
		public int MainTickSize
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="ContractSize", Description="Contract Size", Order=3, GroupName="Parameters")]
		public int ContractSize
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1.0, double.MaxValue)]
		[Display(Name="TakeProfit", Description="TakeProfit Tick size", Order=4, GroupName="Parameters")]
		public double tp
		{ get; set; }
		
		
		[NinjaScriptProperty]
		[Range(1.0, double.MaxValue)]
		[Display(Name="Stoploss", Description="Stoploss Tick size", Order=5, GroupName="Parameters")]
		public double sl
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1.0, double.MaxValue)]
		[Display(Name="TriggeredTrailing", Description="Tick size to trigger Trailing", Order=6, GroupName="Parameters")]
		public double TriggeredTrailing
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1.0, double.MaxValue)]
		[Display(Name="TrailingValue", Description="Trailing Value", Order=7, GroupName="Parameters")]
		public double TrailingValue
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> RecentHigh
		{
			get { return Values[0]; }
		}
		#endregion

	}
}
