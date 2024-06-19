from flask import Flask, render_template

from config import app_host, app_port, app_debug

app = Flask(
        __name__,
        static_folder="web/static",
        template_folder="web/templates"
    )


@app.route("/")
def index():
    return render_template("index.html")


if __name__ == "__main__":
    app.run(host=app_host, port=app_port, debug=app_debug)
    