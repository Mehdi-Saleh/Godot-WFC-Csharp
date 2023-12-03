using Godot;
using System;

public partial class ProgressBarScript : ProgressBar
{
	[Export] WFCGenerator generator;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Value = ((float) generator.currentN/generator.maxN);
	}
}
