using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

public partial class WFCGenerator3D<T>
{
	private Vector3I VECTOR_1 = new Vector3I( -1, -1, -1 );
	private T zeroValue;
	private T emptyValue;
	private int height = 50, width = 30, depth = 10; // Map size, horizontal and vertical
	public int MATCH_RADIUS = 1; // The radius around a tile check for matching tiles with sample
	public int CORRECTION_RADIUS = 2; // The radius around a failed tile that will be cleared on fixing. A number bigger than MATCH_RADIUS is recommended.
	public int CORRECTION_RADIUS_INR_EVERY = 10;
	public GenerationType generationType = GenerationType.Intelligent; // Currently supports one mode only
	public bool chooseByProbablity = false; // If set to True, number of occurance for each tile will be taken into account when choosing tiles
	public ProbabilityImportance probabilityImportance = ProbabilityImportance.NORMAL;
	public int maxN; // Total number of tiles which need to be set
	public int currentN = 0; // Number of tiles that are currently set



	// Holds tile occurrences in the sample for future use as rules
	private Dictionary<T, List<Rule3D<T>>> usedRules = new Dictionary<T, List<Rule3D<T>>>(); // DECOUPLE
	// Holds number of repeatitions of each option. Used for calculating occurance probablity
	private Dictionary<T, int> tilesRepeatitions = new Dictionary<T, int>(); // DECOUPLE

	// Holds tiles data for internal use only. DO NOT USE DIRECTLY! Use SetTile() and GetTile() instead
	private List<List<List<T>>> tilesArray;

	// Holds possible options counts for every tile
	private List<List<List<int>>> tilesCounts;
	// The sample array where rules are generated from. Needs to be set from another class before calling Ready()
	public List<List<List<T>>> sample;

	private bool failed = false; // Will be set to true if generation fails
	private const int TRY_FIX_TIMES = 15; // Will try to fix fails this many times before giving up
	private const int TRY_REGENERATE_TIMES = 10;
	private int times_regenerated = 0;

	private Task generationTask;
	private bool taskLastState = true; // true means done


	// Returns a value between 0.0 and 1.0 that shows how much has the generation progressed so far
	public float Progress { get { return ( float )( currentN ) / ( float )( maxN ); } }

	public Action OnGenerationTaskDone;

	public T floorTile;


	public WFCGenerator3D( T zeroValue, int height, int width, int depth, List<List<List<T>>> sample, int match_radius, int correctionRadius, int correctionRadiusIncrement, GenerationType generationType, bool chooseByProbablity, ProbabilityImportance probabilityImportance, T floorTile )
	{
		this.zeroValue = zeroValue;
		this.height = height;
		this.width = width;
		this.depth = depth;
		this.sample = sample;
		this.MATCH_RADIUS = match_radius;
		this.CORRECTION_RADIUS = correctionRadius;
		this.CORRECTION_RADIUS_INR_EVERY = correctionRadiusIncrement;
		this.generationType = generationType;
		this.chooseByProbablity = chooseByProbablity;
		this.probabilityImportance = probabilityImportance;
		this.floorTile = floorTile;
	}



	[ Signal ] public delegate void OnDoneEventHandler(); // emitted on end of generation


	// Called when the node enters the scene tree for the first time. Needs to be called by another class after generating this class.
	public async void Ready()
	{
		
		tilesArray = new List<List<List<T>>>( height + MATCH_RADIUS * 2 );
		for ( int i = 0; i < height + MATCH_RADIUS * 2; i++ )
		{
			tilesArray.Add( new List<List<T>>( width + MATCH_RADIUS * 2 ) );
			for ( int j = 0; j < width + MATCH_RADIUS * 2; j++ )
			{
				tilesArray[ i ].Add( new List<T>( depth + MATCH_RADIUS * 2 ) );
				for ( int k = 0; k < depth + MATCH_RADIUS * 2; k++ )
					tilesArray[ i ][ j ].Add( zeroValue );
			}
		}

		tilesCounts = new List<List<List<int>>>( height );
		for ( int i = 0; i < height + MATCH_RADIUS * 2; i++ )
		{
			tilesCounts.Add( new List<List<int>>( width ) );
			for ( int j = 0; j < width * MATCH_RADIUS * 2; j++ )
			{
				tilesCounts[ i ].Add( new List<int>( depth ) );
				for ( int k = 0; k < depth * MATCH_RADIUS * 2; k++ )
					tilesCounts[ i ][ j ].Add( -1 );
			}
		}

		maxN = height * width * depth;
		Init(); // Needs to be called to initialize usedTiles (to create rules)
		ClearMap();
		UpdateCountAll();
		GenerateMap();
	}


