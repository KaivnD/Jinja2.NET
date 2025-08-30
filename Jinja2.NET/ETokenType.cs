namespace Jinja2.NET;

public enum ETokenType
{
    Text,
    VariableStart,    // {{
    VariableEnd,      // }}
    BlockStart,       // {%
    BlockEnd,         // %}
    CommentStart,     // {#
    CommentEnd,       // #}
    Identifier,
    Number,
    String,
    Operator,
    Pipe,            // |
    Dot,             // .
    LeftParen,       // (
    RightParen,      // )
    LeftBracket,     // [
    RightBracket,    // ]
    Comma,           // ,
    Colon,           // :
    Equals,          // =
    Plus,      // +
    Minus,     // -
    Multiply,  // *
    Divide,    // /
    EOF
}