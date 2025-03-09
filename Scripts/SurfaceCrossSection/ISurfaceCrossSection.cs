//*************************************************************************************
// SurfaceCrossSection.cs
//
// Author:
//       Gayan Ediriweera
//
using UnityEngine;
using System.Collections;

//-------------------------------------------------------------------------------------
// Surface Cross Section Interface. New cross sections should derive from this
public abstract class SurfaceCrossSection : MonoBehaviour
{
	//-------------------------------------------------------------------------------------
	// Samples function at t and returns local space vector
	public abstract Vector3 SampleLocalPosition(float t);
}