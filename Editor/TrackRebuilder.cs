//*************************************************************************************
// TrackRebuilder.cs
//
// Author:
//       Gayan Ediriweera
//
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

//-------------------------------------------------------------------------------------
// TrackRebuilder
//
// Manages dirty nodes and controls how segments meshes are 
// rebuilt as nodes are manipulated in the editor
public class TrackRebuilder 
{
	private static TrackRebuilder m_instance;
	public static TrackRebuilder Instance
	{
		get
		{
			if (m_instance == null)
			{
				m_instance = new TrackRebuilder();
			}
			return m_instance;
		}
	}

	// Set of nodes which need their mesh updated
	private HashSet<TrackNode> DirtySet = new HashSet<TrackNode>();

	//-------------------------------------------------------------------------------------
	// Adds node to dirty set
	public void AddToDirtySet(TrackNode node)
	{
		// Prevents prefab getting added to dirty set!
		if (!node.TrackRef)
		{
			return;
		}
		DirtySet.Add(node);
	}

	//-------------------------------------------------------------------------------------
	// Limits node rotation to single axis based on previus and next nodes
	// TODO: Implement more intuitive rotation handles in editor
	private void ContstrainNodeRotation(TrackNode node)
	{	
		Track track = node.TrackRef;

		// Get the index of the node in the track
		int index = track.TrackNodes.IndexOf(node);
		int prev = Mathf.Max(index - 1, 0);
		int next = Mathf.Min(index + 1, track.TrackNodes.Count - 1);

		// Not enough nodes
		if (prev == next)
		{
			return;
		}

		// Calculate forward Vector
		Vector3 forward = track.TrackNodes[next].transform.position - track.TrackNodes[prev].transform.position;

		const float epsilon = 0.001f;
		if (forward.sqrMagnitude < epsilon)
		{
			// Not enough delta
			return;
		}

		// Set rotation
		node.transform.rotation = Quaternion.LookRotation(forward.normalized, node.transform.up);
	}

	//-------------------------------------------------------------------------------------
	// Triggers a mesh rebuild on nodes in the dirty set
	private void ProcessDirtyNodes()
	{
		// Iterate through each node in the set
		foreach (TrackNode node in DirtySet)
		{
			if (node == null)
			{
				continue;
			}

			// Rebuilod mesh
			TrackMeshUtility.UpdateAffectedSegments(node);
		}

		// Clear the set
		DirtySet.Clear();
	}

	//-------------------------------------------------------------------------------------
	// Called every frame
	public void Process()
	{
		// Iterate through the objects in the editor selection
		foreach (GameObject g in Selection.gameObjects)
		{
			// Ignore objects that don't contain a TrackNode component
			TrackNode node = (TrackNode)g.GetComponent<TrackNode>();
			if (node == null)
			{
				continue;
			}

			// Check if the transform has changed
			if (node.transform.hasChanged)
			{
				// Apply rotation constraint
				ContstrainNodeRotation(node);

				// Add to dirty set
				AddToDirtySet(node);

				// Unset changed flag
				node.transform.hasChanged = false;
			}
		}

		// Rebuild any dirty nodes
		ProcessDirtyNodes();
	}
}
