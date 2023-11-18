using System;
using System.Collections.Generic;
using System.Linq;
using FlaxEngine;

namespace SCALE;

/// <summary>
/// AsteroidTrigger Script.
/// </summary>
public class AsteroidTrigger : Script
{
    public Actor Player { get; set; }

    /// <inheritdoc/>
    public override void OnStart()
    {
        // Here you can add code that needs to be called when script is created, just before the first game update
    }

    public override void OnEnable()
    {
        // Register for event
        Actor.As<Collider>().CollisionEnter += OnCollisionEnter;
    }

    public override void OnDisable()
    {
        // Unregister for event
        Actor.As<Collider>().CollisionEnter -= OnCollisionEnter;
    }

    /// <inheritdoc/>
    public override void OnUpdate()
    {
        // Here you can add code that needs to be called every frame
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.OtherActor.HasTag("Player"))
        {
            var shipController = Player.GetScript<ShipController>();
            if (shipController.ResourcesCollected <= 0)
            {
                Debug.Log("Player died");
            }
            else
            {
                shipController.ResourcesCollected--;
            }
        }
    }
}
