<?php
    $total_servers = file_get_contents("http://sam.markski.ar:42069/api/GetAmountServers");
    $total_players = file_get_contents("http://sam.markski.ar:42069/api/GetTotalPlayers");
?>
<div class="filterBox">
    <p>Tracking <?=$total_servers?> servers, with <?=$total_players?> players total.</p>
    <form hx-get="./view/bits/list_servers.php" hx-target="#server-list">
        <fieldset>
            <legend>Search</legend>
            <table>
                <tr>
                    <td>Name:</td><td><input type="text" name="name" <?php if (isset($_GET['name'])) echo 'value="{}"'?> /></td>
                </tr>
                <tr>
                    <td>Gamemode:</td><td><input type="text" name="gamemode" /></td>
                </td>
            </table>
        </fieldset>

        <fieldset>
            <legend>Options</legend>
            <label><input type="checkbox" name="show_empty"> Show empty servers</label><br />
            <label><input type="radio" name="order" value="none"> Don't order</label><br />
            <label><input type="radio" name="order" value="players"> Order by players</label><br />
            <label><input type="radio" name="order" checked value="ratio"> Order by players/max ratio</label>
        </fieldset>
        <div style="margin-top: .5rem; margin-bottom: 0; width: 10rem">
            <input type="submit" value="Apply filter" hx-indicator="#filter-indicator" />
            <img style="width: 2rem; vertical-align: middle" src="assets/loading.svg" id="filter-indicator" class="htmx-indicator" />
        </div>
    </form>
</div>
<div id="server-list" class="pageContent" hx-get="view/bits/list_servers.php" hx-trigger="load">
    <h1>Loading servers!</h1>
    <p>Please wait. If servers don't load in, SAMonitor might be having issues, please check in later!. Alternatively, if you're using NoScript, you'll need to disable it.</p>
</div>

<script>
    history.replaceState({}, null, "./");
</script>