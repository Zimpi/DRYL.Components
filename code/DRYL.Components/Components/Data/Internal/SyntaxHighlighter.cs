using System.Text;
using System.Text.Encodings.Web;

namespace DRYL.Components.Internal;

/// <summary>
/// A small, dependency-free, server-side syntax highlighter. It lexes source
/// code into colored <c>&lt;span class="tok-…"&gt;</c> fragments using only the
/// DRYL palette — no JS, no npm, no NuGet (CLAUDE.md rules 2.1 / 2.8).
/// <para>
/// This is lexical (token-level) highlighting, the same granularity browser
/// highlighters use for display — not a semantic/AST analysis.
/// </para>
/// <para>
/// Security: callers render the output as a <c>MarkupString</c>, so every
/// token's text is HTML-encoded here before being wrapped. Model-authored code
/// stays as safe against injection as Razor's own auto-encoding.
/// </para>
/// </summary>
internal static class SyntaxHighlighter
{
    private enum Tok { Plain, Keyword, Type, String, Number, Comment, Punct }

    /// <summary>
    /// Highlight <paramref name="code"/> for the given <paramref name="language"/>.
    /// Unknown or empty languages fall back to plain (HTML-encoded) text.
    /// </summary>
    public static string Highlight(string? code, string? language)
    {
        if (string.IsNullOrEmpty(code))
            return string.Empty;

        var lang = Normalize(language);

        if (lang is "html" or "xml")
            return HighlightMarkup(code!);

        var spec = SpecFor(lang);
        if (spec is null)
            return Encode(code!); // unknown language → safe plain text

        return HighlightGeneric(code!, spec);
    }

    // ── Generic C-family / scripting lexer ──────────────────────────────────

    private static string HighlightGeneric(string s, LangSpec spec)
    {
        var sb = new StringBuilder(s.Length + 64);
        var plain = new StringBuilder();
        int i = 0, n = s.Length;

        void FlushPlain()
        {
            if (plain.Length == 0) return;
            sb.Append(Encode(plain.ToString()));
            plain.Clear();
        }

        while (i < n)
        {
            char c = s[i];

            // Comments
            if (spec.LineSlash && c == '/' && i + 1 < n && s[i + 1] == '/')
            { FlushPlain(); i = Emit(sb, s, i, EndOfLine(s, i), Tok.Comment); continue; }

            if (spec.LineDashDash && c == '-' && i + 1 < n && s[i + 1] == '-')
            { FlushPlain(); i = Emit(sb, s, i, EndOfLine(s, i), Tok.Comment); continue; }

            if (spec.LineHash && c == '#')
            { FlushPlain(); i = Emit(sb, s, i, EndOfLine(s, i), Tok.Comment); continue; }

            if (spec.BlockSlashStar && c == '/' && i + 1 < n && s[i + 1] == '*')
            {
                int end = s.IndexOf("*/", i + 2, StringComparison.Ordinal);
                end = end < 0 ? n : end + 2;
                FlushPlain(); i = Emit(sb, s, i, end, Tok.Comment); continue;
            }

            // Strings
            if (c == '"' || (spec.SingleQuoteString && c == '\'') || (spec.Backtick && c == '`'))
            { FlushPlain(); i = Emit(sb, s, i, ScanString(s, i, spec), Tok.String); continue; }

            // Numbers
            if (char.IsDigit(c) || (c == '.' && i + 1 < n && char.IsDigit(s[i + 1])))
            { FlushPlain(); i = Emit(sb, s, i, ScanNumber(s, i), Tok.Number); continue; }

            // Identifiers / keywords / types
            if (IsIdentStart(c))
            {
                int start = i;
                i++;
                while (i < n && IsIdentPart(s[i])) i++;
                var word = s.Substring(start, i - start);
                var kind = Classify(word, spec);
                if (kind == Tok.Plain) plain.Append(word);
                else { FlushPlain(); Wrap(sb, word, kind); }
                continue;
            }

            // Whitespace stays plain
            if (char.IsWhiteSpace(c)) { plain.Append(c); i++; continue; }

            // Punctuation / operators — group a run for fewer spans
            int pstart = i;
            while (i < n && IsPunct(s[i]) && !StartsToken(s, i, spec)) i++;
            if (i == pstart) i++; // safety: always advance
            FlushPlain(); Wrap(sb, s.Substring(pstart, i - pstart), Tok.Punct);
        }

        FlushPlain();
        return sb.ToString();
    }

    private static Tok Classify(string word, LangSpec spec)
    {
        var key = spec.CaseInsensitiveKeywords ? word.ToLowerInvariant() : word;
        if (spec.Keywords.Contains(key)) return Tok.Keyword;
        if (spec.CapitalizedIsType && word.Length > 0 && char.IsUpper(word[0])) return Tok.Type;
        return Tok.Plain;
    }

