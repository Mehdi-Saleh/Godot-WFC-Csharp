using Godot;
using System;

public partial class Tile : Resource
{
	[Export] Vector2I atlasCoord;

	[ExportGroup("Possible Tiles")]
	[Export] Vector2I right;
//	[Export] Vector2I[] left;
//	[Export] Vector2I[] up;
//	[Export] Vector2I[] down;
//	[Export] Vector2I[] upr;
//	[Export] Vector2I[] upl;
//	[Export] Vector2I[] downr;
//	[Export] Vector2I[] downl;
}
