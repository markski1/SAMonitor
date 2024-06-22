from flask import Blueprint, render_template

from utilities.miscfuncs import htmx_check

basic_bp = Blueprint("root", __name__, url_prefix="/")


@basic_bp.route("/")
@htmx_check
def index(is_htmx):
    return render_template("index.html", htmx=is_htmx)


@basic_bp.route("/about")
@htmx_check
def about(is_htmx):
    return render_template("about.html", htmx=is_htmx)


@basic_bp.route("/blacklist")
@htmx_check
def blacklist(is_htmx):
    return render_template("blacklist.html", htmx=is_htmx)


@basic_bp.route("/masterlist")
@htmx_check
def masterlist(is_htmx):
    return render_template("masterlist.html", htmx=is_htmx)


@basic_bp.route("/add")
@htmx_check
def add(is_htmx):
    return render_template("add.html", htmx=is_htmx)
