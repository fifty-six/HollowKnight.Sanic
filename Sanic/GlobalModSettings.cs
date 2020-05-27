using Modding;

public class GlobalModSettings : IModSettings
{
    public float SpeedMultiplier
    {
        get => GetFloat(1.3f, "SpeedMultiplier");
        set => GetFloat(value, "SpeedMultiplier");
    }
}
