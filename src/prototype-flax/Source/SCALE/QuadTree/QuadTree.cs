using System.Collections.Generic;
using FlaxEngine;

public class Quadtree : Script
{
	private class Node
	{
		public Rectangle Bounds;
		public List<Actor> Items;
		public Node[] Children;

		public Node(Rectangle bounds)
		{
			Bounds = bounds;
			Items = new();
			Children = null;
		}

		public bool IsLeaf => Children == null;
	}

	private Node root;

	public Rectangle Bounds
	{
		get => root.Bounds;
		set => root = new Node(value);
	}
	public int MaxItemsPerNode { get; set; } = 4;
	
	public Quadtree()
	{
	}

	public void Insert(Actor item)
	{
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
		float halfWidth = node.Bounds.Width * 0.5f;
		float halfHeight = node.Bounds.Height * 0.5f;
		float x = node.Bounds.X;
		float y = node.Bounds.Y;

		// Creating four child nodes
		node.Children = new Node[4];
		node.Children[0] = new Node(new Rectangle(x, y, halfWidth, halfHeight)); // Top left
		node.Children[1] = new Node(new Rectangle(x + halfWidth, y, halfWidth, halfHeight)); // Top right
		node.Children[2] = new Node(new Rectangle(x, y + halfHeight, halfWidth, halfHeight)); // Bottom left
		node.Children[3] = new Node(new Rectangle(x + halfWidth, y + halfHeight, halfWidth, halfHeight)); // Bottom right

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

	private void Query(Rectangle area, List<Actor> result)
	{
		Query(area, root, result);
	}
	public List<Actor> Query(Rectangle area)
	{
		var result = new List<Actor>();
		Query(area, result);
		return result;
	}	
	private void Query(Rectangle area, Node node, List<Actor> result)
	{
		if (!node.Bounds.Intersects(ref area))
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

	public List<Rectangle> QueryCells(Rectangle area, bool leafNodesOnly)
	{
		var result = new List<Rectangle>();
		QueryCells(area, leafNodesOnly, result);
		return result;
	}
	public void QueryCells(Rectangle area, bool leafNodesOnly, List<Rectangle> result)
	{
		QueryCells(area, root, leafNodesOnly, result);
	}
	private void QueryCells(Rectangle area, Node node, bool leafNodesOnly, List<Rectangle> result)
	{
		if (!node.Bounds.Intersects(ref area))
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
}