	// Called every frame
	public void Process( double delta )
	{
		// Do stuff on task finish
		if ( taskLastState == false )
		{
			taskLastState = generationTask.IsCompleted;
			if ( taskLastState )
			{
				if ( failed && times_regenerated < TRY_REGENERATE_TIMES )
				{
					taskLastState = false;
					times_regenerated++;
					GD.Print( times_regenerated );
					GenerateMap( true, true );
				}
				else
				{
					OnGenerationTaskDone();
				}
			}
		}
	}


	// Starts generation task
	public void GenerateMap( bool clearTarget = true, bool shouldFixFails = true )
	{
		taskLastState = false;
		generationTask = Task.Run( () => { _GenerateMap( clearTarget, shouldFixFails ); } );
	}



	// Generates Map
	private async void _GenerateMap( bool clearTarget = true, bool shouldFixFails = true )
	{
		if ( clearTarget ) 
		{
			ClearMap();
		}
		FillFloor( floorTile );
		// SetTile( new Vector3I( 0, 0, 0 ), floorTile );


		UpdateCountAll();

		while ( currentN < maxN )
		{
			Vector3I nextTile = GetNextTile(); // Find the next tile to set

			List< T > options = GetOptions( nextTile ); // What can I put in this tile?
			if ( options.Count == 0 )
			{
				failed = true;
				currentN++;
				continue;
			}

			if ( chooseByProbablity )
				SetTile( nextTile, ChooseOption( options ) ); // Set tile to a random possible option
			else
				SetTile( nextTile, options[ ( int ) ( GD.Randi() % options.Count ) ] ); // Set tile to a random possible option

			UpdateCountRadius( nextTile, MATCH_RADIUS );

			currentN++;
		}

		// failed = false;
		// return;
		if ( shouldFixFails )
			for ( int i=0; i<TRY_FIX_TIMES && failed; i++ )
			{
				failed = false;
				FixFail( CORRECTION_RADIUS + i / CORRECTION_RADIUS_INR_EVERY );
			}
	}


	// Analyses sample for rules. Must be called once before _ready
	public void Init()
	{
		usedRules.Clear();

		for ( int i = 0; i < sample.Count; i++ )
			for ( int j = 0; j < sample[ 0 ].Count; j++ )
				for ( int k = 0; k < sample[ 0 ][ 0 ].Count; k++ )
				{
					T cellValue = sample[ i ][ j ][ k ];

					if ( cellValue.Equals( zeroValue ) || cellValue.Equals( floorTile ) )
					{
						continue;
					}

					// generate rule
					if ( !usedRules.ContainsKey( cellValue ) )
					{
						usedRules.Add( cellValue, new List<Rule3D<T>>() );
					}

					Rule3D<T> rule = new Rule3D<T>( MATCH_RADIUS, new Vector3I( i, j, k ), in sample, zeroValue );
					bool repeated = false;
					foreach ( Rule3D<T> r in usedRules[ cellValue ] ) 
						if ( r.CompareWith( rule ) )
						{
							repeated = true;
							break;
						}
					if ( !repeated )
					{
						usedRules[ cellValue ].Add( rule );
					}


					// Add to tilesRepeatitions
					if ( !tilesRepeatitions.ContainsKey( cellValue ) )
					{
						tilesRepeatitions.Add( cellValue, 0 );
					}
					tilesRepeatitions[ cellValue ]++;
				}
	}


