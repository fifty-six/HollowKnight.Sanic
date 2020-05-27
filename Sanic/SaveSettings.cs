// Sanic.SaveSettings
using Modding;

public class SaveSettings : IModSettings
{
        public int Speed
        {
                get => GetInt((int?) null, "Speed");
                set => SetInt(value, "Speed");
        }
}

