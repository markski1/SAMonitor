import requests
import datetime

from utilities.component_funcs import render_server
from flask import Blueprint, render_template, request

components_bp = Blueprint("components", __name__, url_prefix="/components")


@components_bp.route("/")
def components_index():
    return "SAMonitor component endpoint"


@components_bp.get("/server-list")
def server_list():
    options = request.args

    name = options.get("name", None)
    gamemode = options.get("gamemode", None)
    language = options.get("language", None)
    page = int(options.get("page", 0))

    filters = f"hide_empty={options.get('hide_empty', 0)}"
    filters += f"&hide_roleplay={options.get('hide_roleplay', 0)}"
    filters += f"&require_sampcac={options.get('require_sampcac', 0)}"
    filters += f"&order={options.get("order", "none")}"
    filters += "&paging_size=20"

    if name:
        filters += f"&name={name}"
    if gamemode:
        filters += f"&gamemode={gamemode}"
    if language:
        filters += f"&language={language}"

    try:
        result = (requests.get(f"http://127.0.0.1:42069/api/GetFilteredServers?{filters}&page={options.get("page", 0)}")
                  .json())
    except:
        return """
            <center>
                <h1>Error fetching servers.</h1>
                <p>There was an error fetching servers from the SAMonitor API.</p>
                <p>This might be a server issue, in which case, an automated script has already alerted me about this. 
                Please try again in a few minutes.</p>
                <p><a href='https://status.markski.ar/'>Current status of my services</a></p>
            </center>
        """

    return render_template("components/server-list.html", servers=result, filters=filters,
                           next_page=str(page + 1), render_server=render_server, server_count=len(result))


@components_bp.get("/current-stats")
def current_stats():
    try:
        result = requests.get("http://127.0.0.1:42069/api/GetGlobalStats").json()
    except:
        return "<p>Failed to load stats.</p>"

    return f"""
        <p>
            <b>{result['serversOnline']}</b> servers online (<b>{result['serversTracked']}</b> total)<br>
            <b>{result['serversInhabited']}</b> servers have players, 
            <b>{result['serversOnlineOMP']}</b> have open.mp.<br>
            <b>{result['playersOnline']}</b> are playing right now!
        </p>
    """


@components_bp.get("/server/<string:show_type>/<string:server_ip>")
def server_details(show_type, server_ip):
    if "detailed" in show_type:
        details = True
    else:
        details = False

    server_data = requests.get(f"http://127.0.0.1:42069/api/GetServerByIP?ip_addr={server_ip}").json()

    return render_server(server_data, details)


@components_bp.get("/graph/<string:server_ip>")
def server_graph(server_ip):
    hours = request.args.get("hours", 24)

    try:
        result = requests.get(f"http://127.0.0.1:42069/api/GetServerMetrics?hours={hours}&ip_addr={server_ip}").json()
    except:
        return "<p>Error obtaining server metrics to build graph.</p>"

    if len(result) < 3:
        return "<p>Not enough data for the activity graph, please check later.</p>"

    # Sets to feed the graph
    player_set = ""
    time_set = ""
    is_first = True

    server_metrics = list(reversed(result.reverse))

    # Minimums and maximums
    lowest = 69420
    lowest_time = None
    highest = -1
    highest_time = None

    for instant in server_metrics:
        instant_time = datetime.datetime.strptime(instant['time'], "%Y-%m-%dT%H:%M:%S.%fZ")

        if hours > 24:
            human_time = instant_time.strftime("j/m H:i")
        else:
            human_time = instant_time.strftime("H:i")

        if instant['players'] < 0:
            instant['players'] = 0

        if instant['players'] > highest:
            highest = instant['players']
            highest_time = human_time

        if instant['players'] < lowest:
            lowest = instant['players']
            lowest_time = human_time

        if is_first:
            player_set += instant['players']
            time_set += f"'{human_time}'"
        else:
            player_set += f", {instant['players']}"
            time_set += f", '{human_time}'"

    render_template("components/graph.html", highest=highest, highest_time=highest_time,
                    lowest=lowest, lowest_time=lowest_time, time_set=time_set, player_set=player_set)


@components_bp.get("/player-list/<string:server_ip>/<int:players>")
def players_list(server_ip, num_players):
    if num_players > 100:
        return ("<p>There's more than 100 players in the server. "
                "Due to a SA-MP limitation, the player list cannot be fetched.</p>")

    elif num_players < 1:
        return "<p>No one is playing at the moment.</p>"

    try:
        result = requests.get(f"http://127.0.0.1:42069/api/GetServerPlayers?ip_addr={server_ip}").json()
    except:
        return "<p>Error fetching players.</p>"

    if len(result) > 0:
        render_template("components/player-list.html", players=result)
    else:
        return ("<p>Could not fetch players. Server might be empty, "
                "or SAMonitor might have difficulty querying it at the moment.</p>")
