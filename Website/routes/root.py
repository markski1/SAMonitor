from flask import Blueprint, render_template

root_bp = Blueprint("root", __name__, url_prefix="/")


@root_bp.route("/")
def index():
    return render_template("index.html")


@root_bp.route("/about")
def about():
    return render_template("about.html")


@root_bp.route("/blacklist")
def blacklist():
    return render_template("blacklist.html")


@root_bp.route("/server/<string:server_ip>")
def server(server_ip):
    # TODO: Server pages
    return "Not yet implemented."
