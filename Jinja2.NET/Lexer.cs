using System.Diagnostics;
using System.Text.RegularExpressions;

using Jinja2.NET.Interfaces;

namespace Jinja2.NET;

public class Lexer : ILexer
{
    private readonly LexerConfig _config;

    private readonly string _source;

    private readonly Regex _tokenRegex;

    private int _column = 1;

    private int _line = 1;

    public Lexer(string source, LexerConfig? config = null)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        // Normalize all line endings to \n for Jinja2 compatibility
        _source = _source.Replace("\r\n", "\n")
            .Replace("\r", "\n");
        _config = config ?? new LexerConfig();
        _tokenRegex = new Regex(_config.TokenPattern, RegexOptions.Compiled);
    }

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();
        var position = 0;

        while (position < _source.Length)
        {
            position = ProcessNextSection(tokens, position);
        }

        tokens.Add(CreateToken(ETokenType.EOF, ""));
        return tokens;
    }

    public List<string> Validate()
    {
        var errors = new List<string>();
        try
        {
            Tokenize();
        }
        catch (InvalidOperationException ex)
        {
            errors.Add(ex.Message);
        }

        return errors;
    }

    private void AddDelimiterToken(List<Token> tokens, string? delimiter)
    {
        var cleanDelimiter = delimiter?.Replace("-", "");
        var tokenType = GetDelimiterTokenType(cleanDelimiter);

        // Capture position before creating token
        var tokenLine = _line;
        var tokenColumn = _column;

        if (delimiter != null)
        {
            var token = new Token(
                tokenType,
                delimiter,
                tokenLine,
                tokenColumn,
                delimiter.EndsWith("-"),
                delimiter.StartsWith("-")
            );
            tokens.Add(token);
        }

        UpdatePosition(delimiter);
    }

    private void AddEndDelimiterToken(List<Token> tokens, string endDelimiter, bool trimRight)
    {
        var cleanDelimiter = endDelimiter.Replace("-", "");
        var tokenType = GetDelimiterTokenType(cleanDelimiter);

        // Capture position before creating token
        var tokenLine = _line;
        var tokenColumn = _column;

        var token = new Token(
            tokenType,
            endDelimiter,
            tokenLine,
            tokenColumn,
            endDelimiter.EndsWith("-"),
            trimRight
        );
        tokens.Add(token);
        UpdatePosition(endDelimiter);
    }

    private void AddTextToken(List<Token> tokens, string text)
    {
        if (text.Length > 0)
        {
            // For text tokens, we want to report the position of the first visible character
            // If the text starts with newlines, we need to account for them
            var tokenLine = _line;
            var tokenColumn = _column;

            // Find the position after any leading newlines
            var leadingNewlines = 0;
            for (var i = 0; i < text.Length && text[i] == '\n'; i++)
            {
                leadingNewlines++;
            }

            if (leadingNewlines > 0)
            {
                tokenLine += leadingNewlines;
                tokenColumn = 1;
            }

            // Create token with adjusted position
            var token = new Token(ETokenType.Text, text, tokenLine, tokenColumn);
            tokens.Add(token);

            // Then update position
            UpdatePosition(text);
        }
    }

    private void AddTextTokenWithLstrip(List<Token> tokens, string text, bool shouldLstrip)
    {
        if (shouldLstrip && _config.LstripBlocks)
        {
            var trimmed = text.Length - text.TrimStart().Length;
            var processedText = text.TrimStart();

            // Only add the token if there's meaningful content
            if (processedText.Length > 0 || tokens.Count == 0)
            {
                // Advance column for trimmed whitespace before creating the token
                if (trimmed > 0)
                {
                    UpdatePosition(text.Substring(0, trimmed));
                }

                // For text tokens, we want to report the position of the first visible character
                var tokenLine = _line;
                var tokenColumn = _column;

                // Find the position after any leading newlines in processed text
                var leadingNewlines = 0;
                for (var i = 0; i < processedText.Length && processedText[i] == '\n'; i++)
                {
                    leadingNewlines++;
                }

                if (leadingNewlines > 0)
                {
                    tokenLine += leadingNewlines;
                    tokenColumn = 1;
                }

                var token = new Token(ETokenType.Text, processedText, tokenLine, tokenColumn);
                tokens.Add(token);
                UpdatePosition(processedText);
            }
            else
            {
                UpdatePosition(text);
            }
        }
        else
        {
            if (text.Length > 0)
            {
                // For text tokens, we want to report the position of the first visible character
                var tokenLine = _line;
                var tokenColumn = _column;

                // Find the position after any leading newlines
                var leadingNewlines = 0;
                for (var i = 0; i < text.Length && text[i] == '\n'; i++)
                {
                    leadingNewlines++;
                }

                if (leadingNewlines > 0)
                {
                    tokenLine += leadingNewlines;
                    tokenColumn = 1;
                }

                var token = new Token(ETokenType.Text, text, tokenLine, tokenColumn);
                tokens.Add(token);
                UpdatePosition(text);
            }
        }
    }

    private Token CreateToken(ETokenType type, string value)
    {
        return new Token(type, value, _line, _column);
    }

    private InvalidOperationException CreateUnclosedTagException(int position)
    {
        var snippetStart = Math.Max(0, position - 10);
        var snippetLength = Math.Min(20, _source.Length - snippetStart);
        var snippet = _source.Substring(snippetStart, snippetLength);

        return new InvalidOperationException(
            $"Unclosed tag at line {_line}, column {_column} (position {position}): ...{snippet}...");
    }

    private ETokenType DetermineTokenType(Match match)
    {
        if (string.IsNullOrWhiteSpace(match.Value))
        {
            return ETokenType.Text; // Or ETokenType.Whitespace if you have it
        }

        if (match.Groups[2].Success)
        {
            return ETokenType.Identifier;
        }

        if (match.Groups[3].Success)
        {
            return ETokenType.Number;
        }

        if (match.Groups[4].Success)
        {
            return ETokenType.String;
        }

        if (match.Groups[1].Success)
        {
            var value = match.Value;

            // Handle known delimiters
            if (IsKnownDelimiter(value))
            {
                return GetDelimiterTokenType(value);
            }

            // Handle identifiers
            if (IsValidIdentifierStart(value[0]))
            {
                return ETokenType.Identifier;
            }
        }

        return GetOperatorTokenType(match.Value);
    }

    private string FindMatchingEndDelimiter(
        int startPosition,
        string startDelimiter,
        out bool trimRight)
    {
        trimRight = false;

        var candidateDelimiters = _config.EndDelimiters.ContainsKey(startDelimiter)
                                      ? _config.EndDelimiters[startDelimiter]
                                      : Array.Empty<string>();

        if (candidateDelimiters.Length == 0)
            throw CreateUnclosedTagException(startPosition);

        // Scan the source from startPosition and ignore delimiters that appear inside
        // single- or double-quoted string literals. This prevents prematurely ending
        // a tag when the content contains '}}' or '%}' inside strings.
        var nearestPosition = -1;
        string? nearestDelimiter = null;

        var i = startPosition;
        var len = _source.Length;
        // For comment blocks ({# ... #}), string quoting inside the comment
        // should not affect finding the end delimiter. Only skip string
        // contents for variable/block tags ({{ ... }} / {% ... %}).
        var isComment = startDelimiter.Replace("-", "") == "{#";
        var inSingle = false;
        var inDouble = false;
        while (i < len)
        {
            var c = _source[i];

            // handle escape (only relevant when we're tracking string quoting)
            if (!isComment)
            {
                if (c == '\\')
                {
                    // skip escaped char (backslash + next char)
                    i += 2;
                    continue;
                }

                // If we encounter a quote, skip the entire quoted string (respect escapes)
                if (c == '\'')
                {
                    i++; // move past opening single quote
                    while (i < len)
                    {
                        if (_source[i] == '\\')
                        {
                            i += 2; // skip escaped char inside string
                            continue;
                        }

                        if (_source[i] == '\'')
                        {
                            i++; // consume closing quote
                            break;
                        }

                        i++;
                    }

                    continue;
                }

                if (c == '"')
                {
                    i++; // move past opening double quote
                    while (i < len)
                    {
                        if (_source[i] == '\\')
                        {
                            i += 2; // skip escaped char inside string
                            continue;
                        }

                        if (_source[i] == '"')
                        {
                            i++; // consume closing quote
                            break;
                        }

                        i++;
                    }

                    continue;
                }
            }

            if (isComment || (!inSingle && !inDouble))
            {
                foreach (var endDelimiter in candidateDelimiters)
                {
                    if (i + endDelimiter.Length <= len &&
                        _source.AsSpan(i, endDelimiter.Length).SequenceEqual(endDelimiter.AsSpan()))
                    {
                        // record nearest and return
                        nearestPosition = i;
                        nearestDelimiter = endDelimiter;
                        trimRight = nearestDelimiter.StartsWith("-") || nearestDelimiter.EndsWith("-");
                        return nearestDelimiter;
                    }
                }
            }

            i++;
        }

        throw CreateUnclosedTagException(startPosition);
    }

    private int FindNextDelimiter(int startPosition, out string? delimiter)
    {
        var minIndex = -1;
        delimiter = null;

        foreach (var delim in _config.StartDelimiters)
        {
            var index = _source.IndexOf(delim, startPosition, StringComparison.Ordinal);
            if (index != -1 && (minIndex == -1 || index < minIndex))
            {
                minIndex = index;
                delimiter = delim;
            }
        }

        return minIndex;
    }

    private ETokenType GetDelimiterTokenType(string? delimiter)
    {
        if (delimiter != null)
        {
            var cleanDelimiter = delimiter.Replace("-", "");
            return cleanDelimiter switch
                {
                    "{{" => ETokenType.VariableStart,
                    "}}" => ETokenType.VariableEnd,
                    "{%" => ETokenType.BlockStart,
                    "%}" => ETokenType.BlockEnd,
                    "{#" => ETokenType.CommentStart,
                    "#}" => ETokenType.CommentEnd,
                    _ => throw new ArgumentException($"Unknown delimiter: {delimiter}")
                };
        }

        return ETokenType.None;
    }

    private static ETokenType GetOperatorTokenType(string value)
    {
        return value switch
            {
                "|" => ETokenType.Pipe,
                "." => ETokenType.Dot,
                "(" => ETokenType.LeftParen,
                ")" => ETokenType.RightParen,
                "[" => ETokenType.LeftBracket,
                "]" => ETokenType.RightBracket,
                "{" => ETokenType.LeftBrace,
                "}" => ETokenType.RightBrace,
                "," => ETokenType.Comma,
                ":" => ETokenType.Colon,
                "=" => ETokenType.Equals,
                "+" => ETokenType.Plus,
                "-" => ETokenType.Minus,
                "*" => ETokenType.Multiply,
                "/" => ETokenType.Divide,
                _ => ETokenType.Operator
            };
    }

    private int HandlePostDelimiterWhitespace(
        List<Token> tokens,
        int position,
        string startDelimiter,
        string endDelimiter,
        bool trimRight)
    {
        if (_config.LstripBlocks && IsBlockDelimiter(startDelimiter))
        {
            var remainingText = _source.AsSpan(position).ToString();
            var trimmedText = remainingText.TrimStart();
            var trimmedLength = remainingText.Length - trimmedText.Length;

            if (trimmedLength > 0)
            {
                UpdatePosition(remainingText.Substring(0, trimmedLength));
                position += trimmedLength;
            }

            // Process the next text segment
            var nextDelimPos = FindNextDelimiter(position, out _);
            var textLength = (nextDelimPos == -1 ? _source.Length : nextDelimPos) - position;
            if (textLength > 0)
            {
                var textAfterBlock = _source.Substring(position, textLength);
                AddTextToken(tokens, textAfterBlock);
                position += textLength;
            }
        }
        else if (trimRight && endDelimiter.EndsWith("-"))
        {
            var remainingText = _source.AsSpan(position).ToString();
            var trimmedLength = 0;
            for (var i = 0; i < remainingText.Length; i++)
            {
                if (remainingText[i] == ' ' || remainingText[i] == '\t')
                {
                    trimmedLength++;
                }
                else
                {
                    break;
                }
            }

            if (trimmedLength > 0)
            {
                var trimmedWhitespace = remainingText.Substring(0, trimmedLength);
                UpdatePosition(trimmedWhitespace);
                position += trimmedLength;
            }
        }

        return position;
    }

    private bool IsBlockDelimiter(string? delimiter)
    {
        if (delimiter != null)
        {
            var cleanDelimiter = delimiter.Replace("-", "");
            return cleanDelimiter == "{%";
        }

        return false;
    }

    private static bool IsKnownDelimiter(string value)
    {
        return value == "{{" || value == "}}" || value == "{%" ||
               value == "%}" || value == "{#" || value == "#}";
    }

    private bool IsRawBlockStart(
        int delimiterPos,
        string? startDelimiter,
        out int rawBlockStart,
        out int rawBlockEnd)
    {
        rawBlockStart = -1;
        rawBlockEnd = -1;

        if (!IsBlockDelimiter(startDelimiter))
        {
            return false;
        }

        // Look for 'raw' after the block start
        var afterStart = delimiterPos + startDelimiter.Length;
        var match = Regex.Match(_source.Substring(afterStart), @"^\s*raw\b");
        if (!match.Success)
        {
            return false;
        }

        // Find the end of the block tag
        var blockEndPos = _source.IndexOf(
            "%}",
            afterStart + match.Length,
            StringComparison.Ordinal);
        if (blockEndPos == -1)
        {
            return false;
        }

        rawBlockStart = blockEndPos + 2;

        // Find {% endraw %} (allow whitespace and dashes)
        var endRawRegex = new Regex(@"\{%-?\s*endraw\s*-?%\}", RegexOptions.Compiled);
        var endRawMatch = endRawRegex.Match(_source, rawBlockStart);
        if (!endRawMatch.Success)
        {
            return false;
        }

        rawBlockEnd = endRawMatch.Index;
        return true;
    }

    private static bool IsValidIdentifierStart(char c)
    {
        return char.IsLetter(c) || c == '_';
    }

    private void ProcessBlockContent(List<Token> tokens, int startPos, int endPos, string content)
    {
        var trimmedContent = content.Trim();
        if (trimmedContent.StartsWith("if ") || trimmedContent == "endif" ||
            Regex.IsMatch(trimmedContent, @"^\w+\b"))
        {
            ProcessExpressionTokens(tokens, startPos, endPos);
        }
        else
        {
            AddTextToken(tokens, content);
        }
    }

    private int ProcessDelimiterAndContent(
        List<Token> tokens,
        int delimiterPos,
        string? startDelimiter)
    {
        // Add start delimiter token
        if (startDelimiter != null)
        {
            AddDelimiterToken(tokens, startDelimiter);
            var position = delimiterPos + startDelimiter.Length;

            // Find and process content between delimiters
            var endDelimiter = FindMatchingEndDelimiter(
                position,
                startDelimiter,
                out var trimRight);
            var endDelimiterPos = _source.IndexOf(endDelimiter, position, StringComparison.Ordinal);

            if (endDelimiterPos == -1)
            {
                throw CreateUnclosedTagException(position);
            }

            // Handle block content
            var content = _source.Substring(position, endDelimiterPos - position);
            if (IsBlockDelimiter(startDelimiter))
            {
                ProcessBlockContent(tokens, position, endDelimiterPos, content);
            }
            else
            {
                ProcessExpressionTokens(tokens, position, endDelimiterPos);
            }

            // Add end delimiter token
            AddEndDelimiterToken(tokens, endDelimiter, trimRight);

            // Move position past the end delimiter
            var newPosition = endDelimiterPos + endDelimiter.Length;

            // Handle whitespace after block tags
            return HandlePostDelimiterWhitespace(
                tokens,
                newPosition,
                startDelimiter,
                endDelimiter,
                trimRight);
        }

        return -1;
    }

    private void ProcessExpressionTokens(List<Token> tokens, int startPos, int endPos)
    {
        var expression = _source.AsSpan(startPos, endPos - startPos).ToString();
        var currentPos = 0;

        foreach (Match match in _tokenRegex.Matches(expression))
        {
            if (match.Index > currentPos)
            {
                var skipped = expression.Substring(currentPos, match.Index - currentPos);
                UpdatePosition(skipped);
            }

            var tokenType = DetermineTokenType(match);
            var tokenValue = match.Value;

            // Unescape string literals - no
            //if (tokenType == ETokenType.String)
            //{
            //    // Remove surrounding quotes and unescape
            //    tokenValue = Regex.Unescape(tokenValue[1..^1]); // Strip quotes (e.g., "\\n" -> \n)
            //}

            if (!(tokenType == ETokenType.Text && string.IsNullOrWhiteSpace(tokenValue)))
            {
                var tokenLine = _line;
                var tokenColumn = _column;
                tokens.Add(new Token(tokenType, tokenValue, tokenLine, tokenColumn));
            }

            UpdatePosition(match.Value);
            currentPos = match.Index + match.Length;
        }

        if (currentPos < expression.Length)
        {
            var skipped = expression.Substring(currentPos);
            UpdatePosition(skipped);
        }
    }

    private int ProcessNextSection(List<Token> tokens, int position)
    {
        // Find the next delimiter
        var nextDelimiterPos = FindNextDelimiter(position, out var startDelimiter);

        if (nextDelimiterPos == -1)
        {
            // No more delimiters; process remaining text or expression
            ProcessRemainingText(tokens, position);
            return _source.Length;
        }

        // Process text before the delimiter
        if (nextDelimiterPos > position)
        {
            ProcessTextBeforeDelimiter(tokens, position, nextDelimiterPos, startDelimiter);
        }

        // Check for raw block
        if (IsRawBlockStart(
                nextDelimiterPos,
                startDelimiter,
                out var rawBlockStart,
                out var rawBlockEnd))
        {
            return ProcessRawBlock(
                tokens,
                nextDelimiterPos,
                startDelimiter,
                rawBlockStart,
                rawBlockEnd);
        }

        // Process the delimiter and its content
        return ProcessDelimiterAndContent(tokens, nextDelimiterPos, startDelimiter);
    }

    private int ProcessRawBlock(
        List<Token> tokens,
        int delimiterPos,
        string? startDelimiter,
        int rawBlockStart,
        int rawBlockEnd)
    {
        // Add start delimiter token
        AddDelimiterToken(tokens, startDelimiter);

        // Add 'raw' identifier token
        Debug.Assert(startDelimiter != null, nameof(startDelimiter) + " != null");
        var afterStart = delimiterPos + startDelimiter.Length;
        var match = Regex.Match(_source.Substring(afterStart), @"^\s*raw\b");
        if (match.Success)
        {
            var tokenLine = _line;
            var tokenColumn = _column;
            tokens.Add(new Token(ETokenType.Identifier, "raw", tokenLine, tokenColumn));
            UpdatePosition(match.Value);
        }

        // Find the end of the block tag
        var blockEndPos = _source.IndexOf(
            "%}",
            afterStart + match.Length,
            StringComparison.Ordinal);
        var blockEndDelimiter = _source.Substring(blockEndPos, 2);
        AddEndDelimiterToken(tokens, blockEndDelimiter, false);

        // Emit a single Text token for the raw content
        var rawContent = _source.Substring(rawBlockStart, rawBlockEnd - rawBlockStart);
        AddTextToken(tokens, rawContent);

        // Add endraw block start token
        var endRawRegex = new Regex(@"\{%-?\s*endraw\s*-?%\}", RegexOptions.Compiled);
        var endRawMatch = endRawRegex.Match(_source, rawBlockEnd);
        var endRawStart = endRawMatch.Index;
        var endRawStartDelimiter = _source.Substring(
            endRawStart,
            _source[endRawStart + 2] == '-' ? 3 : 2);
        AddDelimiterToken(tokens, endRawStartDelimiter);

        // Add 'endraw' identifier token
        var endrawIdentMatch =
            Regex.Match(
                _source.Substring(endRawStart + endRawStartDelimiter.Length),
                @"^\s*endraw\b");
        if (endrawIdentMatch.Success)
        {
            var tokenLine = _line;
            var tokenColumn = _column;
            tokens.Add(new Token(ETokenType.Identifier, "endraw", tokenLine, tokenColumn));
            UpdatePosition(endrawIdentMatch.Value);
        }

        // Add end block delimiter token
        var endBlockPos = endRawStart + endRawStartDelimiter.Length + endrawIdentMatch.Length;
        var endDelimiterRegex = new Regex(@"-?%\}", RegexOptions.Compiled);
        var endDelimiterMatch = endDelimiterRegex.Match(_source, endBlockPos);
        if (!endDelimiterMatch.Success)
        {
            throw new InvalidOperationException($"Invalid end delimiter at position {endBlockPos}");
        }

        var endBlockDelimiter = endDelimiterMatch.Value;
        AddEndDelimiterToken(tokens, endBlockDelimiter, false);

        // Move position past the entire endraw block
        return endRawMatch.Index + endRawMatch.Length; // Use the full match length
    }

    private void ProcessRemainingText(List<Token> tokens, int position)
    {
        var remainingText = _source.AsSpan(position).ToString();
        if (Regex.IsMatch(remainingText, @"^(""[^""]*""|'[^']*')$"))
        {
            ProcessExpressionTokens(tokens, position, _source.Length);
        }
        else if (position < _source.Length)
        {
            AddTextToken(tokens, remainingText);
        }
    }

    private void ProcessTextBeforeDelimiter(
        List<Token> tokens,
        int startPos,
        int endPos,
        string? startDelimiter)
    {
        var textBeforeDelimiter = _source.AsSpan(startPos, endPos - startPos).ToString();
        var isBlockTag = startDelimiter != null && IsBlockDelimiter(startDelimiter);
        // Apply LstripBlocks for any block tag when enabled, or for hyphenated block tags
        var shouldLstrip = isBlockTag && startDelimiter != null && (_config.LstripBlocks || startDelimiter.StartsWith("-"));
        AddTextTokenWithLstrip(tokens, textBeforeDelimiter, shouldLstrip);
    }

    private void UpdatePosition(string? value)
    {
        if (value != null)
        {
            foreach (var character in value)
            {
                if (character == '\n')
                {
                    _line++;
                    _column = 1; // Reset column to 1 for the new line
                }
                else
                {
                    _column++;
                }
            }
        }
    }
}
