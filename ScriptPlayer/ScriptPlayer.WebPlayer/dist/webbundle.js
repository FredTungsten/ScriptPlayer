var ScriptPlayerBundle =
/******/ (function(modules) { // webpackBootstrap
/******/ 	// The module cache
/******/ 	var installedModules = {};
/******/
/******/ 	// The require function
/******/ 	function __webpack_require__(moduleId) {
/******/
/******/ 		// Check if module is in cache
/******/ 		if(installedModules[moduleId]) {
/******/ 			return installedModules[moduleId].exports;
/******/ 		}
/******/ 		// Create a new module (and put it into the cache)
/******/ 		var module = installedModules[moduleId] = {
/******/ 			i: moduleId,
/******/ 			l: false,
/******/ 			exports: {}
/******/ 		};
/******/
/******/ 		// Execute the module function
/******/ 		modules[moduleId].call(module.exports, module, module.exports, __webpack_require__);
/******/
/******/ 		// Flag the module as loaded
/******/ 		module.l = true;
/******/
/******/ 		// Return the exports of the module
/******/ 		return module.exports;
/******/ 	}
/******/
/******/
/******/ 	// expose the modules object (__webpack_modules__)
/******/ 	__webpack_require__.m = modules;
/******/
/******/ 	// expose the module cache
/******/ 	__webpack_require__.c = installedModules;
/******/
/******/ 	// identity function for calling harmony imports with the correct context
/******/ 	__webpack_require__.i = function(value) { return value; };
/******/
/******/ 	// define getter function for harmony exports
/******/ 	__webpack_require__.d = function(exports, name, getter) {
/******/ 		if(!__webpack_require__.o(exports, name)) {
/******/ 			Object.defineProperty(exports, name, {
/******/ 				configurable: false,
/******/ 				enumerable: true,
/******/ 				get: getter
/******/ 			});
/******/ 		}
/******/ 	};
/******/
/******/ 	// getDefaultExport function for compatibility with non-harmony modules
/******/ 	__webpack_require__.n = function(module) {
/******/ 		var getter = module && module.__esModule ?
/******/ 			function getDefault() { return module['default']; } :
/******/ 			function getModuleExports() { return module; };
/******/ 		__webpack_require__.d(getter, 'a', getter);
/******/ 		return getter;
/******/ 	};
/******/
/******/ 	// Object.prototype.hasOwnProperty.call
/******/ 	__webpack_require__.o = function(object, property) { return Object.prototype.hasOwnProperty.call(object, property); };
/******/
/******/ 	// __webpack_public_path__
/******/ 	__webpack_require__.p = "";
/******/
/******/ 	// Load entry module and return exports
/******/ 	return __webpack_require__(__webpack_require__.s = 18);
/******/ })
/************************************************************************/
/******/ ([
/* 0 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


function __export(m) {
    for (var p in m) if (!exports.hasOwnProperty(p)) exports[p] = m[p];
}
Object.defineProperty(exports, "__esModule", { value: true });
__export(__webpack_require__(5));
__export(__webpack_require__(1));
__export(__webpack_require__(3));
//# sourceMappingURL=index.js.map

/***/ }),
/* 1 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


var __extends = this && this.__extends || function () {
    var extendStatics = Object.setPrototypeOf || { __proto__: [] } instanceof Array && function (d, b) {
        d.__proto__ = b;
    } || function (d, b) {
        for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    };
    return function (d, b) {
        extendStatics(d, b);
        function __() {
            this.constructor = d;
        }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
}();
Object.defineProperty(exports, "__esModule", { value: true });
__webpack_require__(16);
var class_transformer_1 = __webpack_require__(8);
var ButtplugMessage = function () {
    function ButtplugMessage(Id) {
        this.Id = Id;
    }
    ButtplugMessage.prototype.getType = function () {
        return this.constructor.name;
    };
    ButtplugMessage.prototype.toJSON = function () {
        var json_obj = {};
        var instance = this.constructor;
        json_obj[instance.name] = class_transformer_1.classToPlain(this);
        return JSON.stringify(json_obj);
    };
    return ButtplugMessage;
}();
exports.ButtplugMessage = ButtplugMessage;
var ButtplugDeviceMessage = function (_super) {
    __extends(ButtplugDeviceMessage, _super);
    function ButtplugDeviceMessage(DeviceIndex, Id) {
        var _this = _super.call(this, Id) || this;
        _this.DeviceIndex = DeviceIndex;
        _this.Id = Id;
        return _this;
    }
    return ButtplugDeviceMessage;
}(ButtplugMessage);
exports.ButtplugDeviceMessage = ButtplugDeviceMessage;
var ButtplugSystemMessage = function (_super) {
    __extends(ButtplugSystemMessage, _super);
    function ButtplugSystemMessage(Id) {
        if (Id === void 0) {
            Id = 0;
        }
        var _this = _super.call(this, Id) || this;
        _this.Id = Id;
        return _this;
    }
    return ButtplugSystemMessage;
}(ButtplugMessage);
exports.ButtplugSystemMessage = ButtplugSystemMessage;
var Ok = function (_super) {
    __extends(Ok, _super);
    function Ok(Id) {
        var _this = _super.call(this, Id) || this;
        _this.Id = Id;
        return _this;
    }
    return Ok;
}(ButtplugSystemMessage);
exports.Ok = Ok;
var Ping = function (_super) {
    __extends(Ping, _super);
    function Ping(Id) {
        var _this = _super.call(this, Id) || this;
        _this.Id = Id;
        return _this;
    }
    return Ping;
}(ButtplugMessage);
exports.Ping = Ping;
var Test = function (_super) {
    __extends(Test, _super);
    function Test(TestString, Id) {
        if (Id === void 0) {
            Id = 1;
        }
        var _this = _super.call(this, Id) || this;
        _this.TestString = TestString;
        _this.Id = Id;
        return _this;
    }
    return Test;
}(ButtplugMessage);
exports.Test = Test;
var Error = function (_super) {
    __extends(Error, _super);
    function Error(ErrorMessage, Id) {
        if (Id === void 0) {
            Id = 1;
        }
        var _this = _super.call(this, Id) || this;
        _this.ErrorMessage = ErrorMessage;
        _this.Id = Id;
        return _this;
    }
    return Error;
}(ButtplugSystemMessage);
exports.Error = Error;
var DeviceInfo = function () {
    function DeviceInfo(DeviceIndex, DeviceName, DeviceMessages) {
        this.DeviceIndex = DeviceIndex;
        this.DeviceName = DeviceName;
        this.DeviceMessages = DeviceMessages;
    }
    return DeviceInfo;
}();
exports.DeviceInfo = DeviceInfo;
var DeviceList = function (_super) {
    __extends(DeviceList, _super);
    function DeviceList(Devices, Id) {
        var _this = _super.call(this) || this;
        _this.Devices = Devices;
        _this.Id = Id;
        return _this;
    }
    return DeviceList;
}(ButtplugSystemMessage);
exports.DeviceList = DeviceList;
var DeviceAdded = function (_super) {
    __extends(DeviceAdded, _super);
    function DeviceAdded(DeviceIndex, DeviceName, DeviceMessages) {
        var _this = _super.call(this) || this;
        _this.DeviceIndex = DeviceIndex;
        _this.DeviceName = DeviceName;
        _this.DeviceMessages = DeviceMessages;
        return _this;
    }
    return DeviceAdded;
}(ButtplugSystemMessage);
exports.DeviceAdded = DeviceAdded;
var DeviceRemoved = function (_super) {
    __extends(DeviceRemoved, _super);
    function DeviceRemoved(DeviceIndex) {
        var _this = _super.call(this) || this;
        _this.DeviceIndex = DeviceIndex;
        return _this;
    }
    return DeviceRemoved;
}(ButtplugSystemMessage);
exports.DeviceRemoved = DeviceRemoved;
var RequestDeviceList = function (_super) {
    __extends(RequestDeviceList, _super);
    function RequestDeviceList(Id) {
        if (Id === void 0) {
            Id = 1;
        }
        var _this = _super.call(this, Id) || this;
        _this.Id = Id;
        return _this;
    }
    return RequestDeviceList;
}(ButtplugMessage);
exports.RequestDeviceList = RequestDeviceList;
var StartScanning = function (_super) {
    __extends(StartScanning, _super);
    function StartScanning(Id) {
        if (Id === void 0) {
            Id = 1;
        }
        var _this = _super.call(this, Id) || this;
        _this.Id = Id;
        return _this;
    }
    return StartScanning;
}(ButtplugMessage);
exports.StartScanning = StartScanning;
var StopScanning = function (_super) {
    __extends(StopScanning, _super);
    function StopScanning(Id) {
        if (Id === void 0) {
            Id = 1;
        }
        var _this = _super.call(this, Id) || this;
        _this.Id = Id;
        return _this;
    }
    return StopScanning;
}(ButtplugMessage);
exports.StopScanning = StopScanning;
var RequestLog = function (_super) {
    __extends(RequestLog, _super);
    function RequestLog(LogLevel, Id) {
        if (Id === void 0) {
            Id = 1;
        }
        var _this = _super.call(this, Id) || this;
        _this.LogLevel = LogLevel;
        _this.Id = Id;
        return _this;
    }
    return RequestLog;
}(ButtplugMessage);
exports.RequestLog = RequestLog;
var Log = function (_super) {
    __extends(Log, _super);
    function Log(LogLevel, LogMessage) {
        var _this = _super.call(this) || this;
        _this.LogLevel = LogLevel;
        _this.LogMessage = LogMessage;
        return _this;
    }
    return Log;
}(ButtplugSystemMessage);
exports.Log = Log;
var RequestServerInfo = function (_super) {
    __extends(RequestServerInfo, _super);
    function RequestServerInfo(ClientName, Id) {
        if (Id === void 0) {
            Id = 1;
        }
        var _this = _super.call(this, Id) || this;
        _this.ClientName = ClientName;
        _this.Id = Id;
        return _this;
    }
    return RequestServerInfo;
}(ButtplugMessage);
exports.RequestServerInfo = RequestServerInfo;
var ServerInfo = function (_super) {
    __extends(ServerInfo, _super);
    function ServerInfo(MajorVersion, MinorVersion, BuildVersion, MessageVersion, MaxPingTime, ServerName, Id) {
        if (Id === void 0) {
            Id = 1;
        }
        var _this = _super.call(this) || this;
        _this.MajorVersion = MajorVersion;
        _this.MinorVersion = MinorVersion;
        _this.BuildVersion = BuildVersion;
        _this.MessageVersion = MessageVersion;
        _this.MaxPingTime = MaxPingTime;
        _this.ServerName = ServerName;
        _this.Id = Id;
        return _this;
    }
    return ServerInfo;
}(ButtplugSystemMessage);
exports.ServerInfo = ServerInfo;
var FleshlightLaunchRawCmd = function (_super) {
    __extends(FleshlightLaunchRawCmd, _super);
    function FleshlightLaunchRawCmd(Speed, Position, DeviceIndex, Id) {
        if (DeviceIndex === void 0) {
            DeviceIndex = -1;
        }
        if (Id === void 0) {
            Id = 1;
        }
        var _this = _super.call(this, DeviceIndex, Id) || this;
        _this.Speed = Speed;
        _this.Position = Position;
        _this.DeviceIndex = DeviceIndex;
        _this.Id = Id;
        return _this;
    }
    return FleshlightLaunchRawCmd;
}(ButtplugDeviceMessage);
exports.FleshlightLaunchRawCmd = FleshlightLaunchRawCmd;
var KiirooRawCmd = function (_super) {
    __extends(KiirooRawCmd, _super);
    function KiirooRawCmd(Position, DeviceIndex, Id) {
        if (DeviceIndex === void 0) {
            DeviceIndex = -1;
        }
        if (Id === void 0) {
            Id = 1;
        }
        var _this = _super.call(this, DeviceIndex, Id) || this;
        _this.Position = Position;
        _this.DeviceIndex = DeviceIndex;
        _this.Id = Id;
        return _this;
    }
    return KiirooRawCmd;
}(ButtplugDeviceMessage);
exports.KiirooRawCmd = KiirooRawCmd;
var SingleMotorVibrateCmd = function (_super) {
    __extends(SingleMotorVibrateCmd, _super);
    function SingleMotorVibrateCmd(Speed, DeviceIndex, Id) {
        if (DeviceIndex === void 0) {
            DeviceIndex = -1;
        }
        if (Id === void 0) {
            Id = 1;
        }
        var _this = _super.call(this, DeviceIndex, Id) || this;
        _this.Speed = Speed;
        _this.DeviceIndex = DeviceIndex;
        _this.Id = Id;
        return _this;
    }
    return SingleMotorVibrateCmd;
}(ButtplugDeviceMessage);
exports.SingleMotorVibrateCmd = SingleMotorVibrateCmd;
var Messages = {
    Ok: Ok,
    Ping: Ping,
    Test: Test,
    Error: Error,
    DeviceList: DeviceList,
    DeviceAdded: DeviceAdded,
    DeviceRemoved: DeviceRemoved,
    RequestDeviceList: RequestDeviceList,
    StartScanning: StartScanning,
    StopScanning: StopScanning,
    RequestLog: RequestLog,
    Log: Log,
    RequestServerInfo: RequestServerInfo,
    ServerInfo: ServerInfo,
    FleshlightLaunchRawCmd: FleshlightLaunchRawCmd,
    KiirooRawCmd: KiirooRawCmd,
    SingleMotorVibrateCmd: SingleMotorVibrateCmd
};
function FromJSON(str) {
    // TODO We're assuming we'll always get valid json here. While it should pass
    // through the schema parser first, it'd probably be good to make sure it
    // deals with parse failures too.
    var msgarray = JSON.parse(str);
    var msgs = [];
    for (var _i = 0, _a = Array.from(msgarray); _i < _a.length; _i++) {
        var x = _a[_i];
        // Can't get this to resolve nicely as a type, so just start from any and cast
        // after. Not sure how to resolve plainToClass to a type since this is
        // dynamic.
        var msg = class_transformer_1.plainToClass(Messages[Object.getOwnPropertyNames(x)[0]], x[Object.getOwnPropertyNames(x)[0]]);
        msgs.push(msg);
    }
    if (msgs.length == 0) {
        // Backup in case the server sent us a single object outside of an array.
        // Accoring to the schema, this should be illegal, so once schema checking
        // is added this should become dead code.
        var msg = class_transformer_1.plainToClass(Messages[Object.getOwnPropertyNames(msgarray)[0]], msgarray[Object.getOwnPropertyNames(msgarray)[0]]);
        msgs.push(msg);
    }
    return msgs;
}
exports.FromJSON = FromJSON;
//# sourceMappingURL=messages.js.map

/***/ }),
/* 2 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


var TransformOperationExecutor_1 = __webpack_require__(6);
var ClassTransformer = function () {
    function ClassTransformer() {}
    ClassTransformer.prototype.classToPlain = function (object, options) {
        var executor = new TransformOperationExecutor_1.TransformOperationExecutor("classToPlain", options || {});
        return executor.transform(undefined, object, undefined, undefined, undefined, undefined);
    };
    ClassTransformer.prototype.classToPlainFromExist = function (object, plainObject, options) {
        var executor = new TransformOperationExecutor_1.TransformOperationExecutor("classToPlain", options || {});
        return executor.transform(plainObject, object, undefined, undefined, undefined, undefined);
    };
    ClassTransformer.prototype.plainToClass = function (cls, plain, options) {
        var executor = new TransformOperationExecutor_1.TransformOperationExecutor("plainToClass", options || {});
        return executor.transform(undefined, plain, cls, undefined, undefined, undefined);
    };
    ClassTransformer.prototype.plainToClassFromExist = function (clsObject, plain, options) {
        var executor = new TransformOperationExecutor_1.TransformOperationExecutor("plainToClass", options || {});
        return executor.transform(clsObject, plain, undefined, undefined, undefined, undefined);
    };
    ClassTransformer.prototype.classToClass = function (object, options) {
        var executor = new TransformOperationExecutor_1.TransformOperationExecutor("classToClass", options || {});
        return executor.transform(undefined, object, undefined, undefined, undefined, undefined);
    };
    ClassTransformer.prototype.classToClassFromExist = function (object, fromObject, options) {
        var executor = new TransformOperationExecutor_1.TransformOperationExecutor("classToClass", options || {});
        return executor.transform(fromObject, object, undefined, undefined, undefined, undefined);
    };
    ClassTransformer.prototype.serialize = function (object, options) {
        return JSON.stringify(this.classToPlain(object, options));
    };
    /**
     * Deserializes given JSON string to a object of the given class.
     */
    ClassTransformer.prototype.deserialize = function (cls, json, options) {
        var jsonObject = JSON.parse(json);
        return this.plainToClass(cls, jsonObject, options);
    };
    /**
     * Deserializes given JSON string to an array of objects of the given class.
     */
    ClassTransformer.prototype.deserializeArray = function (cls, json, options) {
        var jsonObject = JSON.parse(json);
        return this.plainToClass(cls, jsonObject, options);
    };
    return ClassTransformer;
}();
exports.ClassTransformer = ClassTransformer;

