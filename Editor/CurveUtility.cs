//*************************************************************************************
// CurveUtility.cs
//
// Author:
//       Gayan Ediriweera
//
using UnityEngine;

//-------------------------------------------------------------------------------------
// Utility functions for interpolating using curves
public class CurveUtility 
{
	// TODO: Figure out how to templatize this using C#'s strange generics

	//-------------------------------------------------------------------------------------
	// Interpolates four points using a Catmull-Rom spline. 
	public static Vector3 SplineInterpolate(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
	{
		float tSquared = t * t;
		float tCubed = tSquared * t;

		float a = 0.5f*(-tCubed + 2.0f*tSquared - t);
		float b = 0.5f*(3.0f*tCubed - 5.0f*tSquared + 2.0f);
		float c = 0.5f*(-3.0f*tCubed + 4.0f*tSquared + t);
		float d = 0.5f*(tCubed - tSquared);

		return (a*p0 + b*p1 + c*p2 + d*p3);
	}

	//-------------------------------------------------------------------------------------
	// Interpolates four floats using a Catmull-Rom spline
	public static float SplineInterpolate(float t, float p0, float p1, float p2, float p3)
	{
		float tSquared = t * t;
		float tCubed = tSquared * t;
		
		float a = 0.5f*(-tCubed + 2.0f*tSquared - t);
		float b = 0.5f*(3.0f*tCubed - 5.0f*tSquared + 2.0f);
		float c = 0.5f*(-3.0f*tCubed + 4.0f*tSquared + t);
		float d = 0.5f*(tCubed - tSquared);
		
		return (a*p0 + b*p1 + c*p2 + d*p3);;
	}
}
