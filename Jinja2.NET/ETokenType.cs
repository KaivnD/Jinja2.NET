namespace Jinja2.NET;

public enum ETokenType
{
    None,
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
    LeftBrace,       // {
    RightBrace,      // }
    Comma,           // ,
    Colon,           // :
    Equals,          // =
    Plus,      // +
    Minus,     // -
    Multiply,  // *
    Divide,    // /
    EOF
}