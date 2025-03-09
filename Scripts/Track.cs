//*************************************************************************************
// Track.cs
//
// Author:
//       Gayan Ediriweera
//
using UnityEngine;
using System.Collections.Generic;

//-------------------------------------------------------------------------------------
// Overarching track which holds TrackNodes
[System.Serializable]
public class Track : MonoBehaviour
{
	// List of node references
	[SerializeField]
	public List<TrackNode> TrackNodes = new List<TrackNode>();

	//-------------------------------------------------------------------------------------
	// Returns if the given node index is an end node
	public bool IsEndNode(int nodeIndex)
	{
		// Nodes 0, count-1 and count are end nodes since they have no segment defined
		int nodeCount = TrackNodes.Count;
		if (nodeIndex == 0 || (nodeIndex >= (nodeCount - 2)))
		{
			return true;
		}
		return false;
	}

	//-------------------------------------------------------------------------------------
	// Returns if the given node is an end node	
	public bool IsEndNode(TrackNode node)
	{
		int nodeCount = TrackNodes.Count;

		// If there are less than 3 nodes, every node is an end node
		if (nodeCount < 3)
		{
			return true;
		}
		
		// Avoiding O(n) List<>.indexOf(), instead using O(1) List<>.operator[]
		TrackNode first = TrackNodes[0];
		TrackNode secondLast = TrackNodes[nodeCount - 2];
		TrackNode last = TrackNodes[nodeCount - 1];

		// Nodes 0, count-1 and count are end nodes since they have no segment defined
		if (node == first || 
		    node == secondLast || 
		    node == last)
		{
			return true;
		}
		
		return false;
	}
}