namespace NovelsCollector.Core.Utils
{
    public class Helpers
    {
        /// <summary>
        /// Remove Vietnamese signs from a string. e.g., "áàạảãâấầậẩẫăắằặẳẵ" => "aaaaaaaaaaaaaaaaa"
        /// </summary>
        /// <param name="str"> The string to remove Vietnamese signs from. </param>
        /// <returns> The string without Vietnamese signs. </returns>
        public static string RemoveVietnameseSigns(string str)
        {
            for (var i = 1; i < _VIETNAMESE_SIGNS.Length; i++)
            {
                for (var j = 0; j < _VIETNAMESE_SIGNS[i].Length; j++)
                {
                    str = str.Replace(_VIETNAMESE_SIGNS[i][j], _VIETNAMESE_SIGNS[0][i - 1]);
                }
            }

            return str;
        }

        private static readonly string[] _VIETNAMESE_SIGNS =
        [

            "aAeEoOuUiIdDyY",

            "áàạảãâấầậẩẫăắằặẳẵ",

            "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",

            "éèẹẻẽêếềệểễ",

            "ÉÈẸẺẼÊẾỀỆỂỄ",

            "óòọỏõôốồộổỗơớờợởỡ",

            "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",

            "úùụủũưứừựửữ",

            "ÚÙỤỦŨƯỨỪỰỬỮ",

            "íìịỉĩ",

            "ÍÌỊỈĨ",

            "đ",

            "Đ",

            "ýỳỵỷỹ",

            "ÝỲỴỶỸ"
        ];

    }
}
