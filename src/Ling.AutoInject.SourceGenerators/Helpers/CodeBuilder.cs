using System.Diagnostics;
using System.Text;

namespace Ling.AutoInject.SourceGenerators.Helpers;

/// <summary>
/// Represents a builder for generating code with indentation and line breaks.
/// </summary>
[DebuggerDisplay("Length: {Length}, {GetText()}")]
internal sealed class CodeBuilder
{
    private int _indentLevel;
    private bool _newLine;
    private readonly StringBuilder _sb;

    /// <summary>
    /// Gets the size of the indentation.
    /// </summary>
    public int IndentSize { get; }

    /// <summary>
    /// Gets the length of the code builder.
    /// </summary>
    public int Length => _sb.Length;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeBuilder"/> class.
    /// </summary>
    /// <param name="indentSize">The size of the indentation. Default is 4.</param>
    public CodeBuilder(int indentSize = 4)
    {
        if (indentSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(indentSize), "IndentSize must be greater than zero.");

        _indentLevel = 0;
        _newLine = true;
        _sb = new StringBuilder();

        IndentSize = indentSize;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeBuilder"/> class with the specified value.
    /// </summary>
    /// <param name="value">The initial value of the code builder.</param>
    /// <param name="indentSize">The size of the indentation. Default is 4.</param>
    public CodeBuilder(string value, int indentSize = 4)
    {
        if (indentSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(indentSize), "IndentSize must be greater than zero.");
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        _indentLevel = 0;
        _newLine = false;
        _sb = new StringBuilder(value);

        IndentSize = indentSize;
    }

    /// <summary>
    /// Appends an opening brace to the code builder and increases indent.
    /// </summary>
    public CodeBuilder OpenBrace()
    {
        AppendLine("{");
        _indentLevel++;
        _newLine = true;
        return this;
    }

    /// <summary>
    /// Appends a closing brace to the code builder.
    /// </summary>
    /// <param name="textAfterBrace">Optional text to append after the closing brace, e.g. ')'.</param>
    /// <param name="appendSemicolon">Indicates whether to append a semicolon after the closing brace.</param>
    public CodeBuilder CloseBrace(string? textAfterBrace = null, bool appendSemicolon = false)
    {
        if (_indentLevel <= 0)
            throw new InvalidOperationException("No matching open brace to close.");

        _indentLevel--;
        // closing brace should be indented at the decreased level
        EnsureIndent();
        _sb.Append("}");
        if (textAfterBrace is not null)
            _sb.Append(textAfterBrace);
        if (appendSemicolon)
            _sb.Append(";");

        _sb.AppendLine();
        _newLine = true;
        return this;
    }

    /// <summary>
    /// Closes all open braces and returns this.
    /// </summary>
    public CodeBuilder CloseAllBrace()
    {
        while (_indentLevel > 0)
        {
            CloseBrace();
        }
        return this;
    }

    /// <summary>
    /// Appends text to the code builder.
    /// </summary>
    public CodeBuilder Append(string? text)
    {
        // treat null as empty to simplify caller code
        if (text is null) return this;

        EnsureIndent();
        _sb.Append(text);
        return this;
    }

    /// <summary>
    /// Appends a single character.
    /// </summary>
    public CodeBuilder Append(char ch)
    {
        EnsureIndent();
        _sb.Append(ch);
        return this;
    }

    /// <summary>
    /// Appends object's string representation (null => empty).
    /// </summary>
    public CodeBuilder Append(object? value)
    {
        if (value is null) return this;
        EnsureIndent();
        _sb.Append(value);
        return this;
    }

    /// <summary>
    /// Appends a line of text to the code builder.
    /// </summary>
    public CodeBuilder AppendLine(string? text)
    {
        if (text is null)
        {
            _sb.AppendLine();
            _newLine = true;
            return this;
        }

        EnsureIndent();
        _sb.AppendLine(text);
        _newLine = true;
        return this;
    }

    /// <summary>
    /// Appends a new line.
    /// </summary>
    public CodeBuilder AppendLine()
    {
        _sb.AppendLine();
        _newLine = true;
        return this;
    }

    /// <summary>
    /// Appends a formatted text to the code builder.
    /// </summary>
    public CodeBuilder AppendFormat(string format, params object?[] args)
    {
        if (format is null) throw new ArgumentNullException(nameof(format));
        EnsureIndent();
        _sb.AppendFormat(format, args);
        return this;
    }

    /// <summary>
    /// Appends a formatted line of text to the code builder.
    /// </summary>
    public CodeBuilder AppendFormatLine(string format, params object?[] args)
    {
        if (format is null) throw new ArgumentNullException(nameof(format));
        EnsureIndent();
        _sb.AppendFormat(format, args);
        _sb.AppendLine();
        _newLine = true;
        return this;
    }

    /// <summary>
    /// Increases the indent level by one.
    /// </summary>
    public CodeBuilder IncreaseIndentLevel()
    {
        _indentLevel++;
        return this;
    }

    /// <summary>
    /// Decreases the indent level by one.
    /// </summary>
    public CodeBuilder DecreaseIndentLevel()
    {
        if (_indentLevel <= 0)
            throw new InvalidOperationException("Indent level is already zero.");
        _indentLevel--;
        return this;
    }

    /// <summary>
    /// Returns the code builder content without modifying state.
    /// </summary>
    public string GetText() => _sb.ToString();

    /// <summary>
    /// Closes any open braces and returns the final text.
    /// </summary>
    public string Build()
    {
        CloseAllBrace();
        return _sb.ToString();
    }

    /// <summary>
    /// Convenience: creates a block with braces and executes the provided action inside it.
    /// </summary>
    public CodeBuilder WithBlock(Action<CodeBuilder> content)
    {
        if (content is null) throw new ArgumentNullException(nameof(content));
        OpenBrace();
        try
        {
            content(this);
        }
        finally
        {
            CloseBrace();
        }
        return this;
    }

    /// <summary>
    /// Returns an IDisposable scope that increases indent and decreases on Dispose.
    /// Use: using (cb.Indent()) { ... }
    /// </summary>
    public IDisposable Indent() => new IndentationScope(this);

    private readonly struct IndentationScope : IDisposable
    {
        private readonly CodeBuilder _owner;
        public IndentationScope(CodeBuilder owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _owner.IncreaseIndentLevel();
        }

        public void Dispose()
        {
            // swallow exceptions to avoid throwing in Dispose; keep behavior simple
            if (_owner._indentLevel > 0)
            {
                _owner.DecreaseIndentLevel();
            }
        }
    }

    /// <summary>
    /// Clears the code builder.
    /// </summary>
    public void Clear()
    {
        _indentLevel = 0;
        _newLine = true;
        _sb.Clear();
    }

    /// <summary>
    /// Returns the code builder as a string (no side-effects).
    /// </summary>
    public override string ToString() => GetText();

    /// <summary>
    /// Ensures current line has leading indent.
    /// </summary>
    private void EnsureIndent()
    {
        if (_newLine && _indentLevel > 0)
        {
            _sb.Append(' ', IndentSize * _indentLevel);
        }
        _newLine = false;
    }
}
