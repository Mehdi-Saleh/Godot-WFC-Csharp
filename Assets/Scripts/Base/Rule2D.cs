using Godot;
using Godot.NativeInterop;
using System;
using System.Collections.Generic;
using System.Data;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

public partial class Rule2D<T>
{
	// This is the array of tiles that create this rule.
	public List<List<T>> RuleArray { get; private set; }
	protected static Vector2I Vector_1 = new Vector2I( -1, -1 );
	private static T ZeroValue;


	public Rule2D( int matchRadius, T zeroValue )
	{
		ZeroValue = zeroValue;
		CreateRuleArray( matchRadius );
	}

	public Rule2D( int matchRadius, T[,] suroundings, T zeroValue )
	{
		ZeroValue = zeroValue;
		CreateRuleArray( matchRadius );
		for ( int i = 0; i < RuleArray.Count; i++ )
			for ( int j = 0; j < RuleArray.Count; j++ )
			{
				RuleArray[ i ][ j ] = suroundings[ i,j ];
			}
	}

	public Rule2D( int matchRadius, Vector2I position, in List<List<T>> sample, T zeroValue )
	{
		ZeroValue = zeroValue;
		CreateRuleArray( matchRadius );
		for ( int i = 0; i < RuleArray.Count; i++ )
			for ( int j = 0; j < RuleArray.Count; j++ )
			{
				try
				{
					RuleArray[ i ][ j ] = sample[ position.X-matchRadius+i ][ position.Y-matchRadius+j ];
				}
				catch ( ArgumentOutOfRangeException err )
				{
					RuleArray[ i ][ j ] = zeroValue;
				}
			}
	}
	
	public Rule2D( int matchRadius, Vector2I position, in List<List<T>> tilesArray,  T zeroValue, bool offsetPos=true )
	{
		ZeroValue = zeroValue;
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


	// Creates a new rules array.
	protected void CreateRuleArray(int matchRadius)
	{
		RuleArray = new List<List<T>>( 1 + matchRadius*2 );
		for ( int i = 0; i < 1+matchRadius*2; i++ )
		{
			RuleArray.Add( new List<T>( 1 + matchRadius*2 ) );
			for ( int j = 0; j < 1 + matchRadius*2; j++ )
				RuleArray[i].Add( ZeroValue );
		}
	}


	// Returns match radios based on current RuleArray size.
	public int GetMatchRadius()
	{
		return ( RuleArray.Count-1 )/2;
	}


	// Returns this rule in the shape of an array.
	public T[,] GetArray()
	{
		T[,] arr = new T[ RuleArray.Count, RuleArray.Count ];
		for ( int i=0; i<arr.GetLength( 0 ); i++ )
			for ( int j=0; j<arr.GetLength( 1 ); j++ )
			{
				arr[ i, j ] = RuleArray[ i ][ j ];
			}
		return arr;
	}


	// Returns true if the two rules are identic.
	public bool CompareWith( Rule2D<T> rule, bool ignore_1InRule2 = false )
	{
		return CompareRules( this, rule, ignore_1InRule2 );
	}


	// Returns true if this rule is suitable to be appiled on the set of given tiles.
	public bool CompareWithTiles( T[,] tilesArray )
	{
		if ( tilesArray.GetLength( 0 ) != RuleArray.Count
		|| tilesArray.GetLength( 1 ) != RuleArray.Count )
			return false;


		for ( int i = 0; i < RuleArray.Count; i++ )
			for ( int j = 0; j < RuleArray.Count; j++ )
			{
				if ( !tilesArray[ i, j ].Equals( ZeroValue ) && !tilesArray[ i, j ].Equals( RuleArray[ i ][ j ] ) )
					return false;
			}
		
		return true;
	}


	// Returns true if the two rules are identical.
	public static bool CompareRules( Rule2D<T> rule1, Rule2D<T> rule2, bool ignore_1InRule2 = false )
	{
		int RuleArraySize = rule1.RuleArray.Count;
		if ( RuleArraySize != rule2.RuleArray.Count )
			return false;
		if ( !ignore_1InRule2 )
		{   
			for ( int i = 0; i < RuleArraySize; i++ )
				for ( int j = 0; j < RuleArraySize; j++ )
				{
					if ( !rule1.RuleArray[ i ][ j ].Equals( rule2.RuleArray[ i ][ j ] ) )
						return false;
				}

			return true;
		}
		else
		{
			for ( int i = 0; i < RuleArraySize; i++ )
				for ( int j = 0; j < RuleArraySize; j++ )
				{
					if ( !DoTilesMatch( rule2.RuleArray[ i ][ j ], rule1.RuleArray[ i ][ j ] ) )
						return false;
				}

			return true;
		}
	}


	// Returns true if the two given tiles match (or if the first one is not set).
	private static bool DoTilesMatch( T tile1, T tile2 )
	{
		return tile1.Equals( Vector_1 ) || tile2.Equals( tile1 );
	}


	// Returns true if all tiles are zero 
	public bool IsZero()
	{
		bool zero = true;

		foreach ( var row in RuleArray )
			foreach ( var tile in row )
				if ( !tile.Equals( ZeroValue ) )
				{
					zero = false;
					break;
				}

		return zero;
	}


	// Returns true if any tile is zero 
	public bool HasAnyZero()
	{
		foreach ( var row in RuleArray )
			foreach ( var tile in row )
				if ( tile.Equals( ZeroValue ) )
				{
					return true;
				}
		return false;
	}


	// Prints the rule in console, for debugging purposes.
	public void Print()
	{
		GD.Print( "{" );
		for ( int i = 0; i < RuleArray.Count; i++ )
		{
			string s="";
			for ( int j=0; j<RuleArray.Count; j++ )
			{
				s += RuleArray[ j ][ i ].ToString()+",";
			}
			GD.Print( s );
		}
		GD.Print( "}" );
	}
}
