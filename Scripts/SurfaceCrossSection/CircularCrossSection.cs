//*************************************************************************************
// CircularCrossSection.cs
//
// Author:
//       Gayan Ediriweera
//
using UnityEngine;

//-------------------------------------------------------------------------------------
// Cross Section based on sampling a circle
[System.Serializable]
public class CircularCrossSection : SurfaceCrossSection
{
	// 2D Offset
	public Vector2 Offset = new Vector2();

	// ArcMin and ArcMax are the bounds of an arc of the circle
	public float ArcMin = 90.0f;
	public float ArcMax = 270.0f;

	// Radius of the circle
	public float Radius = 1.5f;

	//-------------------------------------------------------------------------------------
	// Samples function at t and returns local space vector
	public override Vector3 SampleLocalPosition(float t)
	{
		// Calculate total arc angle
		float arcAngle = ArcMax - ArcMin;

		// Calculate theta angle for t
		float theta = ArcMin + t*arcAngle + 90.0f;

		// Sample the circle
		return new Vector3(Offset.x + Radius*Mathf.Cos(theta*Mathf.Deg2Rad), 
			Offset.y + Radius*Mathf.Sin(theta*Mathf.Deg2Rad) + Radius, 0.0f);
	
	}
}
