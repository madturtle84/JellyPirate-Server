function ServerConnection() {
    this.online;
    this.socket;
    this.isLoggedIn;
    this.playerId;
    this.roleId;
    this.serverURL;
    this.u2pEventCallbacks=[];
    this.processU2PEvent;
}

ServerConnection.prototype = {
    serverURL: "http://legato.etc.cmu.edu:3000",

    init: function (online) {
        this.online = online;
        this.isLoggedIn = false;
        console.log('Initializing connection to server');
        if (this.online)
        {
            this.socket = io(serverURL);
            
            // Login
            this.socket.emit("join room", this.getRoomNumber());
            this.socket.on("login accepted", function(msg) {
                console.log("Sent from socket.io: " + msg);
                this.playerId = msg;
                console.log("playerId: " + this.playerId);
                this.isLoggedIn = true;
            }.bind(this)); // <------------------------------------- COOL!!!!!!

            this.socket.on("u2p event",function(unityMsg){
                this.processU2PEvent(unityMsg);
            }.bind(this) );
        }
    },

    sendP2UEvent:function(eventName){
        // Set local variable
        var data={'type':eventName};

        // Got better idea?
        data.p1=arguments[1];// Note: Argument[0] is the eventName
        data.p2=arguments[2];
        data.p3=arguments[3];
        data.p4=arguments[4];
        data.p5=arguments[5];
        data.p6=arguments[6];

        // Send event to server
        this.socket.emit("p2u event", data);
    },

    processU2PEvent: function(unityMsg){
        var eventName=unityMsg.type;
        var parameters=[];
        var pIdx=0;
        for(var key in unityMsg){
            if(unityMsg.hasOwnProperty(key)) parameters[pIdx]=unityMsg[key];
            pIdx++;
        }
        if(this.u2pEventCallbacks[eventName]!=undefined)
            this.u2pEventCallbacks[eventName](parameters[2],parameters[3],parameters[4],parameters[5],parameters[6],parameters[7]);
    },

    registerU2PEvent:function(eventName, callback){
        this.u2pEventCallbacks[eventName] = callback;//console.log(this.u2pEventCallbacks[eventName]);
    },

    /* Get Room number by parsing URL*/
    getRoomNumber: function () {
        var url = window.location.href;
        var queryStart = url.indexOf("=") + 1,
            queryEnd = url.indexOf("#") + 1 || url.length + 1,
            query = url.slice(queryStart, queryEnd - 1);
        if (query === url || query === "") {
            return 0;
        }
        return query;
    }

};
