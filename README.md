# SAMonitor

SAMonitor is a free and open source service, tracking hundreds of SA-MP and open.mp servers.

For now, we mainly provide an API and a Masterlist replacement.

A server browser will be released soon.

## Contents

- [Masterlist](#masterlist)
- [GET Endpoints](#get-endpoints)
- [Data schemas](#data-schemas)
- [Add your server](#add-your-server)

## Masterlist

We offer a masterlist replacement, at http://gateway.markski.ar:42069/api/GetMasterlist

It is a relatively high-quality masterlist, since rather than functioning as a hastily-updated text file or database, it only provides servers which are actively running.
Servers which failed to be online in the last ~24 hours are not included in this list (but are re-added when they return).

## GET Endpoints

All of these endpoints are located at `http://gateway.markski.ar:42069/api/`.
The endpoint is currently `http` only. It might support `https` in the future, but honestly, given the sensitivity (or lack thereof) of this data, I see no benefits to using it here.

### GetAllServers

Return a JSON collection with the latest information of every server SAMonitor tracks.

Try it: http://gateway.markski.ar:42069/api/GetAllServers

### GetServerByIP

Provided an IP address (optionaly with a specified port), returns information about it.

If no port is provided and several servers are under that IP, the one at 7777 will be chosen.

Try it: http://gateway.markski.ar:42069/api/GetServerByIP?ip_addr=151.80.19.151:7777

### GetServersByName

Provided a text, return a list of the servers which include that text in their name. Basically search.

Try it: http://gateway.markski.ar:42069/api/GetServersByName?name=Roleplay

### GetServerPlayers

Provided an IP address (optionaly with a specified port), return a list of players.

Try it: http://gateway.markski.ar:42069/api/GetServerPlayers?ip_addr=51.68.204.178:7777

### GetTotalPlayers

Get a simple integer with the sum of players in the tracked servers.

Try it: http://gateway.markski.ar:42069/api/GetTotalPlayers

### GetAmountServers

A little pointless for most, but returns the amount of servers SAMonitor is tracking.

Try it: http://gateway.markski.ar:42069/api/GetAmountServers

## Data schemas

There are two schemas: Player and Server. All 'integer' are 32-bit unsigned.

The player schema is used for GetServerPlayers. It is the simplest schema, and will likely never change.

```
  id:    Integer
  ping:  Integer
  name:  String
  score: Integer
```

The server schema is likely to change as SAMonitor is fairly in-development. However breaking changes are unlikely and it might just come in the form of adding more fields.

```
  success:       Boolean
  lastUpdated:   DateTime
  worldTime:     DateTime (only the Time portion has the relevant data)
  playersOnline: Integer
  maxPlayers:    Integer
  allowsDL:      Boolean
  lagComp:       Boolean
  name:          String
  gameMode:      String
  ipAddr:        String
  mapName:       String
  website:       String
  language:      String
  sampCac:       String (Version of SAMPCAC required. "Not required" otherwise.)
```

The API ***should*** never return any null values. Either '0' or "Unknown" would come in their placee where appropiate. Still, because bugs exist, you may wish to account for the possibility of 'null' values.

## Add your server

A proper web interface for doing this will be available when the site launches.

However, if you wish to have your server added now, you may do it in the form of the following call:

### AddServer
Provided an IP address (with port!), adds the server to SAMonitor, if queriable. Otherwise fails. Should return a boolean indicating success or lack thereof.

Usage: `http://gateway.markski.ar:42069/api/AddServer?ip_addr=<ip-address:port>`
