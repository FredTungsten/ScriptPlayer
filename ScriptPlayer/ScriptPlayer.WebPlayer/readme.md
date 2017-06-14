# ScriptPlayer.WebPlayer

## About

Simplified version of the ScriptPlayer for web.

Uses [buttplug-js](https://github.com/metafetish/buttplug-js) to send [FunScripts](https://github.com/funjack/funscripts) to the [Buttplug Server](https://github.com/metafetish/buttplug-csharp)

## Usage Example

```html
<script language="javascript" type="text/javascript" src="dist/webbundle.js"></script>
<script language="javascript" type="text/javascript">
    document.addEventListener("DOMContentLoaded", () => {
        // load a funscript file
        var scriptFile = "data/POV Cock Hero Sync - Clubbed to death.json";
        // set up the player
        var scriptPlayer = new ScriptPlayerBundle.ScriptPlayer(scriptFile, document.getElementById("video"));
    });
</script>
```

Certain html elements are expected to exist (this will probably change in the future), see [index.html](./ScriptPlayer.WebPlayer/index.html) for a full minimalistic example.
