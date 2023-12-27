using System;
using System.Collections.Generic;
using FlaxEngine;

namespace SCALE;

public class WorldParameterSampler : Script
{
	public Level level;
	public float radius = 1000;

	private WorldLocalParameters.Interpolator interpolator;
	private List<Actor> queryResult;

	public WorldLocalParameters Sample()
	{
		interpolator ??= new();
		interpolator.Clear();

		queryResult ??= new();
		queryResult.Clear();

		Vector2 position = (Vector2)Parent.Position;
		Vector2 halfExtents = Vector2.One * radius;
		var rect = new QuadTree.Rect(position - halfExtents, position + halfExtents);
		level.QuadTree.Query(rect, queryResult);

		interpolator.Clear();
		foreach (var actor in queryResult)
		{
			if (actor is WorldParameterNode node)
				interpolator.Add(node.Parameters);
		}

		return interpolator.Sample(position);
	}
}
