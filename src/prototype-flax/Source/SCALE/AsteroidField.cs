using System;
using System.Collections.Generic;
using System.Linq;
using FlaxEngine;
using Material = FlaxEngine.Material;

namespace SCALE;

/// <summary>
/// AsteroidField Script.
/// </summary>
public class AsteroidField : Script
{
	public RigidBody Player;
	public Camera Camera;

	[CustomEditorAlias("FlaxEditor.CustomEditors.Editors.ActorLayerEditor")]
	public int AsteroidLayer;
	public double AsteroidDespawnRadius = 10_000.0;
	public int MaxAsteroids = 25;
	public Float2 asteroidScale = new Float2(1, 10);
	public Material asteroidMaterial;

	// radians per second
	public double AsteroidMaxRotationSpeed = Mathd.Pi;
	public double AsteroidMaxVelocity = 100.0;

	private HashSet<Actor> asteroids = new();

	public override void OnUpdate()
	{
		if (Physics.DefaultScene.Origin != MainRenderTask.Instance.View.Origin)
		{
			Physics.DefaultScene.Origin = MainRenderTask.Instance.View.Origin;
			Debug.Log($"Physics origin: {Physics.DefaultScene.Origin}");
		}

		DespawnAsteroidsTooFarAway();

		var toSpawnAsteroidCount = Math.Max(0, MaxAsteroids - asteroids.Count);
		SpawnNewAsteroids(
			toSpawnAsteroidCount,
			allowSpawnInView: asteroids.Count == 0);
	}

	private void DespawnAsteroidsTooFarAway()
	{
		foreach (var asteroid in asteroids.ToList())
		{
			if (Vector3.Distance(asteroid.Position, Player.Position) > AsteroidDespawnRadius)
			{
				asteroids.Remove(asteroid);
				Destroy(asteroid);
			}
		}
	}

	private void SpawnNewAsteroids(int count, bool allowSpawnInView)
	{
		const int sampleLimit = 64;
		int sampleCount = 0;

		float cameraDepth = (float)(-1 * Camera.Position.Z);
		var fustrumWidthPlayerPlane = Camera.Frustum.GetWidthAtDepth(cameraDepth);
		var fustrumHeightPlayerPlane = Camera.Frustum.GetHeightAtDepth(cameraDepth);

		var normalizedPlayerDirection = Player.LinearVelocity.Normalized;
		var minimumSpawnRadius = 0.0;
		if (!allowSpawnInView)
		{
			minimumSpawnRadius = Math.Sqrt(fustrumWidthPlayerPlane * fustrumWidthPlayerPlane + fustrumHeightPlayerPlane * fustrumHeightPlayerPlane) / 2.0;
		}
		var maximumSpawnRadius = AsteroidDespawnRadius - 100;
		if (minimumSpawnRadius >= maximumSpawnRadius)
		{
			Debug.LogError($"The minimum asteroid spawn distance {minimumSpawnRadius} is greater or equal to the maximum asteroid spawn distance {maximumSpawnRadius}");
			return;
		}

		for (int i = 0; i < count; ++i)
		{
			var proceduralAsteroids = Parent.GetScript<ProceduralAsteroids>();
			var variant = proceduralAsteroids.GetRandomVariant();
			var asteroidBoundingBox = variant.Model.GetBox();
			var maximumBoundingBoxRadius = Math.Max(Math.Max(asteroidBoundingBox.Size.X, asteroidBoundingBox.Size.Y), asteroidBoundingBox.Size.Z) / 2.0;

			Vector3 randomDirection;
			if (normalizedPlayerDirection.X + normalizedPlayerDirection.Y + normalizedPlayerDirection.Z < 0.1)
			{
				randomDirection = new Vector3(GetRandomUnitVector2(), 0);
			}
			else
			{
				// The player is moving, only spawn in the general direction of the movement
				var ninetyDegrees = Mathd.Pi / 2.0;
				var randomAngleOffset = RandomUtil.Rand() * ninetyDegrees - (ninetyDegrees / 2.0); // Some angle between -45 and 45 degrees

				var playerDirectionAsAngle = Vector3.Angle(normalizedPlayerDirection, new Vector3(0, 1, 0)) * Mathd.DegreesToRadians;
				var randomAngle = randomAngleOffset + playerDirectionAsAngle;

				randomDirection = new Vector3(Mathd.Cos(randomAngle), Mathd.Sin(randomAngle), 0);
			}

			while (true)
			{
				if (sampleCount >= sampleLimit)
				{
					Debug.LogWarning("Reached sample limit for spawning asteroids");
					return;
				}
				++sampleCount;

				var offset = randomDirection * (minimumSpawnRadius + RandomUtil.Rand() * (maximumSpawnRadius - minimumSpawnRadius));
				Vector3 candidatePosition = Player.Position + offset;

				if (Physics.CheckSphere(
					candidatePosition,
					(float)maximumBoundingBoxRadius,
					hitTriggers: true))
				{
					continue;
				}

				var asteroid = Parent.AddChild<RigidBody>();
				asteroid.Position = candidatePosition;
				asteroid.EulerAngles = new Float3(0, 0, Random.Shared.NextSingle() * 360);
				asteroid.EnableGravity = false;
				asteroid.StaticFlags = StaticFlags.None;
				asteroid.Constraints = RigidbodyConstraints.LockPositionZ;

				asteroid.LinearVelocity = new Vector3(GetRandomUnitVector2() * AsteroidMaxVelocity, 0);
				asteroid.AngularVelocity = new(
					Random.Shared.NextSingle() * AsteroidMaxRotationSpeed,
					Random.Shared.NextSingle() * AsteroidMaxRotationSpeed,
					Random.Shared.NextSingle() * AsteroidMaxRotationSpeed);

				var asteroidModel = asteroid.AddChild<StaticModel>();
				asteroidModel.Model = variant.Model;
				asteroidModel.SetMaterial(0, asteroidMaterial);

				var meshCollider = asteroid.AddChild<MeshCollider>();
				meshCollider.CollisionData = variant.CollisionData;
				meshCollider.Layer = AsteroidLayer;

				// Override the collider set mass
				asteroid.Mass = 3;

				asteroids.Add(asteroid);
				break;
			}
		}
	}

	private static Vector2 GetRandomUnitVector2()
	{
		var randomAngle = (RandomUtil.Random.NextSingle() * 2.0 - 1.0) * Mathd.TwoPi;
		return new(Mathd.Cos(randomAngle), Mathd.Sin(randomAngle));
	}

	private Vector3 GetRandomAsteroidSpawnPosition(BoundingSphere boundingSphere)
	{
		throw new NotImplementedException();
	}
}
