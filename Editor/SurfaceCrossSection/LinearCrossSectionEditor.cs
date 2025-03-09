//*************************************************************************************
// LinearCrossSectionEditor.cs
//
// Author:
//       Gayan Ediriweera
//
using UnityEngine;
using UnityEditor;
using System.Collections;

//-------------------------------------------------------------------------------------
// Custom Editor for LinearCrossSections
[CustomEditor (typeof(LinearCrossSection))]
public class LinearCrossSectionEditor : Editor
{
	enum LineEnd
	{
		Min,
		Max
	};

	//-------------------------------------------------------------------------------------
	// Custom inspector
    public override void OnInspectorGUI()
	{
		EditorGUI.BeginChangeCheck();
			LinearCrossSection sec = ((LinearCrossSection)target);
			// Vector2 field for offset
			sec.Offset = EditorGUILayout.Vector2Field("Offset", sec.Offset);
			EditorGUILayout.BeginHorizontal();
				// Slider for width
				EditorGUILayout.PrefixLabel("Width");
				sec.Width = EditorGUILayout.Slider(sec.Width, 0.2f, 10.0f);
			EditorGUILayout.EndHorizontal();
		if (EditorGUI.EndChangeCheck())
		{
			TrackRebuilder.Instance.AddToDirtySet(sec.GetComponent<TrackNode>());
			EditorUtility.SetDirty (target);
        }
	}

	//-------------------------------------------------------------------------------------
	// Width manipulation handle in scene view
	void WidthHandle(TrackNode node, LinearCrossSection sec, LineEnd end)
	{
		Handles.color = Color.magenta;

		// Determine which end this is
		float lineParameter = (end == LineEnd.Min) ? 0.0f : 1.0f;
		Vector3 worldPos = TrackSurfaceUtility.SampleNodeCrossSectionWorldPosition(node, lineParameter);

		// Get the other end as a reference
		float otherParameter = (end == LineEnd.Min) ? 1.0f : 0.0f;
		Vector3 otherPos = TrackSurfaceUtility.SampleNodeCrossSectionWorldPosition(node, otherParameter);
		
		// Calculate the track slope
		Vector3 trackSlopeDir = worldPos - otherPos;
		trackSlopeDir.Normalize();

		// Update the slide pos
		worldPos = Handles.Slider2D(worldPos,
		                            trackSlopeDir,
		                            sec.transform.right,
		                            sec.transform.up,
		                            HandleUtility.GetHandleSize(worldPos)*0.25f, Handles.CubeCap, 0.0f);

		// Calculate the new delta
		Vector3 newDelta = worldPos - otherPos;

		// Project new delta onto track slope to get new width
		const float minWidth = 0.1f;
		float val = Mathf.Max(Vector3.Dot(newDelta, trackSlopeDir), minWidth);
		sec.Width = val;
	}

	//-------------------------------------------------------------------------------------
	// Handles events in scene view
	void OnSceneGUI()
	{
		// Get target
		LinearCrossSection sec = ((LinearCrossSection)target);

		// Get node
		TrackNode node = sec.GetComponent<TrackNode>();

		// Show handles for width manipulation
		WidthHandle(node, sec, LineEnd.Min);
		WidthHandle(node, sec, LineEnd.Max);
		
		if (GUI.changed)
		{
			// If something changed, mark as dirty
			TrackRebuilder.Instance.AddToDirtySet(sec.GetComponent<TrackNode>());
			EditorUtility.SetDirty (target);
		}
	}
}

