using System;
using System.Collections.Generic;
using FlaxEngine;

namespace SCALE;

/// <summary>
/// HookTrigger Script.
/// </summary>
public class HookTrigger : Script
{
    private Float3 direction = Float3.Zero;

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
        if (Attached)
        {
            return;
        }
        if (direction == Float3.Zero)
        {
            Hook.Position = Player.Position;
            return;
        }

        Hook.Position += direction * Velocity * Time.DeltaTime;

        if (Float3.Distance(Hook.Position, Player.Position) > MaxLength)
        {
            direction = Float3.Zero;
        }
    }

    public void Fire(Float3 origin, Float3 direction)
    {
        Hook.Position = origin;
        Attached = false;
        this.direction = direction;
    }

    public void Reset()
    {
        Hook.Position = Player.Position;
        Attached = false;
        direction = Float3.Zero;
    }

    void OnTriggerEnter(PhysicsColliderActor collider)
    {
        if (collider.HasTag("Asteroid"))
        {
            Player.GetScript<ShipController>().GrappleTarget = collider.AttachedRigidBody;
            Attached = true;
            direction = Float3.Zero;
        }
    }

    private Actor Hook => Parent.Parent;
}
