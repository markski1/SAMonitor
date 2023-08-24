<p>To add a server to SAMonitor, simply enter it's IP address below.</p>

<form hx-target="this" hx-post="./action/add.php">
    IP Address:<br />
    <input type="text" name="ip_addr" style="width: 20rem" placeholder="address:port format please."/> <input type="submit" value="Add server" />
</form>

<p>MUST be an IPv4 address (NOT a domain), and must specify port.</p>
<p>If you change your IP in the future just submit it again, old ones are removed automatically.</p>
<p>We will fetch all information off it automatically.</p>
<p>Please note API endpoints such as full player list may not work immediatly.</p>