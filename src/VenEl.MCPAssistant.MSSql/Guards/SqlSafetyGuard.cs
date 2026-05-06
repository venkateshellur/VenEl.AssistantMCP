using System.Text.RegularExpressions;

namespace VenEl.MCPAssistant.MSSql.Guards;

/// <summary>
/// Inspects SQL text and identifies destructive statements.
/// Destructive operations: DELETE, TRUNCATE, DROP, UPDATE.
/// </summary>
public static partial class SqlSafetyGuard
{
    // ── Blocked leading statement keywords ───────────────────────────────────
    private static readonly string[] BlockedKeywords =
        ["DELETE", "TRUNCATE", "DROP", "UPDATE"];

    // ── Compiled regexes ─────────────────────────────────────────────────────

    /// <summary>Matches single-line SQL comments (<c>-- …</c>).</summary>
    [GeneratedRegex(@"--[^\r\n]*", RegexOptions.Compiled)]
    private static partial Regex SingleLineCommentRegex();

    /// <summary>Matches multi-line SQL comments (<c>/* … */</c>).</summary>
    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline | RegexOptions.Compiled)]
    private static partial Regex MultiLineCommentRegex();

    /// <summary>
    /// Matches a SQL string literal so it can be stripped before keyword inspection.
    /// </summary>
    [GeneratedRegex(@"'(?:''|[^'])*'", RegexOptions.Compiled)]
    private static partial Regex StringLiteralRegex();

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> if <paramref name="sql"/> contains at least one
    /// destructive statement (DELETE / TRUNCATE / DROP / UPDATE).
    /// </summary>
    public static bool IsDestructive(string sql)
        => TryGetBlockedKeyword(sql, out _);

    /// <summary>
    /// Returns the first blocked keyword found in <paramref name="sql"/>,
    /// or <c>null</c> if none is found.
    /// </summary>
    public static string? GetBlockedKeyword(string sql)
    {
        TryGetBlockedKeyword(sql, out var kw);
        return kw;
    }

    /// <summary>
    /// Attempts to find a destructive keyword in <paramref name="sql"/>.
    /// </summary>
    public static bool TryGetBlockedKeyword(string sql, out string? blockedKeyword)
    {
        blockedKeyword = null;

        if (string.IsNullOrWhiteSpace(sql))
            return false;

        var normalized = StripNoise(sql);

        // Split on statement separators (semicolons) and examine each statement.
        foreach (var rawStatement in normalized.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var statement = rawStatement.Trim();
            if (statement.Length == 0)
                continue;

            // Grab the first "word" of the statement.
            var firstToken = GetFirstToken(statement).ToUpperInvariant();

            foreach (var kw in BlockedKeywords)
            {
                if (firstToken == kw)
                {
                    blockedKeyword = kw;
                    return true;
                }
            }
        }

        return false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Strips comments and string literals so keyword detection is not fooled
    /// by content inside them.
    /// </summary>
    private static string StripNoise(string sql)
    {
        var s = MultiLineCommentRegex().Replace(sql, " ");
        s = SingleLineCommentRegex().Replace(s, " ");
        s = StringLiteralRegex().Replace(s, "''");   // replace literals with empty placeholder
        return s;
    }

    private static string GetFirstToken(string statement)
    {
        // Find the first run of word characters.
        var idx = 0;
        while (idx < statement.Length && !char.IsLetter(statement[idx]))
            idx++;

        var start = idx;
        while (idx < statement.Length && (char.IsLetterOrDigit(statement[idx]) || statement[idx] == '_'))
            idx++;

        return statement[start..idx];
    }
}
