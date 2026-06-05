import socket
import struct
from concurrent.futures import ThreadPoolExecutor
from functools import lru_cache
from flask import Flask, request, jsonify

"""
SAMonitor Query Service

This script is run on servers secondary to SAMonitor,
and is used to query servers that block OVH Canada, where
the main SAMonitor monolith is hosted.

It seems to be a common practice by some server owners to block OVH
addresses for reasons related to denial of service attacks.
"""

app = Flask(__name__)


@lru_cache(maxsize=256)
def resolve_host(host):
    return socket.gethostbyname(host)


def _build_packet(ip_str, port, query_type):
    return b"SAMP" + socket.inet_aton(ip_str) + struct.pack('H', port) + query_type


def query_samp_info(sock, ip_str, port):
    try:
        packet = _build_packet(ip_str, port, b'i')
        sock.sendto(packet, (ip_str, port))
        data, _ = sock.recvfrom(8192)

        if len(data) < 11:
            return None

        if data[0:4] != b"SAMP" or data[10] != ord('i'):
            return None

        password = bool(data[11])
        players = struct.unpack('H', data[12:14])[0]
        max_players = struct.unpack('H', data[14:16])[0]

        offset = 16
        if len(data) < offset + 4: return None
        hostname_len = struct.unpack('I', data[offset:offset+4])[0]
        offset += 4
        if len(data) < offset + hostname_len: return None
        hostname = data[offset:offset+hostname_len].decode('cp1251', errors='ignore')
        offset += hostname_len

        if len(data) < offset + 4: return None
        gamemode_len = struct.unpack('I', data[offset:offset+4])[0]
        offset += 4
        if len(data) < offset + gamemode_len: return None
        gamemode = data[offset:offset+gamemode_len].decode('cp1251', errors='ignore')
        offset += gamemode_len

        if len(data) < offset + 4: return None
        language_len = struct.unpack('I', data[offset:offset+4])[0]
        offset += 4
        if len(data) < offset + language_len: return None
        language = data[offset:offset+language_len].decode('cp1251', errors='ignore')

        return {
            "HostName": hostname,
            "GameMode": gamemode,
            "Language": language,
            "Players": players,
            "MaxPlayers": max_players,
            "Password": password
        }
    except Exception as e:
        print(f"Error querying info from {ip_str}:{port}: {e}")
        return None


def query_samp_rules(sock, ip_str, port):
    try:
        packet = _build_packet(ip_str, port, b'r')
        sock.sendto(packet, (ip_str, port))
        data, _ = sock.recvfrom(8192)

        if len(data) < 13:
            return None

        if data[0:4] != b"SAMP" or data[10] != ord('r'):
            return None

        rules_count = struct.unpack('H', data[11:13])[0]
        offset = 13
        rules = {}
        for _ in range(rules_count):
            if len(data) < offset + 1: break
            name_len = data[offset]
            offset += 1
            if len(data) < offset + name_len: break
            name = data[offset:offset+name_len].decode('cp1251', errors='ignore')
            offset += name_len

            if len(data) < offset + 1: break
            val_len = data[offset]
            offset += 1
            if len(data) < offset + val_len: break
            val = data[offset:offset+val_len].decode('cp1251', errors='ignore')
            offset += val_len
            rules[name.lower()] = val

        return {
            "LagComp": rules.get('lagcomp', '0') == 'On',
            "MapName": rules.get('mapname', 'Unknown'),
            "Version": rules.get('version', 'Unknown'),
            "SampcacVersion": rules.get('sampcac_version', 'Not required'),
            "Weather": int(rules.get('weather', '0')),
            "WebUrl": rules.get('weburl', 'Unknown'),
            "WorldTime": rules.get('worldtime', '00:00')
        }
    except Exception as e:
        print(f"Error querying rules from {ip_str}:{port}: {e}")
        return None


def query_samp_players(sock, ip_str, port):
    # Try 'd' (detailed) first, then 'c' (client list) as fallback.
    for query_type in [b'd', b'c']:
        try:
            packet = _build_packet(ip_str, port, query_type)
            sock.sendto(packet, (ip_str, port))
            data, _ = sock.recvfrom(8192)

            if len(data) < 13:
                continue

            if data[0:4] != b"SAMP" or data[10] != ord(query_type):
                continue

            player_count = struct.unpack('H', data[11:13])[0]
            offset = 13
            players = []

            for _ in range(player_count):
                if query_type == b'd':
                    if len(data) < offset + 1: break
                    player_id = data[offset]
                    offset += 1
                else:
                    player_id = 0

                if len(data) < offset + 1: break
                name_len = data[offset]
                offset += 1
                if len(data) < offset + name_len: break
                name = data[offset:offset+name_len].decode('cp1251', errors='ignore')
                offset += name_len

                if len(data) < offset + 4: break
                score = struct.unpack('i', data[offset:offset+4])[0]
                offset += 4

                if query_type == b'd':
                    if len(data) < offset + 4: break
                    ping = struct.unpack('i', data[offset:offset+4])[0]
                    offset += 4
                else:
                    ping = 0

                players.append({
                    "PlayerId": player_id,
                    "PlayerName": name,
                    "PlayerScore": score,
                    "PlayerPing": ping
                })

            return players
        except Exception as e:
            print(f"Error querying players ({query_type.decode()}) from {ip_str}:{port}: {e}")
            continue

    return None


@app.route('/query', methods=['GET'])
def query():
    ip = request.args.get('ip')
    if not ip:
        return jsonify({"status": "online"}), 200

    try:
        if ':' in ip:
            host, port = ip.split(':')
            port = int(port)
        else:
            host = ip
            port = 7777
    except ValueError:
        return jsonify({"error": "Invalid IP format"}), 400

    try:
        ip_str = resolve_host(host)
    except socket.gaierror:
        return jsonify({"error": "Failed to resolve host"}), 400

    def run_query(func):
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        sock.settimeout(5)
        try:
            return func(sock, ip_str, port)
        finally:
            sock.close()

    with ThreadPoolExecutor(max_workers=2) as executor:
        info_future = executor.submit(run_query, query_samp_info)
        rules_future = executor.submit(run_query, query_samp_rules)
        info = info_future.result()
        rules = rules_future.result()

    if not info:
        return jsonify({"error": "Failed to query server info"}), 500

    return jsonify({
        "info": info,
        "rules": rules
    })


@app.route('/query/players', methods=['GET'])
def query_players():
    ip = request.args.get('ip')
    if not ip:
        return jsonify({"error": "Missing IP parameter"}), 400

    try:
        if ':' in ip:
            host, port = ip.split(':')
            port = int(port)
        else:
            host = ip
            port = 7777
    except ValueError:
        return jsonify({"error": "Invalid IP format"}), 400

    try:
        ip_str = resolve_host(host)
    except socket.gaierror:
        return jsonify({"error": "Failed to resolve host"}), 400

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.settimeout(5)
    try:
        players = query_samp_players(sock, ip_str, port)
    finally:
        sock.close()

    if players is None:
        return jsonify({"error": "Failed to query players"}), 500

    return jsonify({"players": players})


if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000, threaded=True)
