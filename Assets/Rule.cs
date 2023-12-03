using Godot;
using System;
using System.Collections.Generic;

public partial class Rule : Node
{
    public List<List<Vector2I>> ruleArray;

    public Rule(int matchRadius)
    {
        ruleArray = new List<List<Vector2I>>(1+matchRadius*2);
        for (int i=0; i<1+matchRadius*2; i++)
        {
            ruleArray.Add(new List<Vector2I>(1+matchRadius*2));
            for (int j=0; j<1+matchRadius*2; j++)
                ruleArray[i].Add(new Vector2I(-1,-1));
        }
    }

    // public static Rule CreateRule()
}
