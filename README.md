# Telegram Round Robin Chat Bot

This is a Telegram bot that manages and enforces round-robin style chat games.

## Usage

### Telegram Setup

* Create a telegram bot by starting a conversation with the *Telegram BotFather* (search for a 'user' called `BotFather`)
  - For a simple tutorial see https://core.telegram.org/bots#how-do-i-create-a-bot
* Use the *BotFather* to obtain a `token` for the bot, this is your *Api Key*
* Create a group and enable topics - each topic will be a separate game
* Invite the bot into the group and set it as administrator (having the bot as administrator is a requirement)

### On your PC

* Clone or download the repository
* Enter directory `LocalConsoleTest`
* Configure your secrets:
  * Run `dotnet user-secrets init`
  * Run `dotnet user-secrets set Telegram:ApiKey "<YOUR API KEY>"`
  * Run `dotnet user-secrets set ConnectionStrings:DefaultConnection "<FULL PATH TO 'rrbot_data' FILE>"`
* Run `dotnet run`

**NOTE:** The bot will function only when running in this way. If the bot is stopped, game rules won't
be enforced. However when the bot is later started again, it will "catch up" and enforce game rules on
all of the messages that accumulated while it was down.

### Managing Games

Type '/help' in a topic to see a list of commands, these are the commands that the bot responds to:

* `/help` - Displays the entire list of bot commands
* `/status` - Displays the status of the current game
* `/play` - Joins the sender to the current game
* `/showturn` - Show the messages sent in a specific turn
* `/kick` - (DM Only): Kick a player out of the game (but they may rejoin)
* `/pause` - (DM Only): Pause a game, everyone may chat normally and messages aren't recorded
* `/resume` - (DM Only): Resume a paused game
* `/endgame` - (DM Only): Permanently end the current game and archive all its messages
* `/start` - Starts a fresh game with you as its DM

## Round Robin Game Rules

When a game is active in a topic, every participant must send exactly one message in each turn.
As soon as all players have sent a message in the current turn, the turn advances automatically.
If a player tries to send a 2nd message in the same turn, the message is deleted from the topic,
and is sent as a private message back to the sender so that it doesn't get lost.

Editing your message in the current turn is allowed, but editing a message from a past turn is not.
If such a message is edited, it is immediately deleted from the topic and sent in a private
message back to the sender. Players in the game may always see the original messages sent at any
turn using the `/showturn` command.

A player who joined the game cannot later quit the game. Players are removed from the game only
when the DM uses the `/kick` command.
