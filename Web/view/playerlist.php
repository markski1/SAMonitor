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
        echo "<p>The server is empty, or has more than 100 players.</p>";
    }
?>

<style>
    p {
        color: white;
        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
        margin: .1rem;
    }
</style>