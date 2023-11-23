using System;
using System.Collections.Generic;
using System.Linq;
using FlaxEditor.Content.Settings;
using FlaxEditor.Surface.Archetypes;
using FlaxEngine;

namespace SCALE;

/// <summary>
/// AsteroidField Script.
/// </summary>
public class AsteroidField : Script
{
	public Actor Player;
	[CustomEditorAlias("FlaxEditor.CustomEditors.Editors.ActorLayerEditor")]
	public int AsteroidLayer;
	public float killDistance = 10000;
	public int maxAsteroids = 10;
	public Float2 asteroidScale = new Float2(1, 10);
	public Float2 asteroidSpawnDistance = new Float2(1000, 10000);

	private HashSet<Actor> asteroids = new HashSet<Actor>();

	public override void OnUpdate()
	{
		if (Physics.DefaultScene.Origin != MainRenderTask.Instance.View.Origin)
		{
			Physics.DefaultScene.Origin = MainRenderTask.Instance.View.Origin;
			Debug.Log($"Physics origin: {Physics.DefaultScene.Origin}");
		}

		// kill
		foreach (var asteroid in asteroids.ToList())
		{
			if (Vector3.Distance(asteroid.Position, Player.Position) > killDistance)
			{
				asteroids.Remove(asteroid);
				Destroy(asteroid);
			}
		}

		// spawn
		int sampleCount = 0;
		while (asteroids.Count < maxAsteroids && sampleCount < 64)
		{
			float angle = (RandomUtil.Random.NextSingle() * 2f - 1f) * Mathf.TwoPi;
			Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
			Vector3 position = Player.Position + direction * (RandomUtil.Random.NextSingle() * (asteroidSpawnDistance.Y - asteroidSpawnDistance.X) + asteroidSpawnDistance.X);
			float scale = RandomUtil.Random.NextSingle() * (asteroidScale.Y - asteroidScale.X) + asteroidScale.X;

			++sampleCount;
			if (Physics.CheckSphere(position, scale * 100))
				continue;

			var proceduralAsteroids = Parent.GetScript<ProceduralAsteroids>();
			var variant = proceduralAsteroids.GetRandomVariant();

			var asteroid = Parent.AddChild<StaticModel>();
			asteroid.Model = variant.Model;
			asteroid.Position = position;
			asteroid.EulerAngles = new Float3(0, 0, Random.Shared.NextSingle() * 360);
			//asteroid.Scale = Vector3.One * scale;

			var meshCollider = asteroid.AddChild<MeshCollider>();
			meshCollider.CollisionData = variant.CollisionData;
			meshCollider.Layer = AsteroidLayer;

			var asteroidTrigger = meshCollider.AddScript<AsteroidTrigger>();
			asteroidTrigger.Player = Player;

			asteroids.Add(asteroid);
		}
	}
}
