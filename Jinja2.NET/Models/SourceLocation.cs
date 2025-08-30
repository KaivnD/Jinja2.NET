namespace Jinja2.NET.Models;

public struct SourceLocation
{
  public int Line { get; }
  public int Column { get; }
  public int Index { get; }

  public SourceLocation(int line, int column, int index)
  {
    Line = line;
    Column = column;
    Index = index;
  }
}