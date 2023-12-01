using System;
using System.Collections.Generic;
using System.Linq;
using FlaxEngine;

namespace SCALE;

/// <summary>
/// ShipResourceTrigger Script.
/// </summary>
public class Collectible : Script
{
    public NpcSpawner NpcSpawner { get; set; }

    public int Units { get; set; }

    /// <inheritdoc/>
    public override void OnStart()
    {
        // Here you can add code that needs to be called when script is created, just before the first game update
    }

    public override void OnEnable()
    {
        // Register for event
    }

    public override void OnDisable()
    {
        // Unregister for event
    }

    /// <inheritdoc/>
    public override void OnUpdate()
    {
        // Here you can add code that needs to be called every frame
    }

    public void OnCollected()
    {
        NpcSpawner.RemoveResource(Parent);
    }
}