//# sourceMappingURL=ClassTransformer.js.map

/***/ }),
/* 3 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", { value: true });
var Messages = __webpack_require__(1);
var Device = function () {
    function Device(_index, _name, _allowedMessages) {
        this._index = _index;
        this._name = _name;
        this._allowedMessages = _allowedMessages;
    }
    Device.fromMsg = function (aMsg) {
        return new Device(aMsg.DeviceIndex, aMsg.DeviceName, aMsg.DeviceMessages);
    };
    Object.defineProperty(Device.prototype, "Name", {
        get: function () {
            return this._name;
        },
        enumerable: true,
        configurable: true
    });
    Object.defineProperty(Device.prototype, "Index", {
        get: function () {
            return this._index;
        },
        enumerable: true,
        configurable: true
    });
    Object.defineProperty(Device.prototype, "AllowedMessages", {
        get: function () {
            return this._allowedMessages;
        },
        enumerable: true,
        configurable: true
    });
    Device.prototype.newMessage = function (allowedMsg) {
        var msg = this._allowedMessages[allowedMsg];
        return Messages[msg];
    };
    return Device;
}();
exports.Device = Device;
//# sourceMappingURL=device.js.map

/***/ }),
/* 4 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


var MetadataStorage_1 = __webpack_require__(11);
/**
 * Default metadata storage is used as singleton and can be used to storage all metadatas.
 */
exports.defaultMetadataStorage = new MetadataStorage_1.MetadataStorage();

//# sourceMappingURL=storage.js.map

/***/ }),
/* 5 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


var __extends = this && this.__extends || function () {
    var extendStatics = Object.setPrototypeOf || { __proto__: [] } instanceof Array && function (d, b) {
        d.__proto__ = b;
    } || function (d, b) {
        for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    };
    return function (d, b) {
        extendStatics(d, b);
        function __() {
            this.constructor = d;
        }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
}();
var __awaiter = this && this.__awaiter || function (thisArg, _arguments, P, generator) {
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) {
            try {
                step(generator.next(value));
            } catch (e) {
                reject(e);
            }
        }
        function rejected(value) {
            try {
                step(generator["throw"](value));
            } catch (e) {
                reject(e);
            }
        }
        function step(result) {
            result.done ? resolve(result.value) : new P(function (resolve) {
                resolve(result.value);
            }).then(fulfilled, rejected);
        }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var __generator = this && this.__generator || function (thisArg, body) {
    var _ = { label: 0, sent: function () {
            if (t[0] & 1) throw t[1];return t[1];
        }, trys: [], ops: [] },
        f,
        y,
        t,
        g;
    return g = { next: verb(0), "throw": verb(1), "return": verb(2) }, typeof Symbol === "function" && (g[Symbol.iterator] = function () {
        return this;
    }), g;
    function verb(n) {
        return function (v) {
            return step([n, v]);
        };
    }
    function step(op) {
        if (f) throw new TypeError("Generator is already executing.");
        while (_) try {
            if (f = 1, y && (t = y[op[0] & 2 ? "return" : op[0] ? "throw" : "next"]) && !(t = t.call(y, op[1])).done) return t;
            if (y = 0, t) op = [0, t.value];
            switch (op[0]) {
                case 0:case 1:
                    t = op;break;
                case 4:
                    _.label++;return { value: op[1], done: false };
                case 5:
                    _.label++;y = op[1];op = [0];continue;
                case 7:
                    op = _.ops.pop();_.trys.pop();continue;
                default:
                    if (!(t = _.trys, t = t.length > 0 && t[t.length - 1]) && (op[0] === 6 || op[0] === 2)) {
                        _ = 0;continue;
                    }
                    if (op[0] === 3 && (!t || op[1] > t[0] && op[1] < t[3])) {
                        _.label = op[1];break;
                    }
                    if (op[0] === 6 && _.label < t[1]) {
                        _.label = t[1];t = op;break;
                    }
                    if (t && _.label < t[2]) {
                        _.label = t[2];_.ops.push(op);break;
                    }
                    if (t[2]) _.ops.pop();
                    _.trys.pop();continue;
            }
            op = body.call(thisArg, _);
        } catch (e) {
            op = [6, e];y = 0;
        } finally {
            f = t = 0;
        }
        if (op[0] & 5) throw op[1];return { value: op[0] ? op[1] : void 0, done: true };
    }
};
Object.defineProperty(exports, "__esModule", { value: true });
var events_1 = __webpack_require__(14);
var Messages = __webpack_require__(1);
var device_1 = __webpack_require__(3);
var ButtplugClient = function (_super) {
    __extends(ButtplugClient, _super);
    function ButtplugClient(aClientName) {
        var _this = _super.call(this) || this;
        _this._devices = new Map();
        _this._counter = 1;
        _this._waitingMsgs = new Map();
        _this.Connect = function (aUrl) {
            return __awaiter(_this, void 0, void 0, function () {
                var _this = this;
                var res, rej, p;
                return __generator(this, function (_a) {
                    this._ws = new WebSocket(aUrl);
                    this._ws.addEventListener('message', function (ev) {
                        _this.ParseIncomingMessage(ev);
                    });
                    p = new Promise(function (resolve, reject) {
                        res = resolve;rej = reject;
                    });
                    this._ws.addEventListener('open', function (ev) {
                        return __awaiter(_this, void 0, void 0, function () {
                            var msg;
                            return __generator(this, function (_a) {
                                switch (_a.label) {
                                    case 0:
                                        return [4 /*yield*/, this.SendMessage(new Messages.RequestServerInfo(this._clientName))];
                                    case 1:
                                        msg = _a.sent();
                                        switch (msg.getType()) {
                                            case 'ServerInfo':
                                                // TODO: Actually deal with ping timing, maybe store server name, do
                                                // something with message template version?
                                                res();
                                                break;
                                            case 'Error':
                                                rej();
                                                break;
                                        }
                                        return [2 /*return*/];
                                }
                            });
                        });
                    });
                    this._ws.addEventListener('close', function (ev) {
                        rej(ev);
                    });
                    return [2 /*return*/, p];
                });
            });
        };
        _this.SendMsgExpectOk = function (aMsg) {
            return __awaiter(_this, void 0, void 0, function () {
                var res, rej, msg, p;
                return __generator(this, function (_a) {
                    switch (_a.label) {
                        case 0:
                            return [4 /*yield*/, this.SendMessage(aMsg)];
                        case 1:
                            msg = _a.sent();
                            p = new Promise(function (resolve, reject) {
                                res = resolve;rej = reject;
                            });
                            switch (msg.getType()) {
                                case 'Ok':
                                    res();
                                    break;
                                default:
                                    rej();
                                    break;
                            }
                            return [2 /*return*/, p];
                    }
                });
            });
        };
        _this.RequestDeviceList = function () {
            return __awaiter(_this, void 0, void 0, function () {
                var _this = this;
                var deviceList;
                return __generator(this, function (_a) {
                    switch (_a.label) {
                        case 0:
                            return [4 /*yield*/, this.SendMessage(new Messages.RequestDeviceList())];
                        case 1:
                            deviceList = _a.sent();
                            deviceList.Devices.forEach(function (d) {
                                if (!_this._devices.has(d.DeviceIndex)) {
                                    var device = device_1.Device.fromMsg(d);
                                    _this._devices.set(d.DeviceIndex, device);
                                    _this.emit('deviceadded', device);
                                }
                            });
                            return [2 /*return*/];
                    }
                });
            });
        };
        _this.StartScanning = function () {
            return __awaiter(_this, void 0, void 0, function () {
                return __generator(this, function (_a) {
                    switch (_a.label) {
                        case 0:
                            return [4 /*yield*/, this.SendMsgExpectOk(new Messages.StartScanning())];
                        case 1:
                            return [2 /*return*/, _a.sent()];
                    }
                });
            });
        };
        _this.StopScanning = function () {
            return __awaiter(_this, void 0, void 0, function () {
                return __generator(this, function (_a) {
                    switch (_a.label) {
                        case 0:
                            return [4 /*yield*/, this.SendMsgExpectOk(new Messages.StopScanning())];
                        case 1:
                            return [2 /*return*/, _a.sent()];
                    }
                });
            });
        };
        _this.RequestLog = function (aLogLevel) {
            return __awaiter(_this, void 0, void 0, function () {
                return __generator(this, function (_a) {
                    switch (_a.label) {
                        case 0:
                            return [4 /*yield*/, this.SendMsgExpectOk(new Messages.RequestLog(aLogLevel))];
                        case 1:
                            return [2 /*return*/, _a.sent()];
                    }
                });
            });
        };
        _this.ParseJSONMessage = function (aJSONMsg) {
            var msgs = Messages.FromJSON(aJSONMsg);
            msgs.forEach(function (x) {
                if (x.Id > 0 && _this._waitingMsgs.has(x.Id)) {
                    var res = _this._waitingMsgs.get(x.Id);
                    // We already checked for this via has, but typescript is bitching if I
                    // don't do it again.
                    if (res === undefined) {
                        return;
                    }
                    res(x);
                    return;
                }
                switch (x.constructor.name) {
                    case 'Log':
                        _this.emit('log', x);
                        break;
                    case 'DeviceAdded':
                        var added_msg = x;
                        var d = device_1.Device.fromMsg(added_msg);
                        _this._devices.set(added_msg.DeviceIndex, d);
                        _this.emit('deviceadded', d);
                        break;
                    case 'DeviceRemoved':
                        var removed_msg = x;
                        if (_this._devices.has(removed_msg.DeviceIndex)) {
                            var d_1 = _this._devices.get(removed_msg.DeviceIndex);
                            _this._devices.delete(removed_msg.DeviceIndex);
                            _this.emit('deviceremoved', d_1);
                        }
                        break;
                }
                ;
            });
        };
        _this.ParseIncomingMessage = function (aEvent) {
            if (typeof aEvent.data === 'string') {
                _this.ParseJSONMessage(aEvent.data);
            } else if (aEvent.data instanceof Blob) {
                var reader = new FileReader();
                reader.addEventListener('load', function (ev) {
                    _this.OnReaderLoad(ev);
                });
                reader.readAsText(aEvent.data);
            }
        };
        _this._clientName = aClientName;
        return _this;
    }
    ButtplugClient.prototype.SendMessage = function (aMsg) {
        return __awaiter(this, void 0, void 0, function () {
            var res, msgPromise;
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0:
                        aMsg.Id = this._counter;
                        msgPromise = new Promise(function (resolve) {
                            res = resolve;
                        });
                        this._waitingMsgs.set(this._counter, res);
                        this._counter += 1;
                        this._ws.send("[" + aMsg.toJSON() + "]");
                        return [4 /*yield*/, msgPromise];
                    case 1:
                        return [2 /*return*/, _a.sent()];
                }
            });
        });
    };
    ButtplugClient.prototype.getDevices = function () {
        var devices = [];
        this._devices.forEach(function (d, i) {
            devices.push(d);
        });
        return devices;
    };
    ButtplugClient.prototype.OnReaderLoad = function (aEvent) {
        this.ParseJSONMessage(aEvent.target.result);
    };
    ButtplugClient.prototype.SendDeviceMessage = function (aDevice, aDeviceMsg) {
        return __awaiter(this, void 0, void 0, function () {
            var dev;
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0:
                        dev = this._devices.get(aDevice.Index);
                        if (dev === undefined) {
                            return [2 /*return*/, Promise.reject(new Error("Device not available."))];
                        }
                        if (dev.AllowedMessages.indexOf(aDeviceMsg.getType()) == -1) {
                            return [2 /*return*/, Promise.reject(new Error("Device does not accept that message type."))];
                        }
                        aDeviceMsg.DeviceIndex = aDevice.Index;
                        return [4 /*yield*/, this.SendMsgExpectOk(aDeviceMsg)];
                    case 1:
                        return [2 /*return*/, _a.sent()];
                }
            });
        });
    };
    return ButtplugClient;
}(events_1.EventEmitter);
exports.ButtplugClient = ButtplugClient;
//# sourceMappingURL=client.js.map

