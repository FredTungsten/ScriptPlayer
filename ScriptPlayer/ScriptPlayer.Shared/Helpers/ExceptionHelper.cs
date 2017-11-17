using System;
using System.Text;

namespace ScriptPlayer.Shared.Helpers
{
    public static class ExceptionHelper
    {
        public static string BuildException(Exception exception)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine($"================= {DateTime.Now:G}  =================");

            Exception current = exception;

            while (current != null)
            {
                builder.AppendLine(current.Message);
                current = current.InnerException;
            }

            builder.AppendLine(exception.StackTrace);
            return builder.ToString();
        }
    }
}
