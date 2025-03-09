//*************************************************************************************
// TrackSurfaceUtility.cs
//
// Author:
//       Gayan Ediriweera
//
using UnityEngine;

//-------------------------------------------------------------------------------------
// Utility functions for sampling the surface curves of 
// the track and getting useful data out
public class TrackSurfaceUtility
{

	//-------------------------------------------------------------------------------------
	// NodeCrossSection functions
	//-------------------------------------------------------------------------------------
	public static Vector3 SampleNodeCrossSectionLocalTangent(TrackNode node, float u, float epsilon)
	{
		SurfaceCrossSection crossSection = node.GetComponent<SurfaceCrossSection>();
		// Estimate tangent using finite difference
		Vector3 localTangent = crossSection.SampleLocalPosition(u + epsilon) 
			- crossSection.SampleLocalPosition(u - epsilon);
		localTangent.Normalize();
		return localTangent;
	}

	public static Vector3 SampleNodeCrossSectionWorldTangent(TrackNode node, float u, float epsilon)
	{
		// Get the tangent in local space and transform into world space
		Vector3 localTangent = SampleNodeCrossSectionLocalTangent(node, u, epsilon);
		Vector3 worldTangent = node.transform.TransformDirection(localTangent);
		return worldTangent;
	}

	public static Vector3 SampleNodeCrossSectionLocalPosition(TrackNode node, float u)
	{
		SurfaceCrossSection crossSection = node.GetComponent<SurfaceCrossSection>();
		Vector3 localPosition = crossSection.SampleLocalPosition(u);
		return localPosition;
	}

	public static Vector3 SampleNodeCrossSectionWorldPosition(TrackNode node, float u)
	{
		SurfaceCrossSection crossSection = node.GetComponent<SurfaceCrossSection>();
		// Get the position in local space and transform into world space
		Vector3 localPosition = crossSection.SampleLocalPosition(u);
		Vector3 worldPosition = node.transform.TransformPoint(localPosition);
		return worldPosition;
	}

	//-------------------------------------------------------------------------------------
	// Segment functions
	//-------------------------------------------------------------------------------------
	public static Vector3 SampleSegmentLocalPosition(TrackNode start, float t, float u)
	{
		// Get the position in world space and transform into local space
		Vector3 worldPosition = SampleSegmentWorldPosition(start, t, u);
		Vector3 localPosition = start.transform.InverseTransformPoint(worldPosition);
		return localPosition;
	}

	public static Vector3 SampleSegmentWorldPosition(TrackNode start, float t, float u)
	{
		Track track = start.TrackRef;

		// Get the index of the node in the track
		int index = track.TrackNodes.IndexOf(start);
		if (index == -1)
		{
			// Track does not contain node, something went wrong
			//Debug.LogWarning("Track does not contain node");
			return Vector3.zero;
		}
		if (track.IsEndNode(index))
		{
			// End nodes do not have a segment defined
			//Debug.LogWarning("End nodes have no track segment");
			return Vector3.zero;
		}

		// Interpolate on two dimensions
		Vector3 worldPosition = CurveUtility.SplineInterpolate(t,
			SampleNodeCrossSectionWorldPosition(track.TrackNodes[index - 1], u), 
			SampleNodeCrossSectionWorldPosition(track.TrackNodes[index], u), 
			SampleNodeCrossSectionWorldPosition(track.TrackNodes[index + 1], u), 
			SampleNodeCrossSectionWorldPosition(track.TrackNodes[index + 2], u));
		
		return worldPosition;
	}

	public static Quaternion SampleSegmentLocalRotation(TrackNode start, float t, float u, float epsilon)
	{
		// Estimate forward and right vectors using finite difference
		Vector3 worldRight = SampleSegmentWorldPosition(start, t, u + epsilon) - SampleSegmentWorldPosition(start, t, u - epsilon);
		Vector3 worldForward = SampleSegmentWorldPosition(start, t + epsilon, u) - SampleSegmentWorldPosition(start, t - epsilon, u);

		// Transform into local space
		Vector3 localRight = start.transform.InverseTransformDirection(worldRight);
		Vector3 localForward = start.transform.InverseTransformDirection(worldForward);

		// Take the cross product to get the up vector
		Vector3 localUp = Vector3.Cross(localForward, localRight);

		// Build quaternion using forward and up
		Quaternion localRot = Quaternion.LookRotation(localForward, localUp);
		return localRot;
	}

