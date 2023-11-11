using System;
using System.Collections.Generic;
using System.Linq;
using FlaxEngine;
using FlaxEngine.Utilities;

namespace Game.Flow.Test;

public class FlowEdgeTest : Script, IFlowEdge
{
	[System.Serializable]
	public struct Descriptor
	{
		public string From;
		public string To;
		public long Capacity;
		public long Latency;
	}

	public Descriptor descriptor;
	private TextRender label;

	public IFlowNode Input { get; private set; }
	public IFlowNode Output { get; private set; }

	private struct InTransit
	{
		public FlowUnit Unit;
		public long Position;
	}

	private List<InTransit> inTransits = new();
	private long availableCapacity;

	public override void OnStart()
	{
		label = Actor.GetChild<TextRender>();
	}
	public override void OnLateUpdate()
	{
		label.Text = $"{inTransits.Sum(i => i.Unit.Amount)}";
		label.Color = Color.Lerp(Color.Red, Color.Green, (inTransits.Sum(i => i.Unit.Amount) / (float)descriptor.Capacity));
		Actor.Position = ((Input as FlowNodeTest).Actor.Position + (Output as FlowNodeTest).Actor.Position) / 2;
	}

	public void SetNodes(FlowNodeTest from, FlowNodeTest to)
	{
		Input = from;
		Output = to;
		descriptor.From = from.descriptor.Name;
		descriptor.To = to.descriptor.Name;
	}

	void IFlowEdge.OnBeginStep()
	{
		availableCapacity = descriptor.Capacity;

		for (int i = 0; i < inTransits.Count; i++)
		{
			var inTransit = inTransits[i];
			++inTransit.Position;
			inTransits[i] = inTransit;
		}
	}
	void IFlowEdge.CollectContent(FlowUnitStore collector)
	{
		foreach (var inTransit in inTransits)
			collector.Add(inTransit.Unit);
	}
	bool IFlowEdge.TryPull(out FlowUnit unit)
	{
		for (int i = 0; i < inTransits.Count; i++)
		{
			InTransit inTransit = inTransits[i];
			if (inTransit.Position < descriptor.Latency)
				continue;
			unit = inTransit.Unit;
			inTransits.RemoveAt(i);
			return true;
		}
		unit = default;
		return false;
	}
	FlowUnit IFlowEdge.TryPush(in FlowUnit unit)
	{
		FlowUnit push = new(unit.Type, Math.Min(unit.Amount, availableCapacity));
		if (push.Amount > 0)
		{
			availableCapacity -= push.Amount;
			inTransits.Add(new() { Unit = push, Position = 0 });
		}
		return push;
	}
}