	// Called on fail to redraw failed parts
	private void FixFail( int radius )
	{
		int clearedCount = 0;
		foreach ( Vector3I tile in GetEmptyTiles() )
		{
			clearedCount += ClearRadius( tile, radius );
			clearedCount++; // Because we need to count the middle tile (which is already empty) as well
		}
		currentN = maxN - clearedCount;

		failed = false;
		_GenerateMap( false, false );
	}


	// Returns the number of possible options for the given tile coordinates
	private int GetOptionsCount( Vector3I coord )
	{
		if ( coord == VECTOR_1 )
			return 0;

		int count = 0;
		foreach ( T atlasCoord in usedRules.Keys )
		{
			bool f = true;
			bool b = false;
			int i, j, k;

			if ( generationType == GenerationType.Intelligent )
			{
				for ( i = -MATCH_RADIUS; i <= MATCH_RADIUS && !b; i++ )
					for ( j = -MATCH_RADIUS; j <= MATCH_RADIUS && !b; j++ )
						for ( k = -MATCH_RADIUS; k <= MATCH_RADIUS && !b; k++ )
						{
							bool anyMatch = false;
							foreach ( Rule3D<T> rule in usedRules[ atlasCoord ] )
							{
								if ( DoTilesMatch( GetTile( coord + new Vector3I( i, j, k ) ), rule.RuleArray[ MATCH_RADIUS+i ][ MATCH_RADIUS+j ][ MATCH_RADIUS+k ] ) )
									anyMatch = true;
							}
							if ( !anyMatch )
							{
								f = false;
								b = true;
							}
						}
			}

			if (f) count++;
		}
		return count;
	}

	// Returns all possible options for the given tile coordinates
	private List<T> GetOptions( Vector3I coord )
	{
		List<T> options = new List<T>();

		// Return an empty list if coords are ( -1, -1 )
		if ( coord == VECTOR_1 )
		{
			return options;
		}

		foreach ( T tileValue in usedRules.Keys )
		{
			bool f = true;
			bool b = false;
			int i = 0, j = 0, k = 0;

			if ( generationType == GenerationType.Intelligent )
			{
				for ( i = -MATCH_RADIUS; i <= MATCH_RADIUS && !b; i++ )
					for ( j = -MATCH_RADIUS; j <= MATCH_RADIUS && !b; j++ )
						for ( k = -MATCH_RADIUS; k <= MATCH_RADIUS && !b; k++ )
						{
							bool anyMatch = false;
							foreach ( Rule3D<T> rule in usedRules[ tileValue ] )
							{
								anyMatch = anyMatch || DoTilesMatch( GetTile( coord + new Vector3I( i, j, k ) ), rule.RuleArray[ MATCH_RADIUS+i ][ MATCH_RADIUS+j ][ MATCH_RADIUS+k ] ) ;
							}
							if ( !anyMatch )
							{
								f = false;
								b = true;
							}
						}
			}

			if ( f ) options.Add( tileValue );
		}

		return options;
	}


	// chooses a tile from the given options based on its occurance probablity
	private T ChooseOption( in List<T> options )
	{
		if (options.Count==0)
			return zeroValue;
		
		float sum = 0;
		foreach (T option in options)
		{
			sum += ( float ) Mathf.Pow( tilesRepeatitions[option], ( float ) probabilityImportance * 0.5 );
		}

		float temp = 0;
		float rand = (float) GD.Randi() % sum;
		foreach (T option in options)
		{
			temp += ( float ) Mathf.Pow( tilesRepeatitions[option], ( float ) probabilityImportance * 0.5 );
			if (temp>=rand)
				return option;
		}

		return zeroValue;
	}


