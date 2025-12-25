namespace Jinja2.NET;

public static class TemplateConstants
{
    public static class BlockNames
    {
        public const string For = "for";
        public const string EndFor = "endfor";
        public const string If = "if";
        public const string EndIf = "endif";
        public const string Elif = "elif";
        public const string Else = "else";
        public const string Set = "set";
        public const string Macro = "macro";
        public const string EndMacro = "endmacro";
        public const string EndSet = "endset";
        public const string Raw = "raw";
        public const string EndRaw = "endraw";
    }

    public static class Delimiters
    {
        public const string BlockStart = "{%";
        public const string BlockEnd = "%}";
        public const string VariableStart = "{{";
        public const string VariableEnd = "}}";
        public const string CommentStart = "{#";
        public const string CommentEnd = "#}";
        public const string VariableStartRaw = "{{";
        public const string VariableEndRaw = "}}";
    }
}