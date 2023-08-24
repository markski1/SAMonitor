<?php
    include 'fragments.php';

    $servers = json_decode(file_get_contents("http://sam.markski.ar:42069/api/GetAllServers"), true);
    $total_servers = file_get_contents("http://sam.markski.ar:42069/api/GetAmountServers");
    $total_players = file_get_contents("http://sam.markski.ar:42069/api/GetTotalPlayers");

    echo "<p>Tracking {$total_players} players across {$total_servers} servers.</p>";

    $num = 0;

    foreach ($servers as $server) {
        echo '<div hx-target="this" class="server">';
        DrawServer($server, $num);
        echo '</div>';
        $num++;
    }
?>