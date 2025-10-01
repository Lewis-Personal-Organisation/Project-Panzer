using UnityEngine;
using System.Collections.Generic;

public static class OnGUISceneViewData
{
    public class LabelEntry
    {
        public string preText;
        public string dataText;
        public Color color;

        public LabelEntry(string preText, string dataText, Color colour)
        {
            this.preText = preText;
            this.dataText = dataText;
            this.color = colour == default ? Color.black : colour;
        }
    }
    
    private static int LabelExists(string preText)
    {
        for (int i = 0; i < labelEntries.Count; i++)
        {
            if (labelEntries[i].preText == preText)
            {
                return i;
            }
        }
        return -1;
    }
    
    public static List<LabelEntry> labelEntries = new List<LabelEntry>();
    public static string GetText(int index) => $"{labelEntries[index].preText}{labelEntries[index].dataText}";
    public static Color GetColour(int index) => labelEntries[index].color;


    public static void AddOrUpdateLabel(string preText, string dataText, Color colour = default)
    {
        int index = LabelExists(preText);
        
        if (index != -1)
        {
            labelEntries[index].dataText = dataText;
        }
        else
        {
            labelEntries.Add(new LabelEntry(preText, dataText, colour));
        }
    }
}