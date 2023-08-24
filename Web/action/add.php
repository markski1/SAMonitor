<?php

    $result = file_get_contents("http://sam.markski.ar:42069/api/AddServer?ip_addr=".$_POST['ip_addr']);

    if ($result == "false") {
        echo '<p>Server not added. Please make sure the server is online and accesible.</p>';
    }
    else {
        echo '<p>Server added succesfully.</p>';
    }

?>