using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using FlaxEngine;

namespace SCALE;

/// <summary>
/// ResourceField Script.
/// </summary>
public class ResourceField : Script
{
    public Actor Player { get; set; }
    public Prefab CollectiblePrefab;
    public float killDistance = 10000;
    public int maxResources = 10;
    public int MaxResourceUnitValue = 100;
    public Float2 resourceScale = new(0.25f, 0.5f);
    public Float2 resourceSpawnDistance = new(1000, 10000);

    private readonly HashSet<Actor> resources = new();

    public override void OnUpdate()
    {
        if (Physics.DefaultScene.Origin != MainRenderTask.Instance.View.Origin)
        {
            Physics.DefaultScene.Origin = MainRenderTask.Instance.View.Origin;
            Debug.Log($"Physics origin: {Physics.DefaultScene.Origin}");
        }

        // kill
        foreach (var resource in resources.ToList())
        {
            if (Vector3.Distance(resource.Position, Player.Position) > killDistance)
            {
                RemoveResource(resource);
            }
        }

        // spawn
        int sampleCount = 0;
        while (resources.Count < maxResources && sampleCount < 64)
        {
            float angle = (RandomUtil.Random.NextSingle() * 2f - 1f) * Mathf.TwoPi;
            Vector3 direction = new(Mathf.Cos(angle), Mathf.Sin(angle), 0);
            Vector3 position = Player.Position + direction * (RandomUtil.Random.NextSingle() * (resourceSpawnDistance.Y - resourceSpawnDistance.X) + resourceSpawnDistance.X);
            var units = 1 + (int)Mathf.Ceil(RandomUtil.Random.NextSingle() * MaxResourceUnitValue);
            var scale = 0.2f + (units / (float)MaxResourceUnitValue);
            ++sampleCount;
            if (Physics.CheckSphere(position, scale * 100))
                continue;

            var collectiblePrefab = PrefabManager.SpawnPrefab(CollectiblePrefab);
            collectiblePrefab.Position = position;
            collectiblePrefab.Scale = new Float3(scale);

            var collectibleScript = collectiblePrefab.GetScript<Collectible>();
            collectibleScript.ResourceField = this;
            collectibleScript.Units = units;

            resources.Add(collectiblePrefab);
        }
    }

    public void RemoveResource(Actor resource)
    {
        resources.Remove(resource);
        Destroy(resource);
    }
}
