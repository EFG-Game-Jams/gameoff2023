using System;
using System.Collections.Generic;
using FlaxEngine;

namespace SCALE;

/// <summary>
/// CameraFollow2D Script.
/// </summary>
public class CameraFollow2D : Script
{
	private const float V = 2f;
	public RigidBody targetBody;
	public float smoothTime = 0.5f;
	public float zoomTime = 1f;
	public float projectTime = V;
	public Float2 speedRange;
	public Float2 zRange;

	private Vector3 velocity;
	public float zoomVelocity;

	public override void OnLateUpdate()
	{
		Vector3 current = Actor.Position;
		Vector3 target = targetBody.Position + targetBody.LinearVelocity * projectTime;
				
		Vector3 smoothed = Vector3.SmoothDamp(current, target, ref velocity, smoothTime);
		smoothed = targetBody.Position; // simple behaviour for now

		float speed = Mathf.Clamp((float)targetBody.LinearVelocity.Length, speedRange.X, speedRange.Y);
		float z = Mathf.Remap(speed, speedRange.X, speedRange.Y, zRange.X, zRange.Y);
		smoothed.Z = Mathf.SmoothDamp((float)current.Z, z, ref zoomVelocity, zoomTime);

		Actor.Position = smoothed;
	}
}
