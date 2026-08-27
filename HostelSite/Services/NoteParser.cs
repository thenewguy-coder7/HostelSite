using System;

namespace HostelSite.Services
{
    // Shared helper for reading the "Key: Value | Key2: Value2" convention
    // stored in AestheticRequest.Notes (there's no dedicated Room/Phone column
    // for aesthetic requests — those fields live inside this free-text blob).
    // Used by the Admin dashboard to pull Room/Phone out for their own columns.
    public static class NoteParser
    {
        public static string? Get(string? notes, string key)
        {
            if (string.IsNullOrEmpty(notes)) return null;
            var parts = notes.Split('|');
            foreach (var part in parts)
            {
                var kv = part.Trim().Split(':', 2);
                if (kv.Length == 2 && kv[0].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                    return kv[1].Trim();
            }
            return null;
        }
    }
}
