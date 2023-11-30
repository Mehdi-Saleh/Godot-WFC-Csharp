using Godot;
using System;
using System.Collections.Generic;

public partial class WFCGenerator : Node2D
{
	private bool done = false;
	private Vector2I Vector_1 = new Vector2I(-1,-1);
	[Export] private const int H=15, V=15;
	private int maxN, currentN=0;
	[Export] public TileMap target;
	[Export] public TileMap sample;
	[Export] public int matchRadius = 1;
	
	public int tempI = 0, tempJ = 0;
	
	private Dictionary<Vector2I, List<Vector2I>> usedTiles = new Dictionary<Vector2I, List<Vector2I>>();
	
	private PriorityQueue<Vector2I, int> queue = new PriorityQueue<Vector2I, int>();
	
	
	// Holds tiles data for internal use only
	private Vector2I[,] tilesArray = new Vector2I[H,V]; 
	
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		sample.Hide();
		maxN = H*V;
		Init();
		
		// set all tiles to unset (-1)
//		for (int i=0; i<H; i++)
//			for (int j=0; j<V; j++)
//			{
//				tilesArray[i,j] = new Vector2I(-1,-1);
//			}
//
//		//tilesArray[1,3] = new Vector2I(1,1);
//		ApplyTileMap();
	}
	
	public override void _Process(double delta)
	{
		if (done) return;
		
		 Vector2I nextTile = GetNextTile();
		if (currentN>=maxN)
		{
			done = true;
			return;
		}
		
		List<Vector2I> options = GetOptions(nextTile);
		target.SetCell(0, nextTile, 1, options[(int)(GD.Randi()%options.Count)]);
		currentN++;
	}
	
	// Applies the tiles array to the target tile map
//	public void ApplyTileMap()
//	{
//		for (int i=0; i<H; i++)
//			for (int j=0; j<V; j++)
//			{
//				target.SetCell(0, new Vector2I(i,j), 1, tilesArray[i,j]);
//			}
//	}
	
	// Call to analyse sample for rules from sample
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
	
	// Returns the number of possible tiles for the given coords
	private int GetOptionsCount(Vector2I coord)
	{
		int count = 0;
		foreach (Vector2I usedTile in usedTiles.Keys)
		{
			bool f = true, b=false;
			int i, j;
			for (i=-matchRadius; i<=matchRadius&&!b; i++)
				for (j=-matchRadius; j<=matchRadius&&!b; j++)
				{
					bool anyMatch = false;
					foreach (Vector2I occurance in usedTiles[usedTile])
					{
						if (target.GetCellAtlasCoords(0, coord+new Vector2I(i,j))==Vector_1)
						{
							anyMatch = true;
						}
						if (sample.GetCellAtlasCoords(0, occurance+new Vector2I(i,j))==target.GetCellAtlasCoords(0, coord+new Vector2I(i,j)))
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
	
	private List<Vector2I> GetOptions(Vector2I coord)
	{
		List<Vector2I> options = new List<Vector2I>();
		foreach (Vector2I usedTile in usedTiles.Keys)
		{
			bool f = true, b=false;
			int i=0, j=0;
			for (i=-matchRadius; i<=matchRadius&&!b; i++)
				for (j=-matchRadius; j<=matchRadius&&!b; j++)
				{
					bool anyMatch = false;
					foreach (Vector2I occurance in usedTiles[usedTile])
					{
						if (target.GetCellAtlasCoords(0,coord+new Vector2I(i,j))==Vector_1)
						{
							anyMatch = true;
							break;
						}
						if (sample.GetCellAtlasCoords(0, occurance+new Vector2I(i,j))==target.GetCellAtlasCoords(0,coord+new Vector2I(i,j)))
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
	
	// Finds the tile with the least possible options. 
	private Vector2I GetNextTile()
	{
		Vector2I bestTile = new Vector2I(0,0);
		int leastOptions = usedTiles.Count;
		for (int i=0; i<H; i++)
			for (int j=0; j<V; j++)
			{
				Vector2I coord = new Vector2I(i,j);
				if (target.GetCellAtlasCoords(0, coord)!=Vector_1) continue;
				int count = GetOptionsCount(coord);
				if (count<leastOptions && count>0)
				{
					leastOptions = count;
					bestTile[0] = i;
					bestTile[1] = j;
				}
			}
		GD.Print(bestTile);
		return bestTile;
	}
	
	// Returns true if the specified Vector2 is in the given list NEED OPTIMIVATION!!
	private bool IsInList(Vector2I atlasCoord, Vector2I[] targetList)
	{
		foreach (Vector2I v in targetList)
		{
			if (v == atlasCoord) return true;
		}
		return false;
	}
}
