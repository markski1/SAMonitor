<?php
    $total_servers = file_get_contents("http://sam.markski.ar:42069/api/GetAmountServers");
    $total_players = file_get_contents("http://sam.markski.ar:42069/api/GetTotalPlayers");
?>
<!DOCTYPE html>
<html>
    <head>
        <title>SAMonitor - SA-MP and open.mp server monitor</title>
        
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <link rel="stylesheet" type="text/css" href="style.css">
        
        <meta name="title" content="SAMonitor">
        <meta name="description" content="A server browser for SA-MP and open.mp servers. Also provides a public API and Masterlist.">

        <meta name="og:title" content="SAMonitor" />
        <meta property="og:description" content="A server browser for SA-MP and open.mp servers. Also provides a public API and Masterlist." />
    </head>
    <body>
        <header>
            <div>
                <h1>SAMonitor</h1>
            </div>
            <div>
                <a href="" hx-get="view/list.php" hx-target="#main">servers</a> <span class="separator">&nbsp;/&nbsp;</span>
                <a href="" hx-get="view/api.php" hx-target="#main">api & masterlist</a> <span class="separator">&nbsp;/&nbsp;</span>
                <a href="" hx-get="view/add.php" hx-target="#main">add server</a> <span class="separator">&nbsp;/&nbsp;</span>
                <!--<a href="" hx-get="view/sponsor.php" hx-target="#main">sponsor</a>  <span class="separator">&nbsp;/&nbsp;</span>-->
                <a href="https://github.com/markski1/SAMonitor">github</a>
            </div>
        </header>
        <main>
            <div class="filterBox">
                <p>Tracking <?=$total_players?> players across <?=$total_servers?> servers.</p>
                <form hx-get="./view/list.php" hx-target="#main">
                    <fieldset>
                        <legend>Search</legend>
                        <label>Name: <input type="text" name="name" <?php if (isset($_GET['name'])) echo 'value="{}"'?> /></label><br />
                        <label>Gamemode: <input type="text" name="gamemode" /></label>
                    </fieldset>
    
                    <fieldset>
                        <legend>Options</legend>
                        <label><input type="checkbox" name="show_empty"> Show empty servers</label><br />
                        <label><input type="radio" name="order" value="none"> Don't order</label><br />
                        <label><input type="radio" name="order" checked value="player"> Order by players</label><br />
                        <label><input type="radio" name="order" value="ratio"> Order by players/max ratio</label>
                    </fieldset>
                    <div style="margin-top: .5rem">
                        <input type="submit" value="Apply filter" />
                    </div>
                </form>
            </div>
            <div id="main" class="pageContent" hx-get="view/list.php" hx-trigger="load">
                <h1>Loading servers!</h1>
                <p>Please wait. If servers don't load in, we might be having issues, please check in later!. Alternatively, if you're using NoScript, you'll need to disable it.</p>
            </div>
        </main>
    </body>
</html>
<script src="https://unpkg.com/htmx.org@1.9.4" integrity="sha384-zUfuhFKKZCbHTY6aRR46gxiqszMk5tcHjsVFxnUo8VMus4kHGVdIYVbOYYNlKmHV" crossorigin="anonymous"></script>
<script>
    function CopyAddress(id) {

        var copyText = document.getElementById(id);
               
        navigator.clipboard.writeText(copyText.innerText);
        
        alert("Address copied.");
    }
</script>