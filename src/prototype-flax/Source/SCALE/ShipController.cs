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

	public float ResourceCollectionRadius { get; set; } = 100f;
	public LayersMask ResourceLayerMask { get; set; }

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

	[ReadOnly]
	public float controlForward;
	[ReadOnly]
	public float controlStrafe;
	[ReadOnly]
	public float controlTurn;
	[ReadOnly]
	public bool autoBrake;

	[ReadOnly]
	public bool fireGrapplingHook;
	[ReadOnly]
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
			FireGrapplingHook();
		}
		else if (releaseGrapplingHook)
		{
			ReleaseGrapplingHook();
		}

		// stick to z = 0, rigid body Z constraint doesn't seem to be enough
		body.AddMovement(new(0, 0, -body.Position.Z));

		CollectResources();
	}

	private void FireGrapplingHook()
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

	private void ReleaseGrapplingHook()
	{
		HookBody.GetChild<SphereCollider>().GetScript<HookTrigger>().Reset();
		DestroyGrapplingHook();
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

	private void CollectResources()
	{
		if (Physics.OverlapSphere(Parent.Position, ResourceCollectionRadius, out PhysicsColliderActor[] hits, ResourceLayerMask.Mask, false))
		{
			foreach (var hit in hits)
			{
				var resourceValue = hit.Parent.GetScript<Collectible>();
				resourcesCollected += resourceValue.Units;
				resourceValue.OnCollected();
			}
		}
	}
}
