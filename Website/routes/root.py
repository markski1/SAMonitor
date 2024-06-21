from functools import wraps

from flask import Blueprint, render_template, request

root_bp = Blueprint("root", __name__, url_prefix="/")


# IS HTMX Decorator


def htmx_check(func):
    @wraps(func)
    def wrapper(*args, **kwargs):
        if 'HX-Request' in request.headers:
            is_htmx = True
        else:
            is_htmx = False

        return func(is_htmx, *args, **kwargs)

    return wrapper


# Routes


@root_bp.route("/")
@htmx_check
def index(is_htmx):
    return render_template("index.html", htmx=is_htmx)


@root_bp.route("/about")
@htmx_check
def about(is_htmx):
    return render_template("about.html", htmx=is_htmx)


@root_bp.route("/blacklist")
@htmx_check
def blacklist(is_htmx):
    return render_template("blacklist.html", htmx=is_htmx)


@root_bp.route("/server/<string:server_ip>")
@htmx_check
def server(is_htmx, server_ip):
    # TODO: Server pages
    return "Not yet implemented."
