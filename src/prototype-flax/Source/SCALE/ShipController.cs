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
	private int resourcesCollected = 0;
	private Float3 originalScale;
	private RigidBody grappleTarget;
	private DistanceJoint grappleJoint;

	public RigidBody body;
	public RigidBody HookBody;
	public ThrusterController ThrusterController;

	public float ResourceCollectionRadius { get; set; } = 25f;
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
			var newScale = 1 + Math.Log10((resourcesCollected / 100.0) + 1.0) * 9.0;
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
		var playerCapsule = Parent.GetChild<CapsuleCollider>();
		if (Physics.OverlapCapsule(
			playerCapsule.Position,
			(playerCapsule.Radius * Parent.Scale.X) + ResourceCollectionRadius,
			(playerCapsule.Height * Parent.Scale.X) + ResourceCollectionRadius,
			out Collider[] hits,
			playerCapsule.Orientation,
			ResourceLayerMask.Mask,
			false))
		{
			foreach (var hit in hits)
			{
				var resourceValue = hit.Parent.GetScript<Collectible>();
				ResourcesCollected += resourceValue.Units;
				resourceValue.OnCollected();
			}
		}
	}
}
