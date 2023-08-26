# SAMonitor

This is the monorepo for SAMonitor, a free and open source service, tracking hundreds of SA-MP and open.mp servers.

Providing: A server browser, a public API and a Masterlist alternative.

## Contents

- [Server browser](#server-browser)
- [Masterlist](#masterlist)
- [GET Endpoints](#get-endpoints)
- - [GetAllServers](#getallservers)
- - [GetFilteredServers](#getfilteredservers)
- - [GetServerByIP](#getserverbyip)
- - [GetServerPlayers](#getserverplayers)
- - [GetTotalPlayers](#gettotalplayers)
- - [GetMetrics](#getmetrics)
- [Data schemas](#data-schemas)
- [Add your server](#add-your-server)

## Server browser

The server browser is available at https://sam.markski.ar

It is still at a very early stage. Eventually it'll have filtering among other features.

It consumes the public API endpoints listed below.

It is written in HTMX and PHP, which provides a very lightweight website where logic largely runs on the server side. Ideal for the types of computers and phones to be expected in regions where San Andreas remains popular.

## Masterlist

We offer a masterlist alternative, at http://sam.markski.ar:42069/api/GetMasterlist

It is a relatively high-quality masterlist, since rather than functioning as a hastily-updated text file or database, it only provides servers which are actively running.
Servers which failed to be online in the last ~24 hours are not included in this list (but are re-added when they return).

## GET Endpoints

All of these endpoints are located at `http://sam.markski.ar:42069/api/`.
The endpoint is currently `http` only. It might support `https` in the future, but honestly, given the sensitivity (or lack thereof) of this data, I see no benefits to using it here.

### GetAllServers

Return a JSON collection with the latest information of every server SAMonitor tracks.

Try it: http://sam.markski.ar:42069/api/GetAllServers

### GetFilteredServers

The most complex endpoint of all, but certainly worth it: Returns the list of servers, but with specified filtering.

By default, it'll omit severs with 0 players.

Parameters, all of which are optional: 
```
  show_empty:
   - Possible values: 0 or 1. If unspecified, 0.

  order:
   - Possible values: "none", "players", "ratio". If unspecified, "none".

  name, gamemode:
   - Possible values: Any specified text. This is basically a search.
  
  paging_size:
   - Provide any number greater than 0 to do paging.
     If specified, only this amount of entries will be returned.

  page:
   - Provide any number greater or equal than 0.
     Specifies the page, to be used along with paging_size.
```

Try it: http://sam.markski.ar:42069/api/GetFilteredServers?name=Roleplay&order=player

### GetServerByIP

Provided an IP address (optionaly with a specified port), returns information about it.

If no port is provided and several servers are under that IP, the one at 7777 will be chosen.

Try it: http://sam.markski.ar:42069/api/GetServerByIP?ip_addr=151.80.19.151:7777

### GetServerPlayers

Provided an IP address (optionaly with a specified port), return a list of players.

Try it: http://sam.markski.ar:42069/api/GetServerPlayers?ip_addr=51.68.204.178:7777

### GetTotalPlayers

Get a simple integer with the sum of players in the tracked servers.

Try it: http://sam.markski.ar:42069/api/GetTotalPlayers

### GetAmountServers

A little pointless for most, but returns the amount of servers SAMonitor is tracking.

Try it: http://sam.markski.ar:42069/api/GetAmountServers

### GetGlobalMetrics

Providing an amount of hours, get metrics for global count of players and servers for several times.

If no hour is provided, defaults to 6.

Try it: http://sam.markski.ar:42069/api/GetGlobalMetrics?hours=6

## Data schemas

There are two schemas: Player and Server.

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
  version:       String
  language:      String
  sampCac:       String (Version of SAMPCAC required. "Not required" otherwise.)
```

The API ***should*** never return any null values. Either '0' or "Unknown" would come in their placee where appropiate. Still, because bugs exist, you may wish to account for the possibility of 'null' values.

## Add your server

You may add your server through the option at the SAMonitor website.