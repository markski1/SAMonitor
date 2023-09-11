<?php
    if (!isset($_GET['ip_addr']) || !isset($_GET['players'])) exit;

    if ($_GET['players'] > 100) {
        echo "<p>There's more than 100 players in the server. Due to a SA-MP limitation, the player list cannot be fetched.</p>";
    }
    else if ($_GET['players'] < 1) {
        echo "<p>No one is playing at the moment.</p>";
    }
    else {
        $players = json_decode(file_get_contents("http://gateway.markski.ar:42069/api/GetServerPlayers?ip_addr=".$_GET['ip_addr']), true);
        
        if (count($players) > 0) {
            echo '<table>';
            echo '<tr style="border: 1px gray solid"><b><td>Id</td><td>Name</td><td>Score</td><td>Ping</td></b></tr>';

            foreach ($players as $player) {
                echo "<tr><td style='width: 100px'>{$player['id']}</td> <td>{$player['name']}</td> <td>{$player['score']}</td> <td>{$player['ping']}</td></tr>";
            }

            echo '</table>';
        }
        else {
            echo "<p>Could not fetch player list.</p>";
        }
    }
?>

<style>
    table {
        width: 100%;
        color: white;
        border: 0;
    }
</style>