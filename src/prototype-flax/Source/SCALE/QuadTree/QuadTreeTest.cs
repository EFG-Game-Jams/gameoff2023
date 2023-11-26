using System;
using System.Collections.Generic;
using FlaxEngine;

namespace SCALE;

/// <summary>
/// QuadTreeTest Script.
/// </summary>
public class QuadTreeTest : Script
{
    public int itemCount = 1024;
    public Rectangle bounds = new Rectangle(-1000, -1000, 2000, 2000);

    public Actor samplePoint;
    public float sampleRadius = 5000f;
    public bool sampleUsingBruteForce;

    public float lastQueryTimeMs;

    private QuadTree tree;
    private List<EmptyActor> items;

    /// <inheritdoc/>
    public override void OnStart()
    {
        tree = Actor.AddScript<QuadTree>();
        tree.Bounds = new(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        tree.MaxItemsPerNode = 16;

        items = new();
		for (int i = 0; i < itemCount; i++)
        {
			EmptyActor actor = new EmptyActor();
			actor.Position = new Vector3(Random.Shared.NextSingle() * bounds.Width + bounds.X, Random.Shared.NextSingle() * bounds.Height + bounds.Y, 0);
			tree.Insert(actor);
            items.Add(actor);
		}
    }

	public override void OnDebugDraw()
	{
        if (Time.GamePaused)
            return;
		//foreach (var item in items)
		//	DebugDraw.DrawCircle(item.Position, Float3.Backward, 50f, Color.Green);
	}

	/// <inheritdoc/>
	public override void OnUpdate()
    {
        if (samplePoint == null)
			return;

        var rect = new QuadTree.Rect(samplePoint.Position.X - sampleRadius, samplePoint.Position.Y - sampleRadius, sampleRadius * 2, sampleRadius * 2);
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        List<Actor> items = null;
        if (sampleUsingBruteForce)
        {
            items = new List<Actor>();
            float d2 = sampleRadius * sampleRadius;
            foreach (var actor in this.items)
            {
                if (Float3.DistanceSquared(actor.Position, samplePoint.Position) <= d2)
                    items.Add(actor);
            }
        }
        else
        {
            items = tree.Query(rect);
        }
        lastQueryTimeMs = (float)stopwatch.Elapsed.TotalMilliseconds;

        foreach (var item in items)
        {
            if (Float3.Distance(item.Position, samplePoint.Position) <= sampleRadius)
    			DebugDraw.DrawCircle(item.Position, Float3.Backward, 5f, Color.Green);
            else
    			DebugDraw.DrawCircle(item.Position, Float3.Backward, 5f, Color.Red);
			// draw a line to the sample point
			//DebugDraw.DrawLine(item.Position, samplePoint.Position, Color.Blue);
		}

        var cells = tree.QueryCells(rect, false);
        foreach (var cell in cells)
        {
            // draw a rectangle around the cell
            var r = cell;
            /*r.X += 10;
            r.Y += 10;
            r.Width -= 20;
            r.Height -= 20;*/
            DebugDraw.DrawWireBox(new BoundingBox((Vector3)r.Min, (Vector3)r.Max), Color.Gray);
		}
    }
}
