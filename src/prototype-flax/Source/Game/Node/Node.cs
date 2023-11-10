using System;
using System.Collections.Generic;
using FlaxEngine;

namespace Game;

/// <summary>
/// Node Script.
/// </summary>
public class Node : Script
{
    private NodeFlowType? nodeType;

    /// <inheritdoc/>
    public override void OnStart()
    {
        var model = Parent.GetChild<SphereCollider>().GetChild<StaticModel>();
        var materialInstance = model.GetMaterial(0).CreateVirtualInstance();
        materialInstance.SetParameterValue("Color", Color.White); // TODO set this to something else
        model.SetMaterial(0, materialInstance);
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
        // Here you can add code that needs to be called every frame
    }

    public void OnFlowTypeChange(NodeFlowType type)
    {
        nodeType = type;
    }
}
