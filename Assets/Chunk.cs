using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Mesh))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
	public Mesh mesh;
	private new MeshCollider collider;

	private Vector2Int sizeInQuads;
	private Vector2Int sizeInVerts;
	private Vector3[] vertices;

	private const MeshUpdateFlags meshUpdateFlags = MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontValidateIndices;

	public void Init(int width, int height, float terrainCellSize, bool generateMesh)
	{
		sizeInQuads = new Vector2Int(width, height);
		sizeInVerts = new Vector2Int(width + 1, height + 1);

		mesh = GetComponent<MeshFilter>().mesh;
		collider = GetComponent<MeshCollider>();
		collider.sharedMesh = mesh;

		if (generateMesh)
		{
			vertices = new Vector3[sizeInVerts.x * sizeInVerts.y];
			GenerateMesh(terrainCellSize);
		}
		else
		{
			vertices = mesh.vertices;
		}
	}

	private void GenerateMesh(float terrainCellSize)
	{
		Vector2[] uvs = new Vector2[sizeInVerts.x * sizeInVerts.y];
		int[] triangles = new int[sizeInQuads.x * sizeInQuads.y * 6];

		for (int y = 0; y < sizeInVerts.y; y++)
		{
			for (int x = 0; x < sizeInVerts.x; x++)
			{
				vertices[y * sizeInVerts.x + x] = new Vector3(x * terrainCellSize, 0f, y * terrainCellSize);
			}
		}

		for (int y = 0; y < sizeInVerts.y; y++)
		{
			for (int x = 0; x < sizeInVerts.x; x++)
			{
				uvs[y * sizeInVerts.x + x] = new Vector2(x * terrainCellSize, y * terrainCellSize);
			}
		}

		//NOTE(Simon): Generate triangles that alternate in direction. i.e.:
		//	______________
		//	| /|\ || /|\ |
		//	|/_|_\||/_|_\|
		//	|\ | /||\ | /|
		//	|_\|/_||_\|/_|
		for (int y = 0; y < sizeInQuads.y; y++)
		{
			for (int x = 0; x < sizeInQuads.x; x++)
			{
				int triangleOffset = (y * sizeInQuads.x + x) * 6;

				if ((y % 2 == 0 && x % 2 == 1)
					|| (y % 2 == 1 && x % 2 == 0))
				{
					triangles[triangleOffset + 0] = y * sizeInVerts.x + x;
					triangles[triangleOffset + 2] = (y + 1) * sizeInVerts.x + (x + 1);
					triangles[triangleOffset + 1] = (y + 1) * sizeInVerts.x + x;

					triangles[triangleOffset + 3] = y * sizeInVerts.x + x;
					triangles[triangleOffset + 5] = y * sizeInVerts.x + (x + 1);
					triangles[triangleOffset + 4] = (y + 1) * sizeInVerts.x + (x + 1);
				}
				else
				{
					triangles[triangleOffset + 0] = y * sizeInVerts.x + x;
					triangles[triangleOffset + 2] = y * sizeInVerts.x + (x + 1);
					triangles[triangleOffset + 1] = (y + 1) * sizeInVerts.x + x;

					triangles[triangleOffset + 3] = y * sizeInVerts.x + (x + 1); ;
					triangles[triangleOffset + 5] = (y + 1) * sizeInVerts.x + (x + 1);
					triangles[triangleOffset + 4] = (y + 1) * sizeInVerts.x + x;
				}
			}
		}

		mesh.vertices = vertices;
		mesh.uv = uvs;
		mesh.triangles = triangles;
		mesh.RecalculateNormals(meshUpdateFlags);
		mesh.RecalculateBounds(meshUpdateFlags);
	}

	private void UpdateMesh()
	{
		mesh.vertices = vertices;
		mesh.RecalculateNormals(meshUpdateFlags);
		mesh.RecalculateBounds(meshUpdateFlags);
	}

	public void SetHeight(int vertexPos, float height)
	{
		vertices[vertexPos].y = height;
		UpdateMesh();
	}

	//NOTE(Simon): Increase height for all vertices in radius, with sin-shaped falloff
	public void IncreaseHeight(Vector3 pos, float radius, float height, float maxHeight = Mathf.Infinity)
	{
		var pos2d = new Vector2(pos.x, pos.z);
		var offset2d = new Vector2(transform.position.x, transform.position.z);
		for (int i = 0; i < vertices.Length; i++)
		{
			var vertexPos2d = new Vector2(vertices[i].x, vertices[i].z);
			float distance = (vertexPos2d + offset2d - pos2d).magnitude;

			if (distance < radius)
			{
				float t = 1 - (distance  / radius);
				float factor = -(Mathf.Cos(Mathf.PI * t) - 1) / 2;
				vertices[i].y = Mathf.Min(vertices[i].y + height * factor, maxHeight);
			}
		}

		UpdateMesh();
	}
}
