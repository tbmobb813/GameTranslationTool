using System.Text;

namespace GameTranslationTool.Utils
{
    public static class SmartLineBreaker
    {
        /// <summary>
        /// Inserts '\n' so that no line exceeds maxChars, breaking on word boundaries.
        /// </summary>
        public static string InsertLineBreaks(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text) || maxChars < 1) 
                return text;

            var words = text.Split(' ');
            var sb = new StringBuilder();
            var lineLen = 0;

            foreach (var w in words)
            {
                if (lineLen + w.Length + 1 > maxChars)
                {
                    sb.Append('\n');
                    lineLen = 0;
                }
                else if (lineLen > 0)
                {
                    sb.Append(' ');
                    lineLen++;
                }
                sb.Append(w);
                lineLen += w.Length;
            }

            return sb.ToString();
        }
    }
}
