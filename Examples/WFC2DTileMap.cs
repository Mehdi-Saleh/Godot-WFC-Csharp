using Godot;
using System;
using System.Collections.Generic;

public partial class WFC2DTileMap : Node
{
	WFCGenerator2D< Vector2I > generator;
	[ Export ] public TileMap target;
	[ Export ] public TileMap sample;
	private List<List<Vector2I>> sampleArray = new List<List<Vector2I>>();
	[ Export ] public Vector2I dimensions;
	[ Export ] public int matchRadius = 1;
	[ Export ] public int correctionRadius = 3;
	[ Export ] public WFCGenerator2D<Vector2I>.GenerationType generationType = WFCGenerator2D<Vector2I>.GenerationType.Intelligent;
	[ Export ] public bool chooseByProbablity = false;


	public override void _Ready()
	{
		generator = new WFCGenerator2D< Vector2I >( new Vector2I( -1, -1 ), dimensions.X, dimensions.Y, matchRadius, correctionRadius, generationType, chooseByProbablity );
		generator.sample = ExtractSample();
		generator.OnGenerationTaskDone = OnGenerationTaskDone;
		sample.Hide();

		generator.Ready();
	}

    public override void _Process( double delta )
    {
        generator.Process( delta );
    }


	public void OnGenerationTaskDone()
	{
		GD.Print( generator.GetTile( 0, 0 ).Equals( new Vector2I( -1, -1 ) ) );
		ApplyTileMapArray();
		GD.Print( "Done!" );
	}


	public List<List<Vector2I>> ExtractSample()
	{
		sampleArray.Clear();
		Vector2I size = sample.GetUsedRect().Size;
		for ( int i = 0; i < size.X; i++ )
		{
			sampleArray.Add( new List<Vector2I>() );

			for ( int j = 0; j < size.Y; j++ )
			{
				sampleArray[ i ].Add( sample.GetCellAtlasCoords( 0, new Vector2I( i, j ) ) );
			}
		}

		return sampleArray;
	}


	public void ApplyTileMapArray()
	{
		for ( int i = 0; i < dimensions.X; i++ )
			for ( int j = 0; j < dimensions.Y; j++ )
			{
				GD.Print( generator.GetTile( i, j ) );
				target.SetCell( 0, new Vector2I( i, j ), 1, generator.GetTile( i, j ) );
			}
	}


	public void _on_button_pressed()
	{
		generator.StartGeneration();
	}
}