/***/ }),
/* 6 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


var storage_1 = __webpack_require__(4);
var TransformOperationExecutor = function () {
    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------
    function TransformOperationExecutor(transformationType, options) {
        this.transformationType = transformationType;
        this.options = options;
        // -------------------------------------------------------------------------
        // Private Properties
        // -------------------------------------------------------------------------
        this.transformedTypes = [];
    }
    // -------------------------------------------------------------------------
    // Public Methods
    // -------------------------------------------------------------------------
    TransformOperationExecutor.prototype.transform = function (source, value, targetType, arrayType, isMap, level) {
        var _this = this;
        if (level === void 0) {
            level = 0;
        }
        if (value instanceof Array || value instanceof Set) {
            var newValue_1 = arrayType && this.transformationType === "plainToClass" ? new arrayType() : [];
            value.forEach(function (subValue, index) {
                var subSource = source ? source[index] : undefined;
                if (!_this.isCircular(subValue, level)) {
                    var value_1 = _this.transform(subSource, subValue, targetType, undefined, subValue instanceof Map, level + 1);
                    if (newValue_1 instanceof Set) {
                        newValue_1.add(value_1);
                    } else {
                        newValue_1.push(value_1);
                    }
                } else if (_this.transformationType === "classToClass") {
                    if (newValue_1 instanceof Set) {
                        newValue_1.add(subValue);
                    } else {
                        newValue_1.push(subValue);
                    }
                }
            });
            return newValue_1;
        } else if (targetType === String && !isMap) {
            return String(value);
        } else if (targetType === Number && !isMap) {
            return Number(value);
        } else if (targetType === Boolean && !isMap) {
            return Boolean(value);
        } else if ((targetType === Date || value instanceof Date) && !isMap) {
            if (value instanceof Date) {
                return new Date(value.valueOf());
            }
            if (value === null || value === undefined) return value;
            return new Date(value);
        } else if (value instanceof Object) {
            // try to guess the type
            if (!targetType && value.constructor !== Object /* && operationType === "classToPlain"*/) targetType = value.constructor;
            if (!targetType && source) targetType = source.constructor;
            // add transformed type to prevent circular references
            this.transformedTypes.push({ level: level, object: value });
            var keys = this.getKeys(targetType, value);
            var newValue = source ? source : {};
            if (!source && (this.transformationType === "plainToClass" || this.transformationType === "classToClass")) {
                if (isMap) {
                    newValue = new Map();
                } else if (targetType) {
                    newValue = new targetType();
                } else {
                    newValue = {};
                }
            }
            // traverse over keys
            var _loop_1 = function (key) {
                var valueKey = key,
                    newValueKey = key,
                    propertyName = key;
                if (!this_1.options.ignoreDecorators && targetType) {
                    if (this_1.transformationType === "plainToClass") {
                        var exposeMetadata = storage_1.defaultMetadataStorage.findExposeMetadataByCustomName(targetType, key);
                        if (exposeMetadata) {
                            propertyName = exposeMetadata.propertyName;
                            newValueKey = exposeMetadata.propertyName;
                        }
                    } else if (this_1.transformationType === "classToPlain" || this_1.transformationType === "classToClass") {
                        var exposeMetadata = storage_1.defaultMetadataStorage.findExposeMetadata(targetType, key);
                        if (exposeMetadata && exposeMetadata.options && exposeMetadata.options.name) newValueKey = exposeMetadata.options.name;
                    }
                }
                // get a subvalue
                var subValue = undefined;
                if (value instanceof Map) {
                    subValue = value.get(valueKey);
                } else if (value[valueKey] instanceof Function) {
                    subValue = value[valueKey]();
                } else {
                    subValue = value[valueKey];
                }
                // determine a type
                var type = undefined,
                    isSubValueMap = subValue instanceof Map;
                if (targetType && isMap) {
                    type = targetType;
                } else if (targetType) {
                    var metadata = storage_1.defaultMetadataStorage.findTypeMetadata(targetType, propertyName);
                    if (metadata) {
                        var options = { newObject: newValue, object: value, property: propertyName };
                        type = metadata.typeFunction(options);
                        isSubValueMap = isSubValueMap || metadata.reflectedType === Map;
                    } else if (this_1.options.targetMaps) {
                        this_1.options.targetMaps.filter(function (map) {
                            return map.target === targetType && !!map.properties[propertyName];
                        }).forEach(function (map) {
                            return type = map.properties[propertyName];
                        });
                    }
                }
                // if value is an array try to get its custom array type
                var arrayType_1 = value[valueKey] instanceof Array ? this_1.getReflectedType(targetType, propertyName) : undefined;
                // const subValueKey = operationType === "plainToClass" && newKeyName ? newKeyName : key;
                var subSource = source ? source[valueKey] : undefined;
                // if its deserialization then type if required
                // if we uncomment this types like string[] will not work
                // if (this.transformationType === "plainToClass" && !type && subValue instanceof Object && !(subValue instanceof Date))
                //     throw new Error(`Cannot determine type for ${(targetType as any).name }.${propertyName}, did you forget to specify a @Type?`);
                // if newValue is a source object that has method that match newKeyName then skip it
                if (newValue.constructor.prototype) {
                    var descriptor = Object.getOwnPropertyDescriptor(newValue.constructor.prototype, newValueKey);
                    if ((this_1.transformationType === "plainToClass" || this_1.transformationType === "classToClass") && (newValue[newValueKey] instanceof Function || descriptor && !descriptor.set)) return "continue";
                }
                if (!this_1.isCircular(subValue, level)) {
                    var transformKey = this_1.transformationType === "plainToClass" ? newValueKey : key;
                    var finalValue = this_1.transform(subSource, subValue, type, arrayType_1, isSubValueMap, level + 1);
                    finalValue = this_1.applyCustomTransformations(finalValue, targetType, transformKey);
                    if (newValue instanceof Map) {
                        newValue.set(newValueKey, finalValue);
                    } else {
                        newValue[newValueKey] = finalValue;
                    }
                } else if (this_1.transformationType === "classToClass") {
                    var finalValue = subValue;
                    finalValue = this_1.applyCustomTransformations(finalValue, targetType, key);
                    if (newValue instanceof Map) {
                        newValue.set(newValueKey, finalValue);
                    } else {
                        newValue[newValueKey] = finalValue;
                    }
                }
            };
            var this_1 = this;
            for (var _i = 0, keys_1 = keys; _i < keys_1.length; _i++) {
                var key = keys_1[_i];
                _loop_1(key);
            }
            return newValue;
        } else {
            return value;
        }
    };
    TransformOperationExecutor.prototype.applyCustomTransformations = function (value, target, key) {
        var _this = this;
        var metadatas = storage_1.defaultMetadataStorage.findTransformMetadatas(target, key, this.transformationType);
        // apply versioning options
        if (this.options.version !== undefined) {
            metadatas = metadatas.filter(function (metadata) {
                if (!metadata.options) return true;
                return _this.checkVersion(metadata.options.since, metadata.options.until);
            });
        }
        // apply grouping options
        if (this.options.groups && this.options.groups.length) {
            metadatas = metadatas.filter(function (metadata) {
                if (!metadata.options) return true;
                return _this.checkGroups(metadata.options.groups);
            });
        } else {
            metadatas = metadatas.filter(function (metadata) {
                return !metadata.options || !metadata.options.groups || !metadata.options.groups.length;
            });
        }
        metadatas.forEach(function (metadata) {
            value = metadata.transformFn(value);
        });
        return value;
    };
    // preventing circular references
    TransformOperationExecutor.prototype.isCircular = function (object, level) {
        return !!this.transformedTypes.find(function (transformed) {
            return transformed.object === object && transformed.level < level;
        });
    };
    TransformOperationExecutor.prototype.getReflectedType = function (target, propertyName) {
        if (!target) return undefined;
        var meta = storage_1.defaultMetadataStorage.findTypeMetadata(target, propertyName);
        return meta ? meta.reflectedType : undefined;
    };
    TransformOperationExecutor.prototype.getKeys = function (target, object) {
        var _this = this;
        // determine exclusion strategy
        var strategy = storage_1.defaultMetadataStorage.getStrategy(target);
        if (strategy === "none") strategy = this.options.strategy || "exposeAll"; // exposeAll is default strategy
        // get all keys that need to expose
        var keys = [];
        if (strategy === "exposeAll") {
            if (object instanceof Map) {
                keys = Array.from(object.keys());
            } else {
                keys = Object.keys(object);
            }
        }
        if (!this.options.ignoreDecorators && target) {
            // add all exposed to list of keys
            var exposedProperties = storage_1.defaultMetadataStorage.getExposedProperties(target, this.transformationType);
            if (this.transformationType === "plainToClass") {
                exposedProperties = exposedProperties.map(function (key) {
                    var exposeMetadata = storage_1.defaultMetadataStorage.findExposeMetadata(target, key);
                    if (exposeMetadata && exposeMetadata.options && exposeMetadata.options.name) {
                        return exposeMetadata.options.name;
                    }
                    return key;
                });
            }
            keys = keys.concat(exposedProperties);
            // exclude excluded properties
            var excludedProperties_1 = storage_1.defaultMetadataStorage.getExcludedProperties(target, this.transformationType);
            if (excludedProperties_1.length > 0) {
                keys = keys.filter(function (key) {
                    return excludedProperties_1.indexOf(key) === -1;
                });
            }
            // apply versioning options
            if (this.options.version !== undefined) {
                keys = keys.filter(function (key) {
                    var exposeMetadata = storage_1.defaultMetadataStorage.findExposeMetadata(target, key);
                    if (!exposeMetadata || !exposeMetadata.options) return true;
                    return _this.checkVersion(exposeMetadata.options.since, exposeMetadata.options.until);
                });
            }
            // apply grouping options
            if (this.options.groups && this.options.groups.length) {
                keys = keys.filter(function (key) {
                    var exposeMetadata = storage_1.defaultMetadataStorage.findExposeMetadata(target, key);
                    if (!exposeMetadata || !exposeMetadata.options) return true;
                    return _this.checkGroups(exposeMetadata.options.groups);
                });
            } else {
                keys = keys.filter(function (key) {
                    var exposeMetadata = storage_1.defaultMetadataStorage.findExposeMetadata(target, key);
                    return !exposeMetadata || !exposeMetadata.options || !exposeMetadata.options.groups || !exposeMetadata.options.groups.length;
                });
            }
        }
        // exclude prefixed properties
        if (this.options.excludePrefixes && this.options.excludePrefixes.length) {
            keys = keys.filter(function (key) {
                return _this.options.excludePrefixes.every(function (prefix) {
                    return key.substr(0, prefix.length) !== prefix;
                });
            });
        }
        // make sure we have unique keys
        keys = keys.filter(function (key, index, self) {
            return self.indexOf(key) === index;
        });
        return keys;
    };
    TransformOperationExecutor.prototype.checkVersion = function (since, until) {
        var decision = true;
        if (decision && since) decision = this.options.version >= since;
        if (decision && until) decision = this.options.version < until;
        return decision;
    };
    TransformOperationExecutor.prototype.checkGroups = function (groups) {
        if (!groups) return true;
        return this.options.groups.some(function (optionGroup) {
            return groups.indexOf(optionGroup) !== -1;
        });
    };
    return TransformOperationExecutor;
}();
exports.TransformOperationExecutor = TransformOperationExecutor;

//# sourceMappingURL=TransformOperationExecutor.js.map

