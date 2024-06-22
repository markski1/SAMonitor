import requests

from flask import Blueprint, request

action_bp = Blueprint("actions", __name__, url_prefix="/actions")


@action_bp.route("/")
def components_index():
    return "SAMonitor action endpoint"


@action_bp.post("/add")
def server_list():
    options = request.form

    ip_addr = options.get("ip_addr", "0.0.0.0")

    ip_addr = ip_addr.strip()

    try:
        result = requests.get("http://127.0.0.1:42069/api/AddServer?ip_addr=" + ip_addr)
    except:
        return "Error contacting SAMonitor to add the server, please try again later."

    return result.text
