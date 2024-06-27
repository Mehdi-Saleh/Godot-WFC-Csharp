using Godot;
using System;

public partial class WFC2DTileMap : Node
{
	WFCGenerator2D< Vector2I > generator = new WFCGenerator2D< Vector2I >( new Vector2I( -1, -1 ) );
	[ Export ] public TileMap target;
	[ Export ] public TileMap sample;


	public override void _Ready()
	{
		generator.Ready();
	}

    public override void _Process( double delta )
    {
        generator.Process( delta );
    }
}
