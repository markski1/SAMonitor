<div>
    <p>SAMonitor accounts for the total amount of servers and players a few times every hour, of every day.</p>
    <div class="innerContent">
        <h3>Global metrics - Last 24 hours</h3>
        <table style="width: 100%; border: 1px gray solid;">
            <tr><th>Time</th><th>Players</th><th>Servers</th></tr>
            
            <?php
                $metrics = json_decode(file_get_contents("http://sam.markski.ar:42069/api/GetGlobalMetrics?hours=24"), true);

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
            ?>
        </table>
        <p>
            <small>
                <p>Notes</p>
                All times are UTC 0.<br />SAMonitor is a very young project. More metrics will be available as we gather more data.
            </small>
        </p>
    </div>
    <p>In the future, I will keep similar metrics for every individual server.</p>
</div>