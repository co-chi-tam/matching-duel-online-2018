
var GameRoom = require('./room'); // ROOM LOGIC

users = []; // Array User names
rooms = {}; // Rooms
column = 7; 
row = 8;

const MAXIMUM_ROOMS = 10; // Maximum rooms

var GameXO = function (http) {
    var io = require('socket.io')(http); // Require socket.io
    var self = this;
    // On client connect.
    io.on('connection', function(socket) {
        console.log('A user connected ' + (socket.client.id));
        // Welcome message
        socket.emit('welcome', { 
            msg: 'Welcome to connect game caro duel online.'
        });
        // INIT PLAYER
        // Set player name.
        socket.on('setPlayername', function(data) {
            if (data && data.playerName) {
                var isDuplicateName = false;
                for (let i = 0; i < users.length; i++) {
                    const u = users[i];
                    if (u.playerName == data.playerName) {
                        isDuplicateName = true;    
                        break;
                    }
                }
                if(isDuplicateName) {
                    socket.emit('msgError', { 
                        msg: data.playerName  + ' username is taken! Try some other username.'
                    });
                } else {
                    if (data.playerName.length < 5) {
                        socket.emit('msgError', { 
                            msg: data.playerName  + ' username must longer than 5 character'
                        });
                    } else {
                        socket.player = data;
                        socket.player.formation = "0:3:1,1:2:2,4:5:1,8:2:4,6:4:4";
                        users.push(data);
                        socket.emit('playerNameSet', { 
                            id: socket.client.id,
                            name: data.playerName 
                        });
                    }
                }
            }
        });
        // Set player formation.
        socket.on('setPlayerFormation', function(data) {
            if (data && data.formation) {
                socket.player.formation = data.formation;
                socket.emit('playerFormationSet', { 
                    id: socket.client.id,
                    formation: data.formation
                });
            }
        });
        // Receive beep mesg
        socket.on('beep', function(data) {
        socket.emit('boop');
        })
        // INIT ROOM
        // Get all room status
        socket.on('getRoomsStatus', function() {
            var results = [];
            for (let i = 0; i < MAXIMUM_ROOMS; i++) {
                const roomName = 'room-' + (i + 1);
                const playerCount = typeof (rooms [roomName]) !== 'undefined' 
                                        ? rooms [roomName].length()
                                        : 0;
                results.push ({
                    roomName: roomName,
                    roomDisplay: '[' + roomName + ']: ' + playerCount + '/2',
                    players: playerCount
                });
            }
            socket.emit('updateRoomStatus', {
                rooms: results
            });
        });
        // Join or create room by name. 
        socket.on('joinOrCreateRoom', function(playerJoin) {
            if(playerJoin && socket.player) {
                var roomName = playerJoin.roomName;
                if (typeof(rooms [roomName]) === 'undefined') {
                    rooms [roomName] = new GameRoom();
                }
                rooms [roomName].roomName = roomName;
                // SHUFFLE MATCHING MAP
                if (typeof(rooms [roomName].matchingMap) === 'undefined') {
                    rooms [roomName].matchingMap = self.generateMap();
                    rooms [roomName].exactlyTurn = 0;
                }
                if (rooms [roomName].contain (socket) == false) {
                    if (rooms [roomName].length() < 2) {
                        rooms [roomName].join (socket);
                        socket.room = rooms [roomName];
                        socket.player.chessLists = [];
                        rooms [roomName].emitAll('newJoinRoom', {
                            roomInfo: rooms [roomName].getInfo(),
                            matchingMap: rooms [roomName].matchingMap
                        });    
                        console.log ("A player join room. " + roomName + " Room: " + rooms [roomName].length());
                    } else {
                        socket.emit('joinRoomFailed', {
                            msg: "Room is full. Please try again late."
                        });
                    }
                } else {
                    socket.emit('joinRoomFailed', {
                        msg: "You are already join room."
                    });
                }
            }
        });
        // Send chess position with check available.
        socket.on('sendChessPosition', function(msg) {
            if(msg && socket.player && socket.room) {
                if (socket.room.length() > 1) {
                    var currentPos = {
                        x: msg.posX,    // parseInt
                        y: msg.posY     // parseInt
                    }
                    var gameCurrentTurn = socket.room.chessLists.length % 2;
                    var sendChecking = socket.game.turnIndex == msg.turnIndex 
                                    && socket.game.turnIndex == gameCurrentTurn;
                    // console.log (socket.game.turnIndex +" / "+ msg.turnIndex + " / " + gameCurrentTurn);
                    // CAN NOT USE INDEXOF HERE  
                    for (let i = 0; i < socket.room.chessLists.length; i++) {
                        const chess = socket.room.chessLists[i];
                        if (currentPos.x == chess.x && currentPos.y == chess.y) {
                            sendChecking = false;
                            break;
                        }
                    }
                    if (sendChecking) {
                        socket.room.emitAll('receiveChessPosition', {
                            user: socket.player.playerName,
                            currentPos,
                            turnIndex: socket.game.turnIndex
                        });
                        socket.room.chessLists.push(currentPos);
                        socket.player.chessLists.push(currentPos);
                    } else {
                        socket.emit('receiveChessFail', {
                            msg: msg.turnIndex != gameCurrentTurn 
                                ? "This is NOT your turn."
                                : "You can NOT do that."
                        });
                    }
                }
            }
        });
        // Send chess position without check available.
        socket.on('sendChessSpot', function(msg) {
            if(msg && socket.player && socket.room) {
                if (socket.room.length() > 1) {
                    var currentPos = {
                        x: msg.posX,    // parseInt
                        y: msg.posY     // parseInt
                    }
                    var gameCurrentTurn = socket.room.chessLists.length % 2;
                    var sendChecking = socket.game.turnIndex == msg.turnIndex 
                                    && socket.game.turnIndex == gameCurrentTurn;
                    if (sendChecking) {
                        socket.room.emitAll('receiveChessPosition', {
                            user: socket.player.playerName,
                            currentPos,
                            turnIndex: socket.game.turnIndex
                        });
                        socket.room.chessLists.push(currentPos);
                        socket.player.chessLists.push(currentPos);
                    } else {
                        socket.emit('receiveChessFail', {
                            msg: msg.turnIndex != gameCurrentTurn 
                                ? "This is NOT your turn."
                                : "You can NOT do that."
                        });
                    }
                }
            }
        });
        // Send Matching position with check available.
        socket.on('sendMatchingSpot', function(msg) {
            if(msg && socket.player && socket.room) {
                if (socket.room.length() > 1 
                    && socket.room.matchingMap) {
                    // Exp: x:0,y:1    y * column + x = 7
                    var mPos1 = {
                        x: msg.pos1X,    // parseInt
                        y: msg.pos1Y     // parseInt
                    }
                    var mPos2 = {
                        x: msg.pos2X,    // parseInt
                        y: msg.pos2Y     // parseInt
                    }
                    var value1 = socket.room.matchingMap[mPos1.y * column + mPos1.x];
                    var value2 = socket.room.matchingMap[mPos2.y * column + mPos2.x];
                    var sendChecking = socket.game.turnIndex == msg.turnIndex
                                        && socket.game.turnIndex == socket.room.exactlyTurn;
                    if (sendChecking) {
                        var turnIndex = value1 == value2 ? socket.room.exactlyTurn : (socket.room.exactlyTurn + 1) % 2;
                        socket.room.exactlyTurn = turnIndex;
                        socket.room.emitAll('receiveMatchingPosition', {
                            user: socket.player.playerName,
                            pos1X: mPos1.x, 
                            pos1Y: mPos1.y,
                            pos2X: mPos2.x, 
                            pos2Y: mPos2.y,
                            turnIndex: turnIndex,
                            result: value1 == value2 ? 1 : 0
                        });
                        socket.room.chessLists.push(mPos1);
                        socket.room.chessLists.push(mPos2);
                        socket.player.chessLists.push(mPos1);
                        socket.player.chessLists.push(mPos2);
                    } else {
                        socket.emit('receiveChessFail', {
                            msg: msg.turnIndex != socket.room.exactlyTurn 
                                ? "This is NOT your turn."
                                : "You can NOT do that."
                        });
                    }
                }
            }
        });
        // Receive client chat in current room.
        socket.on('sendRoomChat', function(msg) {
            if(msg && socket.room) {
                socket.room.emitAll('msgChatRoom', {
                    user: socket.player.playerName,
                    message: msg.message
                });
            }
        });
        // Receive world chat.
        socket.on('sendWorldChat', function(msg) {
            if(msg) {
                // socket.broadcast.emit => will send the message to all the other clients except the newly created connection
                io.sockets.emit('msgWorldChat', {
                    user: socket.player.playerName,
                    message: msg.message
                });
            }
        });
        // Receive leave room mesg.
        socket.on('leaveRoom', function() {
            console.log ('User leave room...' + socket.id);
            if(socket.room) {
                var roomName = socket.room.roomName;
                socket.room.clearRoom();
                delete socket.room;
                delete rooms [roomName];
            }
        });
        // DISCONNECT
        // Disconnect and clear room.
        socket.on('disconnect', function() {
            console.log ('User disconnect...' + socket.id);
            if (socket.player) {
                for (let i = 0; i < users.length; i++) {
                    const u = users[i];
                    if (u.playerName == socket.player.playerName) {
                        users.splice(i, 1);  
                        break;
                    }
                }
            }
            // LEAVE ROOM
            if (socket.room) {
                var roomName = socket.room.roomName;
                socket.room.clearRoom();
                delete socket.room;
                delete rooms [roomName];
            }
        });
    });
    // GENERATE AND SHUFFLE ARRAY
    this.generateMap = function() {
        var a = [ 0,0,1,1,2,2,3,
            3,4,4,5,5,6,6,
            7,7,8,8,9,9,10,
            10,11,11,12,12,13,13,
            14,14,15,15,16,16,17,
            17,18,18,19,19,20,20,
            21,21,22,22,23,23,24,
            24,25,25,26,26,27,27 ]
        var j, x, i;
        for (i = a.length - 1; i > 0; i--) {
            j = Math.floor(Math.random() * (i + 1));
            x = a[i];
            a[i] = a[j];
            a[j] = x;
        }
        return a;
    }
};
// INIT
module.exports = GameXO;