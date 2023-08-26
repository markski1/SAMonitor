<?php
    $players = json_decode(file_get_contents("http://sam.markski.ar:42069/api/GetServerPlayers?ip_addr=".$_GET['ip_addr']), true);
?>

<?php
    if (count($players) > 0) {
        foreach ($players as $player) {
            echo "<p><b>{$player['name']}</b> ({$player['id']}): {$player['score']} score; {$player['ping']} ping.</p>";
        }
    }
    else {
        if ($_GET['players'] == 0) {
            echo "<p>No players in this server.";
        }
        else {
            echo "<p>Can't show players in servers with more than 100 players.<br />This is a SA-MP limitation, not mine!</p>";
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