	public static Quaternion SampleSegmentWorldRotation(TrackNode start, float t, float u, float epsilon)
	{
		// Estimate forward and right vectors using finite difference
		Vector3 worldRight = SampleSegmentWorldPosition(start, t, u + epsilon) - SampleSegmentWorldPosition(start, t, u - epsilon);
		Vector3 worldForward = SampleSegmentWorldPosition(start, t + epsilon, u) - SampleSegmentWorldPosition(start, t - epsilon, u);

		// Take the cross product to get the up vector
		Vector3 worldUp = Vector3.Cross(worldForward, worldRight);

		// Build quaternion using forward and up
		Quaternion worldRot = Quaternion.LookRotation(worldForward, worldUp);
		return worldRot;
	}

	//-------------------------------------------------------------------------------------
	// Segment Wall Functions
	//-------------------------------------------------------------------------------------
	public static void SampleSegmentWallLocalPositions(TrackNode start, float t, 
		ref Vector3 leftBase, ref Vector3 leftTop, ref Vector3 rightBase, ref Vector3 rightTop)
	{
		// Get the positions in world space and transform into local space
		SampleSegmentWallWorldPositions(start, t, ref leftBase, ref leftTop, ref rightBase, ref rightTop);
		leftBase = start.transform.InverseTransformPoint(leftBase);
		leftTop = start.transform.InverseTransformPoint(leftTop);
		rightBase = start.transform.InverseTransformPoint(rightBase);
		rightTop = start.transform.InverseTransformPoint(rightTop);
	}

	public static void SampleSegmentWallWorldPositions(TrackNode start, float t, 
		ref Vector3 leftBase, ref Vector3 leftTop, ref Vector3 rightBase, ref Vector3 rightTop)
	{
		Track track = start.TrackRef;

		// Get the index of the node in the track
		int index = track.TrackNodes.IndexOf(start);
		if (index == -1)
		{
			// Track does not contain node, something went wrong
			//Debug.LogWarning("Track does not contain node");
			return;
		}
		if (track.IsEndNode(index))
		{
			// End nodes do not have a segment defined
			// Debug.LogWarning("End nodes have no track segment");
			return;
		}

		const float epsilon = 0.05f;

		// Left Wall
		// Base Position
		leftBase = CurveUtility.SplineInterpolate(t,
			SampleNodeCrossSectionWorldPosition(track.TrackNodes[index - 1], 0.0f), 
		    SampleNodeCrossSectionWorldPosition(track.TrackNodes[index], 0.0f), 
		    SampleNodeCrossSectionWorldPosition(track.TrackNodes[index + 1], 0.0f), 
		    SampleNodeCrossSectionWorldPosition(track.TrackNodes[index + 2], 0.0f));

		// Track Normal
		Quaternion leftWorldRot = SampleSegmentWorldRotation(start, t, 0.0f, epsilon);
		Vector3 leftNormal = leftWorldRot*Vector3.up;

		// Wall Height by interpolaton
		float leftWallHeight = CurveUtility.SplineInterpolate(t, 
			track.TrackNodes[index - 1].LeftWallHeight,
			track.TrackNodes[index].LeftWallHeight,
			track.TrackNodes[index + 1].LeftWallHeight,
			track.TrackNodes[index + 2].LeftWallHeight);

		// Top position
		leftTop = leftBase + leftNormal*leftWallHeight;

		// Right Wall
		// Base Position
		rightBase = CurveUtility.SplineInterpolate(t,
			SampleNodeCrossSectionWorldPosition(track.TrackNodes[index - 1], 1.0f), 
			SampleNodeCrossSectionWorldPosition(track.TrackNodes[index], 1.0f), 
			SampleNodeCrossSectionWorldPosition(track.TrackNodes[index + 1], 1.0f), 
			SampleNodeCrossSectionWorldPosition(track.TrackNodes[index + 2], 1.0f));

		// Track Normal
		Quaternion rightWorldRot = SampleSegmentWorldRotation(start, t, 1.0f, epsilon);
		Vector3 rightNormal = rightWorldRot*Vector3.up;

		// Wall Height by interpolaton
		float rightWallHeight = CurveUtility.SplineInterpolate(t, 
			track.TrackNodes[index - 1].RightWallHeight,
			track.TrackNodes[index].RightWallHeight,
			track.TrackNodes[index + 1].RightWallHeight,
			track.TrackNodes[index + 2].RightWallHeight);

		// Top position
		rightTop = rightBase + rightNormal*rightWallHeight;
	}
}
