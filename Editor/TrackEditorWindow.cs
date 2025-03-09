//*************************************************************************************
// TrackEditorWindow.cs
//
// Author:
//       Gayan Ediriweera
//
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

//-------------------------------------------------------------------------------------
// Editor Window for the Track Editor System
public class TrackEditorWindow : EditorWindow {
	[MenuItem ("Falcon 3D Track Editor/Track Editor")]
	static void OpenWindow () {
		// Get the existing window, or create a new one
		GetWindow<TrackEditorWindow>("Track Editor");
	}

	[SerializeField]
	private List<TrackNode> SelectedNodes = new List<TrackNode>();

	//-------------------------------------------------------------------------------------
	// TrackRef
	// Class for access to the track reference. Holding a serialzed reference 
	// was causing issues when switching back from play mode 
	// (duplicated track objects)
	private class TrackRef
	{
		private static Track m_instance;
		public static Track Instance
		{
			get
			{
				if (m_instance == null)
				{
					// Attempt to find one
					//Debug.Log("No Reference, attempting to find");
					m_instance = Object.FindObjectOfType(typeof(Track)) as Track;
					// If we couldn't find one
					if (m_instance == null)
					{
						// Craete a new one
						// Debug.Log("Not found, creating");
						GameObject obj = new GameObject("Track");
						m_instance = obj.AddComponent<Track>();
					}
				}
				return m_instance;
			}
		}
	}
	
	//-------------------------------------------------------------------------------------
	// Handler for OnHierarchyChange
	void OnHierarchyChange()
	{
		List<TrackNode> trackNodes = TrackRef.Instance.TrackNodes;
		// Compile a list of indices that have null 
		// nodes. These nodes were deleted in the 
		// editor scene or heirarchy window.
		// TODO: This algorithm needs to correctly
		// handle multiple sparse deletions.
		List<int> nullIndices = new List<int>();
		while (true)
		{
			// Search for a null node
			int index = trackNodes.FindIndex(node => node == null);
			if (index < 0)
			{
				// No more null nodes
				break;
			}
			// Remove null node from track list
			trackNodes.RemoveAt(index);
			// Store the index
			nullIndices.Add(index);
		}

		if (nullIndices.Count > 0)
		{
			// For each of the indices that were previously null
			foreach (int index in nullIndices)
			{
				// Find the node prior to the index
				int prev = Mathf.Max (index-1, 0);
				if (prev >= trackNodes.Count)
				{
					continue;
				}

				// Update the mesh segments on the node
				TrackNode prevNode = trackNodes[prev];
				TrackRebuilder.Instance.AddToDirtySet(prevNode);
			}
			// Rename all the nodes
			UpdateNodeNames();

			// Mark track as dirty
			EditorUtility.SetDirty(TrackRef.Instance);
		}
	}

	//-------------------------------------------------------------------------------------
	// Handler for OnSelectionChange
	void OnSelectionChange()
	{
		// Clear the current list of selected nodes
		SelectedNodes.Clear();

		// For every selected object
		foreach (GameObject g in Selection.gameObjects)
		{
			// If we have a TrackNode component
			TrackNode node = g.GetComponent<TrackNode>();
			if (node)
			{
				// Add to the list of selected nodes
				SelectedNodes.Add(node);
			}
		}

		// Refresh the editor window
		Repaint();
	}

	//-------------------------------------------------------------------------------------
	// Give all the node objects to human readable index names
	private void UpdateNodeNames()
	{
		int nodeCount = TrackRef.Instance.TrackNodes.Count;
		for (int i = 0; i < nodeCount; i++)
		{
			TrackRef.Instance.TrackNodes[i].gameObject.name = "NODE_" + i;
		}
	}

