namespace Jinja2.NET.Interfaces;

public interface ITagParserRegistry
{
  ITagParser GetParser(string tagName);
}