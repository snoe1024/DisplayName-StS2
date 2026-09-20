using System.Collections.Generic;
using BaseLib.Config;
using Godot;

namespace DisplayName.DisplayNameCode.Config;

[ConfigHoverTipsByDefault]
public sealed class DisplayNameConfig : SimpleModConfig
{
    // Matches Steam's own display name length limit (32 characters). Not attempting to replicate Steam's
    // additional multi-byte/character-class rules -- this mod is only ever used among friends, so there's
    // no anti-troll reason to bother.
    private const int MaxDisplayNameLength = 32;

    private static string _displayName = "";

    /// <summary>
    /// The name shown for this player in multiplayer instead of their Steam username. One name per user,
    /// not per save profile. A plain `string` property is rendered as a text field automatically by
    /// SimpleModConfig, no attribute needed.
    /// </summary>
    public static string DisplayName
    {
        get => _displayName;
        set => _displayName = (value ?? "").Length > MaxDisplayNameLength ? value!.Substring(0, MaxDisplayNameLength) : value ?? "";
    }

    public override void SetupConfigUI(Control optionContainer)
    {
        GenerateOptionsForAllProperties(optionContainer);

        // BaseLib's auto-generated LineEdit row renders its text noticeably smaller than the rest of the
        // settings screen, and doesn't enforce any length limit on typing (only our setter's clamp on
        // commit). Reach into the generated control tree to fix both: a larger font, and a real MaxLength
        // so typing itself stops at the limit instead of silently truncating on save.
        foreach (LineEdit lineEdit in FindLineEdits(optionContainer))
        {
            lineEdit.MaxLength = MaxDisplayNameLength;
            int currentFontSize = lineEdit.GetThemeFontSize("font_size");
            lineEdit.AddThemeFontSizeOverride("font_size", Mathf.FloorToInt(Mathf.Max(currentFontSize, 12) * 1.5));
        }
    }

    private static IEnumerable<LineEdit> FindLineEdits(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is LineEdit lineEdit)
            {
                yield return lineEdit;
            }

            foreach (LineEdit nested in FindLineEdits(child))
            {
                yield return nested;
            }
        }
    }
}
