using Microplex.Web.Models;

namespace Microplex.Web.Services;

public static class CosmeticCatalog
{
    public static readonly List<CosmeticItem> Items =
    [
        new CosmeticItem
        {
            Id = 1,
            Name = "Facial Treatment Essence",
            Brand = "SK-II",
            Category = "Essence",
            ShortDescription = "The original Pitera™ essence, a cult favorite for radiant, resilient skin.",
            FullDescription = "SK-II's signature essence, formulated with over 90% Pitera™ — a naturally derived ingredient discovered in a sake brewery. Used daily, it visibly improves skin texture, tone, and radiance.",
            Highlights = ["230ml bottle", "Suitable for all skin types", "Contains Pitera™", "Fragrance available: original or clear"],
            AccentColor = "#c9a7a0"
        },
        new CosmeticItem
        {
            Id = 2,
            Name = "R.N.A. Power Radical New Age Cream",
            Brand = "SK-II",
            Category = "Moisturizer",
            ShortDescription = "A firming cream that targets multiple visible signs of skin aging.",
            FullDescription = "A rich, advanced anti-aging cream that works to visibly firm and lift skin, targeting fine lines, elasticity, and radiance for a smoother, more youthful appearance.",
            Highlights = ["50g jar", "Best for mature or dry skin", "Contains Pitera™", "Use morning & night"],
            AccentColor = "#b98b6a"
        },
        new CosmeticItem
        {
            Id = 3,
            Name = "AQ Meliority Deep Moisture Serum",
            Brand = "DECORTÉ",
            Category = "Serum",
            ShortDescription = "A luxurious serum delivering deep, long-lasting hydration.",
            FullDescription = "Part of DECORTÉ's flagship AQ Meliority line, this serum penetrates deeply to restore moisture balance, leaving skin plump, supple, and luminous.",
            Highlights = ["40ml bottle", "For dry or dehydrated skin", "Lightweight, fast-absorbing", "Iris Encapsulation Complex"],
            AccentColor = "#8a6f9e"
        },
        new CosmeticItem
        {
            Id = 4,
            Name = "Liposome Advanced Repair Serum",
            Brand = "DECORTÉ",
            Category = "Serum",
            ShortDescription = "An iconic overnight repair serum for smoother, healthier-looking skin.",
            FullDescription = "A beloved night serum that supports the skin's natural repair cycle while you sleep, improving texture and resilience by morning. A long-standing favorite among skincare enthusiasts.",
            Highlights = ["40ml bottle", "Best used at night", "Improves skin texture over time", "Suitable for most skin types"],
            AccentColor = "#7c6690"
        },
        new CosmeticItem
        {
            Id = 5,
            Name = "Mild Cleansing Oil",
            Brand = "FANCL",
            Category = "Cleanser",
            ShortDescription = "A gentle, additive-free cleansing oil that removes makeup effortlessly.",
            FullDescription = "FANCL's best-selling cleansing oil, formulated without preservatives or additives. Emulsifies with water to remove makeup and impurities without stripping the skin.",
            Highlights = ["120ml bottle", "Additive-free formula", "Removes waterproof makeup", "For all skin types"],
            AccentColor = "#e0b84f"
        },
        new CosmeticItem
        {
            Id = 6,
            Name = "Enrich Plus Lifting Cream",
            Brand = "FANCL",
            Category = "Moisturizer",
            ShortDescription = "An anti-aging cream designed to firm and lift mature skin.",
            FullDescription = "A nourishing, collagen-supporting cream that helps improve firmness and elasticity, designed for those looking to address sagging and loss of volume.",
            Highlights = ["30g jar", "Best for mature skin", "Additive-free", "Rich, creamy texture"],
            AccentColor = "#d1a53d"
        },
        new CosmeticItem
        {
            Id = 7,
            Name = "Ultimune Power Infusing Concentrate",
            Brand = "Shiseido",
            Category = "Serum",
            ShortDescription = "A powerful serum that strengthens skin's natural defenses.",
            FullDescription = "Shiseido's iconic serum, formulated with ImuGeneration Technology™ to boost the skin's own defenses against environmental stressors, resulting in visibly stronger, more radiant skin.",
            Highlights = ["50ml bottle", "Lightweight, silky texture", "Suitable for all skin types", "Use morning & night"],
            AccentColor = "#a13b52"
        },
        new CosmeticItem
        {
            Id = 8,
            Name = "Gokujyun Premium Lotion",
            Brand = "Hada Labo",
            Category = "Toner",
            ShortDescription = "An ultra-hydrating lotion with five types of hyaluronic acid.",
            FullDescription = "A cult-favorite hydrating toner formulated with five types of hyaluronic acid to deliver deep, multi-layered moisture, leaving skin dewy and plump.",
            Highlights = ["170ml bottle", "5 types of hyaluronic acid", "Fragrance-free", "Suitable for sensitive skin"],
            AccentColor = "#4f8a9e"
        },
        new CosmeticItem
        {
            Id = 9,
            Name = "Sekkisei Lotion",
            Brand = "Kosé",
            Category = "Toner",
            ShortDescription = "A brightening lotion rooted in traditional Japanese herbal ingredients.",
            FullDescription = "A long-standing Japanese skincare icon, this lotion uses a blend of traditional herbal extracts to hydrate, brighten, and even out skin tone over time.",
            Highlights = ["360ml bottle", "Herbal extract blend", "Brightening formula", "Suitable for most skin types"],
            AccentColor = "#5b7fa6"
        },
        new CosmeticItem
        {
            Id = 10,
            Name = "Perfect Whip Face Wash",
            Brand = "Senka",
            Category = "Face Wash",
            ShortDescription = "A rich, whipped foam cleanser for soft, clean skin.",
            FullDescription = "A gentle daily face wash that creates a dense, creamy foam to lift away dirt and makeup residue without over-drying, leaving skin soft and smooth.",
            Highlights = ["120g tube", "Dense whipped foam", "For daily use", "Suitable for all skin types"],
            AccentColor = "#5fa9a1"
        }
    ];

    public static IReadOnlyList<string> Brands => Items.Select(x => x.Brand).Distinct().OrderBy(x => x).ToList();
    public static IReadOnlyList<string> Categories => Items.Select(x => x.Category).Distinct().OrderBy(x => x).ToList();
}
