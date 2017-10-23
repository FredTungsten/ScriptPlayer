using Newtonsoft.Json.Linq;

namespace ScriptPlayer.Shared.Scripts
{
    public abstract class FeelMeJsonLoaderBase : FeelMeLoaderBase
    {
        protected override FeelMeScript GetScriptContent(string content)
        {
            var file = JToken.Parse(content);

            var scriptNode = GetScriptNode(file);

            string script = scriptNode.ToString();
            return FeelMeScript.Json(script);
        }

        protected abstract JToken GetScriptNode(JToken file);
    }
}