	// returns true if the two given tiles match (or if one of them is not set)
	private bool DoTilesMatch(T tile1, T tile2, bool tile2CanBeZero = false )
	{
		return tile1.Equals( zeroValue ) || tile2.Equals( tile1 ) || ( tile2CanBeZero && tile2.Equals( zeroValue ) );
	}


	// Returns the tile with the least possible options. 
	private Vector3I GetNextTile()
	{
		Vector3I bestTile = VECTOR_1;
		int leastOptions = int.MaxValue;
		for ( int i = 0; i < height; i++ )
			for ( int j = 0; j < width; j++ )
				for ( int k = 0; k < depth; k++ )
				{
					if ( tilesCounts[ i ][ j ][ k ] < leastOptions && tilesCounts[ i ][ j ][ k ] > 0 )
					{
						leastOptions = tilesCounts[ i ][ j ][ k ];
						bestTile.X = i;
						bestTile.Y = j;
						bestTile.Z = k;
					}
				}

		return bestTile;
	}


	// Updates all options counts
	private void UpdateCountAll()
	{
		for ( int i = 0; i < height; i++ )
			for ( int j = 0; j < width; j++) 
				for ( int k = 0; k < depth; k++) 
				{
					Vector3I coord = new Vector3I( i, j, k );
					if ( !GetTile( coord ).Equals( zeroValue ) )
						tilesCounts[ i ][ j ][ k ] = 0;
					else
						tilesCounts[ i ][ j ][ k ] = GetOptionsCount( coord );
				}
	}


	private void UpdateCountRadius( Vector3I coord, int radius )
	{
		List< Task< int > > tasks = new List< Task< int > >();
		List< int[] > counts = new List< int[] >();
		for ( int i = coord.X - radius; i <= coord.X + radius; i++ )
			for ( int j = coord.Y - radius; j <= coord.Y + radius; j++ )
				for ( int k = coord.Z - radius; k <= coord.Z + radius; k++ )
			{
				if (
					i < 0
					|| j < 0
					|| k < 0
					|| i >= height
					|| j >= width
					|| k >= depth
				)
				{
					continue;
				}

				Vector3I tempCoord = new Vector3I( i, j, k );
				if ( !GetTile( tempCoord ).Equals( zeroValue ) )
					tilesCounts[ i ][ j ][ k ] = 0;
				else
				{
					tasks.Add( Task<int>.Factory.StartNew( () => { return GetOptionsCount( tempCoord ); } ) );
					counts.Add( new int[ 3 ] );
					counts[ counts.Count - 1 ][ 0 ] = i;
					counts[ counts.Count - 1 ][ 1 ] = j;
					counts[ counts.Count - 1 ][ 2 ] = k;
				}
			}

		Task.WaitAll( tasks.ToArray() );

		for ( int i = 0; i < tasks.Count; i++ )
		{
			tilesCounts[ counts[ i ][ 0 ] ][ counts[ i ][ 1 ] ][ counts[ i ][ 2 ] ] = tasks[ i ].Result;
		}
	}


	// Set every tilesArray cell to (-1, -1)
	private void ClearMap()
	{
		for ( int i = 0; i < height + MATCH_RADIUS * 2; i++ )
			for ( int j = 0; j < width + MATCH_RADIUS * 2; j++ )
				for ( int k = 0; k < depth + MATCH_RADIUS * 2; k++ )
			{
				tilesArray[ i ][ j ][ k ] = zeroValue;
			}
	}


	// Set every tilesArray cell in the specified radius and position to (-1, -1). Returns number of tiles that where not (-1, -1) before clearing.
	private int ClearRadius( Vector3I coord, int radius )
	{
		int clearedCount = 0;

		for ( int i = coord.X - radius; i <= coord.X + radius; i++ )
			for ( int j = coord.Y - radius; j <= coord.Y + radius; j++ )
				for ( int k = coord.Z - radius; k <= coord.Z + radius; k++ )
				{
					if (
						i < 0
						|| j < 0
						|| k < 0
						|| i >= height
						|| j >= width
						|| k >= depth
					)
					{
						continue;
					}
					Vector3I tempCoord = new Vector3I( i, j, k );
					if ( !GetTile( tempCoord ).Equals( zeroValue ) )
						clearedCount++;
					SetTile( tempCoord, zeroValue );
				}

		return clearedCount;
	}


