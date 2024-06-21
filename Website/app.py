from flask import Flask, render_template

from config import app_host, app_port, app_debug
from routes.components import components_bp
from routes.root import root_bp

app = Flask(
        __name__,
        static_folder="web/static",
        template_folder="web/templates"
    )

app.register_blueprint(root_bp)
app.register_blueprint(components_bp)

if __name__ == "__main__":
    app.run(host=app_host, port=app_port, debug=app_debug)
