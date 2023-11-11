using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FlaxEditor.Surface.Archetypes;
using FlaxEngine;

namespace Game.Flow.Test;

public class FlowSolverTest : Script
{
	[System.Serializable]
	public struct Descriptor
	{
		public List<FlowNodeTest.Descriptor> Nodes;
		public List<FlowEdgeTest.Descriptor> Edges;
	}

	public Prefab prefabNode;
	public Prefab prefabEdge;

	private FlowNodeTest[] nodes;
	private FlowEdgeTest[] edges;
	private FlowSolver solver;

	private IEnumerator activeScenario;

	public override void OnStart()
    {
		LoadScenario(TestScenarioComplexBalance);
		solver = new(nodes, edges);
	}
	public override void OnUpdate()
	{
		if (activeScenario != null)
		{
			if (!activeScenario.MoveNext())
				activeScenario = null;
		}
		solver.Step();
	}

	private void Load(in Descriptor descriptor)
	{
		nodes = new FlowNodeTest[descriptor.Nodes.Count];
		for (int i = 0; i < nodes.Length; ++i)
		{
			nodes[i] = PrefabManager.SpawnPrefab(prefabNode, Actor).GetScript<FlowNodeTest>();
			nodes[i].descriptor = descriptor.Nodes[i];
		}

		FlowNodeTest findNode(string name) => nodes.First(n => n.descriptor.Name == name);

		edges = new FlowEdgeTest[descriptor.Edges.Count];
		for (int i = 0; i < edges.Length; ++i)
		{
			edges[i] = PrefabManager.SpawnPrefab(prefabEdge, Actor).GetScript<FlowEdgeTest>();
			edges[i].descriptor = descriptor.Edges[i];
			edges[i].SetNodes(findNode(edges[i].descriptor.From), findNode(edges[i].descriptor.To));
		}
	}

	class DescriptorBuilder
	{
		public Descriptor Result = new Descriptor { Nodes = new(), Edges = new() };

		public int AddNode(FlowNodeTest.Descriptor descriptor)
		{
			if (descriptor.Name == null)
				descriptor.Name = Result.Nodes.Count.ToString();
			Result.Nodes.Add(descriptor);
			return Result.Nodes.Count - 1;
		}
		public void AddEdge(int from, int to, long capacity, long latency, bool twoWay = false)
		{
			Result.Edges.Add(new() { From = Result.Nodes[from].Name, To = Result.Nodes[to].Name, Capacity = capacity, Latency = latency });
			if (twoWay)
				AddEdge(to, from, capacity, latency, false);
		}
	}

	private void LoadScenario(Action<DescriptorBuilder> generate)
	{
		var builder = new DescriptorBuilder();
		generate(builder);
		Load(builder.Result);
	}

	private void TestScenarioSimpleBalance(DescriptorBuilder db)
	{
		db.AddNode(new() { Position = new Vector3(-250,0,0), Rule = new() { allowInflow = true, allowOutflow = true, capacity = 1000, equilibrium = 0 } });
		db.AddNode(new() { Position = new Vector3(250,0,0), Rule = new() { allowInflow = true, allowOutflow = true, capacity = 1000, equilibrium = 0 } });
		db.AddEdge(0, 1, 10, 10, true);
		IEnumerator scenario()
		{
			yield return null;
			nodes[0].SetFill(.5f);
			yield break;
		}
		activeScenario = scenario();
	}

	private void TestScenarioComplexBalance(DescriptorBuilder db)
	{
		for (int i=0; i < 10; i++)
		{
			for (int j =0; j < 10; j++)
			{
				Vector3 pos = new(i, j, 0);
				int index = db.AddNode(new() { Position = pos*250, Rule = new() { allowInflow = true, allowOutflow = true, capacity = 2000, equilibrium = 0 } });
				if (i > 0)
					db.AddEdge(index - 10, index, 100, 10, true);
				if (j > 0)
					db.AddEdge(index - 1, index, 100, 10, true);
			}
		}

		long ioCapacity = 100000;
		int input = db.AddNode(new() { Position = new(-1*250, 0, 0), Rule = new() { allowInflow = true, allowOutflow = true, capacity = ioCapacity, equilibrium = 0 }, Production = 100 });
		int output = db.AddNode(new() { Position = new(10*250, 0, 0), Rule = new() { allowInflow = true, allowOutflow = true, capacity = ioCapacity, equilibrium = 0 }, Consumption = 100 });
		db.AddEdge(input, 0, 200, 10, true);
		db.AddEdge(output, 90, 200, 10, true);

		IEnumerator scenario()
		{
			yield return null;
			nodes[input].SetFill(1f);
			yield break;
		}
		activeScenario = scenario();
	}
}