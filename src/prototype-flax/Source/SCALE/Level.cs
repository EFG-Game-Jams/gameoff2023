using System;
using System.Collections.Generic;
using FlaxEngine;

namespace SCALE;

[ActorContextMenu("New/SCALE/Level")]
public class Level : Actor
{
	[ShowInEditor, Serialize] private float worldScale;

	public float WorldScale => worldScale;
	[Serialize, ShowInEditor] public QuadTree QuadTree { get; private set; }
}
