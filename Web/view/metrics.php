<?php
    // if load_metrics parameter is set, only return the metrics table.
    // this allows for two things:
    //     - External embedding of the table by any 3rd party who wants it.
    //     - Making the main page load faster. HTMX calls this page again with this parameter to get the table without delaying the main page load.
    if (isset($_GET['load_metrics'])) {
        $metrics = json_decode(file_get_contents("http://sam.markski.ar:42069/api/GetGlobalMetrics?hours=24"), true);

        echo '<table style="width: 100%; border: 1px gray solid;">
                <tr><th>Time</th><th>Players</th><th>Servers</th></tr>';

        foreach ($metrics as $instant) {
            $humanTime = strtotime($instant['time']);
            $humanTime = date("Y F jS H:i:s", $humanTime);
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
?>

<div>
    <h2>Metrics</h3>
    <p>SAMonitor accounts for the total amount of servers and players a few times every hour, of every day.</p>
    <div class="innerContent">
        <h3>Global metrics - Last 24 hours</h3>
        <div hx-get="./view/metrics.php?load_metrics" hx-trigger="load">
            <h3>Loading metrics...</h3>
        </div>
        <p>
            <small>
                <p>Notes</p>
                All times are UTC 0.<br />SAMonitor is a very young project. More metrics will be available as we gather more data.
            </small>
        </p>
    </div>
    <p>In the future, I will keep similar metrics for every individual server.</p>
</div>