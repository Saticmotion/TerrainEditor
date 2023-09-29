using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TerrainEditor : MonoBehaviour
{
	public GameObject chunkPrefab;

	private GameObject chunkHolder;
	private List<Chunk> chunks = new List<Chunk>();
	private const int terrainSize = 64;
	private const float terrainCellSize = .25f;
	private const int chunkSize = 9;
	private float numChunksX;
	private float numChunksY;

	private LayerMask terrainLayer;

	private float brushSize = 2f;
	

	private void Awake()
	{
		terrainLayer = LayerMask.GetMask("Terrain");

		chunkHolder = new GameObject("Terrain");
	}

	void Start()
	{
		numChunksX = terrainSize / (float)chunkSize;
		numChunksY = terrainSize / (float)chunkSize;

		Chunk fullChunk = null;
		Chunk rightChunk = null;
		Chunk topChunk = null;

		for (int y = 0; y < numChunksY; y++)
		{
			for (int x = 0; x < numChunksX; x++)
			{
				//NOTE(Simon): Choose smallest of: full chunk or leftover (i.e. a top or right edge chunk)
				int chunkWidth = Mathf.Min(chunkSize, terrainSize - (x * chunkSize));
				int chunkHeight = Mathf.Min(chunkSize, terrainSize - (y * chunkSize));

				bool doingFullChunk = chunkWidth == chunkSize && chunkHeight == chunkSize;
				bool doingRightChunk = chunkWidth < chunkSize && chunkHeight == chunkSize;
				bool doingTopChunk = chunkWidth == chunkSize && chunkHeight < chunkSize;

				Chunk chunk;

				//NOTE(Simon): These checks are used to cache mesh generation results. Each type of mesh is generated once and then copied.
				//NOTE(Simon): The types are full, top, right and top-right chunk.
				//NOTE(Simon): I.e. the chunks at the top and right edges are smaller than a full chunk.
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

				chunk.transform.position = new Vector3(x * chunkSize * terrainCellSize, 0, y * chunkSize * terrainCellSize);
				chunk.transform.SetParent(chunkHolder.transform);
				chunk.name = $"Chunk{x}-{y}";
				chunks.Add(chunk);
			}
		}
	}

	private Vector3 mouseHit;

	void Update()
	{
		var mousePos = Mouse.current.position.ReadValue();
		var ray = Camera.main.ScreenPointToRay(mousePos);

		if (Mouse.current.leftButton.isPressed)
		{
			float amount = 2f;
			float direction = Keyboard.current.shiftKey.ReadValue() > 0 ? -1 : 1;

			var chunkHits = Physics.SphereCastAll(ray, brushSize, Mathf.Infinity, terrainLayer);
			
			if (Physics.Raycast(ray, out var mouseHit, Mathf.Infinity, terrainLayer))
			{
				foreach (var hit in chunkHits)
				{
					var chunk = hit.transform.GetComponent<Chunk>();

					chunk.IncreaseHeight(mouseHit.point, brushSize, direction * amount * Time.deltaTime);
				}
			}
		}

		Physics.Raycast(ray, out var highlightHit, Mathf.Infinity, terrainLayer);
		mouseHit = highlightHit.point;
		chunks[0].GetComponent<Renderer>().sharedMaterial.SetVector("_MousePos", highlightHit.point);
		chunks[0].GetComponent<Renderer>().sharedMaterial.SetFloat("_BrushSize", brushSize);
	}

	private void OnDrawGizmos()
	{
		Gizmos.DrawSphere(mouseHit, .1f);
	}

	private void FixEdgeNormals()
	{
		for (int y = 0; y < numChunksY; y++)
		{
			for (int x = 0; x < numChunksX; x++)
			{
				//var chunk = chunks[y * numChunksY + x];
			}
		}
	}

	private int WorldPosToVertexIndex(int chunkIndex, Vector3 pos)
	{
		var chunkTransform = chunks[chunkIndex].transform;
		var chunkPos = new Vector2(chunkTransform.position.x, chunkTransform.position.z);
		var pos2d = new Vector2(pos.x, pos.z);

		var offset = pos2d - chunkPos;
		var vertexPos = Vector2Int.RoundToInt(offset / terrainCellSize);
		var vertexIndex = vertexPos.y * (chunkSize + 1) + vertexPos.x;

		return vertexIndex;
	}

	private int WorldPosToChunkIndex(Vector3 pos)
	{
		int numChunksX = Mathf.CeilToInt(terrainSize / (float)chunkSize);

		var terrainCell = new Vector2Int((int)(pos.x / terrainCellSize), (int)(pos.z / terrainCellSize));
		int chunkIndex = terrainCell.y / chunkSize * numChunksX + terrainCell.x / chunkSize;
		return chunkIndex;
	}
}
