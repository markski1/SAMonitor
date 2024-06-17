/*
	SAMonitor Announce Filterscript

	Use this filterscript to ensure your server is always in SAMonitor, even if your IP changes.

	Based on announce.pwn by SACNR Monitor.
    Original contributors:
		Blacklite, King_Hual, Jamie, Blake, Markski.
*/

#include <a_samp>
#include <a_http>

#define LOG_PREFIX          "[SAMonitor] "

stock GetIP(ip[], const len)
{
    GetConsoleVarAsString("bind", ip, len);
}

public OnFilterScriptInit()
{
    new queryUrl[96],
        ip[16];

    GetIP(ip, sizeof(ip));
    printf("%sAnnouncing server...", LOG_PREFIX);

    if (!strlen(ip)) {
        printf("%sFailed to obtain IP address, can't announce server", LOG_PREFIX);
    }

    // It's all done through GET, so no POST data.
    format(queryUrl, sizeof(queryUrl), "sam.markski.ar/api/AddServer?ip_addr=%s:%d", ip, GetServerVarAsInt("port"));
    HTTP(0, HTTP_POST, queryUrl, "", "OnAnnounced"); // no need for different announce indices
}

forward OnAnnounced(index, response_code, data[]);
public OnAnnounced(index, response_code, data[])
{
    #pragma unused data
    if (response_code >= 200) {
        printf("%sServer announced successfully.", LOG_PREFIX);
    }
    else {
        printf("%sServer failed to announce (error %d).", LOG_PREFIX, response_code);
    }
}