	//-------------------------------------------------------------------------------------
	// Removes a node from the track NodeList and destroys it
	private TrackNode DeleteNode(TrackNode node)
	{
		Track track = node.TrackRef;

		// Get the index of the node
		int nodeIndex = track.TrackNodes.IndexOf(node);

		// Destroy the node
		DestroyImmediate(node.gameObject);

		// Remove from list
		track.TrackNodes.RemoveAt(nodeIndex);
		UpdateNodeNames();

		// Update mesh segments of the node that now 
		// occupies the index. If we removed the last
		// of first node, clamp it
		int nodeCount = track.TrackNodes.Count;
		nodeIndex = Mathf.Clamp(nodeIndex, 0, nodeCount - 1);
		TrackNode occupyingNode = null;
		if (nodeIndex >= 0 && nodeIndex < track.TrackNodes.Count)
		{
			occupyingNode = track.TrackNodes[nodeIndex];
			TrackMeshUtility.UpdateAffectedSegments(occupyingNode);
		}
		EditorUtility.SetDirty(track);
		return occupyingNode;
	}

	//-------------------------------------------------------------------------------------
	// Copy essential component values from one track node to another
	private void CopyTrackNodeComponents(GameObject from, GameObject to)
	{
		EditorUtility.CopySerialized(from.GetComponent<TrackNode>(), to.GetComponent<TrackNode>());
		// Create a new surface cross section if its different
		TrackNodeEditor.CreateSurfaceCrossSection(to.GetComponent<TrackNode>());
		EditorUtility.CopySerialized(from.GetComponent<SurfaceCrossSection>(), to.GetComponent<SurfaceCrossSection>());
	}

	//-------------------------------------------------------------------------------------
	// Creates a new track node
	private GameObject InstantiateNodeObject(GameObject template)
	{
		// Load up the prefab
		string prefabPath = "Assets/Falcon3DTrackEditor/Resources/TrackNodePrefab.prefab";
		GameObject prefab = (GameObject)Resources.LoadAssetAtPath(prefabPath, typeof(GameObject));

		// Instantiate
		GameObject newNodeObj = null;
		newNodeObj = (GameObject)(PrefabUtility.InstantiatePrefab(prefab));

		// Setup references and initial values
		TrackNode newNode = newNodeObj.GetComponent<TrackNode>();
		newNode.TrackRef = TrackRef.Instance;
		newNode.transform.parent = TrackRef.Instance.transform;
		newNode.transform.position = Vector3.zero;
		newNode.transform.rotation = Quaternion.identity;

		// If we have a template, copy values from it
		if (template)
		{
			CopyTrackNodeComponents(template, newNodeObj);
		}

		// Reassign the wall references
		newNode.LeftWall = newNode.transform.FindChild("LeftWall").gameObject;
		newNode.RightWall = newNode.transform.FindChild("RightWall").gameObject;

		// Return the newly created node
		return newNode.gameObject;
	}

	//-------------------------------------------------------------------------------------
	// Adds a new node to the end of the track node list
	private TrackNode AddNodeAtEnd()
	{
		GameObject prevNodeObj = null;
		int nodeCount = TrackRef.Instance.TrackNodes.Count;
		// If there's a last node, grab it
		if (nodeCount > 0)
		{
			prevNodeObj = TrackRef.Instance.TrackNodes[nodeCount - 1].gameObject;
		}


		// Create a new node based on the last node
		GameObject newNodeObj = InstantiateNodeObject(prevNodeObj);
		if (prevNodeObj)
		{
			// TODO Extrapolate the curve of the track
			const float distanceAhead = 10.0f;
			newNodeObj.transform.position = prevNodeObj.transform.position + prevNodeObj.transform.forward*distanceAhead;
			newNodeObj.transform.rotation = prevNodeObj.transform.rotation;
		}

		// Add the new node to the node list
		TrackNode newNode = newNodeObj.GetComponent<TrackNode>();
		TrackRef.Instance.TrackNodes.Add(newNode);

		// Update node names
		UpdateNodeNames();

		// Add new instance to dirty set
		TrackRebuilder.Instance.AddToDirtySet(newNode);
		EditorUtility.SetDirty(TrackRef.Instance); 
		return newNode;
	}