/***/ }),
/* 7 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


var ClassTransformer_1 = __webpack_require__(2);
var storage_1 = __webpack_require__(4);
var TypeMetadata_1 = __webpack_require__(13);
var ExposeMetadata_1 = __webpack_require__(10);
var ExcludeMetadata_1 = __webpack_require__(9);
var TransformMetadata_1 = __webpack_require__(12);
/**
 * Defines a custom logic for value transformation.
 */
function Transform(transformFn, options) {
    return function (target, key) {
        var metadata = new TransformMetadata_1.TransformMetadata(target.constructor, key, transformFn, options);
        storage_1.defaultMetadataStorage.addTransformMetadata(metadata);
    };
}
exports.Transform = Transform;
/**
 * Specifies a type of the property.
 */
function Type(typeFunction) {
    return function (target, key) {
        var type = Reflect.getMetadata("design:type", target, key);
        var metadata = new TypeMetadata_1.TypeMetadata(target.constructor, key, type, typeFunction);
        storage_1.defaultMetadataStorage.addTypeMetadata(metadata);
    };
}
exports.Type = Type;
/**
 * Marks property as included in the process of transformation. By default it includes the property for both
 * constructorToPlain and plainToConstructor transformations, however you can specify on which of transformation types
 * you want to skip this property.
 */
function Expose(options) {
    return function (object, propertyName) {
        var metadata = new ExposeMetadata_1.ExposeMetadata(object instanceof Function ? object : object.constructor, propertyName, options || {});
        storage_1.defaultMetadataStorage.addExposeMetadata(metadata);
    };
}
exports.Expose = Expose;
/**
 * Marks property as excluded from the process of transformation. By default it excludes the property for both
 * constructorToPlain and plainToConstructor transformations, however you can specify on which of transformation types
 * you want to skip this property.
 */
function Exclude(options) {
    return function (object, propertyName) {
        var metadata = new ExcludeMetadata_1.ExcludeMetadata(object instanceof Function ? object : object.constructor, propertyName, options || {});
        storage_1.defaultMetadataStorage.addExcludeMetadata(metadata);
    };
}
exports.Exclude = Exclude;
/**
 * Transform the object from class to plain object and return only with the exposed properties.
 */
function TransformClassToPlain(params) {
    return function (target, propertyKey, descriptor) {
        var classTransformer = new ClassTransformer_1.ClassTransformer();
        var originalMethod = descriptor.value;
        descriptor.value = function () {
            var args = [];
            for (var _i = 0; _i < arguments.length; _i++) {
                args[_i - 0] = arguments[_i];
            }
            var result = originalMethod.apply(this, args);
            var isPromise = !!result && (typeof result === "object" || typeof result === "function") && typeof result.then === "function";
            return isPromise ? result.then(function (data) {
                return classTransformer.classToPlain(data, params);
            }) : classTransformer.classToPlain(result, params);
        };
    };
}
exports.TransformClassToPlain = TransformClassToPlain;
/**
 * Return the class instance only with the exposed properties.
 */
function TransformClassToClass(params) {
    return function (target, propertyKey, descriptor) {
        var classTransformer = new ClassTransformer_1.ClassTransformer();
        var originalMethod = descriptor.value;
        descriptor.value = function () {
            var args = [];
            for (var _i = 0; _i < arguments.length; _i++) {
                args[_i - 0] = arguments[_i];
            }
            var result = originalMethod.apply(this, args);
            var isPromise = !!result && (typeof result === "object" || typeof result === "function") && typeof result.then === "function";
            return isPromise ? result.then(function (data) {
                return classTransformer.classToClass(data, params);
            }) : classTransformer.classToClass(result, params);
        };
    };
}
exports.TransformClassToClass = TransformClassToClass;

//# sourceMappingURL=decorators.js.map

/***/ }),
/* 8 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


function __export(m) {
    for (var p in m) if (!exports.hasOwnProperty(p)) exports[p] = m[p];
}
var ClassTransformer_1 = __webpack_require__(2);
var ClassTransformer_2 = __webpack_require__(2);
exports.ClassTransformer = ClassTransformer_2.ClassTransformer;
__export(__webpack_require__(7));
var classTransformer = new ClassTransformer_1.ClassTransformer();
function classToPlain(object, options) {
    return classTransformer.classToPlain(object, options);
}
exports.classToPlain = classToPlain;
function classToPlainFromExist(object, plainObject, options) {
    return classTransformer.classToPlainFromExist(object, plainObject, options);
}
exports.classToPlainFromExist = classToPlainFromExist;
function plainToClass(cls, plain, options) {
    return classTransformer.plainToClass(cls, plain, options);
}
exports.plainToClass = plainToClass;
function plainToClassFromExist(clsObject, plain, options) {
    return classTransformer.plainToClassFromExist(clsObject, plain, options);
}
exports.plainToClassFromExist = plainToClassFromExist;
function classToClass(object, options) {
    return classTransformer.classToClass(object, options);
}
exports.classToClass = classToClass;
function classToClassFromExist(object, fromObject, options) {
    return classTransformer.classToClassFromExist(object, fromObject, options);
}
exports.classToClassFromExist = classToClassFromExist;
function serialize(object, options) {
    return classTransformer.serialize(object, options);
}
exports.serialize = serialize;
/**
 * Deserializes given JSON string to a object of the given class.
 */
function deserialize(cls, json, options) {
    return classTransformer.deserialize(cls, json, options);
}
exports.deserialize = deserialize;
/**
 * Deserializes given JSON string to an array of objects of the given class.
 */
function deserializeArray(cls, json, options) {
    return classTransformer.deserializeArray(cls, json, options);
}
exports.deserializeArray = deserializeArray;

//# sourceMappingURL=index.js.map

/***/ }),
/* 9 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


var ExcludeMetadata = function () {
    function ExcludeMetadata(target, propertyName, options) {
        this.target = target;
        this.propertyName = propertyName;
        this.options = options;
    }
    return ExcludeMetadata;
}();
exports.ExcludeMetadata = ExcludeMetadata;

//# sourceMappingURL=ExcludeMetadata.js.map

/***/ }),
/* 10 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


var ExposeMetadata = function () {
    function ExposeMetadata(target, propertyName, options) {
        this.target = target;
        this.propertyName = propertyName;
        this.options = options;
    }
    return ExposeMetadata;
}();
exports.ExposeMetadata = ExposeMetadata;

//# sourceMappingURL=ExposeMetadata.js.map

/***/ }),
/* 11 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";

/**
 * Storage all library metadata.
 */

var MetadataStorage = function () {
    function MetadataStorage() {
        // -------------------------------------------------------------------------
        // Properties
        // -------------------------------------------------------------------------
        this._typeMetadatas = [];
        this._transformMetadatas = [];
        this._exposeMetadatas = [];
        this._excludeMetadatas = [];
    }
    // -------------------------------------------------------------------------
    // Adder Methods
    // -------------------------------------------------------------------------
    MetadataStorage.prototype.addTypeMetadata = function (metadata) {
        this._typeMetadatas.push(metadata);
    };
    MetadataStorage.prototype.addTransformMetadata = function (metadata) {
        this._transformMetadatas.push(metadata);
    };
    MetadataStorage.prototype.addExposeMetadata = function (metadata) {
        this._exposeMetadatas.push(metadata);
    };
    MetadataStorage.prototype.addExcludeMetadata = function (metadata) {
        this._excludeMetadatas.push(metadata);
    };
    // -------------------------------------------------------------------------
    // Public Methods
    // -------------------------------------------------------------------------
    MetadataStorage.prototype.findTransformMetadatas = function (target, propertyName, transformationType) {
        return this.findMetadatas(this._transformMetadatas, target, propertyName).filter(function (metadata) {
            if (!metadata.options) return true;
            if (metadata.options.toClassOnly === true && metadata.options.toPlainOnly === true) return true;
            if (metadata.options.toClassOnly === true) {
                return transformationType === "classToClass" || transformationType === "plainToClass";
            }
            if (metadata.options.toPlainOnly === true) {
                return transformationType === "classToPlain";
            }
            return true;
        });
    };
    MetadataStorage.prototype.findExcludeMetadata = function (target, propertyName) {
        return this.findMetadata(this._excludeMetadatas, target, propertyName);
    };
    MetadataStorage.prototype.findExposeMetadata = function (target, propertyName) {
        return this.findMetadata(this._exposeMetadatas, target, propertyName);
    };
    MetadataStorage.prototype.findExposeMetadataByCustomName = function (target, name) {
        return this._exposeMetadatas.find(function (metadata) {
            return metadata.target === target && metadata.options && metadata.options.name === name;
        });
    };
    MetadataStorage.prototype.findTypeMetadata = function (target, propertyName) {
        return this.findMetadata(this._typeMetadatas, target, propertyName);
    };
    MetadataStorage.prototype.getStrategy = function (target) {
        var exclude = this._excludeMetadatas.find(function (metadata) {
            return metadata.target === target && metadata.propertyName === undefined;
        });
        var expose = this._exposeMetadatas.find(function (metadata) {
            return metadata.target === target && metadata.propertyName === undefined;
        });
        if (exclude && expose || !exclude && !expose) return "none";
        return exclude ? "excludeAll" : "exposeAll";
    };
    MetadataStorage.prototype.getExposedMetadatas = function (target) {
        return this.getMetadata(this._exposeMetadatas, target);
    };
    MetadataStorage.prototype.getExcludedMetadatas = function (target) {
        return this.getMetadata(this._excludeMetadatas, target);
    };
    MetadataStorage.prototype.getExposedProperties = function (target, transformationType) {
        return this.getExposedMetadatas(target).filter(function (metadata) {
            if (!metadata.options) return true;
            if (metadata.options.toClassOnly === true && metadata.options.toPlainOnly === true) return true;
            if (metadata.options.toClassOnly === true) {
                return transformationType === "classToClass" || transformationType === "plainToClass";
            }
            if (metadata.options.toPlainOnly === true) {
                return transformationType === "classToPlain";
            }
            return true;
        }).map(function (metadata) {
            return metadata.propertyName;
        });
    };
    MetadataStorage.prototype.getExcludedProperties = function (target, transformationType) {
        return this.getExcludedMetadatas(target).filter(function (metadata) {
            if (!metadata.options) return true;
            if (metadata.options.toClassOnly === true && metadata.options.toPlainOnly === true) return true;
            if (metadata.options.toClassOnly === true) {
                return transformationType === "classToClass" || transformationType === "plainToClass";
            }
            if (metadata.options.toPlainOnly === true) {
                return transformationType === "classToPlain";
            }
            return true;
        }).map(function (metadata) {
            return metadata.propertyName;
        });
    };
    MetadataStorage.prototype.clear = function () {
        this._typeMetadatas = [];
        this._exposeMetadatas = [];
        this._excludeMetadatas = [];
    };
    // -------------------------------------------------------------------------
    // Private Methods
    // -------------------------------------------------------------------------
    MetadataStorage.prototype.getMetadata = function (metadatas, target) {
        var metadataFromTarget = metadatas.filter(function (meta) {
            return meta.target === target && meta.propertyName !== undefined;
        });
        var metadataFromChildren = metadatas.filter(function (meta) {
            return target.prototype instanceof meta.target && meta.propertyName !== undefined;
        });
        return metadataFromChildren.concat(metadataFromTarget);
    };
    MetadataStorage.prototype.findMetadata = function (metadatas, target, propertyName) {
        var metadataFromTarget = metadatas.find(function (meta) {
            return meta.target === target && meta.propertyName === propertyName;
        });
        var metadataFromChildren = metadatas.find(function (meta) {
            return target.prototype instanceof meta.target && meta.propertyName === propertyName;
        });
        return metadataFromTarget || metadataFromChildren;
    };
    MetadataStorage.prototype.findMetadatas = function (metadatas, target, propertyName) {
        var metadataFromTarget = metadatas.filter(function (meta) {
            return meta.target === target && meta.propertyName === propertyName;
        });
        var metadataFromChildren = metadatas.filter(function (meta) {
            return target.prototype instanceof meta.target && meta.propertyName === propertyName;
        });
        return metadataFromChildren.reverse().concat(metadataFromTarget.reverse());
    };
    return MetadataStorage;
}();
exports.MetadataStorage = MetadataStorage;

//# sourceMappingURL=MetadataStorage.js.map

