using System.Text;

namespace NetMsgGen;

public sealed class CodeWriter
{
    private readonly StringBuilder _sb = new();
    private int _indent;

    public void Line(string text = "")
    {
        if (text.Length > 0) _sb.Append(' ', _indent * 4);
        _sb.Append(text).Append("\r\n");
    }

    public void Blank() => _sb.Append("\r\n");

    public void Open()
    {
        Line("{");
        _indent++;
    }

    public void Close()
    {
        _indent--;
        Line("}");
    }

    public override string ToString() => _sb.ToString();
}