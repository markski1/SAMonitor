import requests
from flask import Blueprint, render_template, request

from utilities.miscfuncs import parse_server_data, htmx_check, parse_datetime

stats_bp = Blueprint("statistics", __name__, url_prefix="/stats")


@stats_bp.route("/")
def index():
    return "SAMonitor stats endpoint"


@stats_bp.route("/main")
def server_page():
    try:
        lang_stats = requests.get("http://127.0.0.1:42069/api/GetLanguageStats").json()
        gm_stats = requests.get("http://127.0.0.1:42069/api/GetGamemodeStats").json()
    except:
        return """
            <div class='innerContent'>
                <h3>Error fetching metrics.</h3>
                <p>There was an error fetching the metrics data from the SAMonitor API.</p>
                <p>This might be a server issue, in which case, an automated script has already alerted me about this. 
                Please try again in a few minutes.</p>
                <p><a href='https://status.markski.ar/'>Current status of my services</a></p>
            </div>
        """

    return render_template("components/stats-view.html", lang_stats=lang_stats, gm_stats=gm_stats)


@stats_bp.route("/graph")
def graph():
    options = request.args

    hours = int(options.get("hours", 24))
    data_type = int(options.get("dataType", 0))

    try:
        metrics = requests.get(f"http://127.0.0.1:42069/api/GetGlobalMetrics?hours={hours}").json()
    except:
        return "<p>Sorry, there was an error generating the graph.</p>"

    metrics = list(reversed(metrics))

    lowest = 69420
    lowest_time = None
    highest = -1
    highest_time = None

    match data_type:
        case 1:
            get_field = 'servers'
            dataset_name = 'Servers online'
        case _:
            get_field = 'players'
            dataset_name = 'Players online'

    total = 0
    is_first = True
    data_set = ""
    time_set = ""

    for instant in metrics:
        instant_time = parse_datetime(instant['time'])

        if hours > 24:
            human_time = instant_time.strftime("%d/%m %H:%M")
        else:
            human_time = instant_time.strftime("%H:%M")

        total += instant[get_field]

        if instant[get_field] > highest:
            highest = instant[get_field]
            highest_time = human_time

        if instant[get_field] < lowest:
            lowest = instant[get_field]
            lowest_time = human_time

        if is_first:
            data_set += f"{instant[get_field]}"
            time_set += f"'{human_time}'"
            is_first = False
        else:
            data_set += f", {instant[get_field]}"
            time_set += f", '{human_time}'"

    minimum = int(lowest / 3)
    minimum = minimum - (minimum % 10)

    return render_template("components/graph.html", data_set=data_set, time_set=time_set,
                           minimum=minimum, dataset_name=dataset_name, highest=highest, lowest=lowest,
                           highest_time=highest_time, lowest_time=lowest_time)


@stats_bp.get("/table")
def table():
    try:
        metrics = requests.get("http://127.0.0.1:42069/api/GetGlobalMetrics?hours=168").json()
    except:
        return "<p>Sorry, there was an error generating the table.</p>"

    result_buffer = """
        <table style="width: 100%; border: 1px rgb(128, 128, 128) solid;">
            <tr><th>Time</th><th>Players online</th><th>Servers online</th></tr>
    """

    for instant in metrics:
        instant_time = parse_datetime(instant['time'])
        human_time = instant_time.strftime("%d/%m/%y %H:%M")

        result_buffer += f"""
            <tr>
                <td>{human_time}</td>
                <td>{"{:,}".format(instant['players'])}</td>
                <td>{"{:,}".format(instant['servers'])}</td>
            </tr>
        """

    result_buffer += "</table>"

    return result_buffer
