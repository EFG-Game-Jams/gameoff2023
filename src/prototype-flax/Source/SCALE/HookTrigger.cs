using System;
using System.Collections.Generic;
using FlaxEditor.Content.Settings;
using FlaxEditor.Surface.Archetypes;
using FlaxEngine;

namespace SCALE;

/// <summary>
/// HookTrigger Script.
/// </summary>
public class HookTrigger : Script
{
    public LayersMask LayersMask { get; set; }
    public Actor Player { get; set; }
    public float Velocity { get; set; } = 800f;
    public float MaxLength { get; set; } = 1000f;

    [ReadOnly]
    public bool Attached { get; private set; }

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
        if (!Attached)
        {
            Hook.Position = Player.Position;
        }
    }

    public void Fire(Float3 origin, Float3 direction)
    {
        Hook.Position = origin;
        Attached = false;

        if (Physics.RayCast(origin, direction, out var hit, maxDistance: MaxLength, layerMask: LayersMask.Mask))
        {
            Player.GetScript<ShipController>().GrappleTarget = hit.Collider.AttachedRigidBody;
            Hook.Position = hit.Point;
            Attached = true;
        }
    }

    public void Reset()
    {
        Hook.Position = Player.Position;
        Attached = false;
    }

    private Actor Hook => Parent.Parent;
}
