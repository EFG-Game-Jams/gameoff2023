﻿using System;
using System.Collections.Generic;
using System.Linq;
using FlaxEngine;

namespace SCALE;

/// <summary>
/// ShipResourceTrigger Script.
/// </summary>
public class ResourceTrigger : Script
{
    public ResourceField ResourceField { get; set; }
    public Actor Player { get; set; }

    /// <inheritdoc/>
    public override void OnStart()
    {
        // Here you can add code that needs to be called when script is created, just before the first game update
    }


    public override void OnEnable()
    {
        // Register for event
        Actor.As<Collider>().TriggerEnter += OnTriggerEnter;
    }

    public override void OnDisable()
    {
        // Unregister for event
        Actor.As<Collider>().TriggerEnter -= OnTriggerEnter;
    }

    /// <inheritdoc/>
    public override void OnUpdate()
    {
        // Here you can add code that needs to be called every frame
    }

    void OnTriggerEnter(PhysicsColliderActor collider)
    {
        Player.GetScript<ShipController>().ResourcesCollected++;
        ResourceField.RemoveResource(Parent.Parent);
    }
}
