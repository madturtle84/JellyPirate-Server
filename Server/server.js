var app = require('express')();
var http = require('http').Server(app);
var io = require('socket.io')(http);
var request = require('request-json');
//var io = require("socket.io").listen(server);
require('json2.js');
var lobby = require('lobby.js');
// ---------- When Server Get http request ------------------
app.get('/', function(req, res){
    res.sendFile(__dirname + '/index.html');
});

//Maybe not use anymore　????
app.get('/unity', function(req, res){
    //uc.onWWWRequest(req,res,(mc.playersJSON));
    res.sendFile(__dirname+'/lobby.html');
    console.log("Not support http request for Unity Any more.");
});

app.get('/lobby', function(req,res){
    res.sendFile(__dirname+'/lobby.html');
});

// ----------- Socket Stuff ------------
io.on('connection', function(socket){
    console.log('Socket connected: '+socket.id+ ', ip = '+socket.request.connection.remoteAddress);
    socket.on('show all', function () {
        lobby.consoleShowAllRooms();
    });
    // ---------- Called by lobby.html -------------
    // -- When user click "Create and Join a New Room" in the lobby page.
    socket.on('create room', function(){
        lobby.createRoom();
    });

    // -- When user click an available room in lobby page.
    socket.on('join room', function(roomNum){
        var pid=lobby.playerJoinRoom(socket.id,roomNum);
        socket.emit('login accepted',pid);

    });

    // --
    socket.on('refresh lobby', function(){
        socket.emit('update lobby', lobby);
    });

    //
    socket.on('unity login', function(roomNum){
        if(lobby.unityJoinRoom(socket.id, roomNum)) socket.emit('unity joined');
        else socket.emit('unity denied');
      //  if(uc.processLoginRequest(socket.handshake.address,socket.id)) socket.emit('unity joined');
    });


    // -- Events Unity sending to Phone
    socket.on('u2p event', function(msg){
        var receiverSID = lobby.getU2PEventReceiver(socket.id, msg);
        // lobby.sendU2PEvent();
        var receiver = io.sockets.connected[receiverSID];
        console.log('receiver SID = '+ receiverSID +', receiver = '+receiver);
        if(receiver!=undefined)  {
            io.sockets.connected[receiverSID].emit('u2p event', msg);
            console.log('Unity message sent.');
        }
        else console.log('Unity message Fail. receiver='+receiver);

    });


    // ------------ Events Phone send to Unity -----------

    // -- Player will emit update whenever they touch the screen.
    // -- The playerData argument is restricted to JSON object due to the limitation of Unity Sockio plugin
    socket.on('position update', function(playerData){
        //console.log(lobby.getRoomWherePlayerLocated(socket.id));
        // If the caller is not in the game, ignore it
        if(lobby.getRoomWherePlayerLocated(socket.id)== -1) return;

        // Send Data to Unity
        var unitySID=lobby.getConnectedUnitySID(socket.id);
        if(unitySID!=0) {
            io.sockets.connected[unitySID].emit('unity update', playerData);
            //console.log(playerData);
        }
    });

    // Non-consistent events (Events other than position update)
    socket.on('p2u event', function(phoneMsg){
        // If the caller is not in the game, ignore it
        if(lobby.getRoomWherePlayerLocated(socket.id)== -1) return;

        // Send Data to Unity
        var unitySID=lobby.getConnectedUnitySID(socket.id);
        if(unitySID!=0) {
            io.sockets.connected[unitySID].emit('p2u event', phoneMsg);
            console.log('p2u event sent: ', phoneMsg);
        }
        else console.log('Unity client has not created');
    });



    // When user DC
    socket.on('disconnect', function(){
        lobby.playerDisconnected(socket.id);
        lobby.unityDisconnected(socket.id);
        console.log('Socket Destroyed');
    });



    // =====================  Debug Message ===========================


    socket.on('kick all', function(){
        //mc.removeAll();
    });
    socket.on('track package', function(isTracking){
        //mc.trackPackage(isTracking);
    });
    socket.on('ping', function(){
        socket.emit('pingback');
    });
    socket.on('ping unity', function(){
        //socket.connected[uc.socketID].emit('pingback');
        console.log('Pinging Unity');
    });
    // Print chat message
    socket.on('chat message', function(msg){
        console.log('Chat message: ' + msg);
        io.emit('chat message', msg);
    });
});
var portUsed = 3000;
http.listen(portUsed, function(){
    console.log('listening on *:'+portUsed);
});