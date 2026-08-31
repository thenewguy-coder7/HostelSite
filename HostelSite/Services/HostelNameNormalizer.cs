using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace HostelSite.Services
{
    // Students type their current hostel freely at checkout, so the same real
    // hall often ends up stored under several different spellings —
    // "Independence hall" vs "Independence Hall", "Katanga" vs "Katanga hall",
    // "Queens hall" vs "Queens Elizabeth hall", "wilkado hostel" vs
    // "Wilkado Hostel". The Admin Dashboard groups pickups by hostel so staff
    // can clear one building at a time — if the same hall shows up as two
    // separate groups because of spelling differences, that grouping breaks.
    //
    // This maps known variants to one canonical display name used for both
    // grouping and display. To merge a new variant you spot in the
    // dashboard, just add a line to KnownAliases below (the key is matched
    // case-insensitively and with extra spaces collapsed, so you only need
    // to add each distinct wording once).
    public static class HostelNameNormalizer
    {
        private static readonly Dictionary<string, string> KnownAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["independence hall"] = "Independence Hall",
            ["katanga"] = "Katanga Hall",
            ["katanga hall"] = "Katanga Hall",
            ["queens hall"] = "Queens Elizabeth Hall",
            ["queens elizabeth hall"] = "Queens Elizabeth Hall",
            ["queen's hall"] = "Queens Elizabeth Hall",
            ["wilkado"] = "Wilkado Hostel",
            ["wilkado hostel"] = "Wilkado Hostel",
            ["africa hall"] = "Africa Hall",
            ["republic hall"] = "Republic Hall",
            ["unity hall"] = "Unity Hall",
            ["guss hostel"] = "Guss Hostels",
            ["guss hostels"] = "Guss Hostels",
        };

        public static string Canonicalize(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "Hostel not specified";

            var trimmed = Regex.Replace(raw.Trim(), @"\s+", " ");

            if (KnownAliases.TryGetValue(trimmed, out var canonical))
                return canonical;

            // Not a known alias yet — fall back to Title Case so at least
            // plain case differences ("wilkado hostel" vs "Wilkado Hostel")
            // still merge into one group instead of splitting.
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(trimmed.ToLowerInvariant());
        }
    }
}
