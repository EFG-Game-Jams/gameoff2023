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

	public float maxSpeed = 500f;
	public float maxAngularSpeed = 10f;

	public float forceScale = 100f;
	public float thrustForward = 2;
	public float thrustReverse = 1;
	public float thrustStrafe = 1;
	public float torque = 1;

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
		controlForward = Input.GetAxis("Thrust");
		controlStrafe = Input.GetAxis("Strafe");
		controlTurn = -Input.GetAxis("Turn");
		if (Input.GetKeyDown(KeyboardKeys.Spacebar))
			autoBrake = !autoBrake;

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

		Float3 lvel = (Float3)body.Transform.WorldToLocalVector(body.LinearVelocity);
		float avel = (float)body.AngularVelocity.Z;

		float targetForward = controlForward != 0
			? controlForward * maxSpeed
			: (autoBrake ? 0 : lvel.Y);
		float forwardError = targetForward - lvel.Y;
		float forwardErrorSign = Mathf.Sign(forwardError);
		float maxForceForward = (forwardError > 0 ? thrustForward : thrustReverse) * forceScale;
		float forceForwardAbs = Mathf.Min(Mathf.Abs(forwardError) * .99f * body.Mass / Time.DeltaTime, maxForceForward);

		float targetStrafe = controlStrafe != 0
			? controlStrafe * maxSpeed
			: (autoBrake ? 0 : lvel.X);
		float strafeError = targetStrafe - lvel.X;
		float strafeErrorSign = Mathf.Sign(strafeError);
		float maxForceStrafe = thrustStrafe * forceScale;
		float forceStrafeAbs = Mathf.Min(Mathf.Abs(strafeError) * .99f * body.Mass / Time.DeltaTime, maxForceStrafe);

		float targetTurn = controlTurn * maxAngularSpeed;
		float turnError = targetTurn - avel;
		float turnErrorSign = Mathf.Sign(turnError);
		float maxTorque = torque * forceScale;
		//float torqueAbs = Mathf.Min(Mathf.Abs(turnError) * body.Mass / Time.DeltaTime, maxTorque);
		float torqueAbs = Mathf.Abs(Math.Clamp(turnError, -1, 1) * maxTorque);

		Vector3 force = body.Transform.LocalToWorldVector(new(strafeErrorSign * forceStrafeAbs, forwardErrorSign * forceForwardAbs, 0));
		force.Z = 0;
		body.AddForce(force);
		DebugDraw.DrawRay(body.Position, body.Transform.LocalToWorldVector(force) * 100, Color.Red, .05f);
		DebugDraw.DrawRay(body.Position, body.LinearVelocity.Normalized * 200, Color.Blue, .05f);
		body.AddTorque(new Vector3(0, 0, turnErrorSign * torqueAbs));

		body.AddMovement(new(0, 0, -body.Position.Z));

		/*
		// limit speed
		Vector3 velocity = body.LinearVelocity;
		float speed = velocity.Length;
		if (speed > maxSpeed)
		{
			velocity /= speed;
			velocity *= maxSpeed;
			body.LinearVelocity = velocity;
			body.AddForce(velocity - body.LinearVelocity, ForceMode.VelocityChange);
		}

		// limit angular speed
		Vector3 angularVelocity = body.AngularVelocity;
		float angularSpeed = angularVelocity.Length;
		if (angularSpeed > maxAngularSpeed)
		{
			angularVelocity /= angularSpeed;
			angularVelocity *= maxAngularSpeed;
			body.AngularVelocity = angularVelocity;
		}
		*/
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
