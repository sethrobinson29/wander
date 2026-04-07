using System.Text.RegularExpressions;

namespace Wander.Client.Services;

public enum TextFieldType
{
    Generic,           // search queries, deck titles, usernames
    PasswordLogin,     // login — required only, no character restrictions
    PasswordRegister,  // registration — whitelist + min 8 chars + at least 1 digit
    Email,             // required + email format
}

public static class InputValidator
{
    // Allows letters, digits, whitespace, and common punctuation/symbols.
    // Blocks < > ` ~ \ which are markup/scripting characters.
    private static readonly Regex AllowedPattern =
        new(@"^[\p{L}\p{N}\s!@#$%^&*()\-_=+\[\]{}|;:'"",./? ]*$",
            RegexOptions.Compiled);

    // Loose email check — server validates authoritatively via Identity.
    private static readonly Regex EmailPattern =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Validates a text field value against rules for the given field type.
    /// Returns null if valid, or an error message string if invalid.
    /// </summary>
    public static string? Validate(string? input, TextFieldType type)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "This field is required.";

        switch (type)
        {
            case TextFieldType.Generic:
                if (!AllowedPattern.IsMatch(input))
                    return "Contains invalid characters.";
                return null;

            case TextFieldType.PasswordLogin:
                // No character restrictions — must never block an existing valid password.
                return null;

            case TextFieldType.PasswordRegister:
                if (!AllowedPattern.IsMatch(input))
                    return "Contains invalid characters.";
                if (input.Length < 8)
                    return "Password must be at least 8 characters.";
                if (!input.Any(char.IsDigit))
                    return "Password must contain at least one number.";
                return null;

            case TextFieldType.Email:
                if (!EmailPattern.IsMatch(input))
                    return "Enter a valid email address.";
                return null;

            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown TextFieldType.");
        }
    }

    /// <summary>
    /// Validates a bulk import decklist line by line.
    /// Returns a list of error messages (one per offending line), or an empty list if valid.
    /// </summary>
    public static List<string> ValidateBulkImport(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ["Decklist is required."];

        var errors = new List<string>();
        var lines = text.Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            if (line.StartsWith("//")) continue;
            if (line.Equals("SIDEBOARD:", StringComparison.OrdinalIgnoreCase)) continue;

            // Strip *CMDR* before checking format
            var stripped = line.Replace("*CMDR*", "", StringComparison.OrdinalIgnoreCase).Trim();

            var spaceIndex = stripped.IndexOf(' ');
            if (spaceIndex < 1 || !int.TryParse(stripped[..spaceIndex], out _))
            {
                errors.Add($"Line {i + 1}: must start with a quantity (e.g. '4 Lightning Bolt').");
                continue;
            }

            var name = stripped[(spaceIndex + 1)..].Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add($"Line {i + 1}: card name is missing.");
                continue;
            }

            if (!AllowedPattern.IsMatch(name))
            {
                errors.Add($"Line {i + 1}: card name contains invalid characters.");
                continue;
            }

            // Reject set codes — parenthetical content like "(LEA) 162" is not supported
            if (name.Contains('('))
                errors.Add($"Line {i + 1}: set codes are not supported. Use '4 Lightning Bolt' format.");
        }

        return errors;
    }
}
