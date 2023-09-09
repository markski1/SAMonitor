<?php
    include 'logic/layout.php';
    PageHeader("add server");
?>

<div>
    <h2>Add server</h2>
    <p>SAMonitor is an open server list, and as such, anyone can add a server.</p>
    <div class="innerContent">
        <p>If you're blocking the range containing gateway.markski.ar (45.153.48.229), this won't work.</p>
        <fieldset>
            <h3>Add server</h3>
            <form hx-target="#result" hx-post="./action/add.php">
                IP Address:<br />
                <input required type="text" name="ip_addr" style="width: 20rem" placeholder="address:port format please."> <input type="submit" value="Add server" hx-indicator="#add-indicator"> <img style="width: 2rem; vertical-align: middle" src="assets/loading.svg" id="add-indicator" class="htmx-indicator">
                <div id="result" style="margin-top: 1rem"><p>Waiting...</p></div>
            </form>
        </fieldset>
        <p>MUST be an IPv4 address (NOT a domain), and must specify port.</p>
        <p>If you change your IP in the future just submit it again, old ones are removed automatically.</p>
        <p>We will fetch all information off it automatically.</p>
        <p>Please note API endpoints such as full player list may not work immediatly.</p>
    </div>
</div>

<script>
    document.title = "SAMonitor - add server";  
</script>

<?php PageBottom() ?>