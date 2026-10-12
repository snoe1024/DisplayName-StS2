namespace DisplayName.DisplayNameCode.Config;

public sealed class DisplayNameSettings
{
    private string _name = "";
    public string Name
    {
        get => _name;
        set => _name = DisplayNameRules.Normalize(value);
    }
}
