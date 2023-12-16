using Godot;
using Godot.NativeInterop;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public partial class Rule : Node
{
    protected List<List<Vector2I>> ruleArray;

    public Rule(int matchRadius)
    {
        CreateRuleArray(matchRadius);
    }

    public Rule(int matchRadius, Vector2I[,] suroundings)
    {
        CreateRuleArray(matchRadius);
        for (int i=0; i<ruleArray.Count; i++)
            for (int j=0; j<ruleArray.Count; j++)
            {
                ruleArray[i][j] = suroundings[i,j];
            }
    }


    // creates a new ruleArray
    protected void CreateRuleArray(int matchRadius)
    {
        ruleArray = new List<List<Vector2I>>(1+matchRadius*2);
        for (int i=0; i<1+matchRadius*2; i++)
        {
            ruleArray.Add(new List<Vector2I>(1+matchRadius*2));
            for (int j=0; j<1+matchRadius*2; j++)
                ruleArray[i].Add(new Vector2I(-1,-1));
        }
    }


    // returns match radios based on current ruleArray size
    public int GetMatchRadius()
    {
        return (ruleArray.Count-1)/2;
    }

    // returns this rule in the shape of an array
    public Vector2I[,] GetArray()
    {
        Vector2I[,] arr = new Vector2I[ruleArray.Count, ruleArray.Count];
        for (int i=0; i<arr.GetLength(0); i++)
            for (int j=0; j<arr.GetLength(1); j++)
            {
                arr[i,j] = ruleArray[i][j];
            }
        return arr;
    }

    // returns true if all tiles have non-negative values
    public bool IsValid()
    {
        for (int i=0; i<ruleArray.Count; i++)
            for (int j=0; j<ruleArray.Count; j++)
                if (ruleArray[i][j][0]<0 || ruleArray[i][j][2]<0)
                    return false;
        return true;
    }

    // returns true if the two rules are identic
    public bool CompareWith(Rule rule)
    {
        return CompareRules(this, rule);
    }

    // returns true if this rule is suitablble to be appiled on the set of given tiles
    public bool CompareWithTiles(Vector2I[,] tilesArray)
    {
        if (tilesArray.GetLength(0)!=ruleArray.Count
        || tilesArray.GetLength(1)!=ruleArray.Count)
            return false;


        Vector2I V_1 = new Vector2I(-1,-1);
        for (int i=0; i<ruleArray.Count; i++)
            for (int j=0; j<ruleArray.Count; j++)
            {
                if (tilesArray[i,j]!=V_1 && tilesArray[i,j]!=ruleArray[i][j])
                    return false;
            }
        
        return true;
    }

    // returns true if the two rules are identic
    public static bool CompareRules(Rule rule1, Rule rule2)
    {
        int ruleArraySize = rule1.ruleArray.Count;
        if (ruleArraySize != rule2.ruleArray.Count)
            return false;
        
        for (int i=0; i<ruleArraySize; i++)
            for (int j=0; j<ruleArraySize; j++)
            {
                if (rule1.ruleArray[i][j] != rule2.ruleArray[i][j])
                    return false;
            }

        return true;
    }

    // print the rule in console, for debugging purposes
    public void Print()
    {
        GD.Print("{");
        for (int i=0; i<ruleArray.Count; i++)
        {
            string s="";
            for (int j=0; j<ruleArray.Count; j++)
            {
                s += ruleArray[j][i].ToString()+",";
            }
            GD.Print(s);
        }
        GD.Print("}");
    }
}
