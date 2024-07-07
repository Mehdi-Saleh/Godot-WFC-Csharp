using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

public partial class WFC3DGridMap : Node
{
	WFCGenerator3D<ItemAndOrientation> generator;
	[ Export ] public GridMap target;
	[ Export ] public GridMap sample;
	[ Export ] public ProgressBar progressBar;
	private List<List<List<ItemAndOrientation>>> sampleArray = new List<List<List<ItemAndOrientation>>>();
	[ Export ] public Vector3I dimensions;
	[ Export ] public int matchRadius = 1;
	[ Export ] public int correctionRadius = 3;
	[ Export ] public int correctionRadiusIncrementEvery = 10;
	[ Export ] public WFCGenerator3D<ItemAndOrientation>.GenerationType generationType = WFCGenerator3D<ItemAndOrientation>.GenerationType.Intelligent;
	[ Export ] public bool chooseByProbability = false;
	[ Export ] public ProbabilityImportance probabilityImportance = ProbabilityImportance.NORMAL;
	[ Export ] public bool showCurrentProgress = true;
	[ Export ] public bool updateProgressBar = true;


	public override void _Ready()
	{
		var sampleArray = ExtractSample();

		generator = new WFCGenerator3D<ItemAndOrientation>(
			new ItemAndOrientation( -1, -1 ), dimensions.X, dimensions.Y, dimensions.Z, sampleArray,
			matchRadius, correctionRadius, correctionRadiusIncrementEvery,
			generationType,
			chooseByProbability, probabilityImportance 
			);

		generator.OnGenerationTaskDone = OnGenerationTaskDone;
		sample.Hide();

		generator.Ready();
	}


    public override void _Process( double delta )
    {
        generator.Process( delta );
		if ( showCurrentProgress )
			ApplyTileMapArray();
		if ( updateProgressBar )
			progressBar.Value = generator.Progress;
    }


	// Called when the generation is finished
	public void OnGenerationTaskDone()
	{
		ApplyTileMapArray();
	}


	// Returns an array of the tiles from the sample tile map.
	public List<List<List<ItemAndOrientation>>> ExtractSample()
	{
		sampleArray.Clear();
		
		// Find used area
		var usedCells = sample.GetUsedCells();
		Vector3I min = usedCells[ 0 ];
		Vector3I max = usedCells[ 0 ];

		foreach ( Vector3I cell in usedCells )
		{
			min.X = Math.Min( min.X, cell.X );
			min.Y = Math.Min( min.Y, cell.Y );
			min.Z = Math.Min( min.Z, cell.Z );

			max.X = Math.Max( max.X, cell.X );
			max.Y = Math.Max( max.Y, cell.Y );
			max.Z = Math.Max( max.Z, cell.Z );
		}

		GD.Print( min, max );

		for ( int i = min.X; i <= max.X; i++ )
		{
			sampleArray.Add( new List<List<ItemAndOrientation>>() );

			for ( int j = min.Y; j <= max.Y; j++ )
			{
				sampleArray[ i - min.X ].Add( new List<ItemAndOrientation>() );
				for ( int k = min.Z; k <= max.Z; k++ )
				{
					Vector3I pos = new Vector3I( i, j, k );
					sampleArray[ i - min.X ][ j - min.Y ].Add( new ItemAndOrientation( sample.GetCellItem( pos ), sample.GetCellItemOrientation( pos ) ) );
				}
			}
		}

		// GD.Print( sampleArray[ 0 ][ 0 ].Equals( new ItemAndOrientation( -1, -1 ) ) );

		return sampleArray;
	}



	// Applies the generated tiles on the tile map.
	public void ApplyTileMapArray()
	{
		for ( int i = 0; i < dimensions.X; i++ )
			for ( int j = 0; j < dimensions.Y; j++ )
				for ( int k = 0; k < dimensions.Z; k++ )
				{
					ItemAndOrientation item = generator.GetTile( i, j, k );
					target.SetCellItem( new Vector3I( i, j, k ), item.id, item.orientation );
				}
	}


	public void _on_button_pressed()
	{
		generator.StartGeneration();
	}
}


public struct ItemAndOrientation
{
	public int id;
	public int orientation;

	public ItemAndOrientation( int itemId, int orientation )
	{
		id = itemId;
		this.orientation = orientation;
	}


    public override bool Equals( [NotNullWhen(true)] object obj )
    {
		if ( obj.GetType() == typeof( ItemAndOrientation ) )
		{
			ItemAndOrientation newObj = ( ItemAndOrientation ) obj;
			return newObj.id == id && newObj.orientation == orientation;
		}

        return base.Equals(obj);
    }


    public override string ToString()
    {
        return "( " + id + ", " + orientation + " )";
    }
}
