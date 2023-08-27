<?php
    $metrics = json_decode(file_get_contents("http://gateway.markski.ar:42069/api/GetGlobalMetrics?hours=24"), true);

    // if load_table or load_graph parameter is set, only return that.
    // this allows for two things:
    //     - External embedding of the table or graph by any 3rd party who wants it.
    //     - Making the main page load faster. HTMX calls this page again with this parameter to get the table without delaying the main page load.
    if (isset($_GET['load_table'])) {
        echo '<table style="width: 100%; border: 1px gray solid;">
                <tr><th>Time</th><th>Players</th><th>Servers</th></tr>';

        foreach ($metrics as $instant) {
            $humanTime = strtotime($instant['time']);
            $humanTime = date("F jS H:i:s", $humanTime);
            echo "
                <tr>
                    <td>{$humanTime}</td>
                    <td>{$instant['players']}</td>
                    <td>{$instant['servers']}</td>
                </tr>
            ";
        }

        echo '</table>';
        exit;
    }

    $playerSet = "";
    $timeSet = "";
    $first = true;

    // API provides data in descendent order, but we'd want to show t
    $metrics = array_reverse($metrics);

    $lowest = 69420;
    $lowest_time = null;
    $highest = -1;
    $highest_time = null;

    $skip = true;

    foreach ($metrics as $instant) {
        $humanTime = strtotime($instant['time']);
        $humanTime = date("H:i", $humanTime);

        if ($instant['players'] > $highest) {
            $highest = $instant['players'];
            $highest_time = $humanTime;
        }
        if ($instant['players'] < $lowest) {
            $lowest = $instant['players'];
            $lowest_time = $humanTime;
        }

        if ($first) {
            $playerSet .= $instant['players'];
            $timeSet .= "'".$humanTime."'";
            $first = false;
        } 
        else {
            $playerSet .= ", ".$instant['players'];
            $timeSet .= ", '".$humanTime."'";
        }
    }
?>

<div>
    <h2>Metrics</h3>
    <p>SAMonitor accounts for the total amount of servers and players a few times every hour, of every day.</p>
    <div class="innerContent">
        <h3>Global player metrics - Last 24 hours</h3>
        <div style='width: 100% !important'>
            <canvas id='globalPlayersChart' style='width: 100%'></canvas>
        </div>
        <div style="margin-top: 1rem" hx-target="this">
            <input type="button" value="Show dataset as a table" hx-get="./view/metrics.php?load_table"/>
        </div>
        <p>The highest player count was <span style='color: green'><?=$highest?></span> at <?=$highest_time?>, and the lowest was <span style='color: red'><?=$lowest?></span> at <?=$lowest_time?></p>
        <p>
            <small>
                Times are UTC 0.
            </small>
        </p>
    </div>
    <div class="innerContent">
        <h3>Server-Specific metrics</h3>
        <p>The same graphs are available in every server's page. Simply click "Show details" and then "All about this server" where desired.</p>
    </div>
</div>

<script>
    history.replaceState({}, null, "./?page=metrics");
</script>

<script>
    new Chart(document.getElementById('globalPlayersChart'), {
        type: 'line',
        options: {
            responsive: false
        },
        data: {
            labels: [<?=$timeSet?>],
            datasets: [
                {
                    label: 'Players online',
                    data: [<?=$playerSet?>],
                    borderWidth: 1
                }
            ]
        }
    });
</script>