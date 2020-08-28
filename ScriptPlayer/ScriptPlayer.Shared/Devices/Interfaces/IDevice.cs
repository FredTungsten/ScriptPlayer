namespace ScriptPlayer.Shared.Interfaces
{
    public interface IDevice
    {
        bool IsEnabled { get; set; }
       
        string Name { get; set; }
    }
}
