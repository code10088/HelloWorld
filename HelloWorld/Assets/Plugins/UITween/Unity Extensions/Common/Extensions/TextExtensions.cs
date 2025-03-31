
namespace UnityExtensions
{
    /// <summary>
    /// Extensions for string.
    /// </summary>
    public static partial class Extensions
    {
        public static int IndexOfNonWhiteSpace(this string text, int startIndex = 0)
        {
            for (; startIndex < text.Length; startIndex++)
            {
                if (!char.IsWhiteSpace(text, startIndex)) return startIndex;
            }
            return -1;
        }

        public static int LastIndexOfNonWhiteSpace(this string text, int startIndex)
        {
            for (; startIndex >= 0; startIndex--)
            {
                if (!char.IsWhiteSpace(text, startIndex)) return startIndex;
            }
            return -1;
        }

        public static int LastIndexOfNonWhiteSpace(this string text)
        {
            return LastIndexOfNonWhiteSpace(text, text.Length - 1);
        }

    } // class Extensions

} // namespace UnityExtensions