	// Set every tilesArray cell in the specified radius and position to (-1, -1). Returns number of tiles that where not (-1, -1) before clearing.
	private int ClearRadius( int i, int j, int k, int radius )
	{
		return ClearRadius( new Vector3I( i, j, k ), radius );
	}


	// Returns a list of all tiles that are empty. (-1, -1)
	private List< Vector3I > GetEmptyTiles()
	{
		List< Vector3I > tiles = new List< Vector3I >();
		for ( int i = 0; i < height; i++ )
			for ( int j = 0; j < width; j++ )
				for ( int k = 0; k < depth; k++ )
				if ( GetTile( i, j, k ).Equals( zeroValue ) )
					tiles.Add( new Vector3I( i, j, k ) );
		
		return tiles;
	}


	// Fills the specified cube with the given tile 
	private void FillArea( Vector3I from, Vector3I to, T item )
	{
		for ( int i = from.X; i <= to.X; i++ )
			for ( int j = from.Y; j <= to.Y; j++ )
				for ( int k = from.Z; k <= to.Z; k++ )
				{
					SetTile( i, j, k, item );
				}
	}


	// Fills depth of 0 with the given item
	private void FillFloor( T itemToUse )
	{
		FillArea( Vector3I.Zero, new Vector3I( height, 0, depth ), itemToUse );
	}


	// Get tile from tilesArray using coordinates
	public T GetTile( int coordX, int coordY, int coordZ )
	{
		return tilesArray[ coordX + MATCH_RADIUS ][ coordY + MATCH_RADIUS ][ coordZ + MATCH_RADIUS ];
	}
	// Get tile from tilesArray using coordinates

	public T GetTile( Vector3I coord )
	{
		return GetTile( coord.X, coord.Y, coord.Z );
	}


	// Set tile on tilesArray using coordinates
	private void SetTile( int coordX, int coordY, int coordZ, T value )
	{
		tilesArray[ coordX + MATCH_RADIUS ][ coordY + MATCH_RADIUS ][ coordZ + MATCH_RADIUS ] = value;
	}

	// Set tile on tilesArray using coordinates
	private void SetTile( Vector3I coord, T value )
	{
		SetTile( coord.X, coord.Y, coord.Z, value );
	}



	// Starts generating a new map. Does not reinitialize rules
	public void StartGeneration()
	{
		taskLastState = true;
		currentN = 0;
		failed = false;
		times_regenerated = 0;

		// Create new arrays
		tilesArray = new List<List<List<T>>>( height + MATCH_RADIUS * 2 );
		for ( int i = 0; i < height + MATCH_RADIUS * 2; i++ )
		{
			tilesArray.Add( new List<List<T>>( width + MATCH_RADIUS * 2 ) );
			for ( int j = 0; j < width + MATCH_RADIUS * 2; j++ )
			{
				tilesArray[ i ].Add( new List<T>(depth + MATCH_RADIUS * 2 ) );
				for ( int k = 0; k < depth + MATCH_RADIUS * 2; k++ )
					tilesArray[ i ][ j ].Add( zeroValue );
			}
		}

		tilesCounts = new List<List<List<int>>>( height );
		for (int i = 0; i < height + MATCH_RADIUS * 2; i++)
		{
			tilesCounts.Add( new List<List<int>>( width ) );
			for ( int j = 0; j < width; j++ )
			{
				tilesCounts[ i ].Add( new List<int>( depth ) );
				for ( int k = 0; k < depth; k++ )
					tilesCounts[ i ][ j ].Add( -1 );
			}
		}

		
		// Generate
		maxN = height * width * depth;
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
