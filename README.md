# SAMonitor - San Andreas Monitor

A free and open source server monitor, tracking over a thousand SA-MP and open.mp servers.

Providing: A server browser, a public API and a Masterlist alternative.

## Contents

- [Server browser](#server-browser)
- [Masterlist](#masterlist)
- [API Endpoints](#api-endpoints)
  - [GetAllServers](#getallservers)
  - [GetFilteredServers](#getfilteredservers)
  - [GetServerByIP](#getserverbyip)
  - [GetServerPlayers](#getserverplayers)
  - [GetGlobalStats](#getglobalstats)
  - [GetGlobalMetrics](#getglobalmetrics)
  - [GetServerMetrics](#getservermetrics)
- [Data schemas](#data-schemas)
- [Add your server](#add-your-server)

## Server browser

The server browser is available at https://sam.markski.ar

It consumes the public API endpoints listed below.

## Masterlist

We offer a masterlist alternative, at `http://sam.markski.ar/api/GetMasterlist`.

You may specify a `version` parameter. Example: `http://sam.markski.ar/api/GetMasterlist?version=0.3.7`

It is a relatively high-quality masterlist: It is both automatically and manually pruned of repeated and illegitimate servers regularly. It omits servers which are password protected or have been offline for more than 6 hours.

To ensure fairness, the position of every server in the list is shuffled every 30 minutes.

If you wish to use SAMonitor's masterlist in SA-MP, check out [SA-MP Masterlist Fix](https://github.com/spmn/sa-mp_masterlist_fix)!

## API Endpoints

All of these endpoints are located at `http://sam.markski.ar/api/`. All of them are GET.

### GetAllServers

Return a collection with the information for EVERY server in SAMonitor. This might include dead/offline servers.

Try it: http://sam.markski.ar/api/GetAllServers

### GetFilteredServers

Return a collection with the information for EVERY ONLINE server in SAMonitor.

By default, it won't include empty servers, but this can be changed.

All parameters:
```
  show_empty:
   - If specified 1, will show empty servers.

  order:
   - Possible values: "none", "players", "ratio". By default, "none".

  name, gamemode, version, language:
   - They are a search filter. Specify text in any of them.

  hide_roleplay:
   - If specified 1, hide roleplay servers.

  require_sampcac:
   - If specified 1, only get servers where SAMPCAC is required.
  
  paging_size:
   - For paging. There is no paging by default.
     If number is provided then it'll be the amount of returns per page.

  page:
   - Specify the page number, if paging_size is used.
```

Try it: http://sam.markski.ar/api/GetFilteredServers?name=Roleplay&order=player

### GetServerByIP

Provide an IP address (optionaly with a specified port), returns information about it.

If no port is provided and several servers are under that IP, the one at 7777 will be chosen.

Try it: http://sam.markski.ar/api/GetServerByIP?ip_addr=151.80.19.151:7777

### GetServerPlayers

Provided an IP address (optionaly with a specified port), return a list of players.

Try it: http://sam.markski.ar/api/GetServerPlayers?ip_addr=51.68.204.178:7777

### GetGlobalStats

Receive counts for amount of servers tracked, amount of servers online, servers that are inhabited, and global amount of players at the moment.

Data updates every 5 minutes.

Try it: http://sam.markski.ar/api/GetGlobalStats

### GetGlobalMetrics

Providing an amount of hours, get metrics for global count of players and servers in the last given hours. Default 6.

Updated every 30 minutes.

Try it: http://sam.markski.ar/api/GetGlobalMetrics?hours=6

### GetServerMetrics

Providing an IP address and an amount of hours, get metrics for the count of players in the last given hours.

If no hour is provided, defaults to 6.

Try it: http://sam.markski.ar/api/GetServerMetrics?hours=6&ip_addr=51.68.204.178:7777

NOTE: By default, returns no entries for times a server failed to respond to a query. To have these, use `include_misses=1`. These are marked with a player count of -1, and can be used to (somewhat loosely) track downtime.

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

The API ***should*** never return any null values. Either '0' or "Unknown" would come in their placee where appropiate, or at worst, an empty [] response. Still, because bugs can exist, you may wish to account for the possibility of 'null' returns.

## Add your server

You may add your server through the [add server page in SAMonitor](https://sam.markski.ar/add.php).

You can also use the [Announce filterscript](https://github.com/markski1/SAMonitor/tree/main/Announce).
