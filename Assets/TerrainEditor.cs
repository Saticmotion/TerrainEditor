using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TerrainEditor : MonoBehaviour
{
	public GameObject chunkPrefab;

	private GameObject chunkHolder;
	private Material chunkMaterial;
	private List<Chunk> chunks = new List<Chunk>();
	private RaycastHit[] sphereCastBuffer;
	private const int terrainSize = 100;
	private const float terrainCellSize = .25f;
	private const int chunkSizeQuads = 20;
	private float numChunksX;
	private float numChunksY;

	private LayerMask terrainLayer;

	private float brushRadius = 2f;

	List<Vector3> normalBuffer1 = new List<Vector3>((chunkSizeQuads + 1) * (chunkSizeQuads + 1));
	List<Vector3> normalBuffer2 = new List<Vector3>((chunkSizeQuads + 1) * (chunkSizeQuads + 1));


	private void Awake()
	{
		terrainLayer = LayerMask.GetMask("Terrain");

		chunkHolder = new GameObject("Terrain");

		ResizeSphereCastBuffer();
	}

	void Start()
	{
		numChunksX = terrainSize / (float)chunkSizeQuads;
		numChunksY = terrainSize / (float)chunkSizeQuads;

		Chunk fullChunk = null;
		Chunk rightChunk = null;
		Chunk topChunk = null;

		for (int y = 0; y < numChunksY; y++)
		{
			for (int x = 0; x < numChunksX; x++)
			{
				//NOTE(Simon): Choose smallest of: full chunk or leftover (i.e. a top or right edge chunk)
				int chunkWidth = SizeForIndex(x, chunkSizeQuads, terrainSize);
				int chunkHeight = SizeForIndex(y, chunkSizeQuads, terrainSize);

				bool doingFullChunk = chunkWidth == chunkSizeQuads && chunkHeight == chunkSizeQuads;
				bool doingRightChunk = chunkWidth < chunkSizeQuads && chunkHeight == chunkSizeQuads;
				bool doingTopChunk = chunkWidth == chunkSizeQuads && chunkHeight < chunkSizeQuads;

				Chunk chunk;

				//NOTE(Simon): These checks are used to cache mesh generation results. Each type of mesh is generated once and then copied.
				//NOTE(cont.): The types are full, top, right and top-right chunk.
				//NOTE(cont.): I.e. the chunks at the top and right edges are smaller than a full chunk.
				if (doingFullChunk)
				{
					if (fullChunk == null)
					{
						fullChunk = Instantiate(chunkPrefab).GetComponent<Chunk>();
						fullChunk.Init(chunkWidth, chunkHeight, terrainCellSize, true);
						chunk = fullChunk;
					}
					else
					{
						chunk = Instantiate(fullChunk);
						chunk.Init(chunkWidth, chunkHeight, terrainCellSize, false);
					}
				}
				else if (doingRightChunk)
				{
					if (rightChunk == null)
					{
						rightChunk = Instantiate(chunkPrefab).GetComponent<Chunk>();
						rightChunk.Init(chunkWidth, chunkHeight, terrainCellSize, true);
						chunk = rightChunk;
					}
					else
					{
						chunk = Instantiate(rightChunk);
						chunk.Init(chunkWidth, chunkHeight, terrainCellSize, false);
					}
				}
				else if (doingTopChunk)
				{
					if (topChunk == null)
					{
						topChunk = Instantiate(chunkPrefab).GetComponent<Chunk>();
						topChunk.Init(chunkWidth, chunkHeight, terrainCellSize, true);
						chunk = topChunk;
					}
					else
					{
						chunk = Instantiate(topChunk);
						chunk.Init(chunkWidth, chunkHeight, terrainCellSize, false);
					}
				}
				//NOTE(Simon): This is a top-right chunk. Only happens once, so no caching needed
				else
				{
					chunk = Instantiate(chunkPrefab).GetComponent<Chunk>();
					chunk.Init(chunkWidth, chunkHeight, terrainCellSize, true);
				}

				chunk.transform.position = new Vector3(x * chunkSizeQuads * terrainCellSize, 0, y * chunkSizeQuads * terrainCellSize);
				chunk.transform.SetParent(chunkHolder.transform);
				chunk.name = $"Chunk{x}-{y}";
				chunks.Add(chunk);
			}
		}

		chunkMaterial = chunks[0].GetComponent<Renderer>().sharedMaterial;


	}

	void Update()
	{
		var mousePos = Mouse.current.position.ReadValue();
		var ray = Camera.main.ScreenPointToRay(mousePos);

		if (Mouse.current.scroll.value.y != 0 && Keyboard.current.ctrlKey.isPressed)
		{
			brushRadius += 0.25f * Mouse.current.scroll.value.normalized.y;
			brushRadius = Mathf.Clamp(brushRadius, 0.25f, 10f);
			ResizeSphereCastBuffer();
		}

		if (Mouse.current.leftButton.isPressed)
		{
			float amount = 2f;
			float direction = Keyboard.current.shiftKey.ReadValue() > 0 ? -1 : 1;

			int chunkHits = Physics.SphereCastNonAlloc(ray, brushRadius, sphereCastBuffer, Mathf.Infinity, terrainLayer);
			
			if (Physics.Raycast(ray, out var mouseHit, Mathf.Infinity, terrainLayer))
			{
				for (int i = 0; i < chunkHits; i++)
				{
					var hit = sphereCastBuffer[i];
					var chunk = hit.transform.GetComponent<Chunk>();

					chunk.IncreaseHeight(mouseHit.point, brushRadius, direction * amount * Time.deltaTime, 3);
				}
				FixEdgeNormals();
			}
		}

		Physics.Raycast(ray, out var highlightHit, Mathf.Infinity, terrainLayer);
		chunkMaterial.SetVector("_MousePos", highlightHit.point);
		chunkMaterial.SetFloat("_BrushSize", brushRadius);
	}

	private void FixEdgeNormals()
	{
		for (int y = 0; y < numChunksY - 1; y++)
		{
			for (int x = 0; x < numChunksX - 1; x++)
			{
				int chunkCenterIndex = y * Mathf.CeilToInt(numChunksY) + x;
				FixEdgeNormalsNorth(chunkCenterIndex);
				FixEdgeNormalsEast(chunkCenterIndex);
			}
		}

		//NOTE(Simon): Update topmost row. Special case because it has no chunks north of it
		for (int x = 0; x < numChunksX - 1; x++)
		{
			int chunkCenterIndex = ((int)numChunksY - 1) * Mathf.CeilToInt(numChunksY) + x;
			FixEdgeNormalsEast(chunkCenterIndex);
		}

		//NOTE(Simon): Update rightmost row. Special case because it has no chunks east of it
		for (int y = 0; y < numChunksY - 1; y++)
		{
			int chunkCenterIndex = y * Mathf.CeilToInt(numChunksY) + ((int)numChunksX - 1);
			FixEdgeNormalsNorth(chunkCenterIndex);
		}
	}

	private void FixEdgeNormalsNorth(int chunkCenterIndex)
	{ 
		const int chunkSizeVerts = chunkSizeQuads + 1;

		int chunkNorthIndex = chunkCenterIndex + Mathf.CeilToInt(numChunksY);
		chunks[chunkCenterIndex].mesh.GetNormals(normalBuffer1);
		chunks[chunkNorthIndex].mesh.GetNormals(normalBuffer2);

		for (int i = 0; i < chunkSizeVerts; i++)
		{
			int indexCenter = chunkSizeVerts * (chunkSizeVerts - 1) + i;
			int indexNorth = i;
			var sum = normalBuffer1[indexCenter] + normalBuffer2[indexNorth];
			var avg = sum / 2;
			normalBuffer1[indexCenter] = avg;
			normalBuffer2[indexNorth] = avg;
		}

		chunks[chunkCenterIndex].mesh.SetNormals(normalBuffer1);
		chunks[chunkNorthIndex].mesh.SetNormals(normalBuffer2);
	}

	private void FixEdgeNormalsEast(int chunkCenterIndex)
	{
		const int chunkSizeVerts = chunkSizeQuads + 1;

		int chunkEastIndex = chunkCenterIndex + 1;
		chunks[chunkCenterIndex].mesh.GetNormals(normalBuffer1);
		chunks[chunkEastIndex].mesh.GetNormals(normalBuffer2);

		for (int i = 0; i < chunkSizeVerts; i++)
		{
			int indexCenter = i * chunkSizeVerts + (chunkSizeVerts - 1);
			int indexEast = i * chunkSizeVerts;
			var sum = normalBuffer1[indexCenter] + normalBuffer2[indexEast];
			var avg = sum / 2;
			normalBuffer1[indexCenter] = avg;
			normalBuffer2[indexEast] = avg;
		}

		chunks[chunkCenterIndex].mesh.SetNormals(normalBuffer1);
		chunks[chunkEastIndex].mesh.SetNormals(normalBuffer2);
	}

	private int WorldPosToVertexIndex(int chunkIndex, Vector3 pos)
	{
		var chunkTransform = chunks[chunkIndex].transform;
		var chunkPos = new Vector2(chunkTransform.position.x, chunkTransform.position.z);
		var pos2d = new Vector2(pos.x, pos.z);

		var offset = pos2d - chunkPos;
		var vertexPos = Vector2Int.RoundToInt(offset / terrainCellSize);
		var vertexIndex = vertexPos.y * (chunkSizeQuads + 1) + vertexPos.x;

		return vertexIndex;
	}

	private int WorldPosToChunkIndex(Vector3 pos)
	{
		int numChunksX = Mathf.CeilToInt(terrainSize / (float)chunkSizeQuads);

		var terrainCell = new Vector2Int((int)(pos.x / terrainCellSize), (int)(pos.z / terrainCellSize));
		int chunkIndex = terrainCell.y / chunkSizeQuads * numChunksX + terrainCell.x / chunkSizeQuads;
		return chunkIndex;
	}

	private void ResizeSphereCastBuffer()
	{
		int chunksPerAxis = Mathf.CeilToInt(brushRadius * 2f / terrainCellSize / chunkSizeQuads) + 1;
		int bufferSize = chunksPerAxis * chunksPerAxis;
		if (sphereCastBuffer == null || bufferSize > sphereCastBuffer.Length)
		{
			sphereCastBuffer = new RaycastHit[bufferSize];
		}
	}

	private int SizeForIndex(int value, int chunkSize, int terrainSize)
	{
		return Mathf.Min(chunkSize, terrainSize - (value * chunkSize));
	}
}
