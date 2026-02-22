# Unity Multiplayer Facade Template

A simple template for Multiplayer Facade. A system that allows switching of multiplayer services with one click of a button. 
Also includes a simple lobby listing and room hosting for testing

Currently support Unity Multiplayer Services and Steamworks via Facepunch. The service can easily be swapped by a dropdown in the MultiplayerFacade gameobject.

Whereby Unity Multiplayer System uses Authentication, Unity Relay, and Unity Lobby System
Whereby Steam service uses Steam Auth, Steam Relay and Steam Lobby System

For Steam, you need to use a different device (with different Steam account) to test it out as 1 instance can only be used on 1 PC as Steam uses your steam client to connect.

You can download/clone the repository OR use the export package to import it into your project. Then follow the instruction below:

# Import Project via Import Package

1. Clone or import the project in
2. Install packages: Authentication, Multiplayer Center, Multiplayer Play Mode, Multiplayer Services, Multiplayer Tools, Netcode for GameObjects and import text mesh pro
3. Go settings>player> Turn on Allow 'unsafe' Code
4. Add All the 4 scenes imported into the scene build list
5. Click Edit>Project Settings>Services
6. Under the Services General Settings: Select organization, chose new or existing cloud project then create the cloud project
7. Setup is done now. You can test it out

# To test out the service
1. Head over to lobby selection scene
2. Click on MultiplayerFacade game object and tick unity service type
3. Go Windows>Multiplayer>Multiplayer Play Mode
4. Open another instance of virtual player 2


# Scenes
1. Game Scene - Game Scene is where a simple cube movement that has client authoritative movement. It will spawn your client cube when you are connected together
2. Lobby Selection - The main scene you should open up. This is where the lobby listing, lobby creation and etc will be at
3. MultiplayerFacadeTesting - A control panel like scene where you can directly call the APIs of the services without caring about the UI
4. RoomLobby - Unable to be run by itself. It is a scene that you are redirected to when you create or join a lobby. It shows the player, the player's ready and lobby code etc.

# How it works
Basically, the project looks at what are the similar events and features in both Unity and Steam then attempt to create an interface to wrap around the two services. 

There is MultiplayerFacade which initializes the ServiceController which basically handles the authorization. Then the rest of the lobby and relay services are initialized by the MultiplayerFacade
When calling

Check out each script and especially the InterfaceService.cs script for more info on the internal working of the scripts

# Settings
Check out these scripts/objects for configs:
1. Project Assets> Settings> Network> SO_Network Config - You can change your AppID here. Open up the script for it to config the Logging Levels
     - Logging levels: Change the LOGGING_LEVEL in the script: Developer - ALL the network logs | Normal - Only important network logs | Error - Only show error logs

# Screenshots

<img width="1572" height="886" alt="image" src="https://github.com/user-attachments/assets/eafb15b2-18f2-4e50-a375-542338874f22" />
<img width="676" height="378" alt="image" src="https://github.com/user-attachments/assets/f6e3c4b9-b374-4aa9-a957-d1fd8d593e55" />
<img width="677" height="376" alt="image" src="https://github.com/user-attachments/assets/40c7aab7-fb83-498e-8da8-7d1a2b581106" />
<img width="675" height="381" alt="image" src="https://github.com/user-attachments/assets/a92f217a-9c89-465c-a7f2-947c54c35cb3" />
<img width="1912" height="927" alt="image" src="https://github.com/user-attachments/assets/e4bd3e33-d70b-4e90-8420-0c86f523664b" />

# Known issues
1. Doesn't handle forced disconnect (game crash) properly. Player will still be shown in lobby
2. Client will have error of "[LobbyRoomManager] Unable to ready up" and gets booted back to lobby listing as normal