    // ── HTML / XML lexer ────────────────────────────────────────────────────

    private static string HighlightMarkup(string s)
    {
        var sb = new StringBuilder(s.Length + 64);
        var plain = new StringBuilder();
        int i = 0, n = s.Length;

        void FlushPlain()
        {
            if (plain.Length == 0) return;
            sb.Append(Encode(plain.ToString()));
            plain.Clear();
        }

        while (i < n)
        {
            char c = s[i];

            if (c == '<' && i + 3 < n && s[i + 1] == '!' && s[i + 2] == '-' && s[i + 3] == '-')
            {
                int end = s.IndexOf("-->", i + 4, StringComparison.Ordinal);
                end = end < 0 ? n : end + 3;
                FlushPlain(); i = Emit(sb, s, i, end, Tok.Comment); continue;
            }

            if (c == '<')
            {
                FlushPlain();
                int j = i + 1;
                if (j < n && s[j] == '/') j++;
                Wrap(sb, s.Substring(i, j - i), Tok.Punct); // < or </
                i = j;
                // tag name
                int tstart = i;
                while (i < n && (char.IsLetterOrDigit(s[i]) || s[i] is '-' or ':' or '_')) i++;
                if (i > tstart) Wrap(sb, s.Substring(tstart, i - tstart), Tok.Type);
                continue;
            }

            if (c is '>' or '=' or '/')
            { FlushPlain(); Wrap(sb, c.ToString(), Tok.Punct); i++; continue; }

            if (c == '"' || c == '\'')
            { FlushPlain(); i = Emit(sb, s, i, ScanString(s, i, MarkupSpec), Tok.String); continue; }

            plain.Append(c); i++;
        }

        FlushPlain();
        return sb.ToString();
    }

    // ── Scanners ────────────────────────────────────────────────────────────

    private static int ScanString(string s, int i, LangSpec spec)
    {
        char quote = s[i];
        int n = s.Length;

        // Triple-quoted (python """ / ''')
        if (spec.TripleQuote && i + 2 < n && s[i + 1] == quote && s[i + 2] == quote)
        {
            int k = i + 3;
            while (k + 2 < n && !(s[k] == quote && s[k + 1] == quote && s[k + 2] == quote)) k++;
            return k + 2 < n ? k + 3 : n;
        }

        i++; // opening quote
        while (i < n)
        {
            char c = s[i];
            if (c == '\\' && spec.StringEscapes && i + 1 < n) { i += 2; continue; }
            if (c == quote) return i + 1;
            if (c == '\n') return i; // unterminated (e.g. mid-stream) → stop at line end
            i++;
        }
        return n; // unterminated at EOF
    }

    private static int ScanNumber(string s, int i)
    {
        int n = s.Length;
        if (s[i] == '0' && i + 1 < n && (s[i + 1] is 'x' or 'X'))
        {
            i += 2;
            while (i < n && (Uri.IsHexDigit(s[i]) || s[i] == '_')) i++;
            return i;
        }
        while (i < n && (char.IsDigit(s[i]) || s[i] is '.' or '_' or 'e' or 'E' or 'f' or 'F' or 'd' or 'D' or 'm' or 'M' or 'L' or 'l')) i++;
        return i;
    }

    private static int EndOfLine(string s, int i)
    {
        int nl = s.IndexOf('\n', i);
        return nl < 0 ? s.Length : nl;
    }

    private static bool StartsToken(string s, int i, LangSpec spec)
    {
        char c = s[i];
        if (c == '"' || (spec.SingleQuoteString && c == '\'') || (spec.Backtick && c == '`')) return true;
        if (spec.LineSlash && c == '/' && i + 1 < s.Length && s[i + 1] == '/') return true;
        if (spec.BlockSlashStar && c == '/' && i + 1 < s.Length && s[i + 1] == '*') return true;
        if (spec.LineDashDash && c == '-' && i + 1 < s.Length && s[i + 1] == '-') return true;
        if (spec.LineHash && c == '#') return true;
        return false;
    }

    // ── Emit helpers ────────────────────────────────────────────────────────

    private static int Emit(StringBuilder sb, string s, int start, int end, Tok kind)
    {
        Wrap(sb, s.Substring(start, end - start), kind);
        return end;
    }

    private static void Wrap(StringBuilder sb, string text, Tok kind)
    {
        var cls = kind switch
        {
            Tok.Keyword => "tok-keyword",
            Tok.Type => "tok-type",
            Tok.String => "tok-string",
            Tok.Number => "tok-number",
            Tok.Comment => "tok-comment",
            Tok.Punct => "tok-punct",
            _ => null,
        };
        if (cls is null) { sb.Append(Encode(text)); return; }
        sb.Append("<span class=\"").Append(cls).Append("\">").Append(Encode(text)).Append("</span>");
    }

