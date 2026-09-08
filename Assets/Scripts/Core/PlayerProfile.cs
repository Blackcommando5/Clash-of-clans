using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Kingdoms
{
    public static class PlayerProfile
    {
        public const int MaximumNameLength = 15;
        public const string NameKey = "Kingdoms.PlayerName.v1";
        public static string PlayerName => PlayerPrefs.GetString(NameKey, "");
        public static bool HasPlayerName => TryValidateName(PlayerName, out _, out _);

        public static bool TryValidateName(string input, out string normalized, out string error)
        {
            normalized = (input ?? "").Trim();
            error = "";
            foreach (char c in normalized)
            {
                bool letter = c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z';
                bool digit = c >= '0' && c <= '9';
                if (!letter && !digit && c != ' ' && c != '-' && c != '_' && c != '\'')
                {
                    error = "Use letters, numbers, spaces, - or _.";
                    return false;
                }
            }
            normalized = Regex.Replace(normalized, " +", " ");
            if (normalized.Length < 2 || normalized.Length > MaximumNameLength)
            {
                error = "Choose a name with 2 to 15 characters.";
                return false;
            }
            if (!Regex.IsMatch(normalized, "[A-Za-z0-9]"))
            {
                error = "Include at least one letter or number.";
                return false;
            }
            return true;
        }

        public static bool TrySaveName(string input, out string error)
        {
            if (!TryValidateName(input, out string name, out error)) return false;
            string previous = PlayerName;
            bool hadPrevious = PlayerPrefs.HasKey(NameKey);
            try
            {
                PlayerPrefs.SetString(NameKey, name);
                PlayerPrefs.Save();
                return true;
            }
            catch (Exception exception)
            {
                if (hadPrevious) PlayerPrefs.SetString(NameKey, previous);
                else PlayerPrefs.DeleteKey(NameKey);
                error = "Couldn't save your name. Please try again.";
                Debug.LogWarning("Player name could not be saved: " + exception.Message);
                return false;
            }
        }
    }
}
