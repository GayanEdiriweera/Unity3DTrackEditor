//*************************************************************************************
// TrackMeshUtility.cs
//
// Author:
//       Gayan Ediriweera
//
using UnityEngine;
using System.Collections.Generic;

//-------------------------------------------------------------------------------------
// Utility functions for creating and manipulating the procedural 
// meshes on segments of the track
public class TrackMeshUtility {
	
	private const int SegmentLengthVerts = 60;
	private const int SegmentWidthVerts = 20;
	private const int WallHeightVerts = 2;
	
	//-------------------------------------------------------------------------------------
	// Updates meshes on all segments affected by this one being changed
	public static void UpdateAffectedSegments(TrackNode changed)
	{
		Track track = changed.TrackRef;

		// Get the index of the node in the track
		int index = track.TrackNodes.IndexOf(changed);

		// Change is localized to the nearby five nodes
		int startIndex = Mathf.Max(index - 2, 0);
		int endIndex = Mathf.Min(index + 2, track.TrackNodes.Count);

		// Iterate through the nodes
		for (int i = startIndex; i < endIndex; i++)
		{
			TrackNode current = track.TrackNodes[i];

			// Update track and wall meshes
			UpdateTrackSegment(current);
			UpdateWallSegments(current);
		}
	}

	//-------------------------------------------------------------------------------------
	// Updates track mesh for the segment of a given node, destroys mesh if it's an end node
	private static void UpdateTrackSegment(TrackNode startNode)
	{
		Track track = startNode.TrackRef;

		// If we are an end node
		if (track.IsEndNode(startNode))
		{
			// Destroy any meshes we have
			DestroyRenderAndCollisionMesh(startNode.gameObject);
			return;
		}

		MeshFilter filterComponent = startNode.GetComponent<MeshFilter>();

		// If we don't have a mesh
		if (filterComponent.sharedMesh == null)
		{
			// Create one
			CreateRenderAndCollisionMesh(startNode.gameObject, SegmentLengthVerts, SegmentWidthVerts);
		}

		// Get existing vertex array
		int currentVertex = 0;
		Vector3[] vertices = filterComponent.sharedMesh.vertices;
		
		// Iterate over the length of the segment
		for (int n = 0; n < SegmentLengthVerts; n++)
		{	
			float t = (float)n/(SegmentLengthVerts-1);

			// Iterate over the width of the segment
			for ( int m = 0; m < SegmentWidthVerts; m++)
			{
				// Sample the actual position for this vertex
				float u = (float)m/(SegmentWidthVerts-1);
				Vector3 vertPos = TrackSurfaceUtility.SampleSegmentLocalPosition(startNode, t, u);
				if (currentVertex >= vertices.Length)
				{
					// Something went wrong
					// Debug.LogWarning("Vertex array size mismatch");
					break;
				}

				// Update the position
				vertices[currentVertex] = vertPos;
				currentVertex++;
			}
		}

		// Update the mesh
		filterComponent.sharedMesh.vertices = vertices;

		// Recalculate bounds and normals
		filterComponent.sharedMesh.RecalculateBounds();
		filterComponent.sharedMesh.RecalculateNormals();
	}

