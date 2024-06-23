from flask import Flask, render_template

from config import app_host, app_port, app_debug
from routes.action import action_bp
from routes.components import components_bp
from routes.basic import basic_bp
from routes.server import server_bp
from routes.statistics import stats_bp

app = Flask(
        __name__,
        static_folder="web/static",
        template_folder="web/templates"
    )

app.register_blueprint(basic_bp)
app.register_blueprint(server_bp)
app.register_blueprint(components_bp)
app.register_blueprint(action_bp)
app.register_blueprint(stats_bp)

if __name__ == "__main__":
    app.run(host=app_host, port=app_port, debug=app_debug)
