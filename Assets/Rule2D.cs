using Godot;
using Godot.NativeInterop;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

public partial class Rule2D<T> // DECOUPLE
{
	public List<List<T>> RuleArray { get; private set; }
	protected static Vector2I Vector_1 = new Vector2I( -1, -1 );
	private T zeroValue;

	public Rule2D( int matchRadius, T zeroValue )
	{
		this.zeroValue = zeroValue;
		CreateRuleArray( matchRadius );
	}

	public Rule2D( int matchRadius, T[,] suroundings, T zeroValue )
	{
		this.zeroValue = zeroValue;
		CreateRuleArray( matchRadius );
		for ( int i = 0; i < RuleArray.Count; i++ )
			for ( int j = 0; j < RuleArray.Count; j++ )
			{
				RuleArray[ i ][ j ] = suroundings[ i,j ];
			}
	}

	public Rule2D( int matchRadius, Vector2I position, in TileMap sample, T zeroValue )
	{
		this.zeroValue = zeroValue;
		Vector2I matchRadVector = new Vector2I( matchRadius, matchRadius );
		CreateRuleArray( matchRadius );
		for ( int i = 0; i < RuleArray.Count; i++ )
			for ( int j = 0; j < RuleArray.Count; j++ )
			{
				RuleArray[ i ][ j ] = sample.GetCellAtlasCoords( 0, position-matchRadVector+new Vector2I( i,j ) );
			}
	}
	
	public Rule2D( int matchRadius, Vector2I position, in List<List<T>> tilesArray, bool offsetPos=true, T zeroValue )
	{
		this.zeroValue = zeroValue;
		Vector2I matchRadVector = new Vector2I( matchRadius, matchRadius );
		CreateRuleArray( matchRadius );
		for ( int i = 0; i < RuleArray.Count; i++ )
			for ( int j = 0; j < RuleArray.Count; j++ )
			{
				if ( offsetPos )
					RuleArray[ i ][ j ] = tilesArray[ position.X+i ][ position.Y+j ];
				else
					RuleArray[ i ][ j ] = tilesArray[ position.X-matchRadius+i ][ position.Y-matchRadius+j ];
			}
	}


	// Creates a new RuleArray
	protected void CreateRuleArray(int matchRadius)
	{
		RuleArray = new List<List<Vector2I>>(1+matchRadius*2);
		for (int i=0; i<1+matchRadius*2; i++)
		{
			RuleArray.Add(new List<Vector2I>(1+matchRadius*2));
			for (int j=0; j<1+matchRadius*2; j++)
				RuleArray[i].Add(new Vector2I(-1,-1));
		}
	}


	// returns match radios based on current RuleArray size
	public int GetMatchRadius()
	{
		return (RuleArray.Count-1)/2;
	}

	// returns this rule in the shape of an array
	public Vector2I[,] GetArray()
	{
		Vector2I[,] arr = new Vector2I[RuleArray.Count, RuleArray.Count];
		for (int i=0; i<arr.GetLength(0); i++)
			for (int j=0; j<arr.GetLength(1); j++)
			{
				arr[i,j] = RuleArray[i][j];
			}
		return arr;
	}

	// returns true if the two rules are identic
	public bool CompareWith(Rule rule, bool ignore_1InRule2 = false)
	{
		return CompareRules(this, rule, ignore_1InRule2);
	}

	// returns true if this rule is suitablble to be appiled on the set of given tiles
	public bool CompareWithTiles(Vector2I[,] tilesArray)
	{
		if (tilesArray.GetLength(0)!=RuleArray.Count
		|| tilesArray.GetLength(1)!=RuleArray.Count)
			return false;


		Vector2I V_1 = new Vector2I(-1,-1);
		for (int i=0; i<RuleArray.Count; i++)
			for (int j=0; j<RuleArray.Count; j++)
			{
				if (tilesArray[i,j]!=V_1 && tilesArray[i,j]!=RuleArray[i][j])
					return false;
			}
		
		return true;
	}

	// returns true if the two rules are identic
	public static bool CompareRules(Rule rule1, Rule rule2, bool ignore_1InRule2 = false)
	{
		int RuleArraySize = rule1.RuleArray.Count;
		if (RuleArraySize != rule2.RuleArray.Count)
			return false;
		if (!ignore_1InRule2)
		{   
			for (int i=0; i<RuleArraySize; i++)
				for (int j=0; j<RuleArraySize; j++)
				{
					if (rule1.RuleArray[i][j] != rule2.RuleArray[i][j])
						return false;
				}

			return true;
		}
		else
		{
			for (int i=0; i<RuleArraySize; i++)
				for (int j=0; j<RuleArraySize; j++)
				{
					if (!DoTilesMatch(rule2.RuleArray[i][j], rule1.RuleArray[i][j]))
						return false;
				}

			return true;
		}
	}


	// returns true if the two given tiles match (or if the first one is not set)
	private static bool DoTilesMatch(Vector2I tile1, Vector2I tile2)
	{
		return tile1==Vector_1 || tile2==tile1;
	}

	// print the rule in console, for debugging purposes
	public void Print()
	{
		GD.Print("{");
		for (int i=0; i<RuleArray.Count; i++)
		{
			string s="";
			for (int j=0; j<RuleArray.Count; j++)
			{
				s += RuleArray[j][i].ToString()+",";
			}
			GD.Print(s);
		}
		GD.Print("}");
	}
}
