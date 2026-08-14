using System.Collections.Generic;
using GrafikPlanerData.DbScripts;
using GrafikPlanerData.Models;

namespace GrafikPlanerCore.Models;

public class ContextMenuOptions
{
    public List<HoursRecord?> Hours { get; } 
    public List<string?> Colors{ get; }
    public List<string?> Icons{ get; }

    public ContextMenuOptions()
    {
        Hours = new List<HoursRecord>();
        Colors = new List<string>();
        Icons = new List<string>();
    }

    public void FillColors()
    {
        Colors.Clear();
        Colors.Add(null);
        Colors.Add("#66E066");
        Colors.Add("#FF8A80");
        Colors.Add("#82B1FF");
        Colors.Add("#FFF176");
        Colors.Add("#EA80FC");
    }

    public void FillIcons()
    {
        Icons.Clear();
        Icons.Add(null);
        Icons.Add("warning");
        Icons.Add("mark");
    }

    public void FillHours()
    {
        var dbTable = new HoursTable();
        dbTable.StartConnectionWithDatabase();
        var getData = dbTable.GetAllHours();
        
        Hours.Clear();
        Hours.Add(null);
        foreach (var hour in getData)
            Hours.Add(hour);
    }
    
}