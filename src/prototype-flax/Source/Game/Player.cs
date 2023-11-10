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
    public float CameraVelocity = 400f;

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


        Float3 inputMapping = new Float3(0, 0, 0);
        if (Input.GetActionState("Move camera up") == InputActionState.Pressing)
        {
            inputMapping.Z += 1;
        }
        if (Input.GetActionState("Move camera down") == InputActionState.Pressing)
        {
            inputMapping.Z -= 1;
        }
        if (Input.GetActionState("Move camera left") == InputActionState.Pressing)
        {
            inputMapping.X -= 1;
        }
        if (Input.GetActionState("Move camera right") == InputActionState.Pressing)
        {
            inputMapping.X += 1;
        }

        Camera.Position += inputMapping * CameraVelocity * Time.DeltaTime;
        // Here you can add code that needs to be called every frame
    }
}
