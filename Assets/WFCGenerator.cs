using Godot;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

public partial class WFCGenerator : Node2D
{
	private Vector2I Vector_1 = new Vector2I(-1, -1);

	[Export] public TileMap target;
	[Export] public TileMap sample;
	[Export] private int H = 50, V = 30; // Map size, horizontal and vertical
	[Export] public int MATCH_RADIUS = 1; // The radius around a tile check for matching tiles with sample
	[Export] public int CORRECTION_RADIUS = 2; // The radius around a failed tile that will be cleared on fixing. A number bigger than MATCH_RADIUS is recommended.
	[Export] public GenerationType generationType = GenerationType.Intelligent;
	[Export] public bool chooseByProbablity = false;
	public int maxN; // Total number of tiles which need to be set
	public int currentN = 0; // Number of tiles that are currently set

	[Export] public bool showProgress = true; // If you know the code works for you disable this as it may impact performance


	// Holds tile occurrences in the sample for future use as rules
	private Dictionary<Vector2I, List<Rule>> usedRules = new Dictionary<Vector2I, List<Rule>>();
	// Holds number of repeatitions of each option. Used for calculating occurance probablity
	private Dictionary<Vector2I, int> tilesRepeatitions = new Dictionary<Vector2I, int>();

	// Holds tiles data for internal use only. DO NOT USED DIRECTLY! Use SetTile() and GetTile() instead
	private List<List<Vector2I>> tileMapArray;

	// Holds possible options counts
	private List<List<int>> tileMapCount;
	private bool done = false;
	private bool failed = false; // Will be set to true if generation fails
	private const int TRY_FIX_TIMES = 10; // Will try to fix fails this many times before giving up

	private Task generationTask;
	private bool taskLastState = true; // true means done



	[Signal] public delegate void OnDoneEventHandler(); // emitted on end of generation


	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		tileMapArray = new List<List<Vector2I>>(H + MATCH_RADIUS * 2);
		for (int i = 0; i < H + MATCH_RADIUS * 2; i++)
		{
			tileMapArray.Add(new List<Vector2I>(V + MATCH_RADIUS * 2));
			for (int j = 0; j < V + MATCH_RADIUS * 2; j++)
				tileMapArray[i].Add(new Vector2I(-1, -1));
		}

		tileMapCount = new List<List<int>>(H);
		for (int i = 0; i < H + MATCH_RADIUS * 2; i++)
		{
			tileMapCount.Add(new List<int>(V));
			for (int j = 0; j < V; j++)
				tileMapCount[i].Add(-1);
		}

		sample.Hide();
		maxN = H * V;
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
		if (taskLastState == false)
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
	public void GenerateMap(bool clearTarget = true)
	{
		taskLastState = false;
        generationTask = Task.Run(() => { _GenerateMap(clearTarget); });
    }



    // Generates Map
    private void _GenerateMap(bool clearTarget = true)
    {
		if (clearTarget) 
			ClearMap();
		UpdateCountAll();

		while (true)
		{
			if (currentN >= maxN)
			{
				break;
			}

			Vector2I nextTile = GetNextTile(); // Find the next tile to set

			if (GetTile(nextTile)!=Vector_1)
				failed = true;

			List<Vector2I> options = GetOptions(nextTile); // What can I put in this tile?
			if (chooseByProbablity)
				SetTile(nextTile, ChooseOption(options)); // Set tile to a random possible option
			else
				SetTile(nextTile, options[(int)(GD.Randi() % options.Count)]); // Set tile to a random possible option
			UpdateCountRadius(nextTile, MATCH_RADIUS);

			currentN++;
		}

		for (int i=0; i<TRY_FIX_TIMES && failed; i++)
		{
			FixFail();
		}
	}

	// Applies the tiles array to the target tile map
	public void ApplyTileMap()
	{
		for (int i = 0; i < H; i++)
			for (int j = 0; j < V; j++)
			{
				target.SetCell(0, new Vector2I(i, j), 1, GetTile(i, j));
			}
	}

	// Analyses sample for rules. Must be called once before _ready
	public void Init()
	{
		usedRules.Clear();
		Godot.Collections.Array<Vector2I> usedCells = sample.GetUsedCells(0);
		foreach (Vector2I cell in usedCells)
		{
			// generate rule
			Vector2I atlasCoord = sample.GetCellAtlasCoords(0, cell);
			if (!usedRules.ContainsKey(atlasCoord))
			{
				usedRules.Add(atlasCoord, new List<Rule>());
			}

			Rule rule = new Rule(MATCH_RADIUS, cell, in sample);

			bool repeated = false;
			foreach (Rule r in usedRules[atlasCoord])
				if (r.CompareWith(rule))
				{
					repeated = true;
					break;
				}
			if (!repeated)
				usedRules[atlasCoord].Add(rule);

			// add to tilesRepeatitions
			if (!tilesRepeatitions.ContainsKey(atlasCoord))
			{
				tilesRepeatitions.Add(atlasCoord, 0);
			}
			tilesRepeatitions[atlasCoord]++;
		}
	}

