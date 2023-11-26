using System;
using System.Collections.Generic;
using FlaxEngine;

namespace SCALE;

public class LevelInitialiser : Script
{
    public Level level;
    public QuadTree quadTree;

    public override void OnStart()
    {
        Vector3 min = Vector3.Maximum;
        Vector3 max = Vector3.Minimum;

        // scale positions of all actors
        foreach (Actor actor in level.Children)
        {
            Vector3 position = actor.Position;
            position *= level.WorldScale;
            min = Vector3.Min(min, position);
            max = Vector3.Max(max, position);
            actor.Position = position;
        }

        // build quad tree
        quadTree.Bounds = new QuadTree.Rect((Vector2)min, (Vector2)max);
        foreach (Actor actor in level.Children)
			quadTree.Insert(actor);
    }
}