	//-------------------------------------------------------------------------------------
	// Inserts a new node in the middle of the track node list		
	private TrackNode InsertNewNode(TrackNode prevNode)
	{
		// If there was no node provided
		if (prevNode == null)
		{
			// Add at the end
			return AddNodeAtEnd();
		}

		int nodeCount = TrackRef.Instance.TrackNodes.Count;
		int prevIndex = TrackRef.Instance.TrackNodes.IndexOf(prevNode);

		if (prevIndex == -1)
		{
			// List does not contain node, something went wrong
			return null;
		}

		// If prev is the final node
		int nextIndex = prevIndex + 1;
		if (nextIndex >= nodeCount)
		{
			// Add at the end
			return AddNodeAtEnd();
		}

		TrackNode nextNode = TrackRef.Instance.TrackNodes[nextIndex];

		// Create a new node based on the previous node
		GameObject newNodeObj = InstantiateNodeObject(prevNode.gameObject);
		TrackNode newNode = newNodeObj.GetComponent<TrackNode>();

		// Position the new node between the previous and next nodes
		newNode.transform.position = Vector3.Slerp(prevNode.transform.position, nextNode.transform.position, 0.5f);
		newNode.transform.rotation = Quaternion.Slerp(prevNode.transform.rotation, nextNode.transform.rotation, 0.5f);

		// Insert the new node into the node list
		TrackRef.Instance.TrackNodes.Insert(prevIndex+1, newNode);

		// Update node names
		UpdateNodeNames();

		// Add new instance to dirty set
		TrackRebuilder.Instance.AddToDirtySet(newNode);
		EditorUtility.SetDirty(TrackRef.Instance); 
		return newNode;
	}

	void Update()
	{
		// Update the rebuilder
		TrackRebuilder.Instance.Process();
	}

	//-------------------------------------------------------------------------------------
	// Window GUI
	void OnGUI()
	{ 
		// Find the last node
		TrackNode lastNode = null;
		if (TrackRef.Instance.TrackNodes.Count > 0)
		{
			lastNode = TrackRef.Instance.TrackNodes[TrackRef.Instance.TrackNodes.Count - 1];
		}

		// If there are nodes selected which aren't the last one
		if (SelectedNodes.Count > 0 && SelectedNodes[0] != lastNode)
		{
			// Display insert button
			if (GUILayout.Button("Insert Node"))
			{
				// Insert new node and select it
				TrackNode newNode = InsertNewNode(SelectedNodes[0]);
				Selection.activeGameObject = newNode.gameObject;
			}
		}
		else
		{
			// Otherwise display add button
			if (GUILayout.Button("Add Node"))
			{
				// Create new node and select it
				TrackNode newNode = AddNodeAtEnd();
				Selection.activeGameObject = newNode.gameObject;
			}
		}

		// If there are no selected nodes
		if (SelectedNodes.Count == 0)
		{
			// Disable the followind GUI
			GUI.enabled = false;
		}
		if (GUILayout.Button("Remove Nodes"))
		{
			// Iterate through the selected nodes
			foreach(TrackNode node in SelectedNodes)
			{
				// and delete
				TrackNode occupyingNode = DeleteNode(node);
				if (occupyingNode)
				{
					Selection.activeGameObject = occupyingNode.gameObject;
				}
			}
		}

		// Reenable GUI
		GUI.enabled = true;
		if (GUILayout.Button("Reset"))
		{
			// Display warning popup
			if (EditorUtility.DisplayDialog("Are you sure you want to Reset?", "This will remove all nodes from the Track", "OK", "Cancel"))
			{
				// If yes, destroy each node one by one
				while(TrackRef.Instance.TrackNodes.Count > 0)
				{
					if (TrackRef.Instance.TrackNodes[0])
					{
						DestroyImmediate(TrackRef.Instance.TrackNodes[0].gameObject);
					}
					TrackRef.Instance.TrackNodes.RemoveAt(0);
				}

				// And add four new nodes
				AddNodeAtEnd();
				AddNodeAtEnd();
				AddNodeAtEnd();
				AddNodeAtEnd();
			}
		}

		if (GUI.changed)
		{
			EditorUtility.SetDirty(TrackRef.Instance);
		}
	}
}
