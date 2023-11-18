using System.Collections.Generic;
using System.Linq;
using FlaxEngine;

namespace SCALE;

/// <summary>
/// ResourceField Script.
/// </summary>
public class ResourceField : Script
{
    public Actor Player { get; set; }
    public Prefab resourcePrefab;
    public float killDistance = 10000;
    public int maxResources = 10;
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
            float scale = RandomUtil.Random.NextSingle() * (resourceScale.Y - resourceScale.X) + resourceScale.X;

            ++sampleCount;
            if (Physics.CheckSphere(position, scale * 100))
                continue;

            var resource = PrefabManager.SpawnPrefab(resourcePrefab);
            resource.Position = position;
            resource.Scale = Vector3.One * scale;

            var resourceTrigger = resource.GetChild<BoxCollider>().GetScript<ResourceTrigger>();
            resourceTrigger.ResourceField = this;
            resourceTrigger.Player = Player;

            resources.Add(resource);
        }
    }

    public void RemoveResource(Actor resource)
    {
        resources.Remove(resource);
        Destroy(resource);
    }
}