/***/ }),
/* 12 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


var TransformMetadata = function () {
    function TransformMetadata(target, propertyName, transformFn, options) {
        this.target = target;
        this.propertyName = propertyName;
        this.transformFn = transformFn;
        this.options = options;
    }
    return TransformMetadata;
}();
exports.TransformMetadata = TransformMetadata;

//# sourceMappingURL=TransformMetadata.js.map

/***/ }),
/* 13 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


var TypeMetadata = function () {
    function TypeMetadata(target, propertyName, reflectedType, typeFunction) {
        this.target = target;
        this.propertyName = propertyName;
        this.reflectedType = reflectedType;
        this.typeFunction = typeFunction;
    }
    return TypeMetadata;
}();
exports.TypeMetadata = TypeMetadata;

//# sourceMappingURL=TypeMetadata.js.map

/***/ }),
/* 14 */
/***/ (function(module, exports) {

// Copyright Joyent, Inc. and other Node contributors.
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to permit
// persons to whom the Software is furnished to do so, subject to the
// following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
// NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
// USE OR OTHER DEALINGS IN THE SOFTWARE.

function EventEmitter() {
  this._events = this._events || {};
  this._maxListeners = this._maxListeners || undefined;
}
module.exports = EventEmitter;

// Backwards-compat with node 0.10.x
EventEmitter.EventEmitter = EventEmitter;

EventEmitter.prototype._events = undefined;
EventEmitter.prototype._maxListeners = undefined;

// By default EventEmitters will print a warning if more than 10 listeners are
// added to it. This is a useful default which helps finding memory leaks.
EventEmitter.defaultMaxListeners = 10;

// Obviously not all Emitters should be limited to 10. This function allows
// that to be increased. Set to zero for unlimited.
EventEmitter.prototype.setMaxListeners = function (n) {
  if (!isNumber(n) || n < 0 || isNaN(n)) throw TypeError('n must be a positive number');
  this._maxListeners = n;
  return this;
};

EventEmitter.prototype.emit = function (type) {
  var er, handler, len, args, i, listeners;

  if (!this._events) this._events = {};

  // If there is no 'error' event listener then throw.
  if (type === 'error') {
    if (!this._events.error || isObject(this._events.error) && !this._events.error.length) {
      er = arguments[1];
      if (er instanceof Error) {
        throw er; // Unhandled 'error' event
      } else {
        // At least give some kind of context to the user
        var err = new Error('Uncaught, unspecified "error" event. (' + er + ')');
        err.context = er;
        throw err;
      }
    }
  }

  handler = this._events[type];

  if (isUndefined(handler)) return false;

  if (isFunction(handler)) {
    switch (arguments.length) {
      // fast cases
      case 1:
        handler.call(this);
        break;
      case 2:
        handler.call(this, arguments[1]);
        break;
      case 3:
        handler.call(this, arguments[1], arguments[2]);
        break;
      // slower
      default:
        args = Array.prototype.slice.call(arguments, 1);
        handler.apply(this, args);
    }
  } else if (isObject(handler)) {
    args = Array.prototype.slice.call(arguments, 1);
    listeners = handler.slice();
    len = listeners.length;
    for (i = 0; i < len; i++) listeners[i].apply(this, args);
  }

  return true;
};

EventEmitter.prototype.addListener = function (type, listener) {
  var m;

  if (!isFunction(listener)) throw TypeError('listener must be a function');

  if (!this._events) this._events = {};

  // To avoid recursion in the case that type === "newListener"! Before
  // adding it to the listeners, first emit "newListener".
  if (this._events.newListener) this.emit('newListener', type, isFunction(listener.listener) ? listener.listener : listener);

  if (!this._events[type])
    // Optimize the case of one listener. Don't need the extra array object.
    this._events[type] = listener;else if (isObject(this._events[type]))
    // If we've already got an array, just append.
    this._events[type].push(listener);else
    // Adding the second element, need to change to array.
    this._events[type] = [this._events[type], listener];

  // Check for listener leak
  if (isObject(this._events[type]) && !this._events[type].warned) {
    if (!isUndefined(this._maxListeners)) {
      m = this._maxListeners;
    } else {
      m = EventEmitter.defaultMaxListeners;
    }

    if (m && m > 0 && this._events[type].length > m) {
      this._events[type].warned = true;
      console.error('(node) warning: possible EventEmitter memory ' + 'leak detected. %d listeners added. ' + 'Use emitter.setMaxListeners() to increase limit.', this._events[type].length);
      if (typeof console.trace === 'function') {
        // not supported in IE 10
        console.trace();
      }
    }
  }

  return this;
};

EventEmitter.prototype.on = EventEmitter.prototype.addListener;

EventEmitter.prototype.once = function (type, listener) {
  if (!isFunction(listener)) throw TypeError('listener must be a function');

  var fired = false;

  function g() {
    this.removeListener(type, g);

    if (!fired) {
      fired = true;
      listener.apply(this, arguments);
    }
  }

  g.listener = listener;
  this.on(type, g);

  return this;
};

// emits a 'removeListener' event iff the listener was removed
EventEmitter.prototype.removeListener = function (type, listener) {
  var list, position, length, i;

  if (!isFunction(listener)) throw TypeError('listener must be a function');

  if (!this._events || !this._events[type]) return this;

  list = this._events[type];
  length = list.length;
  position = -1;

  if (list === listener || isFunction(list.listener) && list.listener === listener) {
    delete this._events[type];
    if (this._events.removeListener) this.emit('removeListener', type, listener);
  } else if (isObject(list)) {
    for (i = length; i-- > 0;) {
      if (list[i] === listener || list[i].listener && list[i].listener === listener) {
        position = i;
        break;
      }
    }

    if (position < 0) return this;

    if (list.length === 1) {
      list.length = 0;
      delete this._events[type];
    } else {
      list.splice(position, 1);
    }

    if (this._events.removeListener) this.emit('removeListener', type, listener);
  }

  return this;
};

EventEmitter.prototype.removeAllListeners = function (type) {
  var key, listeners;

  if (!this._events) return this;

  // not listening for removeListener, no need to emit
  if (!this._events.removeListener) {
    if (arguments.length === 0) this._events = {};else if (this._events[type]) delete this._events[type];
    return this;
  }

  // emit removeListener for all listeners on all events
  if (arguments.length === 0) {
    for (key in this._events) {
      if (key === 'removeListener') continue;
      this.removeAllListeners(key);
    }
    this.removeAllListeners('removeListener');
    this._events = {};
    return this;
  }

  listeners = this._events[type];

  if (isFunction(listeners)) {
    this.removeListener(type, listeners);
  } else if (listeners) {
    // LIFO order
    while (listeners.length) this.removeListener(type, listeners[listeners.length - 1]);
  }
  delete this._events[type];

  return this;
};

EventEmitter.prototype.listeners = function (type) {
  var ret;
  if (!this._events || !this._events[type]) ret = [];else if (isFunction(this._events[type])) ret = [this._events[type]];else ret = this._events[type].slice();
  return ret;
};

EventEmitter.prototype.listenerCount = function (type) {
  if (this._events) {
    var evlistener = this._events[type];

    if (isFunction(evlistener)) return 1;else if (evlistener) return evlistener.length;
  }
  return 0;
};

EventEmitter.listenerCount = function (emitter, type) {
  return emitter.listenerCount(type);
};

function isFunction(arg) {
  return typeof arg === 'function';
}

function isNumber(arg) {
  return typeof arg === 'number';
}

function isObject(arg) {
  return typeof arg === 'object' && arg !== null;
}

function isUndefined(arg) {
  return arg === void 0;
}

/***/ }),
/* 15 */
/***/ (function(module, exports) {

// shim for using process in browser
var process = module.exports = {};

// cached from whatever global is present so that test runners that stub it
// don't break things.  But we need to wrap it in a try catch in case it is
// wrapped in strict mode code which doesn't define any globals.  It's inside a
// function because try/catches deoptimize in certain engines.

var cachedSetTimeout;
var cachedClearTimeout;

function defaultSetTimout() {
    throw new Error('setTimeout has not been defined');
}
function defaultClearTimeout() {
    throw new Error('clearTimeout has not been defined');
}
(function () {
    try {
        if (typeof setTimeout === 'function') {
            cachedSetTimeout = setTimeout;
        } else {
            cachedSetTimeout = defaultSetTimout;
        }
    } catch (e) {
        cachedSetTimeout = defaultSetTimout;
    }
    try {
        if (typeof clearTimeout === 'function') {
            cachedClearTimeout = clearTimeout;
        } else {
            cachedClearTimeout = defaultClearTimeout;
        }
    } catch (e) {
        cachedClearTimeout = defaultClearTimeout;
    }
})();
function runTimeout(fun) {
    if (cachedSetTimeout === setTimeout) {
        //normal enviroments in sane situations
        return setTimeout(fun, 0);
    }
    // if setTimeout wasn't available but was latter defined
    if ((cachedSetTimeout === defaultSetTimout || !cachedSetTimeout) && setTimeout) {
        cachedSetTimeout = setTimeout;
        return setTimeout(fun, 0);
    }
    try {
        // when when somebody has screwed with setTimeout but no I.E. maddness
        return cachedSetTimeout(fun, 0);
    } catch (e) {
        try {
            // When we are in I.E. but the script has been evaled so I.E. doesn't trust the global object when called normally
            return cachedSetTimeout.call(null, fun, 0);
        } catch (e) {
            // same as above but when it's a version of I.E. that must have the global object for 'this', hopfully our context correct otherwise it will throw a global error
            return cachedSetTimeout.call(this, fun, 0);
        }
    }
}
function runClearTimeout(marker) {
    if (cachedClearTimeout === clearTimeout) {
        //normal enviroments in sane situations
        return clearTimeout(marker);
    }
    // if clearTimeout wasn't available but was latter defined
    if ((cachedClearTimeout === defaultClearTimeout || !cachedClearTimeout) && clearTimeout) {
        cachedClearTimeout = clearTimeout;
        return clearTimeout(marker);
    }
    try {
        // when when somebody has screwed with setTimeout but no I.E. maddness
        return cachedClearTimeout(marker);
    } catch (e) {
        try {
            // When we are in I.E. but the script has been evaled so I.E. doesn't  trust the global object when called normally
            return cachedClearTimeout.call(null, marker);
        } catch (e) {
            // same as above but when it's a version of I.E. that must have the global object for 'this', hopfully our context correct otherwise it will throw a global error.
            // Some versions of I.E. have different rules for clearTimeout vs setTimeout
            return cachedClearTimeout.call(this, marker);
        }
    }
}
var queue = [];
var draining = false;
var currentQueue;
var queueIndex = -1;

function cleanUpNextTick() {
    if (!draining || !currentQueue) {
        return;
    }
    draining = false;
    if (currentQueue.length) {
        queue = currentQueue.concat(queue);
    } else {
        queueIndex = -1;
    }
    if (queue.length) {
        drainQueue();
    }
}

function drainQueue() {
    if (draining) {
        return;
    }
    var timeout = runTimeout(cleanUpNextTick);
    draining = true;

    var len = queue.length;
    while (len) {
        currentQueue = queue;
        queue = [];
        while (++queueIndex < len) {
            if (currentQueue) {
                currentQueue[queueIndex].run();
            }
        }
        queueIndex = -1;
        len = queue.length;
    }
    currentQueue = null;
    draining = false;
    runClearTimeout(timeout);
}

process.nextTick = function (fun) {
    var args = new Array(arguments.length - 1);
    if (arguments.length > 1) {
        for (var i = 1; i < arguments.length; i++) {
            args[i - 1] = arguments[i];
        }
    }
    queue.push(new Item(fun, args));
    if (queue.length === 1 && !draining) {
        runTimeout(drainQueue);
    }
};

// v8 likes predictible objects
function Item(fun, array) {
    this.fun = fun;
    this.array = array;
}
Item.prototype.run = function () {
    this.fun.apply(null, this.array);
};
process.title = 'browser';
process.browser = true;
process.env = {};
process.argv = [];
process.version = ''; // empty string to avoid regexp issues
process.versions = {};

function noop() {}

process.on = noop;
process.addListener = noop;
process.once = noop;
process.off = noop;
process.removeListener = noop;
process.removeAllListeners = noop;
process.emit = noop;
process.prependListener = noop;
process.prependOnceListener = noop;

process.listeners = function (name) {
    return [];
};

process.binding = function (name) {
    throw new Error('process.binding is not supported');
};

process.cwd = function () {
    return '/';
};
process.chdir = function (dir) {
    throw new Error('process.chdir is not supported');
};
process.umask = function () {
    return 0;
};

/***/ }),
/* 16 */
/***/ (function(module, exports, __webpack_require__) {

/* WEBPACK VAR INJECTION */(function(process, global) {/*! *****************************************************************************
Copyright (C) Microsoft. All rights reserved.
Licensed under the Apache License, Version 2.0 (the "License"); you may not use
this file except in compliance with the License. You may obtain a copy of the
License at http://www.apache.org/licenses/LICENSE-2.0

THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
MERCHANTABLITY OR NON-INFRINGEMENT.

See the Apache Version 2.0 License for specific language governing permissions
and limitations under the License.
***************************************************************************** */
var Reflect;
(function (Reflect) {
    "use strict";

    var hasOwn = Object.prototype.hasOwnProperty;
    // feature test for Symbol support
    var supportsSymbol = typeof Symbol === "function";
    var toPrimitiveSymbol = supportsSymbol && typeof Symbol.toPrimitive !== "undefined" ? Symbol.toPrimitive : "@@toPrimitive";
    var iteratorSymbol = supportsSymbol && typeof Symbol.iterator !== "undefined" ? Symbol.iterator : "@@iterator";
    var HashMap;
    (function (HashMap) {
        var supportsCreate = typeof Object.create === "function"; // feature test for Object.create support
        var supportsProto = { __proto__: [] } instanceof Array; // feature test for __proto__ support
        var downLevel = !supportsCreate && !supportsProto;
        // create an object in dictionary mode (a.k.a. "slow" mode in v8)
        HashMap.create = supportsCreate ? function () {
            return MakeDictionary(Object.create(null));
        } : supportsProto ? function () {
            return MakeDictionary({ __proto__: null });
        } : function () {
            return MakeDictionary({});
        };
        HashMap.has = downLevel ? function (map, key) {
            return hasOwn.call(map, key);
        } : function (map, key) {
            return key in map;
        };
        HashMap.get = downLevel ? function (map, key) {
            return hasOwn.call(map, key) ? map[key] : undefined;
        } : function (map, key) {
            return map[key];
        };
    })(HashMap || (HashMap = {}));
    // Load global or shim versions of Map, Set, and WeakMap
    var functionPrototype = Object.getPrototypeOf(Function);
    var usePolyfill = typeof process === "object" && process.env && process.env["REFLECT_METADATA_USE_MAP_POLYFILL"] === "true";
    var _Map = !usePolyfill && typeof Map === "function" && typeof Map.prototype.entries === "function" ? Map : CreateMapPolyfill();
    var _Set = !usePolyfill && typeof Set === "function" && typeof Set.prototype.entries === "function" ? Set : CreateSetPolyfill();
    var _WeakMap = !usePolyfill && typeof WeakMap === "function" ? WeakMap : CreateWeakMapPolyfill();
    // [[Metadata]] internal slot
    // https://rbuckton.github.io/reflect-metadata/#ordinary-object-internal-methods-and-internal-slots
    var Metadata = new _WeakMap();
    /**
      * Applies a set of decorators to a property of a target object.
      * @param decorators An array of decorators.
      * @param target The target object.
      * @param propertyKey (Optional) The property key to decorate.
      * @param attributes (Optional) The property descriptor for the target key.
      * @remarks Decorators are applied in reverse order.
      * @example
      *
      *     class Example {
      *         // property declarations are not part of ES6, though they are valid in TypeScript:
      *         // static staticProperty;
      *         // property;
      *
      *         constructor(p) { }
      *         static staticMethod(p) { }
      *         method(p) { }
      *     }
      *
      *     // constructor
      *     Example = Reflect.decorate(decoratorsArray, Example);
      *
      *     // property (on constructor)
      *     Reflect.decorate(decoratorsArray, Example, "staticProperty");
      *
      *     // property (on prototype)
      *     Reflect.decorate(decoratorsArray, Example.prototype, "property");
      *
      *     // method (on constructor)
      *     Object.defineProperty(Example, "staticMethod",
      *         Reflect.decorate(decoratorsArray, Example, "staticMethod",
      *             Object.getOwnPropertyDescriptor(Example, "staticMethod")));
      *
      *     // method (on prototype)
      *     Object.defineProperty(Example.prototype, "method",
      *         Reflect.decorate(decoratorsArray, Example.prototype, "method",
      *             Object.getOwnPropertyDescriptor(Example.prototype, "method")));
      *
      */
    function decorate(decorators, target, propertyKey, attributes) {
        if (!IsUndefined(propertyKey)) {
            if (!IsArray(decorators)) throw new TypeError();
            if (!IsObject(target)) throw new TypeError();
            if (!IsObject(attributes) && !IsUndefined(attributes) && !IsNull(attributes)) throw new TypeError();
            if (IsNull(attributes)) attributes = undefined;
            propertyKey = ToPropertyKey(propertyKey);
            return DecorateProperty(decorators, target, propertyKey, attributes);
        } else {
            if (!IsArray(decorators)) throw new TypeError();
            if (!IsConstructor(target)) throw new TypeError();
            return DecorateConstructor(decorators, target);
        }
    }
    Reflect.decorate = decorate;
    // 4.1.2 Reflect.metadata(metadataKey, metadataValue)
    // https://rbuckton.github.io/reflect-metadata/#reflect.metadata
    /**
      * A default metadata decorator factory that can be used on a class, class member, or parameter.
      * @param metadataKey The key for the metadata entry.
      * @param metadataValue The value for the metadata entry.
      * @returns A decorator function.
      * @remarks
      * If `metadataKey` is already defined for the target and target key, the
      * metadataValue for that key will be overwritten.
      * @example
      *
      *     // constructor
      *     @Reflect.metadata(key, value)
      *     class Example {
      *     }
      *
      *     // property (on constructor, TypeScript only)
      *     class Example {
      *         @Reflect.metadata(key, value)
      *         static staticProperty;
      *     }
      *
      *     // property (on prototype, TypeScript only)
      *     class Example {
      *         @Reflect.metadata(key, value)
      *         property;
      *     }
      *
      *     // method (on constructor)
      *     class Example {
      *         @Reflect.metadata(key, value)
      *         static staticMethod() { }
      *     }
      *
      *     // method (on prototype)
      *     class Example {
      *         @Reflect.metadata(key, value)
      *         method() { }
      *     }
      *
      */
    function metadata(metadataKey, metadataValue) {
        function decorator(target, propertyKey) {
            if (!IsObject(target)) throw new TypeError();
            if (!IsUndefined(propertyKey) && !IsPropertyKey(propertyKey)) throw new TypeError();
            OrdinaryDefineOwnMetadata(metadataKey, metadataValue, target, propertyKey);
        }
        return decorator;
    }
    Reflect.metadata = metadata;
    /**
      * Define a unique metadata entry on the target.
      * @param metadataKey A key used to store and retrieve metadata.
      * @param metadataValue A value that contains attached metadata.
      * @param target The target object on which to define metadata.
      * @param propertyKey (Optional) The property key for the target.
      * @example
      *
      *     class Example {
      *         // property declarations are not part of ES6, though they are valid in TypeScript:
      *         // static staticProperty;
      *         // property;
      *
      *         constructor(p) { }
      *         static staticMethod(p) { }
      *         method(p) { }
      *     }
      *
      *     // constructor
      *     Reflect.defineMetadata("custom:annotation", options, Example);
      *
      *     // property (on constructor)
      *     Reflect.defineMetadata("custom:annotation", options, Example, "staticProperty");
      *
      *     // property (on prototype)
      *     Reflect.defineMetadata("custom:annotation", options, Example.prototype, "property");
      *
      *     // method (on constructor)
      *     Reflect.defineMetadata("custom:annotation", options, Example, "staticMethod");
      *
      *     // method (on prototype)
      *     Reflect.defineMetadata("custom:annotation", options, Example.prototype, "method");
      *
      *     // decorator factory as metadata-producing annotation.
      *     function MyAnnotation(options): Decorator {
      *         return (target, key?) => Reflect.defineMetadata("custom:annotation", options, target, key);
      *     }
      *
      */
    function defineMetadata(metadataKey, metadataValue, target, propertyKey) {
        if (!IsObject(target)) throw new TypeError();
        if (!IsUndefined(propertyKey)) propertyKey = ToPropertyKey(propertyKey);
        return OrdinaryDefineOwnMetadata(metadataKey, metadataValue, target, propertyKey);
    }
    Reflect.defineMetadata = defineMetadata;
    /**
      * Gets a value indicating whether the target object or its prototype chain has the provided metadata key defined.
      * @param metadataKey A key used to store and retrieve metadata.
      * @param target The target object on which the metadata is defined.
      * @param propertyKey (Optional) The property key for the target.
      * @returns `true` if the metadata key was defined on the target object or its prototype chain; otherwise, `false`.
      * @example
      *
      *     class Example {
      *         // property declarations are not part of ES6, though they are valid in TypeScript:
      *         // static staticProperty;
      *         // property;
      *
      *         constructor(p) { }
      *         static staticMethod(p) { }
      *         method(p) { }
      *     }
      *
      *     // constructor
      *     result = Reflect.hasMetadata("custom:annotation", Example);
      *
      *     // property (on constructor)
      *     result = Reflect.hasMetadata("custom:annotation", Example, "staticProperty");
      *
      *     // property (on prototype)
      *     result = Reflect.hasMetadata("custom:annotation", Example.prototype, "property");
      *
      *     // method (on constructor)
      *     result = Reflect.hasMetadata("custom:annotation", Example, "staticMethod");
      *
      *     // method (on prototype)
      *     result = Reflect.hasMetadata("custom:annotation", Example.prototype, "method");
      *
      */
    function hasMetadata(metadataKey, target, propertyKey) {
        if (!IsObject(target)) throw new TypeError();
        if (!IsUndefined(propertyKey)) propertyKey = ToPropertyKey(propertyKey);
        return OrdinaryHasMetadata(metadataKey, target, propertyKey);
    }
    Reflect.hasMetadata = hasMetadata;
    /**
      * Gets a value indicating whether the target object has the provided metadata key defined.
      * @param metadataKey A key used to store and retrieve metadata.
      * @param target The target object on which the metadata is defined.
      * @param propertyKey (Optional) The property key for the target.
      * @returns `true` if the metadata key was defined on the target object; otherwise, `false`.
      * @example
      *
      *     class Example {
      *         // property declarations are not part of ES6, though they are valid in TypeScript:
      *         // static staticProperty;
      *         // property;
      *
      *         constructor(p) { }
      *         static staticMethod(p) { }
      *         method(p) { }
      *     }
      *
      *     // constructor
      *     result = Reflect.hasOwnMetadata("custom:annotation", Example);
      *
      *     // property (on constructor)
      *     result = Reflect.hasOwnMetadata("custom:annotation", Example, "staticProperty");
      *
      *     // property (on prototype)
      *     result = Reflect.hasOwnMetadata("custom:annotation", Example.prototype, "property");
      *
      *     // method (on constructor)
      *     result = Reflect.hasOwnMetadata("custom:annotation", Example, "staticMethod");
      *
      *     // method (on prototype)
      *     result = Reflect.hasOwnMetadata("custom:annotation", Example.prototype, "method");
      *
      */
    function hasOwnMetadata(metadataKey, target, propertyKey) {
        if (!IsObject(target)) throw new TypeError();
        if (!IsUndefined(propertyKey)) propertyKey = ToPropertyKey(propertyKey);
        return OrdinaryHasOwnMetadata(metadataKey, target, propertyKey);
    }
    Reflect.hasOwnMetadata = hasOwnMetadata;
    /**
      * Gets the metadata value for the provided metadata key on the target object or its prototype chain.
      * @param metadataKey A key used to store and retrieve metadata.
      * @param target The target object on which the metadata is defined.
      * @param propertyKey (Optional) The property key for the target.
      * @returns The metadata value for the metadata key if found; otherwise, `undefined`.
      * @example
      *
      *     class Example {
      *         // property declarations are not part of ES6, though they are valid in TypeScript:
      *         // static staticProperty;
      *         // property;
      *
      *         constructor(p) { }
      *         static staticMethod(p) { }
      *         method(p) { }
      *     }
      *
      *     // constructor
      *     result = Reflect.getMetadata("custom:annotation", Example);
      *
      *     // property (on constructor)
      *     result = Reflect.getMetadata("custom:annotation", Example, "staticProperty");
      *
      *     // property (on prototype)
      *     result = Reflect.getMetadata("custom:annotation", Example.prototype, "property");
      *
      *     // method (on constructor)
      *     result = Reflect.getMetadata("custom:annotation", Example, "staticMethod");
      *
      *     // method (on prototype)
      *     result = Reflect.getMetadata("custom:annotation", Example.prototype, "method");
      *
      */
    function getMetadata(metadataKey, target, propertyKey) {
        if (!IsObject(target)) throw new TypeError();
        if (!IsUndefined(propertyKey)) propertyKey = ToPropertyKey(propertyKey);
        return OrdinaryGetMetadata(metadataKey, target, propertyKey);
    }
    Reflect.getMetadata = getMetadata;
    /**
      * Gets the metadata value for the provided metadata key on the target object.
      * @param metadataKey A key used to store and retrieve metadata.
      * @param target The target object on which the metadata is defined.
      * @param propertyKey (Optional) The property key for the target.
      * @returns The metadata value for the metadata key if found; otherwise, `undefined`.
      * @example
      *
      *     class Example {
      *         // property declarations are not part of ES6, though they are valid in TypeScript:
      *         // static staticProperty;
      *         // property;
      *
      *         constructor(p) { }
      *         static staticMethod(p) { }
      *         method(p) { }
      *     }
      *
      *     // constructor
      *     result = Reflect.getOwnMetadata("custom:annotation", Example);
      *
      *     // property (on constructor)
      *     result = Reflect.getOwnMetadata("custom:annotation", Example, "staticProperty");
      *
      *     // property (on prototype)
      *     result = Reflect.getOwnMetadata("custom:annotation", Example.prototype, "property");
      *
      *     // method (on constructor)
      *     result = Reflect.getOwnMetadata("custom:annotation", Example, "staticMethod");
      *
      *     // method (on prototype)
      *     result = Reflect.getOwnMetadata("custom:annotation", Example.prototype, "method");
      *
      */
    function getOwnMetadata(metadataKey, target, propertyKey) {
        if (!IsObject(target)) throw new TypeError();
        if (!IsUndefined(propertyKey)) propertyKey = ToPropertyKey(propertyKey);
        return OrdinaryGetOwnMetadata(metadataKey, target, propertyKey);
    }
    Reflect.getOwnMetadata = getOwnMetadata;
    /**
      * Gets the metadata keys defined on the target object or its prototype chain.
      * @param target The target object on which the metadata is defined.
      * @param propertyKey (Optional) The property key for the target.
      * @returns An array of unique metadata keys.
      * @example
      *
      *     class Example {
      *         // property declarations are not part of ES6, though they are valid in TypeScript:
      *         // static staticProperty;
      *         // property;
      *
      *         constructor(p) { }
      *         static staticMethod(p) { }
      *         method(p) { }
      *     }
      *
      *     // constructor
      *     result = Reflect.getMetadataKeys(Example);
      *
      *     // property (on constructor)
      *     result = Reflect.getMetadataKeys(Example, "staticProperty");
      *
      *     // property (on prototype)
      *     result = Reflect.getMetadataKeys(Example.prototype, "property");
      *
      *     // method (on constructor)
      *     result = Reflect.getMetadataKeys(Example, "staticMethod");
      *
      *     // method (on prototype)
      *     result = Reflect.getMetadataKeys(Example.prototype, "method");
      *
      */
    function getMetadataKeys(target, propertyKey) {
        if (!IsObject(target)) throw new TypeError();
        if (!IsUndefined(propertyKey)) propertyKey = ToPropertyKey(propertyKey);
        return OrdinaryMetadataKeys(target, propertyKey);
    }
    Reflect.getMetadataKeys = getMetadataKeys;
    /**
      * Gets the unique metadata keys defined on the target object.
      * @param target The target object on which the metadata is defined.
      * @param propertyKey (Optional) The property key for the target.
      * @returns An array of unique metadata keys.
      * @example
      *
      *     class Example {
      *         // property declarations are not part of ES6, though they are valid in TypeScript:
      *         // static staticProperty;
      *         // property;
      *
      *         constructor(p) { }
      *         static staticMethod(p) { }
      *         method(p) { }
      *     }
      *
      *     // constructor
      *     result = Reflect.getOwnMetadataKeys(Example);
      *
      *     // property (on constructor)
      *     result = Reflect.getOwnMetadataKeys(Example, "staticProperty");
      *
      *     // property (on prototype)
      *     result = Reflect.getOwnMetadataKeys(Example.prototype, "property");
      *
      *     // method (on constructor)
      *     result = Reflect.getOwnMetadataKeys(Example, "staticMethod");
      *
      *     // method (on prototype)
      *     result = Reflect.getOwnMetadataKeys(Example.prototype, "method");
      *
      */
    function getOwnMetadataKeys(target, propertyKey) {
        if (!IsObject(target)) throw new TypeError();
        if (!IsUndefined(propertyKey)) propertyKey = ToPropertyKey(propertyKey);
        return OrdinaryOwnMetadataKeys(target, propertyKey);
    }
    Reflect.getOwnMetadataKeys = getOwnMetadataKeys;
    /**
      * Deletes the metadata entry from the target object with the provided key.
      * @param metadataKey A key used to store and retrieve metadata.
      * @param target The target object on which the metadata is defined.
      * @param propertyKey (Optional) The property key for the target.
      * @returns `true` if the metadata entry was found and deleted; otherwise, false.
      * @example
      *
      *     class Example {
      *         // property declarations are not part of ES6, though they are valid in TypeScript:
      *         // static staticProperty;
      *         // property;
      *
      *         constructor(p) { }
      *         static staticMethod(p) { }
      *         method(p) { }
      *     }
      *
      *     // constructor
      *     result = Reflect.deleteMetadata("custom:annotation", Example);
      *
      *     // property (on constructor)
      *     result = Reflect.deleteMetadata("custom:annotation", Example, "staticProperty");
      *
      *     // property (on prototype)
      *     result = Reflect.deleteMetadata("custom:annotation", Example.prototype, "property");
      *
      *     // method (on constructor)
      *     result = Reflect.deleteMetadata("custom:annotation", Example, "staticMethod");
      *
      *     // method (on prototype)
      *     result = Reflect.deleteMetadata("custom:annotation", Example.prototype, "method");
      *
      */
    function deleteMetadata(metadataKey, target, propertyKey) {
        if (!IsObject(target)) throw new TypeError();
        if (!IsUndefined(propertyKey)) propertyKey = ToPropertyKey(propertyKey);
        var metadataMap = GetOrCreateMetadataMap(target, propertyKey, /*Create*/false);
        if (IsUndefined(metadataMap)) return false;
        if (!metadataMap.delete(metadataKey)) return false;
        if (metadataMap.size > 0) return true;
        var targetMetadata = Metadata.get(target);
        targetMetadata.delete(propertyKey);
        if (targetMetadata.size > 0) return true;
        Metadata.delete(target);
        return true;
    }
    Reflect.deleteMetadata = deleteMetadata;
    function DecorateConstructor(decorators, target) {
        for (var i = decorators.length - 1; i >= 0; --i) {
            var decorator = decorators[i];
            var decorated = decorator(target);
            if (!IsUndefined(decorated) && !IsNull(decorated)) {
                if (!IsConstructor(decorated)) throw new TypeError();
                target = decorated;
            }
        }
        return target;
    }
    function DecorateProperty(decorators, target, propertyKey, descriptor) {
        for (var i = decorators.length - 1; i >= 0; --i) {
            var decorator = decorators[i];
            var decorated = decorator(target, propertyKey, descriptor);
            if (!IsUndefined(decorated) && !IsNull(decorated)) {
                if (!IsObject(decorated)) throw new TypeError();
                descriptor = decorated;
            }
        }
        return descriptor;
    }
    function GetOrCreateMetadataMap(O, P, Create) {
        var targetMetadata = Metadata.get(O);
        if (IsUndefined(targetMetadata)) {
            if (!Create) return undefined;
            targetMetadata = new _Map();
            Metadata.set(O, targetMetadata);
        }
        var metadataMap = targetMetadata.get(P);
        if (IsUndefined(metadataMap)) {
            if (!Create) return undefined;
            metadataMap = new _Map();
            targetMetadata.set(P, metadataMap);
        }
        return metadataMap;
    }
    // 3.1.1.1 OrdinaryHasMetadata(MetadataKey, O, P)
    // https://rbuckton.github.io/reflect-metadata/#ordinaryhasmetadata
    function OrdinaryHasMetadata(MetadataKey, O, P) {
        var hasOwn = OrdinaryHasOwnMetadata(MetadataKey, O, P);
        if (hasOwn) return true;
        var parent = OrdinaryGetPrototypeOf(O);
        if (!IsNull(parent)) return OrdinaryHasMetadata(MetadataKey, parent, P);
        return false;
    }
    // 3.1.2.1 OrdinaryHasOwnMetadata(MetadataKey, O, P)
    // https://rbuckton.github.io/reflect-metadata/#ordinaryhasownmetadata
    function OrdinaryHasOwnMetadata(MetadataKey, O, P) {
        var metadataMap = GetOrCreateMetadataMap(O, P, /*Create*/false);
        if (IsUndefined(metadataMap)) return false;
        return ToBoolean(metadataMap.has(MetadataKey));
    }
    // 3.1.3.1 OrdinaryGetMetadata(MetadataKey, O, P)
    // https://rbuckton.github.io/reflect-metadata/#ordinarygetmetadata
    function OrdinaryGetMetadata(MetadataKey, O, P) {
        var hasOwn = OrdinaryHasOwnMetadata(MetadataKey, O, P);
        if (hasOwn) return OrdinaryGetOwnMetadata(MetadataKey, O, P);
        var parent = OrdinaryGetPrototypeOf(O);
        if (!IsNull(parent)) return OrdinaryGetMetadata(MetadataKey, parent, P);
        return undefined;
    }
    // 3.1.4.1 OrdinaryGetOwnMetadata(MetadataKey, O, P)
    // https://rbuckton.github.io/reflect-metadata/#ordinarygetownmetadata
    function OrdinaryGetOwnMetadata(MetadataKey, O, P) {
        var metadataMap = GetOrCreateMetadataMap(O, P, /*Create*/false);
        if (IsUndefined(metadataMap)) return undefined;
        return metadataMap.get(MetadataKey);
    }
    // 3.1.5.1 OrdinaryDefineOwnMetadata(MetadataKey, MetadataValue, O, P)
    // https://rbuckton.github.io/reflect-metadata/#ordinarydefineownmetadata
    function OrdinaryDefineOwnMetadata(MetadataKey, MetadataValue, O, P) {
        var metadataMap = GetOrCreateMetadataMap(O, P, /*Create*/true);
        metadataMap.set(MetadataKey, MetadataValue);
    }
    // 3.1.6.1 OrdinaryMetadataKeys(O, P)
    // https://rbuckton.github.io/reflect-metadata/#ordinarymetadatakeys
    function OrdinaryMetadataKeys(O, P) {
        var ownKeys = OrdinaryOwnMetadataKeys(O, P);
        var parent = OrdinaryGetPrototypeOf(O);
        if (parent === null) return ownKeys;
        var parentKeys = OrdinaryMetadataKeys(parent, P);
        if (parentKeys.length <= 0) return ownKeys;
        if (ownKeys.length <= 0) return parentKeys;
        var set = new _Set();
        var keys = [];
        for (var _i = 0, ownKeys_1 = ownKeys; _i < ownKeys_1.length; _i++) {
            var key = ownKeys_1[_i];
            var hasKey = set.has(key);
            if (!hasKey) {
                set.add(key);
                keys.push(key);
            }
        }
        for (var _a = 0, parentKeys_1 = parentKeys; _a < parentKeys_1.length; _a++) {
            var key = parentKeys_1[_a];
            var hasKey = set.has(key);
            if (!hasKey) {
                set.add(key);
                keys.push(key);
            }
        }
        return keys;
    }
    // 3.1.7.1 OrdinaryOwnMetadataKeys(O, P)
    // https://rbuckton.github.io/reflect-metadata/#ordinaryownmetadatakeys
    function OrdinaryOwnMetadataKeys(O, P) {
        var keys = [];
        var metadataMap = GetOrCreateMetadataMap(O, P, /*Create*/false);
        if (IsUndefined(metadataMap)) return keys;
        var keysObj = metadataMap.keys();
        var iterator = GetIterator(keysObj);
        var k = 0;
        while (true) {
            var next = IteratorStep(iterator);
            if (!next) {
                keys.length = k;
                return keys;
            }
            var nextValue = IteratorValue(next);
            try {
                keys[k] = nextValue;
            } catch (e) {
                try {
                    IteratorClose(iterator);
                } finally {
                    throw e;
                }
            }
            k++;
        }
    }
    // 6 ECMAScript Data Typ0es and Values
    // https://tc39.github.io/ecma262/#sec-ecmascript-data-types-and-values
    function Type(x) {
        if (x === null) return 1 /* Null */;
        switch (typeof x) {
            case "undefined":
                return 0 /* Undefined */;
            case "boolean":
                return 2 /* Boolean */;
            case "string":
                return 3 /* String */;
            case "symbol":
                return 4 /* Symbol */;
            case "number":
                return 5 /* Number */;
            case "object":
                return x === null ? 1 /* Null */ : 6 /* Object */;
            default:
                return 6 /* Object */;
        }
    }
    // 6.1.1 The Undefined Type
    // https://tc39.github.io/ecma262/#sec-ecmascript-language-types-undefined-type
    function IsUndefined(x) {
        return x === undefined;
    }
    // 6.1.2 The Null Type
    // https://tc39.github.io/ecma262/#sec-ecmascript-language-types-null-type
    function IsNull(x) {
        return x === null;
    }
    // 6.1.5 The Symbol Type
    // https://tc39.github.io/ecma262/#sec-ecmascript-language-types-symbol-type
    function IsSymbol(x) {
        return typeof x === "symbol";
    }
    // 6.1.7 The Object Type
    // https://tc39.github.io/ecma262/#sec-object-type
    function IsObject(x) {
        return typeof x === "object" ? x !== null : typeof x === "function";
    }
    // 7.1 Type Conversion
    // https://tc39.github.io/ecma262/#sec-type-conversion
    // 7.1.1 ToPrimitive(input [, PreferredType])
    // https://tc39.github.io/ecma262/#sec-toprimitive
    function ToPrimitive(input, PreferredType) {
        switch (Type(input)) {
            case 0 /* Undefined */:
                return input;
            case 1 /* Null */:
                return input;
            case 2 /* Boolean */:
                return input;
            case 3 /* String */:
                return input;
            case 4 /* Symbol */:
                return input;
            case 5 /* Number */:
                return input;
        }
        var hint = PreferredType === 3 /* String */ ? "string" : PreferredType === 5 /* Number */ ? "number" : "default";
        var exoticToPrim = GetMethod(input, toPrimitiveSymbol);
        if (exoticToPrim !== undefined) {
            var result = exoticToPrim.call(input, hint);
            if (IsObject(result)) throw new TypeError();
            return result;
        }
        return OrdinaryToPrimitive(input, hint === "default" ? "number" : hint);
    }
    // 7.1.1.1 OrdinaryToPrimitive(O, hint)
    // https://tc39.github.io/ecma262/#sec-ordinarytoprimitive
    function OrdinaryToPrimitive(O, hint) {
        if (hint === "string") {
            var toString_1 = O.toString;
            if (IsCallable(toString_1)) {
                var result = toString_1.call(O);
                if (!IsObject(result)) return result;
            }
            var valueOf = O.valueOf;
            if (IsCallable(valueOf)) {
                var result = valueOf.call(O);
                if (!IsObject(result)) return result;
            }
        } else {
            var valueOf = O.valueOf;
            if (IsCallable(valueOf)) {
                var result = valueOf.call(O);
                if (!IsObject(result)) return result;
            }
            var toString_2 = O.toString;
            if (IsCallable(toString_2)) {
                var result = toString_2.call(O);
                if (!IsObject(result)) return result;
            }
        }
        throw new TypeError();
    }
    // 7.1.2 ToBoolean(argument)
    // https://tc39.github.io/ecma262/2016/#sec-toboolean
    function ToBoolean(argument) {
        return !!argument;
    }
    // 7.1.12 ToString(argument)
    // https://tc39.github.io/ecma262/#sec-tostring
    function ToString(argument) {
        return "" + argument;
    }
    // 7.1.14 ToPropertyKey(argument)
    // https://tc39.github.io/ecma262/#sec-topropertykey
    function ToPropertyKey(argument) {
        var key = ToPrimitive(argument, 3 /* String */);
        if (IsSymbol(key)) return key;
        return ToString(key);
    }
    // 7.2 Testing and Comparison Operations
    // https://tc39.github.io/ecma262/#sec-testing-and-comparison-operations
    // 7.2.2 IsArray(argument)
    // https://tc39.github.io/ecma262/#sec-isarray
    function IsArray(argument) {
        return Array.isArray ? Array.isArray(argument) : argument instanceof Object ? argument instanceof Array : Object.prototype.toString.call(argument) === "[object Array]";
    }
    // 7.2.3 IsCallable(argument)
    // https://tc39.github.io/ecma262/#sec-iscallable
    function IsCallable(argument) {
        // NOTE: This is an approximation as we cannot check for [[Call]] internal method.
        return typeof argument === "function";
    }
    // 7.2.4 IsConstructor(argument)
    // https://tc39.github.io/ecma262/#sec-isconstructor
    function IsConstructor(argument) {
        // NOTE: This is an approximation as we cannot check for [[Construct]] internal method.
        return typeof argument === "function";
    }
    // 7.2.7 IsPropertyKey(argument)
    // https://tc39.github.io/ecma262/#sec-ispropertykey
    function IsPropertyKey(argument) {
        switch (Type(argument)) {
            case 3 /* String */:
                return true;
            case 4 /* Symbol */:
                return true;
            default:
                return false;
        }
    }
    // 7.3 Operations on Objects
    // https://tc39.github.io/ecma262/#sec-operations-on-objects
    // 7.3.9 GetMethod(V, P)
    // https://tc39.github.io/ecma262/#sec-getmethod
    function GetMethod(V, P) {
        var func = V[P];
        if (func === undefined || func === null) return undefined;
        if (!IsCallable(func)) throw new TypeError();
        return func;
    }
    // 7.4 Operations on Iterator Objects
    // https://tc39.github.io/ecma262/#sec-operations-on-iterator-objects
    function GetIterator(obj) {
        var method = GetMethod(obj, iteratorSymbol);
        if (!IsCallable(method)) throw new TypeError(); // from Call
        var iterator = method.call(obj);
        if (!IsObject(iterator)) throw new TypeError();
        return iterator;
    }
    // 7.4.4 IteratorValue(iterResult)
    // https://tc39.github.io/ecma262/2016/#sec-iteratorvalue
    function IteratorValue(iterResult) {
        return iterResult.value;
    }
    // 7.4.5 IteratorStep(iterator)
    // https://tc39.github.io/ecma262/#sec-iteratorstep
    function IteratorStep(iterator) {
        var result = iterator.next();
        return result.done ? false : result;
    }
    // 7.4.6 IteratorClose(iterator, completion)
    // https://tc39.github.io/ecma262/#sec-iteratorclose
    function IteratorClose(iterator) {
        var f = iterator["return"];
        if (f) f.call(iterator);
    }
    // 9.1 Ordinary Object Internal Methods and Internal Slots
    // https://tc39.github.io/ecma262/#sec-ordinary-object-internal-methods-and-internal-slots
    // 9.1.1.1 OrdinaryGetPrototypeOf(O)
    // https://tc39.github.io/ecma262/#sec-ordinarygetprototypeof
    function OrdinaryGetPrototypeOf(O) {
        var proto = Object.getPrototypeOf(O);
        if (typeof O !== "function" || O === functionPrototype) return proto;
        // TypeScript doesn't set __proto__ in ES5, as it's non-standard.
        // Try to determine the superclass constructor. Compatible implementations
        // must either set __proto__ on a subclass constructor to the superclass constructor,
        // or ensure each class has a valid `constructor` property on its prototype that
        // points back to the constructor.
        // If this is not the same as Function.[[Prototype]], then this is definately inherited.
        // This is the case when in ES6 or when using __proto__ in a compatible browser.
        if (proto !== functionPrototype) return proto;
        // If the super prototype is Object.prototype, null, or undefined, then we cannot determine the heritage.
        var prototype = O.prototype;
        var prototypeProto = prototype && Object.getPrototypeOf(prototype);
        if (prototypeProto == null || prototypeProto === Object.prototype) return proto;
        // If the constructor was not a function, then we cannot determine the heritage.
        var constructor = prototypeProto.constructor;
        if (typeof constructor !== "function") return proto;
        // If we have some kind of self-reference, then we cannot determine the heritage.
        if (constructor === O) return proto;
        // we have a pretty good guess at the heritage.
        return constructor;
    }
    // naive Map shim
    function CreateMapPolyfill() {
        var cacheSentinel = {};
        var arraySentinel = [];
        var MapIterator = function () {
            function MapIterator(keys, values, selector) {
                this._index = 0;
                this._keys = keys;
                this._values = values;
                this._selector = selector;
            }
            MapIterator.prototype["@@iterator"] = function () {
                return this;
            };
            MapIterator.prototype[iteratorSymbol] = function () {
                return this;
            };
            MapIterator.prototype.next = function () {
                var index = this._index;
                if (index >= 0 && index < this._keys.length) {
                    var result = this._selector(this._keys[index], this._values[index]);
                    if (index + 1 >= this._keys.length) {
                        this._index = -1;
                        this._keys = arraySentinel;
                        this._values = arraySentinel;
                    } else {
                        this._index++;
                    }
                    return { value: result, done: false };
                }
                return { value: undefined, done: true };
            };
            MapIterator.prototype.throw = function (error) {
                if (this._index >= 0) {
                    this._index = -1;
                    this._keys = arraySentinel;
                    this._values = arraySentinel;
                }
                throw error;
            };
            MapIterator.prototype.return = function (value) {
                if (this._index >= 0) {
                    this._index = -1;
                    this._keys = arraySentinel;
                    this._values = arraySentinel;
                }
                return { value: value, done: true };
            };
            return MapIterator;
        }();
        return function () {
            function Map() {
                this._keys = [];
                this._values = [];
                this._cacheKey = cacheSentinel;
                this._cacheIndex = -2;
            }
            Object.defineProperty(Map.prototype, "size", {
                get: function () {
                    return this._keys.length;
                },
                enumerable: true,
                configurable: true
            });
            Map.prototype.has = function (key) {
                return this._find(key, /*insert*/false) >= 0;
            };
            Map.prototype.get = function (key) {
                var index = this._find(key, /*insert*/false);
                return index >= 0 ? this._values[index] : undefined;
            };
            Map.prototype.set = function (key, value) {
                var index = this._find(key, /*insert*/true);
                this._values[index] = value;
                return this;
            };
            Map.prototype.delete = function (key) {
                var index = this._find(key, /*insert*/false);
                if (index >= 0) {
                    var size = this._keys.length;
                    for (var i = index + 1; i < size; i++) {
                        this._keys[i - 1] = this._keys[i];
                        this._values[i - 1] = this._values[i];
                    }
                    this._keys.length--;
                    this._values.length--;
                    if (key === this._cacheKey) {
                        this._cacheKey = cacheSentinel;
                        this._cacheIndex = -2;
                    }
                    return true;
                }
                return false;
            };
            Map.prototype.clear = function () {
                this._keys.length = 0;
                this._values.length = 0;
                this._cacheKey = cacheSentinel;
                this._cacheIndex = -2;
            };
            Map.prototype.keys = function () {
                return new MapIterator(this._keys, this._values, getKey);
            };
            Map.prototype.values = function () {
                return new MapIterator(this._keys, this._values, getValue);
            };
            Map.prototype.entries = function () {
                return new MapIterator(this._keys, this._values, getEntry);
            };
            Map.prototype["@@iterator"] = function () {
                return this.entries();
            };
            Map.prototype[iteratorSymbol] = function () {
                return this.entries();
            };
            Map.prototype._find = function (key, insert) {
                if (this._cacheKey !== key) {
                    this._cacheIndex = this._keys.indexOf(this._cacheKey = key);
                }
                if (this._cacheIndex < 0 && insert) {
                    this._cacheIndex = this._keys.length;
                    this._keys.push(key);
                    this._values.push(undefined);
                }
                return this._cacheIndex;
            };
            return Map;
        }();
        function getKey(key, _) {
            return key;
        }
        function getValue(_, value) {
            return value;
        }
        function getEntry(key, value) {
            return [key, value];
        }
    }
    // naive Set shim
    function CreateSetPolyfill() {
        return function () {
            function Set() {
                this._map = new _Map();
            }
            Object.defineProperty(Set.prototype, "size", {
                get: function () {
                    return this._map.size;
                },
                enumerable: true,
                configurable: true
            });
            Set.prototype.has = function (value) {
                return this._map.has(value);
            };
            Set.prototype.add = function (value) {
                return this._map.set(value, value), this;
            };
            Set.prototype.delete = function (value) {
                return this._map.delete(value);
            };
            Set.prototype.clear = function () {
                this._map.clear();
            };
            Set.prototype.keys = function () {
                return this._map.keys();
            };
            Set.prototype.values = function () {
                return this._map.values();
            };
            Set.prototype.entries = function () {
                return this._map.entries();
            };
            Set.prototype["@@iterator"] = function () {
                return this.keys();
            };
            Set.prototype[iteratorSymbol] = function () {
                return this.keys();
            };
            return Set;
        }();
    }
    // naive WeakMap shim
    function CreateWeakMapPolyfill() {
        var UUID_SIZE = 16;
        var keys = HashMap.create();
        var rootKey = CreateUniqueKey();
        return function () {
            function WeakMap() {
                this._key = CreateUniqueKey();
            }
            WeakMap.prototype.has = function (target) {
                var table = GetOrCreateWeakMapTable(target, /*create*/false);
                return table !== undefined ? HashMap.has(table, this._key) : false;
            };
            WeakMap.prototype.get = function (target) {
                var table = GetOrCreateWeakMapTable(target, /*create*/false);
                return table !== undefined ? HashMap.get(table, this._key) : undefined;
            };
            WeakMap.prototype.set = function (target, value) {
                var table = GetOrCreateWeakMapTable(target, /*create*/true);
                table[this._key] = value;
                return this;
            };
            WeakMap.prototype.delete = function (target) {
                var table = GetOrCreateWeakMapTable(target, /*create*/false);
                return table !== undefined ? delete table[this._key] : false;
            };
            WeakMap.prototype.clear = function () {
                // NOTE: not a real clear, just makes the previous data unreachable
                this._key = CreateUniqueKey();
            };
            return WeakMap;
        }();
        function CreateUniqueKey() {
            var key;
            do key = "@@WeakMap@@" + CreateUUID(); while (HashMap.has(keys, key));
            keys[key] = true;
            return key;
        }
        function GetOrCreateWeakMapTable(target, create) {
            if (!hasOwn.call(target, rootKey)) {
                if (!create) return undefined;
                Object.defineProperty(target, rootKey, { value: HashMap.create() });
            }
            return target[rootKey];
        }
        function FillRandomBytes(buffer, size) {
            for (var i = 0; i < size; ++i) buffer[i] = Math.random() * 0xff | 0;
            return buffer;
        }
        function GenRandomBytes(size) {
            if (typeof Uint8Array === "function") {
                if (typeof crypto !== "undefined") return crypto.getRandomValues(new Uint8Array(size));
                if (typeof msCrypto !== "undefined") return msCrypto.getRandomValues(new Uint8Array(size));
                return FillRandomBytes(new Uint8Array(size), size);
            }
            return FillRandomBytes(new Array(size), size);
        }
        function CreateUUID() {
            var data = GenRandomBytes(UUID_SIZE);
            // mark as random - RFC 4122 § 4.4
            data[6] = data[6] & 0x4f | 0x40;
            data[8] = data[8] & 0xbf | 0x80;
            var result = "";
            for (var offset = 0; offset < UUID_SIZE; ++offset) {
                var byte = data[offset];
                if (offset === 4 || offset === 6 || offset === 8) result += "-";
                if (byte < 16) result += "0";
                result += byte.toString(16).toLowerCase();
            }
            return result;
        }
    }
    // uses a heuristic used by v8 and chakra to force an object into dictionary mode.
    function MakeDictionary(obj) {
        obj.__ = undefined;
        delete obj.__;
        return obj;
    }
    // patch global Reflect
    (function (__global) {
        if (typeof __global.Reflect !== "undefined") {
            if (__global.Reflect !== Reflect) {
                for (var p in Reflect) {
                    if (hasOwn.call(Reflect, p)) {
                        __global.Reflect[p] = Reflect[p];
                    }
                }
            }
        } else {
            __global.Reflect = Reflect;
        }
    })(typeof global !== "undefined" ? global : typeof self !== "undefined" ? self : Function("return this;")());
})(Reflect || (Reflect = {}));
//# sourceMappingURL=Reflect.js.map
/* WEBPACK VAR INJECTION */}.call(exports, __webpack_require__(15), __webpack_require__(17)))

/***/ }),
/* 17 */
/***/ (function(module, exports) {

var g;

// This works in non-strict mode
g = function () {
	return this;
}();

try {
	// This works if eval is allowed (see CSP)
	g = g || Function("return this")() || (1, eval)("this");
} catch (e) {
	// This works if the window reference is available
	if (typeof window === "object") g = window;
}

// g can still be undefined, but nothing to do about it...
// We return undefined, instead of nothing here, so it's
// easier to handle this case. if(!global) { ...}

module.exports = g;

/***/ }),
/* 18 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";
//Run the Webpack task runner to pack it


Object.defineProperty(exports, "__esModule", { value: true });
/* Imports */
const buttplug_1 = __webpack_require__(0);
const Messages = __webpack_require__(0);
/* classes */
class DeviceHandler {
    constructor(url, onConnected, onReady) {
        this.devices = [];
        this.establishConnection = () => {
            this.client.Connect(this.url).then(this.connectedToClient, this.connectionFailed).then(this.requestDeviceList, this.logError).then(this.receivedDeviceList, this.logError);
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
            if (this.devices.length === 0) return;
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
                setTimeout(() => {
                    this.client.RequestDeviceList().then(this.receivedDeviceList, this.logError);
                }, 2000);
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
        };
        this.loadFunscript = response => {
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
        if (this.script == null) return 0;
        for (var i = 0; i < this.script.actions.length; i++) {
            if (this.script.actions[i].at >= time) return i;
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
ScriptPlayer.clampSpeed = speed => {
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

/***/ })
/******/ ]);