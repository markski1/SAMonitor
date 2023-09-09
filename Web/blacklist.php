<?php
    include 'logic/layout.php';
    PageHeader("blacklist");
?>

<div>
    <h2>Blacklist</h2>
    <p>Harmful activity will result in servers being blacklisted.</p>

    <div class="innerContent">
        <h3>Defining harm</h3>
        <p>While I reserve the right to decide what does and doesn't constitute harmful activity, here's a few likely examples.</p>
        <ul>
            <li>Faking your player count or other data.</li>
            <li>The server is an illegitimate clone of another server.</li>
            <li>The server's staff have taken a hostile stance towards the San Andreas community.</li>
            <li>The server's staff have taken a hostile stance towards this service or my person.</li>
        </ul>
        <small>* An example of a hostile stance could be a threat or an attempt at causing harm or disruption.</small>
    </div>

    <p>There are no permanent blocks in SAMonitor. No matter the size of the transgression, if you have truly rectified it, you may appeal and be unblocked.</p>
    <p>If you own a blocked server and wish to appeal, contact me.</p>
    <p>Email: me@markski.ar<br/>Discord: markski.ar (yes, that's a username)</p>

    <div class="innerContent">
        <h3>List of blocked servers</h3>
        <fieldset>
            <legend>LS.CITY</legend>
            <p>Blocked for faking player count.<br />Permanently reporting 2000/2000 despite that not being the case.</p>
        </fieldset>
        <fieldset>
            <legend>Golden State RP (AKA DixelRP)</legend>
            <p>Blocked for faking player count.<br />Reporting anywhere from 19 to 22 players online at all times, but server was empty all times it was checked.</p>
        </fieldset>
    </div>
    <div class="innerContent">
        <h3>IP Blocking regardless of server</h3>
        <p>
            If your server is found to register several IP's pointing to the same server, the server itself won't be blocked, but any IP found to be repeated will be blocked.
        </p>
        <p>
            Attempts to bypass the anti-repeat measures, succesful or not, will result in blacklisting.
        </p>
    </div>
</div>

<script>
    document.title = "SAMonitor - blacklist";
</script>

<?php PageBottom() ?>