<?php
    $total_servers = file_get_contents("http://sam.markski.ar:42069/api/GetAmountServers");
    $total_players = file_get_contents("http://sam.markski.ar:42069/api/GetTotalPlayers");
?>
<div class="filterBox">
    <p>Tracking <?=$total_players?> players across <?=$total_servers?> servers.</p>
    <form hx-get="./view/list.php" hx-target="#server-list">
        <fieldset>
            <legend>Search</legend>
            <label>Name: <input type="text" name="name" <?php if (isset($_GET['name'])) echo 'value="{}"'?> /></label><br />
            <label>Gamemode: <input type="text" name="gamemode" /></label>
        </fieldset>

        <fieldset>
            <legend>Options</legend>
            <label><input type="checkbox" name="show_empty"> Show empty servers</label><br />
            <label><input type="radio" name="order" value="none"> Don't order</label><br />
            <label><input type="radio" name="order" value="players"> Order by players</label><br />
            <label><input type="radio" name="order" checked value="ratio"> Order by players/max ratio</label>
        </fieldset>
        <div style="margin-top: .5rem">
            <input type="submit" value="Apply filter" />
        </div>
    </form>
</div>
<div id="server-list" class="pageContent" hx-get="view/list.php" hx-trigger="load">
    <h1>Loading servers!</h1>
    <p>Please wait. If servers don't load in, SAMonitor might be having issues, please check in later!. Alternatively, if you're using NoScript, you'll need to disable it.</p>
</div>