	// Called on fail to redraw failed parts
	private void FixFail()
	{
		int clearedCount = 0;
		foreach (Vector2I tile in GetEmptyTiles())
		{
			clearedCount += ClearRadius(tile, MATCH_RADIUS*2);
			clearedCount++; // Because we need to count the middle tile (which is already empty) as well
		}
		currentN = maxN - clearedCount;

		failed = false;
		GenerateMap(false);
	}

	// Returns the number of possible options for the given tile coordinates
	private int GetOptionsCount(Vector2I coord)
	{
		int count = 0;
		foreach (Vector2I atlasCoord in usedRules.Keys)
		{
			bool f = true;
			bool b = false;
			int i, j;
			// if (generationType==GenerationType.Intelligent)
			{
				for (i = -MATCH_RADIUS; i <= MATCH_RADIUS && !b; i++)
					for (j = -MATCH_RADIUS; j <= MATCH_RADIUS && !b; j++)
					{
						bool anyMatch = false;
						foreach (Rule rule in usedRules[atlasCoord])
						{
							if (DoTilesMatch(GetTile(coord + new Vector2I(i, j)), rule.RuleArray[MATCH_RADIUS+i][MATCH_RADIUS+j]))
								anyMatch = true;
						}
						if (!anyMatch)
						{
							f = false;
							b = true;
						}
					}
			}
			// else if (generationType==GenerationType.Exact)
			// {
			// 	Rule rCoord = new Rule(MATCH_RADIUS, coord, sample);
			// 	bool anyMatch = false;
			// 	foreach (Rule rule in usedRules[atlasCoord])
			// 	{
			// 		if (rule.CompareWith(rCoord, true))
			// 			anyMatch = true;
			// 	}
			// 	if (!anyMatch)
			// 	{
			// 		f = false;
			// 		b = true;
			// 	}
			// }
			if (f) count++;
		}
		return count;
	}

	// Returns all possible options for the given tile coordinates
	private List<Vector2I> GetOptions(Vector2I coord)
	{
		List<Vector2I> options = new List<Vector2I>();
		foreach (Vector2I atlasCoord in usedRules.Keys)
		{
			bool f = true;
			bool b = false;
			int i = 0, j = 0;
			// if (generationType==GenerationType.Intelligent)
			{
				for (i = -MATCH_RADIUS; i <= MATCH_RADIUS && !b; i++)
					for (j = -MATCH_RADIUS; j <= MATCH_RADIUS && !b; j++)
					{
						bool anyMatch = false;
						foreach (Rule rule in usedRules[atlasCoord])
						{
							if (DoTilesMatch(GetTile(coord + new Vector2I(i, j)), rule.RuleArray[MATCH_RADIUS+i][MATCH_RADIUS+j]))
								anyMatch = true;
						}
						if (!anyMatch)
						{
							f = false;
							b = true;
						}
					}
			}
			// else if (generationType==GenerationType.Exact)
			// {
			// 	Rule rCoord = new Rule(MATCH_RADIUS, coord, sample);
			// 	bool anyMatch = false;
			// 	foreach (Rule rule in usedRules[atlasCoord])
			// 	{
			// 		if (rule.CompareWith(rCoord, true))
			// 			anyMatch = true;
			// 	}
			// 	if (!anyMatch)
			// 	{
			// 		f = false;
			// 		b = true;
			// 	}
			// }
			if (f) options.Add(atlasCoord);
		}
		return options;
	}

	// chooses a tile from the given options based on its occurance probablity
	private Vector2I ChooseOption(in List<Vector2I> options)
	{
		if (options.Count==0)
			return Vector_1;
		
		int sum = 0;
		foreach (Vector2I option in options)
		{
			sum += tilesRepeatitions[option];
		}

		int temp = 0;
		int rand = (int) GD.Randi() % sum;
		foreach (Vector2I option in options)
		{
			temp += tilesRepeatitions[option];
			if (temp>=rand)
				return option;
		}

		return Vector_1;
	}

	// returns true if the two given tiles match (or if one of them is not set)
	private bool DoTilesMatch(Vector2I tile1, Vector2I tile2)
	{
		bool match = false;

		if (tile1 == Vector_1)
			match = true;
		if (tile2 == tile1)
			match = true;

		return match;
	}

	// Returns the tile with the least possible options. 
	private Vector2I GetNextTile()
	{
		Vector2I bestTile = new Vector2I(0, 0);
		int leastOptions = int.MaxValue;
		for (int i = 0; i < H; i++)
			for (int j = 0; j < V; j++)
			{
				if (tileMapCount[i][j] < leastOptions && tileMapCount[i][j] > 0)
				{
					leastOptions = tileMapCount[i][j];
					bestTile.X = i;
					bestTile.Y = j;
				}
			}
			
		return bestTile;
	}


