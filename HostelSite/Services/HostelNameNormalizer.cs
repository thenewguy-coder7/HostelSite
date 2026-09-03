using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace HostelSite.Services
{
    // Groups the many ways students actually type a traditional hall's name
    // into one canonical spelling, so the Admin dashboard's "bookings by
    // hall" grouping doesn't split one hall into several rows just because
    // of a typo or an alternate name.
    //
    // Real examples that prompted this (from actual bookings):
    //   "Katanga hall" / "University hall(Katanga)"   → Katanga Hall
    //   "Unity hall"   / "Unity halll"                → Unity Hall
    //   "Queen Elizabeth" / "Queen Elizabeth li hall"  → Queen Elizabeth Hall
    //
    // Rather than maintain a hand-typed list of every misspelling anyone
    // could produce, this matches on a single distinctive keyword unique to
    // each traditional hall — "katanga", "unity", "elizabeth", etc. — so it
    // survives typos (extra/missing letters), casing, punctuation, and
    // alternate official names (e.g. Katanga is also called "University
    // Hall") without needing an update every time a new variant shows up.
    //
    // Now that the logistics booking form (Views/Logistics/Index.cshtml)
    // offers these 6 halls as a dropdown, new bookings should already come
    // in with the canonical spelling — this normalizer mainly exists to
    // keep older, freely-typed bookings grouped correctly too.
    public static class HostelNameNormalizer
    {
        // (keyword to look for, canonical display name) — order doesn't
        // matter since the keywords don't overlap with each other.
        private static readonly (string Keyword, string Canonical)[] TraditionalHalls =
        {
            ("unity",       "Unity Hall"),
            ("independence","Independence Hall"),
            ("elizabeth",   "Queen Elizabeth Hall"),
            ("africa",      "Africa Hall"),
            ("katanga",     "Katanga Hall"),
            ("republic",    "Republic Hall"),
        };

        public static string Canonicalize(string? rawHostelName)
        {
            if (string.IsNullOrWhiteSpace(rawHostelName))
                return "Not specified";

            // Lowercased, letters-only version, purely for matching against
            // the keywords above — punctuation like "(Katanga)" or extra
            // spaces shouldn't stop a match.
            var lettersOnly = Regex.Replace(rawHostelName, "[^a-zA-Z]", "").ToLowerInvariant();

            foreach (var (keyword, canonical) in TraditionalHalls)
            {
                if (lettersOnly.Contains(keyword))
                    return canonical;
            }

            // Not one of the 6 traditional halls — e.g. a homestel or hostel
            // typed in under "Other…". Just tidy up spacing/casing so at
            // least "evandy hostel" and "Evandy Hostel" still group as one.
            var collapsedWhitespace = Regex.Replace(rawHostelName.Trim(), @"\s+", " ");
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(collapsedWhitespace.ToLowerInvariant());
        }
    }
}