    private static string Encode(string text) => HtmlEncoder.Default.Encode(text);

    private static bool IsIdentStart(char c) => char.IsLetter(c) || c == '_' || c == '$';
    private static bool IsIdentPart(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '$';
    private static bool IsPunct(char c) => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c) && c != '_';

    // ── Language specs ──────────────────────────────────────────────────────

    private sealed class LangSpec
    {
        public required HashSet<string> Keywords;
        public bool LineSlash;
        public bool LineHash;
        public bool LineDashDash;
        public bool BlockSlashStar;
        public bool Backtick;
        public bool SingleQuoteString = true;
        public bool TripleQuote;
        public bool StringEscapes = true;
        public bool CaseInsensitiveKeywords;
        public bool CapitalizedIsType;
    }

    private static readonly LangSpec MarkupSpec = new()
    {
        Keywords = new(),
        StringEscapes = false,
    };

    private static string Normalize(string? lang) => (lang ?? "").Trim().ToLowerInvariant() switch
    {
        "cs" or "c#" or "csharp" or "dotnet" => "csharp",
        "js" or "jsx" or "javascript" or "node" => "javascript",
        "ts" or "tsx" or "typescript" => "typescript",
        "json" or "json5" or "jsonc" => "json",
        "html" or "htm" or "xhtml" or "razor" or "cshtml" => "html",
        "xml" or "svg" or "xaml" => "xml",
        "css" or "scss" or "less" => "css",
        "sh" or "bash" or "shell" or "zsh" or "console" => "bash",
        "sql" or "postgres" or "postgresql" or "mysql" => "sql",
        "py" or "python" => "python",
        _ => "",
    };

    private static LangSpec? SpecFor(string lang) => lang switch
    {
        "csharp" => CSharp,
        "javascript" or "typescript" => JsTs,
        "json" => Json,
        "css" => Css,
        "bash" => Bash,
        "sql" => Sql,
        "python" => Python,
        _ => null,
    };

    private static readonly LangSpec CSharp = new()
    {
        LineSlash = true, BlockSlashStar = true, CapitalizedIsType = true,
        Keywords = Words(
            "abstract as async await base bool break byte case catch char checked class const continue " +
            "decimal default delegate do double else enum event explicit extern false finally fixed float " +
            "for foreach goto if implicit in int interface internal is lock long namespace new null object " +
            "operator out override params private protected public readonly record ref return sbyte sealed " +
            "short sizeof stackalloc static string struct switch this throw true try typeof uint ulong " +
            "unchecked unsafe ushort using var virtual void volatile while yield get set value nameof when with init global"),
    };

    private static readonly LangSpec JsTs = new()
    {
        LineSlash = true, BlockSlashStar = true, Backtick = true, CapitalizedIsType = true,
        Keywords = Words(
            "abstract any as async await boolean break case catch class const continue debugger declare " +
            "default delete do else enum export extends false finally for from function get if implements " +
            "import in instanceof interface let namespace never new null number object of private protected " +
            "public readonly return set static string super switch this throw true try type typeof undefined " +
            "var void while with yield unknown keyof"),
    };

    private static readonly LangSpec Json = new()
    {
        SingleQuoteString = false,
        Keywords = Words("true false null"),
    };

    private static readonly LangSpec Css = new()
    {
        BlockSlashStar = true,
        Keywords = Words(
            "important inherit initial unset none auto flex grid block inline absolute relative fixed sticky " +
            "var calc rgba rgb hsl hsla linear-gradient radial-gradient and or not all screen print media supports keyframes import"),
    };

    private static readonly LangSpec Bash = new()
    {
        LineHash = true,
        Keywords = Words(
            "if then else elif fi for while until do done case esac function in select return break continue " +
            "echo cd export local readonly set unset source alias sudo true false test"),
    };

    private static readonly LangSpec Sql = new()
    {
        LineDashDash = true, BlockSlashStar = true, SingleQuoteString = true, CaseInsensitiveKeywords = true,
        Keywords = Words(
            "select from where insert into values update set delete create table alter drop add column " +
            "primary key foreign references index view join inner left right outer on group by order having " +
            "limit offset distinct as and or not null is in like between exists union all count sum avg min " +
            "max case when then else end asc desc default constraint unique check cascade returning with"),
    };

    private static readonly LangSpec Python = new()
    {
        LineHash = true, TripleQuote = true, CapitalizedIsType = true,
        Keywords = Words(
            "and as assert async await break class continue def del elif else except False finally for from " +
            "global if import in is lambda None nonlocal not or pass raise return True try while with yield " +
            "match case self print"),
    };

    private static HashSet<string> Words(string s) =>
        new(s.Split(' ', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
}
