using System;

namespace ScriptPlayer.Shared
{
    public interface IOnScreenDisplay
    {
        void ShowMessage(string designation, string text, TimeSpan duration);
        void HideMessage(string designation);

        void ShowSkipButton();
        void ShowSkipNextButton();
        void HideSkipButton();
    }
}