using System;
using System.Collections.Generic;
using System.Linq;
using FlaxEngine;
using Material = FlaxEngine.Material;

namespace SCALE;

public class NpcSpawner : Script
{
	public RigidBody Player;
	public Camera Camera;
	public WorldParameterSampler WorldParameterSampler;

	public double DespawnRadius = 10_000.0;

	#region Asteroids
	[EditorDisplay("Asteroids")]
	[CustomEditorAlias("FlaxEditor.CustomEditors.Editors.ActorLayerEditor")]
	public int AsteroidLayer;

	[EditorDisplay("Asteroids")]
	public Float2 asteroidScale = new(1, 10);

	[EditorDisplay("Asteroids")]
	public Material asteroidMaterial;

	// radians per second
	[EditorDisplay("Asteroids")]
	public double AsteroidMaxRotationSpeed = Mathd.Pi;

	[EditorDisplay("Asteroids")]
	public double AsteroidMaxVelocity = 100.0;
	#endregion

	#region Collectibles
	[EditorDisplay("Collectibles")]
	public Prefab ResourcePrefab;

	[EditorDisplay("Collectibles")]
	public int MaxResourceUnitValue = 100;

	[EditorDisplay("Collectibles")]
	public Float2 ResourceScaling = new(0.25f, 0.5f);
	#endregion

	private readonly HashSet<Actor> asteroids = new();
	private readonly HashSet<Actor> resources = new();

	public override void OnUpdate()
	{
		if (Physics.DefaultScene.Origin != MainRenderTask.Instance.View.Origin)
		{
			Physics.DefaultScene.Origin = MainRenderTask.Instance.View.Origin;
			Debug.Log($"Physics origin: {Physics.DefaultScene.Origin}");
		}

		DespawnObjectsTooFarAway();

		var parameters = WorldParameterSampler.Sample();

		SpawnNewAsteroids(
			parameters,
			allowSpawnInView: asteroids.Count == 0);

		SpawnNewResources(
			parameters,
			allowSpawnInView: resources.Count == 0);
	}

	private void DespawnObjectsTooFarAway()
	{
		foreach (var asteroid in asteroids.AsEnumerable())
		{
			if (Vector3.Distance(asteroid.Position, Player.Position) > DespawnRadius)
			{
				asteroids.Remove(asteroid);
				Destroy(asteroid);
			}
		}
		foreach (var resource in resources.AsEnumerable())
		{
			if (Vector3.Distance(resource.Position, Player.Position) > DespawnRadius)
			{
				RemoveResource(resource);
			}
		}
	}

	private void SpawnNewAsteroids(WorldLocalParameters parameters, bool allowSpawnInView)
	{
		var desiredAsteroidCount = Math.Round(Math.Max(20, parameters.AsteroidDensity) * 10);
		if (desiredAsteroidCount == 0)
		{
			return;
		}

		var proceduralAsteroids = Parent.GetScript<ProceduralAsteroids>();

		var variants = new List<ProceduralAsteroids.Variant>();
		for (var i = 0; i < desiredAsteroidCount; ++i)
		{
			variants.Add(proceduralAsteroids.GetRandomVariant());
		}

		SpawnNewNpcs(
			variants
				.Select(a =>
				{
					var boundingBox = a.Model.GetBox();
					return (float)Math.Max(boundingBox.Size.X, Math.Max(boundingBox.Size.Y, boundingBox.Size.Z));
				}),
			allowSpawnInView,
			SpawnAsteroid);

		void SpawnAsteroid(int n, Vector3 position)
		{
			var variant = variants[n];
			var asteroid = Parent.AddChild<RigidBody>();
			asteroid.Position = position;
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
		}
	}

	private void SpawnNewResources(WorldLocalParameters parameters, bool allowSpawnInView)
	{
		var count = 0; // TODO
		if (count == 0)
		{
			return;
		}

		var variants = new List<(int units, float scale)>();
		for (var i = 0; i < count; ++i)
		{
			var units = 1 + (int)Mathf.Ceil(RandomUtil.Random.NextSingle() * MaxResourceUnitValue);
			var scale = 0.2f + (units / (float)MaxResourceUnitValue);
			variants.Add((units, scale));
		}

		SpawnNewNpcs(
			variants.Select(a => a.scale),
			allowSpawnInView,
			SpawnResource);

		void SpawnResource(int n, Vector3 position)
		{
			var parameters = variants[n];
			var resourcePrefab = PrefabManager.SpawnPrefab(ResourcePrefab, Parent);
			resourcePrefab.Position = position;
			resourcePrefab.Scale = new Float3(parameters.scale);

			var collectibleScript = resourcePrefab.GetScript<Collectible>();
			collectibleScript.NpcSpawner = this;
			collectibleScript.Units = parameters.units;

			resources.Add(resourcePrefab);
		}
	}

	private void SpawnNewNpcs(
		IEnumerable<float> npcCollisionSphereRadi,
		bool allowSpawnInView,
		Action<int, Vector3> spawnNpc)
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
		var maximumSpawnRadius = DespawnRadius - 100;
		if (minimumSpawnRadius >= maximumSpawnRadius)
		{
			Debug.LogError($"The minimum spawn distance {minimumSpawnRadius} is greater or equal to the maximum spawn distance {maximumSpawnRadius}");
			return;
		}

		var i = 0;
		foreach (var collsionSphereRadius in npcCollisionSphereRadi)
		{
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

				var playerDirectionAsAngle = Mathd.Atan2(normalizedPlayerDirection.Y, normalizedPlayerDirection.X);
				var randomAngle = randomAngleOffset + playerDirectionAsAngle;

				randomDirection = new Vector3(Mathd.Cos(randomAngle), Mathd.Sin(randomAngle), 0);
			}

			while (true)
			{
				if (sampleCount >= sampleLimit)
				{
					Debug.LogWarning("Reached sample limit for spawning NPCs");
					return;
				}
				++sampleCount;

				var offset = randomDirection * (minimumSpawnRadius + RandomUtil.Rand() * (maximumSpawnRadius - minimumSpawnRadius));
				Vector3 candidatePosition = Player.Position + offset;

				if (Physics.CheckSphere(
					candidatePosition,
					collsionSphereRadius,
					hitTriggers: true))
				{
					continue;
				}

				spawnNpc(i, candidatePosition);
				++i;
				break;
			}
		}
	}

	private static Vector2 GetRandomUnitVector2()
	{
		var randomAngle = (RandomUtil.Random.NextSingle() * 2.0 - 1.0) * Mathd.TwoPi;
		return new(Mathd.Cos(randomAngle), Mathd.Sin(randomAngle));
	}

	public void RemoveResource(Actor resource)
	{
		resources.Remove(resource);
		Destroy(resource);
	}
}
