<?php
    if (str_contains($_POST['ip_addr'], ' ')) {
        echo '<p>An IP address cannot contain a space.</p>';
        exit;
    }

    $result = file_get_contents("http://gateway.markski.ar:42069/api/AddServer?ip_addr=".trim($_POST['ip_addr']));

    echo "<p>{$result}</p>";
?>