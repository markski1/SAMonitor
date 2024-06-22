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

    last_updated = parse_datetime(server['lastUpdated'])

    if last_updated.hour > 0:
        if last_updated.hour == 1:
            last_updated_str = f"{last_updated.hour} hour ago"
        else:
            last_updated_str = f"{last_updated.hour} hours ago"
    else:
        if last_updated.minute == 1:
            last_updated_str = f"{last_updated.minute} minute ago"
        else:
            last_updated_str = f"{last_updated.minute} minutes ago"

    server_name = server["name"].strip()

    return render_template("components/server-snippet.html",
                           server=server, name=server_name, website=website, lag_comp=lag_comp,
                           last_updated=last_updated_str, detailed=details, minutes_ago=last_updated)


def parse_datetime(datetime_str):
    if '.' in datetime_str and datetime_str.endswith('Z'):
        # Truncate the microseconds to 6 digits
        datetime_str = datetime_str[:-8] + datetime_str[-8:-2] + 'Z'
        return datetime.datetime.strptime(datetime_str, "%Y-%m-%dT%H:%M:%S.%fZ")
    else:
        return datetime.datetime.strptime(datetime_str, "%Y-%m-%dT%H:%M:%S")