using Microplex.Web.Models;

namespace Microplex.Web.Services;

public static class CosmeticCatalog
{
    public static readonly List<CosmeticItem> Items =
    [
        // SK-II
        new CosmeticItem
        {
            Id = 1,
            Name = "PITERA™ Facial Treatment Essence",
            Brand = "SK-II",
            Category = "Essence",
            ShortDescription = "SK-II's signature essence built around its Pitera fermentation ingredient.",
            FullDescription = "The essence that built the SK-II brand, formulated around Pitera, an ingredient derived from a yeast fermentation process. Used as a daily treatment step, it's aimed at improving the look and feel of skin texture over time.",
            Highlights = ["Signature Pitera formula", "Suitable for daily use", "Foundational step in the SK-II routine", "Available in original and clear (fragrance-free) versions"],
            AccentColor = "#c9a7a0"
        },
        new CosmeticItem
        {
            Id = 2,
            Name = "Facial Treatment Serum",
            Brand = "SK-II",
            Category = "Serum",
            ShortDescription = "A concentrated treatment serum for overall skin renewal.",
            FullDescription = "A treatment serum designed to work alongside SK-II's essence step, targeting an even, radiant complexion with continued use as part of a full skincare routine.",
            Highlights = ["Concentrated treatment formula", "Layers with the Facial Treatment Essence", "Aimed at improving radiance"],
            AccentColor = "#d9c2b8"
        },
        new CosmeticItem
        {
            Id = 3,
            Name = "SKINPOWER Re-New Cream",
            Brand = "SK-II",
            Category = "Moisturizer",
            ShortDescription = "An anti-aging cream from SK-II's SKINPOWER line.",
            FullDescription = "Part of SK-II's SKINPOWER anti-aging collection, this rich cream is formulated to support skin firmness and a smoother appearance with regular use.",
            Highlights = ["Part of the SKINPOWER line", "Rich, nourishing texture", "Best for night or daytime use under sunscreen"],
            AccentColor = "#b23b4a"
        },
        new CosmeticItem
        {
            Id = 4,
            Name = "SKINPOWER Eye + Line Filler Cream",
            Brand = "SK-II",
            Category = "Eye Cream",
            ShortDescription = "A targeted eye cream for the fine, delicate skin around the eyes.",
            FullDescription = "A lightweight cream formulated specifically for the eye area, designed to be layered into an anti-aging routine alongside SK-II's other SKINPOWER products.",
            Highlights = ["Formulated for the eye area", "Lightweight texture", "Part of the SKINPOWER line"],
            AccentColor = "#a34a5a"
        },
        new CosmeticItem
        {
            Id = 5,
            Name = "PITERA™ Facial Treatment Mask",
            Brand = "SK-II",
            Category = "Mask",
            ShortDescription = "A sheet mask soaked in SK-II's Pitera essence for an intensive moisture boost.",
            FullDescription = "A single-use sheet mask saturated with SK-II's Pitera-based essence, intended as an occasional intensive treatment for extra hydration.",
            Highlights = ["Individually packaged sheet masks", "Intensive moisture treatment", "Suitable for occasional use"],
            AccentColor = "#cbb7ae"
        },
        new CosmeticItem
        {
            Id = 6,
            Name = "PITERA™ Facial Treatment Clear Lotion",
            Brand = "SK-II",
            Category = "Toner",
            ShortDescription = "A clarifying toner formulated to gently refine skin texture.",
            FullDescription = "A toning lotion used after cleansing to help refine and prep the skin, formulated with SK-II's Pitera-based approach to skin conditioning.",
            Highlights = ["Used after cleansing", "Helps refine skin texture", "Contains Pitera"],
            AccentColor = "#6a8fa6"
        },
        new CosmeticItem
        {
            Id = 7,
            Name = "PITERA™ Facial Treatment Cleanser",
            Brand = "SK-II",
            Category = "Cleanser",
            ShortDescription = "A foaming daily cleanser formulated to cleanse without over-drying.",
            FullDescription = "A daily facial cleanser designed to remove impurities while maintaining skin's moisture balance, formulated as part of the wider PITERA line.",
            Highlights = ["Daily use foaming cleanser", "Formulated to avoid over-drying", "Part of the PITERA line"],
            AccentColor = "#8fa6b0"
        },
        new CosmeticItem
        {
            Id = 8,
            Name = "Facial Treatment Cleansing Oil",
            Brand = "SK-II",
            Category = "Cleanser",
            ShortDescription = "An oil-based cleanser for removing makeup and daily buildup.",
            FullDescription = "A cleansing oil formulated to dissolve makeup and sunscreen before a second, water-based cleanse, as part of a double-cleansing routine.",
            Highlights = ["Removes makeup and sunscreen", "Designed for double-cleansing routines", "Emulsifies with water"],
            AccentColor = "#9fb0a6"
        },
        new CosmeticItem
        {
            Id = 9,
            Name = "GenOptics InfinitAura Serum",
            Brand = "SK-II",
            Category = "Serum",
            ShortDescription = "A brightening serum from SK-II's GenOptics range.",
            FullDescription = "Part of SK-II's GenOptics range, this serum is aimed at addressing an uneven-looking complexion and supporting a brighter overall appearance with continued use.",
            Highlights = ["Part of the GenOptics range", "Aimed at an uneven-looking complexion", "Lightweight serum texture"],
            AccentColor = "#e0c9a0"
        },
        new CosmeticItem
        {
            Id = 10,
            Name = "LXP Ultimate Revival Cream",
            Brand = "SK-II",
            Category = "Moisturizer",
            ShortDescription = "A premium anti-aging cream from SK-II's LXP line.",
            FullDescription = "SK-II's higher-tier LXP line, formulated as an intensive anti-aging moisturizer for those wanting a richer, more advanced step in their routine.",
            Highlights = ["Premium LXP line", "Intensive anti-aging formula", "Rich cream texture"],
            AccentColor = "#8a3b45"
        },

        // DECORTÉ
        new CosmeticItem
        {
            Id = 11,
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
            Id = 12,
            Name = "Liposome Advanced Repair Serum",
            Brand = "DECORTÉ",
            Category = "Serum",
            ShortDescription = "An iconic overnight repair serum for smoother, healthier-looking skin.",
            FullDescription = "A beloved night serum that supports the skin's natural repair cycle while you sleep, improving texture and resilience by morning. A long-standing favorite among skincare enthusiasts.",
            Highlights = ["40ml bottle", "Best used at night", "Improves skin texture over time", "Suitable for most skin types"],
            AccentColor = "#7c6690"
        },

        // FANCL
        new CosmeticItem
        {
            Id = 13,
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
            Id = 14,
            Name = "Enrich Plus Lifting Cream",
            Brand = "FANCL",
            Category = "Moisturizer",
            ShortDescription = "An anti-aging cream designed to firm and lift mature skin.",
            FullDescription = "A nourishing, collagen-supporting cream that helps improve firmness and elasticity, designed for those looking to address sagging and loss of volume.",
            Highlights = ["30g jar", "Best for mature skin", "Additive-free", "Rich, creamy texture"],
            AccentColor = "#d1a53d"
        },

        // Shiseido
        new CosmeticItem
        {
            Id = 15,
            Name = "Ultimune Power Infusing Concentrate",
            Brand = "Shiseido",
            Category = "Serum",
            ShortDescription = "A powerful serum that strengthens skin's natural defenses.",
            FullDescription = "Shiseido's iconic serum, formulated with ImuGeneration Technology™ to boost the skin's own defenses against environmental stressors, resulting in visibly stronger, more radiant skin.",
            Highlights = ["50ml bottle", "Lightweight, silky texture", "Suitable for all skin types", "Use morning & night"],
            AccentColor = "#a13b52"
        },

        // Hada Labo
        new CosmeticItem
        {
            Id = 16,
            Name = "Gokujyun Premium Lotion",
            Brand = "Hada Labo",
            Category = "Toner",
            ShortDescription = "An ultra-hydrating lotion with five types of hyaluronic acid.",
            FullDescription = "A cult-favorite hydrating toner formulated with five types of hyaluronic acid to deliver deep, multi-layered moisture, leaving skin dewy and plump.",
            Highlights = ["170ml bottle", "5 types of hyaluronic acid", "Fragrance-free", "Suitable for sensitive skin"],
            AccentColor = "#4f8a9e"
        },

        // Kosé
        new CosmeticItem
        {
            Id = 17,
            Name = "Sekkisei Lotion",
            Brand = "Kosé",
            Category = "Toner",
            ShortDescription = "A brightening lotion rooted in traditional Japanese herbal ingredients.",
            FullDescription = "A long-standing Japanese skincare icon, this lotion uses a blend of traditional herbal extracts to hydrate, brighten, and even out skin tone over time.",
            Highlights = ["360ml bottle", "Herbal extract blend", "Brightening formula", "Suitable for most skin types"],
            AccentColor = "#5b7fa6"
        },

        // Senka
        new CosmeticItem
        {
            Id = 18,
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
