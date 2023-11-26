using System.Collections.Generic;
using FlaxEngine;
using FlaxEngine.Assertions;

#if USE_LARGE_WORLDS
using Real = System.Double;
using Mathr = FlaxEngine.Mathd;
#else
using Real = System.Single;
using Mathr = FlaxEngine.Mathf;
#endif

public class QuadTree : Script
{
	public struct Rect
	{
		public Vector2 Min;
		public Vector2 Max;

		public Real Width => Max.X - Min.X;
		public Real Height => Max.Y - Min.Y;

		public Rect(Vector2 min, Vector2 max)
		{
			Min = min;
			Max = max;
		}
		public Rect(Real x0, Real y0, Real width, Real height)
			: this(new Vector2(x0, y0), new Vector2(x0 + width, y0 + height))
		{
		}
		public bool Contains(Vector2 point)
		{
			return point.X >= Min.X && point.X <= Max.X && point.Y >= Min.Y && point.Y <= Max.Y;
		}
		public bool Intersects(in Rect other)
		{
			return Min.X <= other.Max.X && Max.X >= other.Min.X && Min.Y <= other.Max.Y && Max.Y >= other.Min.Y;
		}
	}

	private class Node
	{
		public Rect Bounds;
		public List<Actor> Items;
		public Node[] Children;

		public Node(Rect bounds)
		{
			Bounds = bounds;
			Items = new();
			Children = null;
		}

		public bool IsLeaf => Children == null;
	}

	private Rect bounds;
	private Node root;

	public Rect Bounds
	{
		get => bounds;
		set
		{
			bounds = value;
			Assert.IsNull(root);
		}
	}
	public int MaxItemsPerNode { get; set; } = 4;
	
	public QuadTree()
	{
	}

	public void Insert(Actor item)
	{
		root ??= new(bounds);
		Insert(item, root);
	}

	private void Insert(Actor item, Node node)
	{
		Vector2 position = (Vector2)item.Position;

		// If the probe is not within this node, return
		if (!node.Bounds.Contains(position))
			return;

		if (node.IsLeaf)
		{
			// Add the probe to the node
			node.Items.Add(item);

			// Split the node if needed
			if (node.Items.Count > MaxItemsPerNode)
				Split(node);
		}
		else
		{
			// Insert the probe into the appropriate child
			foreach (var child in node.Children)
				Insert(item, child);
		}
	}

	private void Split(Node node)
	{
		var halfWidth = node.Bounds.Width / 2;
		var halfHeight = node.Bounds.Height / 2;
		var x = node.Bounds.Min.X;
		var y = node.Bounds.Min.Y;

		// Creating four child nodes
		node.Children = new Node[4];
		node.Children[0] = new Node(new Rect(x, y, halfWidth, halfHeight)); // Top left
		node.Children[1] = new Node(new Rect(x + halfWidth, y, halfWidth, halfHeight)); // Top right
		node.Children[2] = new Node(new Rect(x, y + halfHeight, halfWidth, halfHeight)); // Bottom left
		node.Children[3] = new Node(new Rect(x + halfWidth, y + halfHeight, halfWidth, halfHeight)); // Bottom right

		// Reassign items to children
		foreach (Actor item in node.Items)
		{
			Vector2 position = (Vector2)item.Position;
			foreach (var child in node.Children)
			{
				if (child.Bounds.Contains(position))
				{
					child.Items.Add(item);
					break;
				}
			}
		}

		// Clear items from the current node after reassigning
		node.Items.Clear();
	}

	public void Query(Rect area, List<Actor> result)
	{
		Query(area, root, result);
	}
	public List<Actor> Query(Rect area)
	{
		var result = new List<Actor>();
		Query(area, result);
		return result;
	}	
	private void Query(Rect area, Node node, List<Actor> result)
	{
		if (!node.Bounds.Intersects(area))
			return;

		if (node.IsLeaf)
		{
			foreach (var probe in node.Items)
			{
				Vector2 position = (Vector2)probe.Position;
				if (area.Contains(position))
					result.Add(probe);
			}
		}
		else
		{
			foreach (var child in node.Children)
				Query(area, child, result);
		}
	}

	public List<Rect> QueryCells(Rect area, bool leafNodesOnly)
	{
		var result = new List<Rect>();
		QueryCells(area, leafNodesOnly, result);
		return result;
	}
	public void QueryCells(Rect area, bool leafNodesOnly, List<Rect> result)
	{
		QueryCells(area, root, leafNodesOnly, result);
	}
	private void QueryCells(Rect area, Node node, bool leafNodesOnly, List<Rect> result)
	{
		if (!node.Bounds.Intersects(area))
			return;

		if (node.IsLeaf)
		{
			result.Add(node.Bounds);
		}
		else
		{
			if (!leafNodesOnly)
				result.Add(node.Bounds);
			foreach (var child in node.Children)
				QueryCells(area, child, leafNodesOnly, result);
		}
	}

	public override void OnDebugDraw()
	{
		DebugDraw.DrawWireBox(new BoundingBox((Vector3)bounds.Min, (Vector3)bounds.Max), Color.Wheat);
	}
}
