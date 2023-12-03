using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public partial class WFCGenerator : Node2D
{
	private Vector2I Vector_1 = new Vector2I(-1,-1);

	[Export] public TileMap target;
	[Export] public TileMap sample;
	[Export] private int H=50, V=30; // Map size, horizontal and vertical
	[Export] public int MATCH_RADIUS = 1; // The radius around a tile check for matching tiles with sample
	private int maxN; // Total number of tiles which need to be set
	private int currentN=0; // Number of tiles that are currently set

	[Export] public bool showProgress = true; // If you know the code works for you disable this as it may impact performance
	
	
	// Holds tile occurrences in the sample for future use as rules
	private Dictionary<Vector2I, List<Vector2I>> usedTiles = new Dictionary<Vector2I, List<Vector2I>>();
	
	// Holds tiles data for internal use only. DO NOT USED DIRECTLY! Use SetTile() and GetTile() instead
	private List<List<Vector2I>> tileMapArray; 

	// Holds possible options counts
	private List<List<int>> tileMapCount;
	private bool done = false;

	private Task generationTask;
	private bool taskLastState = true; // true means done


	[Signal] public delegate void OnDoneEventHandler(); // emitted on end of generation
	

	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		tileMapArray = new List<List<Vector2I>>(H+MATCH_RADIUS*2);
		for (int i=0; i<H+MATCH_RADIUS*2; i++)
		{
			tileMapArray.Add(new List<Vector2I>(V+MATCH_RADIUS*2));
			for (int j=0; j<V+MATCH_RADIUS*2; j++)
				tileMapArray[i].Add(new Vector2I(-1,-1));
		}

		tileMapCount = new List<List<int>>(H);
		for (int i=0; i<H+MATCH_RADIUS*2; i++)
		{
			tileMapCount.Add(new List<int>(V));
			for (int j=0; j<V; j++)
				tileMapCount[i].Add(-1);
		}

		sample.Hide();
		maxN = H*V;
		Init(); // Needs to be called to initialize usedTiles (to create rules)
		ClearMap();
		UpdateCountAll();
		GenerateMap();
	}


	// Called every frame
	public override void _Process(double delta)
	{
		if (showProgress)
			ApplyTileMap();


		// Do stuff on task finish
		if (taskLastState==false)
		{
			taskLastState = generationTask.IsCompleted;
			if (taskLastState)
			{
				ApplyTileMap();
				EmitSignal(SignalName.OnDone);
			}
		}
	}


	// Starts generation task
	public void GenerateMap(bool clearTarget=true)
	{
		taskLastState = false;
		generationTask = Task.Run(() => {_GenerateMap(clearTarget);});
	}


	// Generates Map
	private async Task _GenerateMap(bool clearTarget=true)
	{
		if (clearTarget) ClearMap();
		UpdateCountAll();

		while(true)
		{
			if (currentN>=maxN)
			{
				break;
			}
			
			Vector2I nextTile = GetNextTile(); // Find the next tile to set
			List<Vector2I> options = GetOptions(nextTile); // What can I put in this tile?
			SetTile(nextTile, options[(int)(GD.Randi()%options.Count)]); // Set tile to a random possible option
			UpdateCountRadius(nextTile, MATCH_RADIUS);
			
			currentN++;
		}
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
		// DeleteRepeatedRules();
		GD.Print(usedTiles[new Vector2I(0,0)].Count);
	}

	// Delete repeated rules
	private void DeleteRepeatedRules()
	{
		foreach (Vector2I occIndex in usedTiles.Keys)
		{
			foreach (Vector2I occurrence in usedTiles[occIndex])
			{
				int count = 0;
				int lastIndex = -1;
				while(true)
				{
					int index = usedTiles[occIndex].FindIndex(lastIndex, (Vector2I val) => val==occurrence);
					if (index==-1) break;

					lastIndex = index;
					count++;
				}

				for (int i=1; i<count; i++)
					usedTiles[occIndex].Remove(occurrence);
			}
		}
			// for (int k1=0; k1<occurrences.Count; k1++)
			// 	for (int k2=0; k2<occurrences.Count; k2++)
			// 	{
			// 		if (k1==k2) continue;
			// 		bool doMatch = true;
			// 		for (int i=-MATCH_RADIUS; i<=MATCH_RADIUS&&doMatch; i++)
			// 			for (int j=-MATCH_RADIUS; j<=MATCH_RADIUS&&doMatch; j++)
			// 			{
			// 				Vector2I tempVector = new Vector2I(i,j);
			// 				if (
			// 					!sample.GetCellAtlasCoords(0, occurrences[k1]+tempVector)
			// 					== sample.GetCellAtlasCoords(0, occurrences[k2]+tempVector)
			// 					)
			// 					doMatch = false;
			// 			}
					
			// 		if (!doMatch)
			// 		{
			// 			occurrences.Delete(k2);
			// 			if (k1<k2) k1--;
			// 			k2--;
			// 		}
			//	 }
			// {
			// 	Vector2I tempCoord = new Vector2I(i,j);
			// 	if (GetTile(tempCoord)!=Vector_1) 
			// 		tileMapCount[i][j]=0;
			// 	else
			// 	{
			// 		tasks.Add(Task<int>.Factory.StartNew(() => {return GetOptionsCount(tempCoord);}));
			// 		counts.Add(new int[2]);
			// 		counts[counts.Count-1][0] = i;
			// 		counts[counts.Count-1][1] = j;
			// 	}
			// }
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
					foreach (Vector2I occurrence in usedTiles[usedTile])
					{
						if (DoTilesMatch(GetTile(coord+new Vector2I(i,j)), sample.GetCellAtlasCoords(0, occurrence+new Vector2I(i,j))))
							anyMatch = true;
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
					foreach (Vector2I occurrence in usedTiles[usedTile])
					{
						if (DoTilesMatch(GetTile(coord+new Vector2I(i,j)), sample.GetCellAtlasCoords(0, occurrence+new Vector2I(i,j))))
							anyMatch = true;
					}
					if (!anyMatch)
					{
						f = false;
						b = true;
					}
				}
			if (f) options.Add(usedTile);
		}
		return options;
	}

	// returns true if the two given tiles match (or if one of them is not set)
	private bool DoTilesMatch(Vector2I tile1, Vector2I tile2)
	{
		bool match = false;

		if (tile1==Vector_1)
			match = true;
		if (tile2==tile1)
			match = true;

		return match;
	}
	
	// Returns the tile with the least possible options. 
	private Vector2I GetNextTile()
	{
		Vector2I bestTile = new Vector2I(0,0);
		int leastOptions = int.MaxValue;
		for (int i=0; i<H; i++)
			for (int j=0; j<V; j++)
			{
				if (tileMapCount[i][j]<leastOptions && tileMapCount[i][j]>0)
				{
					leastOptions = tileMapCount[i][j];
					bestTile[0] = i;
					bestTile[1] = j;
				}
			}
		return bestTile;
	}


	// Updates all options counts
	private void UpdateCountAll()
	{
		for (int i=0; i<H; i++)
			for (int j=0; j<V; j++)
			{
				Vector2I coord = new Vector2I(i,j);
				if (GetTile(coord)!=Vector_1) 
					tileMapCount[i][j]=0;
				else
					tileMapCount[i][j] = GetOptionsCount(coord);
			}
	}

	private void UpdateCountRadius(Vector2I coord, int radius)
	{
		List<Task<int>> tasks = new List<Task<int>>();
		List<int[]> counts = new List<int[]>();
		for (int i=coord[0]-radius; i<=coord[0]+radius; i++)
			for (int j=coord[1]-radius; j<=coord[1]+radius; j++)
			{
				if (
					i<0
					|| j<0
					|| i>=H
					|| j>=V
				)
				{
					continue;
				}
				Vector2I tempCoord = new Vector2I(i,j);
				if (GetTile(tempCoord)!=Vector_1) 
					tileMapCount[i][j]=0;
				else
				{
					tasks.Add(Task<int>.Factory.StartNew(() => {return GetOptionsCount(tempCoord);}));
					counts.Add(new int[2]);
					counts[counts.Count-1][0] = i;
					counts[counts.Count-1][1] = j;
				}
			}
		Task.WaitAll(tasks.ToArray());
		for (int i=0; i<tasks.Count; i++)
		{
			tileMapCount[counts[i][0]][counts[i][1]] = tasks[i].Result;
		}
	}

	// Set every tileMapArray cell to (-1, -1)
	private void ClearMap()
	{
		for (int i=0; i<H+MATCH_RADIUS*2; i++)
			for (int j=0; j<V+MATCH_RADIUS*2; j++)
			{
				tileMapArray[i][j] = Vector_1;
			}
	}

	// Get tile from tileMapArray using coordinates
	private Vector2I GetTile(int coordX, int coordY)
	{
		return tileMapArray[coordX+MATCH_RADIUS][coordY+MATCH_RADIUS];
	}
	// Get tile from tileMapArray using coordinates
	private Vector2I GetTile(Vector2I coord)
	{
		return GetTile(coord[0], coord[1]);
	}

	// Set tile on tileMapArray using coordinates
	private void SetTile(int coordX, int coordY, Vector2I value)
	{
		tileMapArray[coordX+MATCH_RADIUS][coordY+MATCH_RADIUS] = value;
	}
	// Set tile on tileMapArray using coordinates
	private void SetTile(Vector2I coord, Vector2I value)
	{
		SetTile(coord[0], coord[1], value);
	}
}
