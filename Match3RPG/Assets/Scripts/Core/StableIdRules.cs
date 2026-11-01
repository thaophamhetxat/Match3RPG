namespace Match3RPG.Core
{
    public static class StableIdRules
    {
        public static bool IsValid(string stableId)
        {
            if (string.IsNullOrEmpty(stableId) || !IsLowercaseLetter(stableId[0]))
                return false;

            bool previousWasSeparator = false;
            for (int index = 1; index < stableId.Length; index++)
            {
                char character = stableId[index];
                if (character == '_')
                {
                    if (previousWasSeparator || index == stableId.Length - 1)
                        return false;
                    previousWasSeparator = true;
                    continue;
                }

                if (!IsLowercaseLetter(character) && (character < '0' || character > '9'))
                    return false;
                previousWasSeparator = false;
            }

            return true;
        }

        static bool IsLowercaseLetter(char character)
        {
            return character >= 'a' && character <= 'z';
        }
    }
}
