//*************************************************************************************
// LinearCrossSection.cs
//
// Author:
//       Gayan Ediriweera
//
using UnityEngine;
using System.Collections;


//-------------------------------------------------------------------------------------
// Cross Section based on sampling a straight line
[System.Serializable]
public class LinearCrossSection : SurfaceCrossSection
{
	// 2D Offset
	public Vector2 Offset = new Vector2();

	// The width of the line
	public float Width = 2.0f;

	//-------------------------------------------------------------------------------------
	// Samples function at t and returns local space vector
	public override Vector3 SampleLocalPosition(float t)
	{
		// Sample a straight line on the x-axis
		return new Vector3(Offset.x - Width*0.5f + Width*t, Offset.y, 0.0f);
	}
}

