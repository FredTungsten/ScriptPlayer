/// <binding AfterBuild='Watch - Development' ProjectOpened='Watch - Development' />
"use strict";

module.exports = {
    entry: "./src/scriptplayer.js",
    output: {
        filename: "./dist/webbundle.js",
        library: "ScriptPlayerBundle"
    },
    devServer: {
        contentBase: ".",
        host: "localhost",
        port: 9000
    },
    module: {
        loaders: [
            {
                test: /\.jsx?$/,
                loader: "babel-loader"
            }
        ]
    }
};