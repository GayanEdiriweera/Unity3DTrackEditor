//*************************************************************************************
// TrackNode.cs
//
// Author:
//       Gayan Ediriweera
//
using UnityEngine;

//-------------------------------------------------------------------------------------
// Basic unit of the Track
[RequireComponent (typeof (SurfaceCrossSection))]
[System.Serializable]
public class TrackNode : MonoBehaviour 
{
	// Cross section type enum, manipulated by custom inspector
	[HideInInspector]
	public SurfaceCrossSectionType CrossSectionType;	

	// Back reference to the track
	public Track TrackRef;

	// References to wall objects
	public GameObject LeftWall;
	public GameObject RightWall;

	// Wall height values
	public float LeftWallHeight = 0.5f;
	public float RightWallHeight = 0.5f;

	//-------------------------------------------------------------------------------------
	// Handler for OnDrawGizmos
	void OnDrawGizmos()
	{
		// If we are an end node
		if (TrackRef.IsEndNode(this))
		{
			// Draw ourselves, so we are visible to the user
			const float alpha = 0.5f;
			const float radius = 0.5f;
			Color c = Color.blue;
			c.a = alpha;
			Gizmos.color = c;
			Gizmos.DrawSphere(transform.position, radius);
		}
	}
}

//-------------------------------------------------------------------------------------
// Types of cross sections for the surface of the node
public enum SurfaceCrossSectionType
{
	LINEAR,
	CIRCULAR
}