	//-------------------------------------------------------------------------------------
	// Updates wall meshes for of a given node, destroys mesh if it's an end node
	private static void UpdateWallSegments(TrackNode startNode)
	{
		Track track = startNode.TrackRef;
		GameObject leftWall = startNode.LeftWall.gameObject;
		GameObject rightWall = startNode.RightWall.gameObject;

		// If we are an end node
		if (track.IsEndNode(startNode))
		{
			// Destroy any meshes we have
			DestroyRenderAndCollisionMesh(leftWall);
			DestroyRenderAndCollisionMesh(rightWall);
			return;
		}

		MeshFilter leftfilterComponent = leftWall.GetComponent<MeshFilter>();
		MeshFilter rightfilterComponent = rightWall.GetComponent<MeshFilter>();
		// If we don't have a mesh
		if (leftfilterComponent.sharedMesh == null)
		{
			// Create one
			CreateRenderAndCollisionMesh(leftWall, SegmentLengthVerts, WallHeightVerts);
		}
		if (rightfilterComponent.sharedMesh == null)
		{
			CreateRenderAndCollisionMesh(rightWall, SegmentLengthVerts, WallHeightVerts);
		}

		// Get existing vertex array
		int currentVertex = 0;
		Vector3[] leftVertices = leftfilterComponent.sharedMesh.vertices;
		Vector3[] rightVertices = rightfilterComponent.sharedMesh.vertices;

		// Iterate over the length of the segment
		for (int n = 0; n < SegmentLengthVerts; n++)
		{	
			// Sample the actual positions for this vertex set
			float t = (float)n/(SegmentLengthVerts-1);
			Vector3 leftBase = Vector3.zero;
			Vector3 leftTop = Vector3.zero;
			Vector3 rightBase = Vector3.zero;
			Vector3 rightTop = Vector3.zero;
			TrackSurfaceUtility.SampleSegmentWallLocalPositions(startNode, t, ref leftBase, ref leftTop, ref rightBase, ref rightTop);//.SectionSpaceToWallHeight(t, startNode);
			
			if (currentVertex + 1 >= leftVertices.Length || 
			    currentVertex + 1>= rightVertices.Length)
			{
				// Something went wrong
				// Debug.LogWarning("Vertex array size mismatch");
				break;
			}

			// Update the positions
			leftVertices[currentVertex+1] = leftBase;
			leftVertices[currentVertex] = leftTop;
			rightVertices[currentVertex] = rightBase;
			rightVertices[currentVertex+1] = rightTop;
			currentVertex += WallHeightVerts;

		}

		// Update the meshes, recalculate bounds and normals
		leftfilterComponent.sharedMesh.vertices = leftVertices;
		leftfilterComponent.sharedMesh.RecalculateBounds();
		leftfilterComponent.sharedMesh.RecalculateNormals();

		rightfilterComponent.sharedMesh.vertices = rightVertices;
		rightfilterComponent.sharedMesh.RecalculateBounds();
		rightfilterComponent.sharedMesh.RecalculateNormals();
	}

	//-------------------------------------------------------------------------------------
	// Destroy procedural meshes on the object, usually when a node becomes an end node
	private static void DestroyRenderAndCollisionMesh(GameObject obj)
	{
		obj.GetComponent<MeshCollider>().sharedMesh = null;
		Mesh.DestroyImmediate(obj.GetComponent<MeshFilter>().sharedMesh);
	}

	//-------------------------------------------------------------------------------------
	// Creates procedural meshes on the object, with given size
	private static void CreateRenderAndCollisionMesh(GameObject obj, int rows, int columns)
	{
		Mesh mesh = InstantiateNewMesh(rows, columns);
		obj.GetComponent<MeshFilter>().sharedMesh = mesh;
		obj.GetComponent<MeshCollider>().sharedMesh = mesh;
	}

	//-------------------------------------------------------------------------------------
	// Instantiate a new mesh asset, with given size
	private static Mesh InstantiateNewMesh(int rows, int columns)
	{
		// Create lists of verts, uvs and tris
		List<Vector3> newVertices = new List<Vector3>();
		List<Vector2> newUVs = new List<Vector2>();
		List<int> newTriangles = new List<int>();

		// Fill out verts and uvs to the correct size
		for( int i = 0; i < rows; i++)
		{
			for ( int j = 0; j < columns; j++)
			{
				newVertices.Add(Vector3.zero);
				Vector2 uv = Vector2.zero;
				uv.x = (float)i/(rows-1);
				uv.y = (float)j/(columns-1);
				newUVs.Add(uv);
			}
		}

		// Fill out tris with the correct values for single face
		for( int i = 0; i < rows-1; i++)
		{
			for ( int j = 0; j < columns -1; j++)
			{
				newTriangles.Add(i*columns + j);
				newTriangles.Add((i+1)*columns + j);
				newTriangles.Add(i*columns + j + 1);
				
				newTriangles.Add(i*columns + j + 1);
				newTriangles.Add((i+1)*columns + j);
				newTriangles.Add((i+1)*columns + j + 1);
			}
		}

		// Create new mesh asset
		Mesh mesh = new Mesh();

		// Assign a semi-readable name
		mesh.name = "Mesh" + mesh.GetInstanceID();

		// Convert lists to arrays and assign
		mesh.vertices = newVertices.ToArray();
		mesh.triangles = newTriangles.ToArray();
		mesh.uv = newUVs.ToArray();
		return mesh;
	}
}

