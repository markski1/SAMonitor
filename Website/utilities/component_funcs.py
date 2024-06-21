import datetime

from flask import render_template


def render_server(server, details=False):
    if server["website"] != "Unknown":
        website = f"<a href='{server["website"]}'>{server['website']}</a>"
    else:
        website = "No website specified."

    match server["lagComp"]:
        case 1:
            lag_comp = "Enabled"
        case _:
            lag_comp = "Disabled"

    last_updated = datetime.datetime.strptime(server['lastUpdated'])

    # If detailed, show as normal, else make it a clickable to show more details.
    if details:
        list_contents = '<div hx-swap="outerHTML" hx-target="this" class="server">'
    else:
        list_contents = (f'<div hx-swap="outerHTML" hx-target="this"'
                         f'hx-get="./components/server/detailed/{server["ipAddr"]}" class="server server_clickable">')

    server_name = server["name"].trim()

    return render_template("/components/server.html",
                           name=server_name, website=website, lag_comp = lag_comp, last_updated = last_updated,
                           detailed=details)
