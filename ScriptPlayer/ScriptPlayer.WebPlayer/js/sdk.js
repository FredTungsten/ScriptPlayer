class ScriptPlayer {
    constructor(scriptLocation) {
        this.url = "http://localhost:6969";
        this.handleFrameUpdate = () => {
            window.requestAnimationFrame(() => {
                if (this.script == null)
                    return;
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
                        };
                        DeviceController.post(this.url + "/senddata", data);
                        this.checkpoint = this.checkpoint + 1;
                    }
                }
                if (this.video.paused === false) {
                    this.handleFrameUpdate();
                }
            });
        };
        this.jsonLoaded = (response) => {
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
        this.checkpoint = 0;
        this.video = document.getElementById("video");
        this.scriptFile = scriptLocation;
        this.loadJson(this.scriptFile, this.jsonLoaded);
        this.device = new DeviceController(this.video, this.url);
        this.video.addEventListener("playing", () => {
            this.device.kiiroo.style.display = "none";
            this.handleFrameUpdate();
        }, false);
        this.video.addEventListener("seeked", ev => {
            this.checkpoint = 9999999;
            var currentTime = ev.target.currentTime * 1000;
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
class Connection {
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
        var promise = new Promise((resolve, reject) => {
            DeviceController.get(this.baseUrl + "/status").then((status) => {
                resolve(new StatusObject(status.statusCode));
            }, () => {
                reject(new StatusObject(0));
            });
        });
        return promise;
    }
}
Connection.statusList = ["Connect device", "Device connected", "Waiting confirmation", "Devices connected", "Waiting for partner...", "Connecting..."];
class StatusObject {
    constructor(statusCode) {
        this.code = statusCode;
        this.text = Connection.statusList[statusCode];
    }
}
class ServerStatusObject {
}
class DeviceController {
    constructor(video, url) {
        this.periodicStatusCheck = () => {
            this.connection.checkStatus().then(response => {
                if (response.code > 0) {
                    this.status.classList.remove("device-not-connected");
                    this.status.classList.add("device-connected");
                    this.onDeviceConnected();
                }
                else {
                    this.status.classList.remove("device-connected");
                    this.status.classList.add("device-not-connected");
                    this.onDeviceDisconnected();
                }
            }, error => {
                this.status.classList.remove("device-connected");
                this.status.classList.add("device-not-connected");
                this.onDeviceNotLaunched();
            });
        };
        this.onDeviceConnected = () => {
            this.title.style.display = "none";
            this.kiiroobutton.style.display = "block";
            this.playbutton.onclick = () => {
                this.status.classList.add("on-video");
                this.video.controls = true;
                this.video.play();
                //fullscreen
                if (this.video.requestFullscreen) {
                    this.video.requestFullscreen();
                }
                //else if (elem.msRequestFullscreen) {
                //    elem.msRequestFullscreen();
                //} else if (elem.mozRequestFullScreen) {
                //    elem.mozRequestFullScreen();
                //} else if (elem.webkitRequestFullscreen) {
                //    elem.webkitRequestFullscreen();
                //}
                //fullscreen             
            };
        };
        this.onDeviceDisconnected = () => {
            this.title.innerHTML = "Searching for Kiiroo device, please wait...";
            this.title.style.display = "block";
            this.title.style.display = "none";
            this.video.controls = false;
            this.video.pause();
            this.status.classList.remove("on-video");
            this.kiiroo.style.display = "block";
        };
        this.onDeviceNotLaunched = () => {
            this.title.innerHTML = 'Please, <a href="kiiroo://noUI" style="color: #fff;">\
    start your application</a> if you have a Kiiroo ONYX device<br>\
    or <a href="https://kiiroo.com/product/onyx-bobbi/" style="color: #fff;">order it via our store</a>';
            this.title.style.display = "block";
            this.kiiroobutton.style.display = "none";
            this.video.controls = false;
            this.video.pause();
            this.status.classList.remove("on-video");
            this.kiiroo.style.display = "block";
        };
        this.streamToDevice = (enabled) => {
            DeviceController.get(this.url + "/streamToDevice?enabled=" + enabled);
        };
        this.isDeviceConnected = () => {
            return this.statusCode > 0;
        };
        this.joinRoom = (code) => {
            var url = this.url;
            this.streamToDevice(true);
            return DeviceController.post(url + "/joinRoom", {
                accessToken: "whatever",
                authCode: "DCPRS9PKW11AEHJFG7QT",
                callCode: code
            });
        };
        this.getConnectionStatusText = () => {
            return Connection.statusList[this.statusCode];
        };
        this.setDeviceStatus = (statusCode) => {
            this.statusCode = statusCode;
        };
        this.closeRoom = () => {
            DeviceController.post(this.url + "/stopStreaming");
        };
        this.url = url;
        this.connection = new Connection(url);
        this.status = document.getElementById("connection-status");
        this.video = video;
        this.title = document.getElementById("kiiroo-title");
        this.kiiroobutton = document.getElementById("kiiroobutton");
        this.playbutton = document.getElementById("kiiroo-play");
        this.kiiroo = document.getElementById("kiiroo");
        setInterval(this.periodicStatusCheck, 1000);
    }
}
DeviceController.request = (method, url, data = null) => {
    var xhr = new XMLHttpRequest();
    xhr.open(method, url, true);
    return new Promise((resolve, reject) => {
        xhr.addEventListener("readystatechange", () => {
            if (xhr.readyState === 4) {
                if (xhr.status === 200) {
                    resolve(JSON.parse(xhr.responseText));
                }
                else {
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
        }
        else {
            xhr.send();
        }
    });
};
DeviceController.get = (url) => {
    return DeviceController.request("GET", url);
};
DeviceController.post = (url, data = null) => {
    return DeviceController.request("POST", url, data);
};
//# sourceMappingURL=sdk.js.map