	// Updates all options counts
	private void UpdateCountAll()
	{
		for (int i = 0; i < H; i++)
			for (int j = 0; j < V; j++)
			{
				Vector2I coord = new Vector2I(i, j);
				if (GetTile(coord) != Vector_1)
					tileMapCount[i][j] = 0;
				else
					tileMapCount[i][j] = GetOptionsCount(coord);
			}
	}

	private void UpdateCountRadius(Vector2I coord, int radius)
	{
		List<Task<int>> tasks = new List<Task<int>>();
		List<int[]> counts = new List<int[]>();
		for (int i = coord[0] - radius; i <= coord[0] + radius; i++)
			for (int j = coord[1] - radius; j <= coord[1] + radius; j++)
			{
				if (
					i < 0
					|| j < 0
					|| i >= H
					|| j >= V
				)
				{
					continue;
				}
				Vector2I tempCoord = new Vector2I(i, j);
				if (GetTile(tempCoord) != Vector_1)
					tileMapCount[i][j] = 0;
				else
				{
					tasks.Add(Task<int>.Factory.StartNew(() => { return GetOptionsCount(tempCoord); }));
					counts.Add(new int[2]);
					counts[counts.Count - 1][0] = i;
					counts[counts.Count - 1][1] = j;
				}
			}
		Task.WaitAll(tasks.ToArray());
		for (int i = 0; i < tasks.Count; i++)
		{
			tileMapCount[counts[i][0]][counts[i][1]] = tasks[i].Result;
		}
	}

	// Set every tileMapArray cell to (-1, -1)
	private void ClearMap()
	{
		for (int i = 0; i < H + MATCH_RADIUS * 2; i++)
			for (int j = 0; j < V + MATCH_RADIUS * 2; j++)
			{
				tileMapArray[i][j] = Vector_1;
			}
	}

	// Set every tileMapArray cell in the specified radius and position to (-1, -1). Returns number of tiles that where not (-1, -1) before clearing.
	private int ClearRadius(Vector2I coord, int radius)
	{
		int clearedCount = 0;

		for (int i = coord[0] - radius; i <= coord[0] + radius; i++)
			for (int j = coord[1] - radius; j <= coord[1] + radius; j++)
			{
				if (
					i < 0
					|| j < 0
					|| i >= H
					|| j >= V
				)
				{
					continue;
				}
				Vector2I tempCoord = new Vector2I(i, j);
				if (GetTile(tempCoord)!=Vector_1)
					clearedCount++;
				SetTile(tempCoord, Vector_1);
			}
		return clearedCount;
	}
	// Set every tileMapArray cell in the specified radius and position to (-1, -1). Returns number of tiles that where not (-1, -1) before clearing.
	private int ClearRadius(int i, int j, int radius)
	{
		return ClearRadius(new Vector2I(i, j), radius);
	}

	// Returns a list of all tiles that are empty. (-1, -1)
	private List<Vector2I> GetEmptyTiles()
	{
		List<Vector2I> tiles = new List<Vector2I>();
		for (int i = 0; i < H; i++)
			for (int j = 0; j < V; j++)
				if (GetTile(i, j) == Vector_1)
					tiles.Add(new Vector2I(i, j));
		
		return tiles;
	}

	// Get tile from tileMapArray using coordinates
	private Vector2I GetTile(int coordX, int coordY)
	{
		return tileMapArray[coordX + MATCH_RADIUS][coordY + MATCH_RADIUS];
	}
	// Get tile from tileMapArray using coordinates
	private Vector2I GetTile(Vector2I coord)
	{
		return GetTile(coord[0], coord[1]);
	}

	// Set tile on tileMapArray using coordinates
	private void SetTile(int coordX, int coordY, Vector2I value)
	{
		tileMapArray[coordX + MATCH_RADIUS][coordY + MATCH_RADIUS] = value;
	}
	// Set tile on tileMapArray using coordinates
	private void SetTile(Vector2I coord, Vector2I value)
	{
		SetTile(coord[0], coord[1], value);
	}



	private void OnButtonPressed()
	{
		done = false;
		taskLastState = true;
		currentN = 0;
		failed = false;

		tileMapArray = new List<List<Vector2I>>(H + MATCH_RADIUS * 2);
		for (int i = 0; i < H + MATCH_RADIUS * 2; i++)
		{
			tileMapArray.Add(new List<Vector2I>(V + MATCH_RADIUS * 2));
			for (int j = 0; j < V + MATCH_RADIUS * 2; j++)
				tileMapArray[i].Add(new Vector2I(-1, -1));
		}

		tileMapCount = new List<List<int>>(H);
		for (int i = 0; i < H + MATCH_RADIUS * 2; i++)
		{
			tileMapCount.Add(new List<int>(V));
			for (int j = 0; j < V; j++)
				tileMapCount[i].Add(-1);
		}

		sample.Hide();
		maxN = H * V;
		ClearMap();
		UpdateCountAll();
		GenerateMap();
	}

	public enum GenerationType
	{
		// Exact,
		Intelligent
	}
}
