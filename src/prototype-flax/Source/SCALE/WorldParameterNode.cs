using System;
using System.Collections.Generic;
using FlaxEngine;

namespace SCALE;

/// <summary>
/// Represents a point in the world with associated parameters
/// </summary>
[ActorContextMenu("New/SCALE/WorldParameterNode")]
public class WorldParameterNode : StaticModel
{
	[Serialize, ShowInEditor] private WorldLocalParameters parameters;

	[NoSerialize, HideInEditor]
	public WorldLocalParameters Parameters
	{
		get
		{
			var p = parameters;
			p.Position = (Vector2)Position;
			return p;
		}
		set
		{
			parameters = value;
		}
	}

	public override void OnDebugDraw()
	{
		base.OnDebugDraw();

		string s =
			$"Position: {parameters.Position}\n" +
			$"AsteroidDensity: {parameters.AsteroidDensity}\n" +
			$"AsteroidSize: {parameters.AsteroidSize}\n" +
			$"AsteroidLinearSpeed: {parameters.AsteroidLinearSpeed}\n" +
			$"AsteroidAngularSpeed: {parameters.AsteroidAngularSpeed}\n";
		DebugDraw.DrawText(s, Transform, Color.Wheat);
	}
}
