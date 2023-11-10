using System;
using System.Collections.Generic;
using FlaxEngine;

namespace Game;

/// <summary>
/// Edge Script.
/// </summary>
public class Edge : Script
{
    public Node NodeA;
    public Node NodeB;

    /// <inheritdoc/>
    public override void OnStart()
    {
        Vector3 nodeAPosition = NodeA.Parent.Position;
        Vector3 nodeBPosition = NodeB.Parent.Position;

        Parent.Scale = new Float3(
            Math.Abs(Vector3.Distance(nodeBPosition, nodeAPosition) / 100f) - 1f,
            Parent.Scale.Y,
            Parent.Scale.Z);

        Parent.Position = (nodeAPosition + nodeBPosition) / 2;
        var angle = 90 + Mathf.RadiansToDegrees * Mathf.Atan2(nodeBPosition.X - nodeAPosition.X, nodeBPosition.Z - nodeAPosition.Z);
        Parent.LocalEulerAngles = new Float3(
            0,
            angle,
            0);
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
        // Here you can add code that needs to be called every frame
    }
}
