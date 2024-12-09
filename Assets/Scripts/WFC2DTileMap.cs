using Godot;
using System;
using System.Collections.Generic;

public partial class WFC2DTileMap : Node
{
	WFCGenerator2D< Vector2I > generator;
	[ Export ] public TileMap target;
	[ Export ] public TileMap sample;
	[ Export ] public ProgressBar progressBar;
	private List<List<Vector2I>> sampleArray = new List<List<Vector2I>>();
	[ Export ] public Vector2I dimensions;
	[ Export ] public int matchRadius = 1;
	[ Export ] public int correctionRadius = 3;
	[ Export ] public int correctionRadiusIncrementEvery = 10;
	[ Export ] public WFCGenerator2D<Vector2I>.GenerationType generationType = WFCGenerator2D<Vector2I>.GenerationType.Intelligent;
	[ Export ] public bool chooseByProbability = false;
	[ Export ] public ProbabilityImportance probabilityImportance = ProbabilityImportance.NORMAL;
	[ Export ] public bool showCurrentProgress = true;
	[ Export ] public bool updateProgressBar = true;


	public override void _Ready()
	{
		var sampleArray = ExtractSample();

		generator = new WFCGenerator2D<Vector2I>(
			new Vector2I( -1, -1 ), dimensions.X, dimensions.Y, sampleArray,
			matchRadius, correctionRadius, correctionRadiusIncrementEvery,
			generationType, chooseByProbability,
			probabilityImportance 
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
	public List<List<Vector2I>> ExtractSample()
	{
		sampleArray.Clear();
		Vector2I size = sample.GetUsedRect().Size;
		Vector2I offset = sample.GetUsedRect().Position;
		for ( int i = 0; i < size.X; i++ )
		{
			sampleArray.Add( new List<Vector2I>() );

			for ( int j = 0; j < size.Y; j++ )
			{
				sampleArray[ i ].Add( sample.GetCellAtlasCoords( 0, new Vector2I( i, j ) + offset ) );
			}
		}

		return sampleArray;
	}


	// Applies the generated tiles on the tile map.
	public void ApplyTileMapArray()
	{
		for ( int i = 0; i < dimensions.X; i++ )
			for ( int j = 0; j < dimensions.Y; j++ )
			{
				// GD.Print( generator.GetTile( i, j ) );
				target.SetCell( 0, new Vector2I( i, j ), 1, generator.GetTile( i, j ) );
			}
	}


	public void _on_button_pressed()
	{
		generator.StartGeneration();
	}
}

