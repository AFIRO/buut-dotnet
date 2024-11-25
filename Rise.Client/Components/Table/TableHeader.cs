namespace Rise.Client.Components.Table;

public class TableHeader
{
    public string Title { get; set; }
    public string CssClass { get; set; } 

    public TableHeader(string title, string cssClass = "")
    {
        Title = title;
        CssClass = cssClass;
    }
}