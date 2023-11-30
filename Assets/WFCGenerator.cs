using Godot;
using System;
using System.Collections.Generic;

public partial class WFCGenerator : Node2D
{
	private bool done = false;
	private Vector2I Vector_1 = new Vector2I(-1,-1);
	[Export] private const int H=15, V=15; // Map size, horizontal and vertical
	[Export] public const int MATCH_RADIUS = 1;
	private int maxN; // Total number of tiles which need to be set
	private int currentN=0; // Number of tiles that are currently set
	[Export] public TileMap target;
	[Export] public TileMap sample;

	[Export] public bool showProgress = true; // If you know the code works for you disable this as it has a big impact on performance
	
	private int tempI = 0, tempJ = 0;
	
	// Holds tile occurances in the sample for future use as rules
	private Dictionary<Vector2I, List<Vector2I>> usedTiles = new Dictionary<Vector2I, List<Vector2I>>();
	
	private PriorityQueue<Vector2I, int> queue = new PriorityQueue<Vector2I, int>();
	
	
	// Holds tiles data for internal use only. DO NOT USED DIRECTLY! Use SetTile() and GetTile() instead
	private Vector2I[,] tileMapArray = new Vector2I[H+MATCH_RADIUS*2,V+MATCH_RADIUS*2]; 
	
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		sample.Hide();
		maxN = H*V;
		Init(); // Needs to be called to initialize usedTiles (to create rules)
		ClearMap();
	}
	
	// Called every frame
	public override void _Process(double delta)
	{
		if (done)
		{
			ApplyTileMap();
			return;
		}
		if (currentN>=maxN)
		{
			done = true;
			return;
		}
		
		Vector2I nextTile = GetNextTile(); // Find the next tile to set
		List<Vector2I> options = GetOptions(nextTile); // What can I put in this tile?
		SetTile(nextTile, options[(int)(GD.Randi()%options.Count)]); // Set tile to a random possible option
		
		if (showProgress)
		{
			ApplyTileMap();
		}

		currentN++;
	}
	
	// Applies the tiles array to the target tile map
	public void ApplyTileMap()
	{
		for (int i=0; i<H; i++)
			for (int j=0; j<V; j++)
			{
				target.SetCell(0, new Vector2I(i,j), 1, GetTile(i,j));
			}
	}
	
	// Analyses sample for rules. Must be called once before on ready
	public void Init()
	{
		usedTiles.Clear();
		Godot.Collections.Array<Vector2I> usedCells = sample.GetUsedCells(0);
		foreach (Vector2I cell in usedCells)
		{
			Vector2I atlasCoord = sample.GetCellAtlasCoords(0, cell);
			if (!usedTiles.ContainsKey(atlasCoord))
			{
				usedTiles.Add(atlasCoord, new List<Vector2I>());
			}
			usedTiles[atlasCoord].Add(cell);
		}
	}
	// Returns the number of possible options for the given tile coordinates
	private int GetOptionsCount(Vector2I coord)
	{
		int count = 0;
		foreach (Vector2I usedTile in usedTiles.Keys)
		{
			bool f = true, b=false;
			int i, j;
			for (i=-MATCH_RADIUS; i<=MATCH_RADIUS&&!b; i++)
				for (j=-MATCH_RADIUS; j<=MATCH_RADIUS&&!b; j++)
				{
					bool anyMatch = false;
					foreach (Vector2I occurance in usedTiles[usedTile])
					{
						if (GetTile(coord+new Vector2I(i,j))==Vector_1)
						{
							anyMatch = true;
						}
						if (sample.GetCellAtlasCoords(0, occurance+new Vector2I(i,j))==GetTile(coord+new Vector2I(i,j)))
						{
							anyMatch = true;
						}
					}
					if (!anyMatch)
					{
						f = false;
						b = true;
					}
				}
			if (f) count++;
		}

		return count;
	}
	// Returns all possible options for the given tile coordinates
	private List<Vector2I> GetOptions(Vector2I coord)
	{
		List<Vector2I> options = new List<Vector2I>();
		foreach (Vector2I usedTile in usedTiles.Keys)
		{
			bool f = true, b=false;
			int i=0, j=0;
			for (i=-MATCH_RADIUS; i<=MATCH_RADIUS&&!b; i++)
				for (j=-MATCH_RADIUS; j<=MATCH_RADIUS&&!b; j++)
				{
					bool anyMatch = false;
					foreach (Vector2I occurance in usedTiles[usedTile])
					{
						if (GetTile(coord+new Vector2I(i,j))==Vector_1)
						{
							anyMatch = true;
							break;
						}
						if (sample.GetCellAtlasCoords(0, occurance+new Vector2I(i,j))==GetTile(coord+new Vector2I(i,j)))
						{
							anyMatch = true;
							break;
						}
					}
					if (!anyMatch)
					{
						f = false;
						b = true;
					}
				}
			if (f) options.Add(usedTile);
		}
		GD.Print(options.Count);
		return options;
	}
	
	// Returns the tile with the least possible options. 
	private Vector2I GetNextTile()
	{
		Vector2I bestTile = new Vector2I(0,0);
		int leastOptions = usedTiles.Count;
		for (int i=0; i<H; i++)
			for (int j=0; j<V; j++)
			{
				Vector2I coord = new Vector2I(i,j);
				if (GetTile(coord)!=Vector_1) continue;
				int count = GetOptionsCount(coord);
				if (count<leastOptions && count>0)
				{
					leastOptions = count;
					bestTile[0] = i;
					bestTile[1] = j;
				}
			}
		return bestTile;
	}
	
	
	// Returns true if the specified Vector2 is in the given list NEED OPTIMIVATION,NOT USED!
	private bool IsInList(Vector2I atlasCoord, Vector2I[] targetList)
	{
		foreach (Vector2I v in targetList)
		{
			if (v == atlasCoord) return true;
		}
		return false;
	}

	// Set every tileMapArray cell to (-1, -1)
	private void ClearMap()
	{
		for (int i=0; i<H+MATCH_RADIUS*2; i++)
			for (int j=0; j<V+MATCH_RADIUS*2; j++)
			{
				tileMapArray[i,j] = Vector_1;
			}
	}

	// Get tile from tileMapArray using coordinates
	private Vector2I GetTile(int coordX, int coordY)
	{
		return tileMapArray[coordX+MATCH_RADIUS, coordY+MATCH_RADIUS];
	}
	// Get tile from tileMapArray using coordinates
	private Vector2I GetTile(Vector2I coord)
	{
		return GetTile(coord[0], coord[1]);
	}

	// Set tile on tileMapArray using coordinates
	private void SetTile(int coordX, int coordY, Vector2I value)
	{
		tileMapArray[coordX+MATCH_RADIUS, coordY+MATCH_RADIUS] = value;
	}
	// Set tile on tileMapArray using coordinates
	private void SetTile(Vector2I coord, Vector2I value)
	{
		SetTile(coord[0], coord[1], value);
	}
}
