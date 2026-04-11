import socket
import struct
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

def query_samp_info(host, port):
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.settimeout(5)

    try:
        ip_parts = socket.gethostbyname(host).split('.')
        packet = b"SAMP"
        for part in ip_parts:
            packet += struct.pack('B', int(part))
        packet += struct.pack('H', port)
        packet += b'i'

        sock.sendto(packet, (host, port))
        data, _ = sock.recvfrom(2048)

        if len(data) < 11:
            return None

        # Basic validation of the response header
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
        print(f"Error querying info from {host}:{port}: {e}")
        return None
    finally:
        sock.close()


def query_samp_rules(host, port):
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.settimeout(5)

    try:
        ip_parts = socket.gethostbyname(host).split('.')
        packet = b"SAMP"
        for part in ip_parts:
            packet += struct.pack('B', int(part))
        packet += struct.pack('H', port)
        packet += b'r'

        sock.sendto(packet, (host, port))
        data, _ = sock.recvfrom(2048)

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
        print(f"Error querying rules from {host}:{port}: {e}")
        return None
    finally:
        sock.close()


@app.route('/query', methods=['GET'])
def query():
    ip = request.args.get('ip')
    if not ip:
        # Just inform SAMonitor that we're up and running.
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

    info = query_samp_info(host, port)
    if not info:
        return jsonify({"error": "Failed to query server info"}), 500

    rules = query_samp_rules(host, port)

    return jsonify({
        "info": info,
        "rules": rules
    })

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)
