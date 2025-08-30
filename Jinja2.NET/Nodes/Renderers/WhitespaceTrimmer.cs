namespace Jinja2.NET.Nodes.Renderers;

public static class WhitespaceTrimmer
{
    public static void ApplyWhitespaceTrimming(List<ASTNode> children)
    {
        for (var i = 0; i < children.Count; i++)
        {
            var current = children[i];

            // Handle BlockNode trimming
            if (current is BlockNode block && block.TrimLeft && i > 0 && children[i - 1] is TextNode prevText)
            {
                prevText.TrimRight = true;
            }

            if (current is BlockNode block2 && block2.TrimRight && i < children.Count - 1)
            {
                // Find the next TextNode after this block, not just the immediately adjacent node
                for (var j = i + 1; j < children.Count; j++)
                    if (children[j] is TextNode nextText)
                    {
                        nextText.TrimLeft = true;
                        break; // Found the text node, stop searching
                    }
            }

            // Handle VariableNode trimming
            if (current is VariableNode varNode && varNode.TrimLeft && i > 0 && children[i - 1] is TextNode prevTextVar)
            {
                prevTextVar.TrimRight = true;
            }

            if (current is VariableNode varNode2 && varNode2.TrimRight && i < children.Count - 1)
            {
                // Find the next TextNode after this variable, not just the immediately adjacent node
                for (var j = i + 1; j < children.Count; j++)
                    if (children[j] is TextNode nextTextVar)
                    {
                        nextTextVar.TrimLeft = true;
                        break;
                    }
            }

            // Handle CommentNode trimming
            if (current is CommentNode commentNode && commentNode.TrimLeft && i > 0 &&
                children[i - 1] is TextNode prevTextComment)
            {
                prevTextComment.TrimRight = true;
            }

            if (current is CommentNode commentNode2 && commentNode2.TrimRight && i < children.Count - 1)
            {
                // Find the next TextNode after this comment, not just the immediately adjacent node
                for (var j = i + 1; j < children.Count; j++)
                    if (children[j] is TextNode nextTextComment)
                    {
                        nextTextComment.TrimLeft = true;
                        break;
                    }
            }
        }
    }
}