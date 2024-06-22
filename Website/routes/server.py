import requests
from flask import Blueprint, render_template

from utilities.miscfuncs import parse_server_data, htmx_check

server_bp = Blueprint("server", __name__, url_prefix="/")


@server_bp.route("/server/<string:server_ip>")
@htmx_check
def server_page(is_htmx, server_ip):
    # 1. Obtain server data and metrics.
    try:
        server_data = requests.get(
            f"http://127.0.0.1:42069/api/GetServerByIP?ip_addr={server_ip}"
        )
        server = server_data.json()
        metrics = requests.get(
            f"http://127.0.0.1:42069/api/GetServerMetrics?hours=168&include_misses=1&ip_addr={server_ip}"
        ).json()
    except:
        return "<p>Error contacting API.</p>"

    if server_data.status_code == 204:  # Resource does not exist
        return "<p>IP Address does not correspond to a server in SAMonitor.</p>"

    # 2. Measure uptime and average players.
    total_reqs = len(metrics)
    missed_reqs = 0
    total_players_m = 0

    for instant in metrics:
        if instant["players"] < 0:
            missed_reqs += 1
        else:
            total_players_m += instant["players"]

    uptime_pct = 100.0
    avg_players = 0.0

    if total_reqs > 0:
        if missed_reqs > 0:
            downtime_pct = (total_reqs / total_reqs) * 100
            uptime_pct = 100 - downtime_pct

        req_success = total_reqs - missed_reqs
        if req_success > 0:
            avg_players = total_players_m / req_success

    # 3. Parse server information.

    server_data = parse_server_data(server)

    return render_template("server.html", htmx=is_htmx, server=server,
                           server_name=server_data["name"], website=server_data['website'],
                           uptime=uptime_pct, avg_players=avg_players,
                           last_updated=server_data['last_updated'], server_software=server_data['software'])
