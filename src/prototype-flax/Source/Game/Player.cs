using System;
using System.Collections.Generic;
using System.Linq;
using FlaxEngine;

namespace Game;

/// <summary>
/// Player Script.
/// </summary>
public class Player : Script
{
    public Camera Camera;

    /// <inheritdoc/>
    public override void OnStart()
    {
        // Here you can add code that needs to be called when script is created, just before the first game update
    }

    /// <inheritdoc/>
    public override void OnEnable()
    {
        // Here you can add code that needs to be called when script is enabled (eg. register for events)
    }

    /// <inheritdoc/>
    public override void OnDisable()
    {
        // Here you can add code that needs to be called when script is disabled (eg. unregister from events)
    }

    /// <inheritdoc/>
    public override void OnUpdate()
    {
        if (Input.GetMouseButtonDown(MouseButton.Left))
        {
            var pos = Input.MousePosition;
            var ray = Camera.MainCamera.ConvertMouseToRay(pos);
            foreach (var node in Scene.GetChild("Nodes").Children)
            {
                foreach (var control in node.GetChild("PlayerControls").GetChildren<SpriteRender>())
                {
                    if (ray.Intersects(new BoundingSphere(control.Position, control.Size.X / 2f)))
                    {
                        var nodeModel = node.GetChild("SphereCollider").GetChild<StaticModel>();
                        node.GetScript<Node>().OnFlowTypeChange(control.GetScript<NodeControl>().Type);
                        nodeModel.GetMaterial(0).SetParameterValue("Color", control.Color);
                    }
                }
            }
        }
        // Here you can add code that needs to be called every frame
    }
}
