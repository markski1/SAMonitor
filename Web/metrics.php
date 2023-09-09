<?php
    include 'logic/layout.php';
    PageHeader("metrics");

    if (isset($_GET['hours'])) {
        $hours = intval($_GET['hours']);
    }
    else $hours = 24;

    $metrics = json_decode(file_get_contents("http://gateway.markski.ar:42069/api/GetGlobalMetrics?hours=".$hours), true);

    // if load_table or load_graph parameter is set, only return that.
    // this allows for two things:
    //     - External embedding of the table or graph by any 3rd party who wants it.
    //     - Making the main page load faster. HTMX calls this page again with this parameter to get the table without delaying the main page load.
    if (isset($_GET['load_table'])) {
        echo '<table style="width: 100%; border: 1px gray solid;">
                <tr><th>Time</th><th>Players online</th><th>Servers online</th><th>API Hits</th></tr>';

        foreach ($metrics as $instant) {
            $humanTime = strtotime($instant['time']);
            $humanTime = date("F jS H:i:s", $humanTime);
            echo "
                <tr>
                    <td>{$humanTime}</td>
                    <td>{$instant['players']}</td>
                    <td>{$instant['servers']}</td>
                    <td>{$instant['apiHits']}</td>
                </tr>
            ";
        }

        echo '</table>';
        exit;
    }

    if (isset($_GET['load_graph'])) {
        $dataSet = "";
        $timeSet = "";
        $first = true;

        // API provides data in descendent order, but we'd want to show it as a graph, so it should be ascending.
        $metrics = array_reverse($metrics);

        $lowest = 69420;
        $lowest_time = null;
        $highest = -1;
        $highest_time = null;

        $skip = true;

        $dataType = $_GET['type'] ?? 0;

        switch ($dataType) {
            case 0:
                $getField = 'players';
                $datasetName = 'Players online';
                break;
            case 1:
                $getField = 'servers';
                $datasetName = 'Servers online';
                break;
            case 2:
                $getField = 'apiHits';
                $datasetName = 'API hits';
                break;
        }

        foreach ($metrics as $instant) {
            $humanTime = strtotime($instant['time']);

            // only specify the day if we're listing more than 24 hours.
            if ($hours > 24) {
                $humanTime = date("j/m H:i", $humanTime);
            }
            else $humanTime = date("H:i", $humanTime);

            if ($instant[$getField] > $highest) {
                $highest = $instant[$getField];
                $highest_time = $humanTime;
            }
            if ($instant[$getField] < $lowest) {
                $lowest = $instant[$getField];
                $lowest_time = $humanTime;
            }

            if ($first) {
                $dataSet .= $instant[$getField];
                $timeSet .= "'".$humanTime."'";
                $first = false;
            } 
            else {
                $dataSet .= ", ".$instant[$getField];
                $timeSet .= ", '".$humanTime."'";
            }
        }

        // Horrible! But, we put the script here to be loaded by HTMX when a new graph is required.
        echo "
            <canvas id='globalPlayersChart' style='width: 100%'></canvas>
            <script>
                new Chart(document.getElementById('globalPlayersChart'), {
                    type: 'line', options: { responsive: false }, data: {
                        labels: [{$timeSet}],
                        datasets: [
                            {
                                label: '{$datasetName}',
                                data: [{$dataSet}],
                                borderWidth: 1
                            }
                        ]
                    }
                });
            </script>
            <p>The highest count was <span style='color: green'>{$highest}</span> at {$highest_time}, and the lowest was <span style='color: red'>{$lowest}</span> at {$lowest_time}</p>
        ";
        exit;
    }
?>

<div>
    <h2>Metrics</h3>
    <p>SAMonitor accounts for the total amount of servers and players a few times every hour, of every day.</p>
    <div class="innerContent">
        <form hx-target="#graph-cnt" hx-get="metrics.php?load_graph" hx-trigger="change">
            <h3>Global 
                <select name="type" style="width: 7rem">
                    <option value=0>player</option>
                    <option value=1>server</option>
                    <option value=2>api hits</option>
                </select>
            metrics | 
                <select name="hours">
                    <option value=24>Last 24 hours</option>
                    <option value=72>Last 72 hours</option>
                    <option value=168>Last week</option>
                </select>
            </h3>
        </form>
        <div id="graph-cnt" hx-get="metrics.php?load_graph" hx-trigger="load">
        
        </div>
        <div style="margin-top: 1rem" hx-target="this">
            <input type="button" value="Show last week's stats in a table." hx-get="./metrics.php?load_table&hours=168"/>
        </div>
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
    document.title = "SAMonitor - metrics";
</script>

<?php PageBottom() ?>