# MultiplayerP2P
This is a server which would allow players to play together without hosting a dedicated server. \
Both the server and the client are clients of the Peer.

## Setup
1) Start the server and close it
2) Edit the `config.json` as you want
```js
{
    // The security check
    "security": {
        "checksAmount": 2,
        // Size is checksAmount
        "numbers": [
            // Has to be 4 numbers, the second one must be bigger than the third one
            [26, 54, 24, 78],
            [79, 67, 53, 38]
        ]
    },
    "peerPorts": [1338, 1339, 1340, 1341, 1342], // Peer ports
    "mainPort": 1337 // Main server port
}
```

## Protocol
I dunno how to document it, so check the TestApp code instead.
