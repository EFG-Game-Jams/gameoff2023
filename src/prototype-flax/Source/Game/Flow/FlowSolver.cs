using FlaxEngine.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;

namespace Game.Flow;

public class FlowSolver
{
	private class NodeData
	{
		public IFlowNode owner;
		public FlowUnitStore content = new();
		public FlowUnitStore inbound = new();

		public void Clear()
		{
			content.Clear();
			inbound.Clear();
		}
	}
	private class EdgeData
	{
		public IFlowEdge owner;
		public NodeData nodeIn;
		public NodeData nodeOut;
		public FlowUnitStore content = new();
		public FlowUnitStore input = new();
		public FlowUnitStore output = new();

		public void Clear()
		{
			content.Clear();
			input.Clear();
			output.Clear();
		}
	}

	private Dictionary<IFlowNode, NodeData> nodeToData;
	private List<NodeData> nodes;
	private List<EdgeData> edges;

	public FlowSolver(IEnumerable<IFlowNode> nodes, IEnumerable<IFlowEdge> edges)
	{
		nodeToData = new();
		this.nodes = new();
		nodes.ForEach(node =>
		{
			NodeData data = new() { owner = node };
			nodeToData.Add(node, data);
			this.nodes.Add(data);
		});
		
		this.edges = new(edges.Select(e => new EdgeData
		{
			owner = e,
			nodeIn = nodeToData[e.Input],
			nodeOut = nodeToData[e.Output]
		}));
	}

	public void Step()
	{
		ShuffleEdges();

		// prepare
		foreach (var node in nodes)
		{
			node.Clear();
			node.owner.OnBeginStep();
			node.owner.CollectContent(node.content);
		}
        foreach (var edge in edges)
		{
			edge.Clear();
			edge.owner.OnBeginStep();
			// edge content is collected after output is consumed
		}

		// receive
		foreach (var edge in edges)
		{
			while (edge.owner.TryPull(out FlowUnit unit))
			{
				edge.nodeOut.content.Add(unit);
				edge.output.Add(unit);
			}			
			edge.owner.CollectContent(edge.content); // collect remaining edge content
			edge.nodeOut.inbound.Add(edge.content); // and add it to the node inbound
		}

		// produce and interact
		foreach (var node in nodes)
		{
			node.owner.ProcessGain(node.content);
			node.owner.ProcessInteraction(node.content);
		}

		// send
		List<FlowUnit> flowBuffer = new();
		foreach (var edge in edges)
		{
			var from = edge.nodeIn;
			var to = edge.nodeOut;

			flowBuffer.Clear();			
			foreach (var fromUnit in from.content.Units)
			{
				long type = fromUnit.Type;
				if (!edge.owner.CanFlow(type))
					continue;

				long flow = ComputeFlow(from, to, type);
				if (flow > 0)
					flowBuffer.Add(new(type, flow));
			}

			foreach (var desiredFlow in flowBuffer)
			{
				var flow = edge.owner.TryPush(desiredFlow);
				if (flow.Amount == 0)
					continue;
				edge.output.Add(flow);
				from.content.Remove(flow);
				to.inbound.Add(flow);
			}
		}

		// consume and apply
		foreach (var node in nodes)
		{
			node.owner.ProcessLoss(node.content);
			node.owner.ApplyResults(node.content);
		}
	}

	private long ComputeFlow(NodeData from, NodeData to, long type)
	{
		var fromRule = from.owner.GetFlowRule(type);
		var toRule = to.owner.GetFlowRule(type);

		if (!fromRule.allowOutflow || !toRule.allowInflow)
			return 0; // no flow allowed

		var fromContent = from.content.Get(type).Amount;
		var fromInbound = from.inbound.Get(type).Amount;
		var fromFuture = fromContent + fromInbound;
		var toContent = to.content.Get(type).Amount;
		var toInbound = to.inbound.Get(type).Amount;
		var toFuture = toContent + toInbound;

		// compute perfect flow
		double idealFlow = 0;
		var fromEquilibrium = fromRule.equilibrium;
		var toEquilibrium = toRule.equilibrium;
		if (fromEquilibrium == 0 && toEquilibrium != 0)
		{
			// overflow => balanced, reach future equilibrium at destination
			idealFlow = toEquilibrium - toFuture;
		}
		else if (fromEquilibrium != 0 && toEquilibrium == 0)
		{
			// balanced => overflow, reach immediate equilibrium at source
			idealFlow = fromContent - fromEquilibrium;
		}
		else
		{
			if (fromEquilibrium == 0)
			{
				// overflow => overflow, target equilibrium at mean pressure
				double fromPressure = fromFuture / (double)fromRule.capacity;
				double toPressure = toFuture / (double)toRule.capacity;
				double meanPressure = (fromPressure + toPressure) / 2;
				fromEquilibrium = (long)Math.Round(fromRule.capacity * meanPressure);
				toEquilibrium = (long)Math.Round(toRule.capacity * meanPressure);
			}

			// at equiilibirum, pressures are equal
			// pressure = future / equilibrium
			// (fromFuture - flow) / fromEquilibrium = (toFuture + flow) / toEquilibrium
			idealFlow = (fromFuture * toEquilibrium - toFuture * fromEquilibrium) / (double)(fromEquilibrium + toEquilibrium);
		}

		// restricted flow
		long flow = (long)Math.Floor(idealFlow);
		flow = Math.Min(flow, fromContent); // don't underflow source
		flow = Math.Min(flow, toRule.capacity - (toContent + toInbound)); // don't overflow destination
		flow = Math.Max(0, flow); // forward flow only

		return flow;
	}

	private void ShuffleEdges()
	{
		var rng = Random.Shared;
		for (int i = edges.Count - 1; i > 1; i--)
		{
			int j = rng.Next(i + 1);
			var temp = edges[i];
			edges[i] = edges[j];
			edges[j] = temp;
		}
	}
}
