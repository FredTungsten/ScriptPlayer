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

class ScriptPlayer {

    scriptFile: string;
    device: DeviceController;
    video: HTMLVideoElement;
    url: string = "http://localhost:6969";
    checkpoint: number;
    script: IFunScript;

    constructor(scriptLocation: string, video:HTMLVideoElement) {
        this.checkpoint = 0;
        this.video = video;
        this.scriptFile = scriptLocation;
        this.loadJson(this.scriptFile, this.jsonLoaded);
        this.device = new DeviceController(this.video, this.url);


        this.video.addEventListener("playing", () => {
            this.device.scriptplayer.style.display = "none";
            this.handleFrameUpdate();
        }, false);

        this.video.addEventListener("seeked", ev => {
            this.checkpoint = 9999999;
            var currentTime = (ev.target as HTMLVideoElement).currentTime * 1000;
            this.checkpoint = this.findActionIndex(currentTime);
        });
    }

    handleFrameUpdate = () => {
        window.requestAnimationFrame(() => {

            if (this.script == null) return;

            var timestamp = this.video.currentTime * 1000;

            if (timestamp >= this.script.actions[this.checkpoint].at) {

                while (this.script.actions[this.checkpoint + 1].at <= timestamp && this.checkpoint < this.script.actions.length - 1) {
                    this.checkpoint++;
                }

                if (this.checkpoint < this.script.actions.length - 1) {

                    var tNow = this.script.actions[this.checkpoint].at;
                    var tNext = this.script.actions[this.checkpoint + 1].at;
                    var cur = this.script.actions[this.checkpoint].pos;

                    var data = {
                        currentTime: tNow,
                        nextTime: tNext,
                        position: cur
                    }

                    DeviceController.post(this.url + "/senddata", data);

                    this.checkpoint = this.checkpoint + 1;
                }
            }

            if (this.video.paused === false) {
                this.handleFrameUpdate();
            }
        });
    }

    jsonLoaded = (response) => {
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

class Connection {
    baseUrl: string;
    static statusList: string[] = ["Connect device", "Device connected", "Waiting confirmation", "Devices connected", "Waiting for partner...", "Connecting..."];

    constructor(baseUrl) {
        this.baseUrl = baseUrl;
    }

    disconnect() {
        return DeviceController.post(this.baseUrl + "/disconnect");
    }

    connectDevice() {
        return DeviceController.post(this.baseUrl + "/connect");
    }

    checkStatus() {
        var promise = new Promise<StatusObject>((resolve, reject) => {
            DeviceController.get(this.baseUrl + "/status").then((status: ServerStatusObject) => {
                resolve(new StatusObject(status.statusCode));
            }, () => {
                reject(new StatusObject(0));
            });
        });
        return promise;
    }
}

class StatusObject {
    text: string;
    code: number;

    constructor(statusCode: number) {
        this.code = statusCode;
        this.text = Connection.statusList[statusCode];
    }
}

class ServerStatusObject {
    statusCode: number;
}

class DeviceController {
    video: HTMLVideoElement;
    status: HTMLElement;
    title: HTMLElement;
    scriptplayerbutton: HTMLElement;
    playbutton: HTMLElement;
    scriptplayer: HTMLElement;

    connection: Connection;
    url: string;
    statusCode: number;


    constructor(video: HTMLVideoElement, url: string) {
        this.url = url;
        this.connection = new Connection(url);
        this.video = video;

        this.status = document.getElementById("connection-status");
        this.title = document.getElementById("scriptplayer-title");
        this.scriptplayerbutton = document.getElementById("scriptplayer-button");
        this.playbutton = document.getElementById("scriptplayer-play");
        this.scriptplayer = document.getElementById("scriptplayer");

        setInterval(this.periodicStatusCheck, 1000);
    }

    periodicStatusCheck = () => {
        this.connection.checkStatus().then(response => {
            if (response.code > 0) {
                this.status.classList.remove("device-not-connected");
                this.status.classList.add("device-connected");
                this.deviceConnected();
            } else {
                this.status.classList.remove("device-connected");
                this.status.classList.add("device-not-connected");
                this.onDeviceDisconnected();
            }
        }, error => {
            this.status.classList.remove("device-connected");
            this.status.classList.add("device-not-connected");
            this.onDeviceNotLaunched();
        });
    }

    deviceConnected = () => {
        this.title.style.display = "none";
        this.scriptplayerbutton.style.display = "block";
        this.playbutton.onclick = () => {
            this.status.classList.add("on-video");
            this.video.controls = true;
            this.video.play();

            //fullscreen
            //if (this.video.requestFullscreen) {
            //    this.video.requestFullscreen();
            //}       
        };
    }

    onDeviceDisconnected = () => {
        this.title.innerHTML = "Searching for device, please wait...";
        this.title.style.display = "block";
        this.title.style.display = "none";
        this.video.controls = false;
        this.video.pause();
        this.status.classList.remove("on-video");
        this.scriptplayer.style.display = "block";
    }

    onDeviceNotLaunched = () => {
        this.title.innerHTML = 'Please, start your application';
        this.title.style.display = "block";
        this.scriptplayerbutton.style.display = "none";
        this.video.controls = false;
        this.video.pause();
        this.status.classList.remove("on-video");
        this.scriptplayer.style.display = "block";
    }

    static request = (method, url, data = null) => {
        var xhr = new XMLHttpRequest();
        xhr.open(method, url, true);
        return new Promise((resolve, reject) => {
            xhr.addEventListener("readystatechange", () => {
                if (xhr.readyState === 4) {
                    if (xhr.status === 200) {
                        resolve(JSON.parse(xhr.responseText));
                    } else {
                        reject(xhr.responseText);
                    }
                }
            });
            if (method === "POST") {
                data = data || {};
                xhr.setRequestHeader("Content-type", "application/x-www-form-urlencoded");
                let request = "";
                const keys = Object.keys(data);
                for (let i = 0; i < keys.length; i++) {
                    request += keys[i] + "=" + data[keys[i]] + "&";
                }
                xhr.send(request.slice(0, -1));
            } else {
                xhr.send();
            }
        });
    }

    static get = (url: string) => {
        return DeviceController.request("GET", url);
    }

    static post = (url: string, data = null) => {
        return DeviceController.request("POST", url, data);
    }
}