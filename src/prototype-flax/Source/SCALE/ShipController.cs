using System;
using System.Collections.Generic;
using System.Linq;
using FlaxEngine;

namespace SCALE;

/// <summary>
/// ShipController Script.
/// </summary>
public class ShipController : Script
{
	private const float resourceScalingFactor = 1.2f;

	private int resourcesCollected = 0;
	private Float3 originalScale;
	private RigidBody grappleTarget;
	private DistanceJoint grappleJoint;

	public RigidBody body;

	public RigidBody HookBody;

	public ThrusterController ThrusterController;

	[ReadOnly]
	public RigidBody GrappleTarget
	{
		get => grappleTarget;
		set
		{
			if (grappleJoint != null)
			{
				DestroyGrapplingHook();
			}

			if (value != null)
			{
				grappleJoint = body.AddChild<DistanceJoint>();
				grappleJoint.LocalPosition = body.CenterOfMass;
				grappleJoint.Target = value;
				grappleJoint.MaxDistance = Float3.Distance(body.Position, value.Position);
				grappleJoint.Flags = DistanceJointFlag.MaxDistance;
			}

			grappleTarget = value;
		}
	}

	[ReadOnly]
	public int ResourcesCollected
	{
		get => resourcesCollected;
		set
		{
			resourcesCollected = value;
			var newScale = MathF.Max(1, (1 + resourcesCollected) * resourceScalingFactor);
			Parent.Scale = originalScale * newScale;
		}
	}

	public float controlForward;
	public float controlStrafe;
	public float controlTurn;
	public bool autoBrake;

	public bool fireGrapplingHook;
	public bool releaseGrapplingHook;

	public override void OnEnable()
	{
		base.OnEnable();
		originalScale = Parent.Scale;
	}

	public override void OnUpdate()
	{
		ThrusterController.LinearVelocityControl = new(
			Input.GetAxis("Strafe"),
			Input.GetAxis("Thrust")
		);
		ThrusterController.AngularVelocityControl = -Input.GetAxis("Turn");

		if (Input.GetKeyDown(KeyboardKeys.Spacebar))
			ThrusterController.KillTranslation = !ThrusterController.KillTranslation;
		ThrusterController.KillRotation = true;

		fireGrapplingHook = Input.GetAction("Fire");
		releaseGrapplingHook = Input.GetAction("Release");
	}

	public override void OnFixedUpdate()
	{
		if (fireGrapplingHook)
		{
			DestroyGrapplingHook();
			var hookTrigger = HookBody.GetChild<SphereCollider>().GetScript<HookTrigger>();
			hookTrigger.Fire(
				Parent.Position,
				 new Float3(
					(Input.MousePosition.X / Screen.Size.X) - 0.5f,
					-1f * ((Input.MousePosition.Y / Screen.Size.Y) - 0.5f),
					0f).Normalized);
		}
		else if (releaseGrapplingHook)
		{
			HookBody.GetChild<SphereCollider>().GetScript<HookTrigger>().Reset();
			DestroyGrapplingHook();
		}
		
		// stick to z = 0, rigid body Z constraint doesn't seem to be enough
		body.AddMovement(new(0, 0, -body.Position.Z));
	}

	private void DestroyGrapplingHook()
	{
		if (grappleJoint != null)
		{
			Destroy(grappleJoint);
		}
		grappleTarget = null;
		grappleJoint = null;
	}
}
