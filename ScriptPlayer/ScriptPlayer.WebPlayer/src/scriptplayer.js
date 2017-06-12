//Run the Webpack task runner to pack it
"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
/* Imports */
const buttplug_1 = require("buttplug");
const Messages = require("buttplug");
/* classes */
class DeviceHandler {
    constructor(url, onConnected, onReady) {
        this.devices = [];
        this.establishConnection = () => {
            this.client.Connect(this.url)
                .then(this.connectedToClient, this.connectionFailed)
                .then(this.requestDeviceList, this.logError)
                .then(this.receivedDeviceList, this.logError);
        };
        this.logError = err => {
            console.log("Error: " + err); // Error: "It broke"
        };
        this.connectedToClient = result => {
            console.log("Buttplug Client connected");
            console.log(result); // "Stuff worked!"
            this.onConnected();
            return this.client.StartScanning();
        };
        this.connectionFailed = err => {
            console.log("Could not connect to buttplug sever:");
            console.log(err); // Error: "It broke"
            console.log("Retrying ...");
            setTimeout(this.establishConnection(), 2000);
        };
        this.setPosition = (position, speed) => {
            if (this.devices.length === 0)
                return;
            this.client.SendDeviceMessage(this.devices[0], new Messages.FleshlightLaunchRawCmd(speed, position));
        };
        this.requestDeviceList = () => {
            console.log("Requesting Device List");
            return this.client.RequestDeviceList();
        };
        this.receivedDeviceList = () => {
            this.devices = this.client.getDevices();
            console.log("Device list fetched: " + this.devices.length);
            if (this.devices.length === 0) {
                console.log("Fetch again: " + this.devices.length);
                setTimeout(() => { this.client.RequestDeviceList().then(this.receivedDeviceList, this.logError); }, 2000);
                return;
            }
            this.onReady();
        };
        this.url = url;
        this.onReady = onReady;
        this.onConnected = onConnected;
        this.client = new buttplug_1.ButtplugClient("ScriptPlayer.WebPlayer");
        this.establishConnection();
    }
}
class ScriptPlayer {
    constructor(scriptLocation, video) {
        this.wsLocation = "ws://localhost:12345/buttplug";
        this.setStateButtplugConnected = () => {
            this.uiController.setStateApplicationLaunched();
        };
        this.setStateDevicesReady = () => {
            this.uiController.setStateDeviceConnected();
        };
        this.handleFrameUpdate = () => {
            window.requestAnimationFrame(() => {
                if (this.script == null)
                    return;
                var timestamp = this.video.currentTime * 1000 - this.scriptDelay;
                if (timestamp >= this.script.actions[this.checkpoint].at) {
                    while (this.script.actions[this.checkpoint + 1].at <= timestamp && this.checkpoint < this.script.actions.length - 1) {
                        this.checkpoint++;
                    }
                    if (this.checkpoint < this.script.actions.length - 1) {
                        var tNow = this.script.actions[this.checkpoint].at;
                        var tNext = this.script.actions[this.checkpoint + 1].at;
                        var pNow = this.script.actions[this.checkpoint].pos;
                        var pNext = this.script.actions[this.checkpoint + 1].pos;
                        var speed = ScriptPlayer.predictSpeed((tNext - tNow) / 1000, Math.abs(pNow - pNext));
                        this.deviceHandler.setPosition(pNext, speed);
                        this.checkpoint = this.checkpoint + 1;
                    }
                }
                if (this.video.paused === false) {
                    this.handleFrameUpdate();
                }
            });
        };
        this.loadFunscript = (response) => {
            var responseObject = JSON.parse(response);
            this.script = responseObject;
        };
        this.loadJson = (scriptFile, callback) => {
            var xobj = new XMLHttpRequest();
            xobj.overrideMimeType("application/json");
            xobj.open("GET", scriptFile, true);
            xobj.onreadystatechange = () => {
                if (xobj.readyState === 4 && xobj.status === 200) {
                    callback(xobj.responseText);
                }
            };
            xobj.send(null);
        };
        this.deviceHandler = new DeviceHandler(this.wsLocation, this.setStateButtplugConnected, this.setStateDevicesReady);
        this.checkpoint = 0;
        this.video = video;
        this.scriptFile = scriptLocation;
        this.loadJson(this.scriptFile, this.loadFunscript);
        this.uiController = new UiController(this.video);
        this.scriptDelay = -0.1;
        this.video.addEventListener("playing", () => {
            this.uiController.overlay.style.display = "none";
            this.handleFrameUpdate();
        }, false);
        this.video.addEventListener("seeked", ev => {
            this.checkpoint = 9999999;
            var currentTime = ev.target.currentTime * 1000 - this.scriptDelay;
            this.checkpoint = this.findActionIndex(currentTime);
        });
    }
    findActionIndex(time) {
        if (this.script == null)
            return 0;
        for (var i = 0; i < this.script.actions.length; i++) {
            if (this.script.actions[i].at >= time)
                return i;
        }
        return this.script.actions.length;
    }
}
ScriptPlayer.predictSpeed = (time, range) => {
    var fullLengthsPerSecond = 6.0;
    var turnaroundDelay = 0.02; // 0.05;
    var relativeLength = range / 99.0;
    var durationAtFullSpeed = turnaroundDelay + relativeLength / fullLengthsPerSecond;
    var requiredSpeed = durationAtFullSpeed / time;
    var actualSpeed = ScriptPlayer.clampSpeed(requiredSpeed * 99.0);
    return actualSpeed;
};
ScriptPlayer.clampSpeed = (speed) => {
    return Math.min(99, Math.max(0, Math.round(speed)));
};
exports.ScriptPlayer = ScriptPlayer;
class UiController {
    constructor(video) {
        this.setStateApplicationNotLaunched = () => {
            this.title.innerHTML = 'Please, start <a href="https://buttplug.io">Buttplug</a>';
            this.title.style.display = "block";
            this.video.controls = false;
            this.video.pause();
            this.overlay.style.display = "block";
        };
        this.setStateApplicationLaunched = () => {
            this.setStateDeviceNotConnected();
        };
        this.setStateDeviceNotConnected = () => {
            this.title.innerText = "Waiting for device ...";
            this.title.style.display = "block";
            this.video.controls = false;
            this.video.pause();
            this.overlay.style.display = "block";
        };
        this.setStateDeviceConnected = () => {
            this.title.innerText = "Device connected! Press play to start.";
            this.playbutton.onclick = () => {
                this.title.style.display = "none";
                this.video.controls = true;
                this.video.play();
                //fullscreen
                if (this.video.requestFullscreen) {
                    this.video.requestFullscreen();
                }
            };
        };
        this.video = video;
        this.title = document.getElementById("banner-text");
        this.playbutton = document.getElementById("play-link");
        this.overlay = document.getElementById("overlay");
        this.setStateApplicationNotLaunched();
    }
}
//# sourceMappingURL=scriptplayer.js.map