<?php

    $blacklist = [ "107.175.134.251" ];

    foreach ($blacklist as $entry) {
        if (str_contains($_POST['ip_addr'], $entry)) {
            exit("<p>This server is blacklisted. Check the 'blacklist' page for appeal information.</p>");
        }
    }

    $result = file_get_contents("http://sam.markski.ar:42069/api/AddServer?ip_addr=".$_POST['ip_addr']);

    if ($result == "false") {
        echo '<p>Server not added. Please make sure:</p>
        <ul>
            <li>The entered IP address is valid and includes a port.</li>
            <li>The server is online and accesible.</li>
            <li>The server isn\'t already in SAMonitor.</p>
        </ul>';
    }
    else {
        echo '<p>Server added succesfully.</p>';
    }

?>