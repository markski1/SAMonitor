<?php

    $result = file_get_contents("http://sam.markski.ar:42069/api/AddServer?ip_addr=".$_POST['ip_addr']);

    echo "<p>{$result}</p>";

?>