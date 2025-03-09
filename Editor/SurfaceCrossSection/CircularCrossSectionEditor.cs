//*************************************************************************************
// CircularCrossSectionEditor.cs
//
// Author:
//       Gayan Ediriweera
//
using UnityEngine;
using UnityEditor;

//-------------------------------------------------------------------------------------
// Custom Editor for CircularCrossSections
[CustomEditor (typeof(CircularCrossSection))]
[CanEditMultipleObjects]
public class CircularCrossSectionEditor : Editor
{
	enum ArcEnd
	{
		Min,
		Max
	};

	//-------------------------------------------------------------------------------------
	// Custom inspector
	public override void OnInspectorGUI()
	{
		EditorGUI.BeginChangeCheck();
			CircularCrossSection sec = ((CircularCrossSection)target);
			// Vector2 field for offset
			sec.Offset = EditorGUILayout.Vector2Field("Offset", sec.Offset);
			EditorGUILayout.BeginHorizontal();
				// Slider for radius
				EditorGUILayout.PrefixLabel("Radius");
				sec.Radius = EditorGUILayout.Slider(sec.Radius, 0.5f, 10.0f);
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.BeginHorizontal();
				// MinMax Slider for Arc
				EditorGUILayout.PrefixLabel("Arc");
				EditorGUILayout.MinMaxSlider(ref sec.ArcMin, ref sec.ArcMax, 0.0f, 360.0f);
			EditorGUILayout.EndHorizontal();

		if (EditorGUI.EndChangeCheck())
		{
			TrackRebuilder.Instance.AddToDirtySet(sec.GetComponent<TrackNode>());
			EditorUtility.SetDirty (target);
		}
	}

	//-------------------------------------------------------------------------------------
	// Radius manipulation handle in scene view
	void RadiusHandle(TrackNode node, CircularCrossSection sec)
	{
		Handles.color = Color.magenta;

		// Use the built in radius handle
		Vector3 localPos = sec.Offset;
		localPos += node.transform.up*sec.Radius;
		Vector3 worldPos = node.transform.TransformPoint(localPos);
		sec.Radius = Handles.RadiusHandle(node.transform.rotation, worldPos, sec.Radius);
	}

	//-------------------------------------------------------------------------------------
	// Arc manipulation handle in scene view
	void ArcHandle(TrackNode node, CircularCrossSection sec, ArcEnd end)
	{
		Handles.color = Color.magenta;

		// Determine which end this is
		float arcParameter = (end == ArcEnd.Min) ? 0.0f : 1.0f;

		// Figure out the position and tangent
		Vector3 worldPos = TrackSurfaceUtility.SampleNodeCrossSectionWorldPosition(node, arcParameter);
		Vector3 worldTangent = TrackSurfaceUtility.SampleNodeCrossSectionWorldTangent(node, arcParameter, 0.05f);

		// Slider
		const float handleSize = 0.25f;
		worldPos = Handles.Slider2D(worldPos,
			worldTangent,
			sec.transform.right,
			sec.transform.up,
			HandleUtility.GetHandleSize(worldPos)*handleSize, Handles.CubeCap, 0.0f);

		// Take the new world pos and convert it to local space
		Vector3 localPos = sec.transform.InverseTransformPoint(worldPos);
		localPos -= (Vector3.right*sec.Offset.x + Vector3.up*(sec.Offset.y + sec.Radius));

		// Use Atan2 to calculate the new angle value
		float val = Mathf.Atan2(localPos.y, localPos.x)*Mathf.Rad2Deg + 360.0f - 90.0f;
		while (val >=360.0f)
		{
			val -= 360.0f;
		}

		// Use a threshold to make it easier to set a full 360 degree arc
		const float SnapThreshold = 30.0f;
		if (end == ArcEnd.Min)
		{
			sec.ArcMin = val;
			if (sec.ArcMin > sec.ArcMax - SnapThreshold)
			{
				// Snap to 0
				sec.ArcMin = 0.0f;
			}
		}
		else
		{
			sec.ArcMax = val;
			if (sec.ArcMax < sec.ArcMin + SnapThreshold)
			{
				// Snap to 360
				sec.ArcMax = 360.0f;
			}
		}
	}

	//-------------------------------------------------------------------------------------
	// Handles events in scene view
	void OnSceneGUI()
	{
		// Get target
		CircularCrossSection sec = ((CircularCrossSection)target);
		TrackNode node = sec.GetComponent<TrackNode>();

		// Show handles for arc manipulation
		ArcHandle(node, sec, ArcEnd.Min);
		ArcHandle(node, sec, ArcEnd.Max);

		// Show handles for radius
		RadiusHandle(node, sec);

		if (GUI.changed)
		{
			// If something changed, mark as dirty
			TrackRebuilder.Instance.AddToDirtySet(sec.GetComponent<TrackNode>());
			EditorUtility.SetDirty (target);
		}
	}
}