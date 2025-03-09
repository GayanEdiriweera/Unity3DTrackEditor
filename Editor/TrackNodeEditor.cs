//*************************************************************************************
// TrackNodeEditor.cs
//
// Author:
//       Gayan Ediriweera
//
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

//-------------------------------------------------------------------------------------
// TrackNodeEditor
//
// Custom Editor for Track Nodes
[CustomEditor (typeof(TrackNode))]
[CanEditMultipleObjects]
public class TrackNodeEditor : Editor {
	private enum Wall
	{
		Left,
		Right
	};

	//-------------------------------------------------------------------------------------
	// Custom inspector
	public override void OnInspectorGUI()
    {
		// Get target
		TrackNode node = (TrackNode)target;

		EditorGUI.BeginChangeCheck();

		// If the surface cross section type changes, update the component
		SurfaceCrossSectionType oldType = node.CrossSectionType;
		node.CrossSectionType = 
			(SurfaceCrossSectionType)EditorGUILayout.EnumPopup("Surface Cross Section Type", node.CrossSectionType);
		if (node.GetComponent<SurfaceCrossSection>() == null || oldType != node.CrossSectionType)
		{
			CreateSurfaceCrossSection(node);
			EditorUtility.SetDirty (target);
		}

		// Draw the default inspector GUI
		DrawDefaultInspector();
		if (EditorGUI.EndChangeCheck())
		{
			// Mark as dirty if anything changed
			TrackRebuilder.Instance.AddToDirtySet(node);
			EditorUtility.SetDirty(target);
		}
	}

	//-------------------------------------------------------------------------------------
	// Creates a new surface cross section component, destroying any that exist
	public static void CreateSurfaceCrossSection(TrackNode node)
	{
		// Get a reference to any SurfaceCrossSections
		Component[] existing = node.GetComponents<SurfaceCrossSection>();

		// Add a new one
		switch (node.CrossSectionType)
		{
		case SurfaceCrossSectionType.LINEAR:
			node.gameObject.AddComponent<LinearCrossSection>();
			break;
		case SurfaceCrossSectionType.CIRCULAR:
			node.gameObject.AddComponent<CircularCrossSection>();
			break;
		}

		// Destroy the previously existing ones
		for (int i = 0; i < existing.Length; i++)
		{
			Component.DestroyImmediate(existing[i]);
		}
	}

	//-------------------------------------------------------------------------------------
	// Wall manipulation handle in scene view
	private void WallHandle(TrackNode node, Vector3 wallBase, Vector3 wallTop, Wall wall)
	{
		Handles.color = Color.magenta;

		// Get which end of the wall we are on
		float wallParameter = (wall == Wall.Left) ? 0.0f : 1.0f;

		// Calculate rotation at the point
		const float epsilon = 0.05f;
		Quaternion rot = TrackSurfaceUtility.SampleSegmentWorldRotation(node, 0.0f, wallParameter, epsilon);
		Vector3 trackNormal = rot*Vector3.up;

		// Draw the slider handle
		const float handleSize = 0.4f;
		Vector3 newHandlePos = Handles.Slider2D(
			wallTop,
			trackNormal,
			node.transform.right, 
			node.transform.up,
			HandleUtility.GetHandleSize(wallTop)*handleSize, 
			Handles.CylinderCap,
			Vector2.zero);
		Handles.DrawLine(wallBase, wallTop);

		// Get the vector from the base to the new handle
		Vector3 baseToHandle = newHandlePos - wallBase;

		// The length of the projection onto the track normal is the new wall height
		float wallHeight = Vector3.Dot(baseToHandle, trackNormal);

		// Set the wall height on the appropriate wall
		if (wall == Wall.Left)
		{
			node.LeftWallHeight = Mathf.Max(wallHeight, 0.0f);
		}
		else
		{
			node.RightWallHeight = Mathf.Max(wallHeight, 0.0f);
		}
	}

	//-------------------------------------------------------------------------------------
	// Handles events in scene view
	void OnSceneGUI()
	{
		// Get target
		TrackNode node = ((TrackNode)target);
		if (node.TrackRef.IsEndNode(node))
		{
			return;
		}

		// Get wall positions at the node
		Vector3 leftBase = Vector3.zero;
		Vector3 leftTop = Vector3.zero;
		Vector3 rightBase = Vector3.zero;
		Vector3 rightTop = Vector3.zero;
		TrackSurfaceUtility.SampleSegmentWallWorldPositions(node, 0.0f, ref leftBase, ref leftTop, ref rightBase, ref rightTop);

		// Show handles for wall manipulation
		WallHandle(node, leftBase, leftTop, Wall.Left);
		WallHandle(node, rightBase, rightTop, Wall.Right);

		if (GUI.changed)
		{
			// If something changed, mark as dirty
			TrackRebuilder.Instance.AddToDirtySet(node);
			EditorUtility.SetDirty (target);
		}
	}
}
