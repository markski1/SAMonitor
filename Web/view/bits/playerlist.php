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
            foreach ($players as $player) {
                echo "<p><b>{$player['name']}</b> ({$player['id']}): {$player['score']} score; {$player['ping']} ping.</p>";
            }
        }
        else {
            echo "<p>Could not fetch player list.</p>";
        }
    }
?>

<style>
    p {
        color: white;
        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
        margin: .1rem;
    }
</style>