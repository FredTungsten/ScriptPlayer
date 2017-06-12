//Run the Webpack task runner to pack it,
//Compiled Version should be @ dist/webbundle.js

/* Imports */
import { ButtplugClient } from "buttplug";
import { Device } from "buttplug";
import * as Messages from "buttplug";

/* interfaces */
interface IFunAction {
    pos: number;
    at: number;
}

interface IFunScript {
    version: string;
    inverted: boolean;
    range: number;
    actions: IFunAction[];
}

/* classes */
class DeviceHandler {

    url:string;
    devices: Device[] = [];
    client: ButtplugClient;
    onReady: Function;
    onConnected: Function;

    constructor(url:string, onConnected: Function, onReady : Function) {
        this.url = url;

        this.onReady = onReady;
        this.onConnected = onConnected;
        this.client = new ButtplugClient("ScriptPlayer.WebPlayer");

        this.establishConnection();
    }

    establishConnection = () => {
        this.client.Connect(this.url)
            .then(this.connectedToClient, this.connectionFailed)
            .then(this.requestDeviceList, this.logError)
            .then(this.receivedDeviceList, this.logError);
    }

    logError = err => {
        console.log("Error: " + err); // Error: "It broke"
    }

    connectedToClient = result => {
        console.log("Buttplug Client connected");
        console.log(result); // "Stuff worked!"

        this.onConnected();
        return this.client.StartScanning();
    }

    connectionFailed = err => {
        console.log("Could not connect to buttplug sever:");
        console.log(err); // Error: "It broke"
        console.log("Retrying ...");

        setTimeout(this.establishConnection(), 2000);
    }

    setPosition = (position: number, speed: number) => {
        if (this.devices.length === 0) return;
        this.client.SendDeviceMessage(this.devices[0], new Messages.FleshlightLaunchRawCmd(speed, position));
    }
    requestDeviceList = () => {
            console.log("Requesting Device List");
            return this.client.RequestDeviceList();
        }

    receivedDeviceList = () => {
        this.devices = this.client.getDevices();
        console.log("Device list fetched: " + this.devices.length);
        if (this.devices.length === 0) {
            console.log("Fetch again: " + this.devices.length);
            setTimeout(() => { this.client.RequestDeviceList().then(this.receivedDeviceList, this.logError); }, 2000);
            return;
        }

        this.onReady();
    };
}

export class ScriptPlayer {

    scriptFile: string;
    uiController: UiController;
    video: HTMLVideoElement;
    wsLocation: string = "ws://localhost:12345/buttplug";
    checkpoint: number;
    script: IFunScript;
    deviceHandler: DeviceHandler;
    scriptDelay: number;

    constructor(scriptLocation: string, video: HTMLVideoElement) {
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
            var currentTime = (ev.target as HTMLVideoElement).currentTime * 1000 - this.scriptDelay;
            this.checkpoint = this.findActionIndex(currentTime);
        });
    }

    setStateButtplugConnected = () => {
        this.uiController.setStateApplicationLaunched();
    }

    setStateDevicesReady = () => {
        this.uiController.setStateDeviceConnected();
    }

    static predictSpeed = (time: number, range: number): number => {

        var fullLengthsPerSecond = 6.0;
        var turnaroundDelay = 0.02; // 0.05;

        var relativeLength = range / 99.0;
        var durationAtFullSpeed = turnaroundDelay + relativeLength / fullLengthsPerSecond;
        var requiredSpeed = durationAtFullSpeed / time;
        var actualSpeed = ScriptPlayer.clampSpeed(requiredSpeed * 99.0);
        return actualSpeed;
    }

    static clampSpeed = (speed : number) : number => {
        return Math.min(99, Math.max(0, Math.round(speed)));
    }

    handleFrameUpdate = () => {
        window.requestAnimationFrame(() => {

            if (this.script == null) return;

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
    }

    loadFunscript = (response) => {
        var responseObject: IFunScript = JSON.parse(response);
        this.script = responseObject;
    }

    loadJson = (scriptFile: string, callback) => {
        var xobj = new XMLHttpRequest();
        xobj.overrideMimeType("application/json");
        xobj.open("GET", scriptFile, true);
        xobj.onreadystatechange = () => {
            if (xobj.readyState === 4 && xobj.status === 200) {
                callback(xobj.responseText);
            }
        };
        xobj.send(null);
    }

    findActionIndex(time: number): number {
        if (this.script == null) return 0;

        for (var i = 0; i < this.script.actions.length; i++) {
            if (this.script.actions[i].at >= time)
                return i;
        }
        return this.script.actions.length;
    }
}

class UiController {
    video: HTMLVideoElement;
    title: HTMLElement;
    playbutton: HTMLElement;
    overlay: HTMLElement;

    constructor(video: HTMLVideoElement) {
        this.video = video;
        this.title = document.getElementById("banner-text");
        this.playbutton = document.getElementById("play-link");
        this.overlay = document.getElementById("overlay");

        this.setStateApplicationNotLaunched();
    }

    setStateApplicationNotLaunched = () => {
        this.title.innerHTML = 'Please, start <a href="https://buttplug.io">Buttplug</a>';
        this.title.style.display = "block";

        this.video.controls = false;
        this.video.pause();
        this.overlay.style.display = "block";
    }


    setStateApplicationLaunched = () => {
        this.setStateDeviceNotConnected();
    }

    setStateDeviceNotConnected = () => {
        this.title.innerText = "Waiting for device ...";
        this.title.style.display = "block";

        this.video.controls = false;
        this.video.pause();
        this.overlay.style.display = "block";
    }

    setStateDeviceConnected = () => {
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
    }
}