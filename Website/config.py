import os
from dotenv import load_dotenv

load_dotenv()

app_host = os.getenv('APP_HOST')
app_port = os.getenv('APP_PORT')
app_debug = os.getenv('APP_DEBUG')
