using System;
using System.Collections.Generic;
using System.Linq;
using FlaxEngine;
using FlaxEngine.Utilities;

namespace Game.Flow.Test;

public class FlowNodeTest : Script, IFlowNode
{
	[System.Serializable]
	public struct Descriptor
	{
		public Vector3 Position;
		public string Name;
		public long Faction;
		public long Production;
		public long Consumption;
		public FlowRule Rule;
	}
	public Descriptor descriptor;

	private TextRender label;
	private FlowUnitStore containedUnits = new();

	public override void OnStart()
	{
		label = Actor.GetChild<TextRender>();
	}
	public override void OnLateUpdate()
	{
		label.Text = $"{descriptor.Name}\n{containedUnits.GetTotalAmount()}\n{descriptor.Rule.capacity}";
		Actor.Position = descriptor.Position;
	}

	public void SetFill(float fill)
	{
		containedUnits.Set(new FlowUnit(descriptor.Faction, (long)(fill * descriptor.Rule.capacity)));
	}

	void IFlowNode.OnBeginStep()
	{
		
	}
	void IFlowNode.ApplyResults(FlowUnitStore content)
	{
		containedUnits.Set(content);
	}

	void IFlowNode.CollectContent(FlowUnitStore collector)
		=> collector.Add(containedUnits);
	FlowRule IFlowNode.GetFlowRule(long unitType)
		=> descriptor.Rule;
	void IFlowNode.ProcessGain(FlowUnitStore content)
		=> content.Add(new FlowUnit(descriptor.Faction, descriptor.Production));
	void IFlowNode.ProcessLoss(FlowUnitStore content)
		=> content.EnumerableTypes.ForEach(t
			=> content.Remove(new FlowUnit(t, descriptor.Consumption)));

	void IFlowNode.ProcessInteraction(FlowUnitStore content)
	{
		if (content.Units.Count < 2)
			return;

		long minUnits = content.Units.Min(u => u.Amount);
		content.EnumerableTypes.ForEach(t => content.Remove(new FlowUnit(t, minUnits)));
	}
}
