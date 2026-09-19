namespace Microplex.Web.Models;

public sealed class CosmeticItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Origin { get; set; } = "Made in Japan";
    public string ShortDescription { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;
    public List<string> Highlights { get; set; } = [];
    public string AccentColor { get; set; } = "#c9a7a0";
}
