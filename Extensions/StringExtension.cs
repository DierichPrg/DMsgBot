using System.Text;

namespace DMsgBot.Extensions
{
    public static class StringExtension
    {
        public static string LineSeparator => "----------------------------------------";
        
        public static string StringsToLines(params string[] args)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var s in args)
            {
                sb.AppendLine(s);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public static string StringsToLines(this IEnumerable<string> args)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var s in args)
            {
                sb.AppendLine(s);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public static IEnumerable<string> FormatJsonToPrint(this string json)
        {
            var indent = 0;
            var quote = false;
            var sb = new StringBuilder();
            foreach (var c in json)
            {
                switch (c)
                {
                    case '"':
                        quote = !quote;
                        sb.Append(c);
                        break;
                    case '{':
                    case '[':
                        sb.Append(c);
                        if (!quote)
                        {
                            sb.AppendLine();
                            sb.Append(' ', ++indent * 2);
                        }
                        break;
                    case '}':
                    case ']':
                        if (!quote)
                        {
                            sb.AppendLine();
                            sb.Append(' ', --indent * 2);
                        }
                        sb.Append(c);
                        break;
                    case ',':
                        sb.Append(c);
                        if (!quote)
                        {
                            sb.AppendLine();
                            sb.Append(' ', indent * 2);
                        }
                        break;
                    case ':':
                        sb.Append(c);
                        if (!quote)
                            sb.Append(' ');
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        }
    }
}
